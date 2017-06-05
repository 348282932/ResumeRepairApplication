using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;
using ResumeMatchApplication.Api;

namespace ResumeMatchApplication.Works
{
    public class MatchResumeWork : BaseSpider
    {
        private static readonly object lockObj = new object();

        private static readonly ConcurrentQueue<string> hostQueue = new ConcurrentQueue<string>();

        private static readonly List<string> hostList = new List<string>();

        private static bool isEnd = false;

        private static int processCount;

        //private static ActionBlock<KeyValuePair<string,List<ResumeSearch>>> actionBlock = new ActionBlock<KeyValuePair<string, List<ResumeSearch>>>(data =>
        //{
        //    Work(data.Key, data.Value);

        //}, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

        /// <summary>
        /// 工作流
        /// </summary>
        /// <param name="host"></param>
        /// <param name="resumeSearches"></param>
        /// <returns></returns>
        private static bool Work(string host, List<ResumeSearch> resumeSearches)
        {
            processCount = 0;

            var zhaoPinGou = true;

            var proxyIsEnable = true;

            var queue = new ConcurrentQueue<ResumeSearch>();

            resumeSearches.ForEach(f => { queue.Enqueue(f); });

            // 招聘狗平台匹配块

            var zhaoPinGouActionBlock = new ActionBlock<ResumeSearch>(data =>
            {
                var dataResult = Platform.ZhaoPinGou.SearchResumeSpider.GetResumeId(data, host);

                if (dataResult != null && dataResult.IsSuccess)
                {
                    if (string.IsNullOrWhiteSpace(dataResult.Data))
                    {
                        data.ZhaoPinGouIsMatch = 2;

                        queue.Enqueue(data);
                    }
                    else
                    {
                        LogFactory.Info($"匹配成功！简历ID：{data.ResumeId}，姓名：{data.Name}",MessageSubjectEnum.ZhaoPinGou);

                        data.IsEnd = true;

                        Interlocked.Add(ref processCount, 1);
                    }
                }
                else
                {
                    queue.Enqueue(data);

                    if (dataResult == null)
                    {
                        zhaoPinGou = false;

                        LogFactory.Error($"Host:{host} 程序异常！", MessageSubjectEnum.ZhaoPinGou);
                    }
                    else
                    {
                        switch (dataResult.Code)
                        {
                            case ResultCodeEnum.ProxyDisable:

                                LogFactory.Info($"Host:{host} 代理失效！", MessageSubjectEnum.ZhaoPinGou);

                                proxyIsEnable = false;

                                break;

                            case ResultCodeEnum.RequestUpperLimit:

                                zhaoPinGou = false;

                                LogFactory.Info($"Host:{host} 请求达到当日上限！", MessageSubjectEnum.ZhaoPinGou);

                                break;

                            case ResultCodeEnum.NoUsers:

                                zhaoPinGou = false;

                                LogFactory.Info($"Host:{host} 对应的Host没有可用用户！", MessageSubjectEnum.ZhaoPinGou);

                                break;

                            default:

                                LogFactory.Warn($"匹配结果返回异常！异常消息：{dataResult.ErrorMsg} ", MessageSubjectEnum.ZhaoPinGou);

                                data.ZhaoPinGouIsMatch = 2;

                                break;
                        }
                    }
                }
            });

            using (var db = new ResumeMatchDBEntities())
            {
                while (processCount < resumeSearches.Count)
                {
                    if (!proxyIsEnable || !zhaoPinGou) // Todo：添加平台需改动
                    {
                        var users = db.User.Where(w => w.Host == host).ToList();

                        foreach (var user in users)
                        {
                            user.IsLocked = false;
                        }

                        db.TransactionSaveChanges();

                        return false;
                    }

                    ResumeSearch resumeSearch;

                    if (queue.TryDequeue(out resumeSearch))
                    {
                        while (true)
                        {
                            if (resumeSearch.ZhaoPinGouIsMatch != 0 && !resumeSearch.IsEnd) // Todo：添加平台需改动
                            {
                                var resume = db.ResumeComplete.FirstOrDefault(f => f.ResumeId == resumeSearch.ResumeId && f.ZhaoPinGouIsMatch == 2);

                                if (resume != null)
                                {
                                    resume.Status = 3;

                                    //TODO:由于纷简历原因注释掉回传失败结果

                                    //var matchedResult = new List<ResumeMatchResult>();

                                    //matchedResult.Add(new ResumeMatchResult
                                    //{
                                    //    ResumeNumber = resumeSearch.ResumeNumber,
                                    //    Status = 3
                                    //});

                                    //resume.Status = 3;

                                    //resume.PostBackStatus = 2;

                                    //if (ResumeProducer.PostResumes(matchedResult))
                                    //{
                                    //    resume.PostBackStatus = 1;
                                    //}

                                    LogFactory.Info($"匹配失败！简历ID：{resume.ResumeId}，姓名：{resume.Name}", MessageSubjectEnum.System);

                                    resumeSearch.IsEnd = true;

                                    processCount++;
                                }
                            }

                            if (zhaoPinGouActionBlock.InputCount < 2 && zhaoPinGou)
                            {
                                zhaoPinGouActionBlock.Post(resumeSearch);

                                break;
                            }

                            if (!zhaoPinGou) break;
                        }
                    }
                }

                db.TransactionSaveChanges();
            }

            return true;
        }

