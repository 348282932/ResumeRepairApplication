using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;
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

        //private static bool isEnd = false;

        private static bool isFirst = true;

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
        private static bool Work(string host, IEnumerable<ResumeSearch> resumeSearches)
        {
            //foreach (var platform in Enum.GetValues(typeof(MatchPlatform)))

            var isUnBreak = true;

            using (var db = new ResumeMatchDBEntities())
            {
                if (resumeSearches.Any(resume => !MatchResume(resume, host)))
                {
                    isUnBreak = false;
                }

                var users = db.User.Where(w => w.Host == host).ToList();

                foreach (var user in users)
                {
                    user.IsLocked = false;
                }

                db.TransactionSaveChanges();
            }

            return isUnBreak;
        }



        /// <summary>
        /// 匹配简历
        /// </summary>
        /// <param name="data"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        private static bool MatchResume(ResumeSearch data, string host)
        {
            var dataResult = Platform.ZhaoPinGou.SearchResumeSpider.GetResumeId(data, host);

            if (dataResult == null) return false;

            if (!dataResult.IsSuccess)
            {
                switch (dataResult.Code)
                {
                    case ResultCodeEnum.ProxyDisable:

                        LogFactory.Info($"Host:{host} 代理失效！", MessageSubjectEnum.ZhaoPinGou);

                        return string.IsNullOrWhiteSpace(host) || host == "210.83.225.31:15839";

                    case ResultCodeEnum.RequestUpperLimit:

                        LogFactory.Info($"Host:{host} 请求达到当日上限！", MessageSubjectEnum.ZhaoPinGou);

                        return string.IsNullOrWhiteSpace(host) || host == "210.83.225.31:15839";

                    case ResultCodeEnum.NoUsers:

                        LogFactory.Info($"Host:{host} 对应的Host没有可用用户！", MessageSubjectEnum.ZhaoPinGou);

                        return string.IsNullOrWhiteSpace(host) || host == "210.83.225.31:15839";

                    case ResultCodeEnum.WebNoConnection:

                        LogFactory.Warn("网站无法建立链接！", MessageSubjectEnum.ZhaoPinGou);

                        break;

                    default:

                        LogFactory.Warn($"匹配结果返回异常！异常消息：{dataResult.ErrorMsg} ", MessageSubjectEnum.ZhaoPinGou);

                        return string.IsNullOrWhiteSpace(host) || host == "210.83.225.31:15839";
                }
            }

            if (!string.IsNullOrWhiteSpace(dataResult.Data))
            {
                LogFactory.Info($"匹配成功！简历ID：{data.ResumeId}，姓名：{data.Name}", MessageSubjectEnum.ZhaoPinGou);

                return true;
            }

            dataResult = Platform.FenJianLi.SearchResumeSpider.GetResumeId(data, host);

            if (dataResult == null) return false;

            if (!dataResult.IsSuccess)
            {
                switch (dataResult.Code)
                {
                    case ResultCodeEnum.ProxyDisable:

                        LogFactory.Info($"Host:{host} 代理失效！", MessageSubjectEnum.FenJianLi);

                        return string.IsNullOrWhiteSpace(host) || host == "210.83.225.31:15839";

                    case ResultCodeEnum.RequestUpperLimit:

                        LogFactory.Info($"Host:{host} 请求达到当日上限！", MessageSubjectEnum.FenJianLi);

                        return string.IsNullOrWhiteSpace(host) || host == "210.83.225.31:15839";

                    case ResultCodeEnum.NoUsers:

                        LogFactory.Info($"Host:{host} 对应的Host没有可用用户！", MessageSubjectEnum.FenJianLi);

                        return string.IsNullOrWhiteSpace(host) || host == "210.83.225.31:15839";
                    
                    case ResultCodeEnum.WebNoConnection:

                        LogFactory.Warn("网站无法建立链接！",MessageSubjectEnum.FenJianLi);

                        break;

                    default:

                        LogFactory.Warn($"匹配结果返回异常！异常消息：{dataResult.ErrorMsg} ", MessageSubjectEnum.FenJianLi);

                        return string.IsNullOrWhiteSpace(host) || host == "210.83.225.31:15839";
                }
            }

            if (!string.IsNullOrWhiteSpace(dataResult.Data))
            {
                LogFactory.Info($"匹配成功！简历ID：{data.ResumeId}，姓名：{data.Name}", MessageSubjectEnum.FenJianLi);

                return true;
            }

            using (var db = new ResumeMatchDBEntities())
            {
                var resume = db.ResumeComplete.FirstOrDefault(f => f.ResumeId == data.ResumeId);

                if (resume != null)
                {
                    resume.Status = 3;

                    if (dataResult.Code == ResultCodeEnum.WebNoConnection) resume.Status = 9;

                    LogFactory.Info($"匹配失败！简历ID：{resume.ResumeId}，姓名：{resume.Name}");

                    db.TransactionSaveChanges();
                }
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
                if (!hostQueue.Any() && isFirst)
                {
                    hostQueue.Enqueue("210.83.225.31:15839");

                    isFirst = false;
                }

                var users = new List<User>();

                using (var db = new ResumeMatchDBEntities())
                {
                    db.UsingTransaction(() =>
                    {
                        var dateTime = DateTime.UtcNow.AddHours(-1);

                        var nowDate = DateTime.UtcNow.Date;

                        users = db.User
                                    .Where(w => w.IsEnable && w.Status == 1 && ( w.RequestDate.Value == null || w.RequestDate.Value < nowDate || w.RequestDate.Value == nowDate && w.RequestNumber < Global.TodayMaxRequestNumber) && (!w.IsLocked || w.IsLocked && w.LockedTime < dateTime) && (string.IsNullOrEmpty(w.Host) || !string.IsNullOrEmpty(w.Host) == Global.IsEnanbleProxy))
                                    .OrderBy(o=>o.RequestNumber)
                                    .ThenByDescending(o => o.Host)
                                    //.Take(Global.PlatformCount * Global.PlatformHostCount * 2)
                                    .ToList();

                        foreach (var user in users)
                        {
                            user.IsLocked = true;

                            user.LockedTime = DateTime.UtcNow;
                        }

                        db.SaveChanges();
                    }); 
                }

                var hosts = users
                    .Where(w=>w.IsEnable && w.Host != "210.83.225.31:15839")
                    .GroupBy(g => g.Host)
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

                if (!string.IsNullOrWhiteSpace(hostTemp))
                {
                    GetProxy("Match", hostTemp);
                }

                while (true)
                {
                    List<ResumeSearch> resumes;

                    while (true)
                    {
                        try
                        {
                            resumes = ResumeProducer.PullResumes();

                            break;
                        }
                        catch (Exception ex)
                        {
                            while (true)
                            {
                                if (ex.InnerException == null) break;

                                ex = ex.InnerException;
                            }

                            LogFactory.Error($"拉取待匹配的简历异常！异常信息：{ex.Message},堆栈信息：{ex.StackTrace}");
                        }
                    }

                    if (resumes != null && resumes.Count > 0)
                    {
                        Console.WriteLine(string.Join(",", resumes.Select(s => s.Name).ToArray()) );

                        if (!Work(hostTemp, resumes)) break;
                    }
                }

                ReleaseProxy("Match", hostTemp);

                //hostList.Remove(hostTemp);

                LogFactory.Info("Host 消费记录：" + JsonConvert.SerializeObject(hostQueue));
            }

            HostUnLock();
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
                            if (string.IsNullOrWhiteSpace(item)) continue;

                            var users = db.User.Where(w => w.Host == item && w.IsLocked).ToList();

                            foreach (var user in users)
                            {
                                user.IsLocked = false;
                            }
                        }

                        db.SaveChanges();

                        hostList.Clear();
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

                while (true)
                {
                    if (ex.InnerException == null) break;

                    ex = ex.InnerException;
                }

                return new DataResult($"匹配程序异常！异常信息：{ex.Message},堆栈信息：{ex.StackTrace}");
            }
            
            return new DataResult();
        }
    }
}