        /// <summary>
        /// 匹配
        /// </summary>
        private void Match()
        {
            lock (lockObj)
            {
                var users = new List<User>();

                using (var db = new ResumeMatchDBEntities())
                {
                    db.UsingTransaction(() =>
                    {
                        var dateTime = DateTime.UtcNow.AddHours(-1);

                        users = db.User.Where(w => w.IsEnable && w.Status == 1 && w.RequestNumber < Global.TodayMaxRequestNumber && (!w.IsLocked || w.IsLocked && w.LockedTime < dateTime)).Take(Global.PlatformCount * Global.PlatformHostCount * 2).ToList();

                        if (Global.IsEnanbleProxy)
                        {
                            users = users.Where(w => !string.IsNullOrEmpty(w.Host)).ToList();
                        }

                        foreach (var user in users)
                        {
                            user.IsLocked = true;

                            user.LockedTime = DateTime.UtcNow;
                        }

                        db.SaveChanges();
                    }); 
                }

                var hosts = users.GroupBy(g => g.Host)
                    .Select(s => new { Host = s.Key, Count = s.Count() })
                    .OrderByDescending(o => o.Count)
                    .ToList();

                hosts.ForEach(f => { hostQueue.Enqueue(f.Host); });
            }

            string host;

            while (hostQueue.TryDequeue(out host))
            {
                var hostTemp = host;

                hostList.Add(hostTemp);

                if (!string.IsNullOrWhiteSpace(host))
                {
                    while (true)
                    {
                        if (GetProxy(host)) break;

                        Thread.Sleep(3000);
                    }
                }

                while (true)
                {
                    List<ResumeSearch> resumes;

                    using (var db = new ResumeMatchDBEntities())
                    {
                        var list = db.ResumeComplete.Where(w => (w.Host == host || w.Host == null) && w.Status == 1).ToList(); //TODO:添加平台需改动条件

                        resumes = list.Select(s => new ResumeSearch
                        {
                            Degree = s.Degree,
                            Gender = s.Gender,
                            Introduction = s.Introduction,
                            LastCompany = s.LastCompany,
                            Name = s.Name,
                            ResumeId = s.ResumeId,
                            ResumeNumber = s.ResumeNumber,
                            University = s.University,
                            UserMasterExtId = s.UserMasterExtId,
                            ZhaoPinGouIsMatch = s.ZhaoPinGouIsMatch
                        }).ToList();
                    }

                    if (!resumes.Any())
                    {
                        resumes = ResumeProducer.PullResumes();
                    }

                    if (resumes != null && resumes.Count > 0)
                    {
                        if (!Work(host, resumes)) break;
                    }

                    if (isEnd)
                    {
                        HostUnLock();
                    }
                }

                hostList.Remove(hostTemp);
            }
        }

        /// <summary>
        /// 解锁Host用户
        /// </summary>
        private static void HostUnLock()
        {
            lock (lockObj)
            {
                using (var db = new ResumeMatchDBEntities())
                {
                    db.UsingTransaction(() =>
                    {
                        string host;

                        while (hostQueue.TryDequeue(out host))
                        {
                            var hostUnLock = host;

                            var users = db.User.Where(w => w.Host == hostUnLock && w.IsLocked).ToList();

                            foreach (var user in users)
                            {
                                user.IsLocked = false;
                            }
                        }

                        foreach (var item in hostList)
                        {
                            var users = db.User.Where(w => w.Host == item && w.IsLocked).ToList();

                            foreach (var user in users)
                            {
                                user.IsLocked = false;
                            }
                        }

                        db.SaveChanges();
                    });
                }
            }
        }

        public override DataResult Init()
        {
            try
            {
                Match();
            }
            catch (Exception ex)
            {
                HostUnLock();

                return new DataResult($"匹配程序异常！异常信息：{ex.Message},堆栈信息：{ex.StackTrace}");
            }
            
            return new DataResult();
        }
    }
}