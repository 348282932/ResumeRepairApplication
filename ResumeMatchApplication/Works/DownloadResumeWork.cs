using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication.Works
{
    public class DownloadResumeWork : BaseSpider
    {
        private static readonly object lockObj = new object();

        private static readonly ConcurrentQueue<KeyValuePair<string,List<ResumeComplete>>> resumeQueue = new ConcurrentQueue<KeyValuePair<string, List<ResumeComplete>>>();

        private static readonly List<KeyValuePair<string, List<ResumeComplete>>> hostResumeList = new List<KeyValuePair<string, List<ResumeComplete>>>();

        private static bool isEnd = false;

        private static void Work(string host, IReadOnlyCollection<ResumeComplete> resumes)
        {
            var zhaoPinGou = true;

            var proxyIsEnable = true;

            // 招聘狗平台匹配块

            var zhaoPinGouActionBlock = new ActionBlock<ResumeComplete>(data =>
            {
                var dataResult = Platform.ZhaoPinGou.DownloadResumeSpider.DownloadResume(data, host);

                if(dataResult == null) return;

                if (!dataResult.IsSuccess)
                {
                    switch (dataResult.Code)
                    {
                        case ResultCodeEnum.ProxyDisable:

                            LogFactory.Info($"Host:{host} 代理失效！", MessageSubjectEnum.ZhaoPinGou);

                            proxyIsEnable = false;

                            break;

                        case ResultCodeEnum.NoUsers:

                            zhaoPinGou = false;

                            LogFactory.Info($"Host:{host} 对应的Host没有可用用户用于下载简历！", MessageSubjectEnum.ZhaoPinGou);

                            break;

                        default:

                            LogFactory.Warn($"下载简历异常！异常消息：{dataResult.ErrorMsg} ", MessageSubjectEnum.ZhaoPinGou);

                            zhaoPinGou = false;

                            break;
                    }
                }
                else
                {
                    LogFactory.Info($"简历补全成功！ResumeId：{data.ResumeId}",MessageSubjectEnum.ZhaoPinGou);
                }
            });

            var zhaoPinGouResumes = resumes.Where(w => w.MatchPlatform == (short)MatchPlatform.ZhaoPinGou).ToList();

            if (zhaoPinGouResumes.Count == 0) zhaoPinGou = false;

            zhaoPinGouResumes = Api.ResumeFiler.ZhaoPinGou(zhaoPinGouResumes); // 过滤已有的招聘狗简历

            zhaoPinGouResumes.ForEach(f => { zhaoPinGouActionBlock.Post(f); });

            while (true)
            {
                if (!proxyIsEnable || !zhaoPinGou || zhaoPinGouActionBlock.InputCount == 0) // Todo：添加平台需改动
                {
                    zhaoPinGouActionBlock.Complete();

                    zhaoPinGouActionBlock.Completion.Wait();

                    using (var db = new ResumeMatchDBEntities())
                    {
                        var resumeIdArr = resumes.Select(s => s.Id).ToArray();

                        var resumeList = db.ResumeComplete.Where(w => resumeIdArr.Any(a => a == w.Id) && w.IsLocked).ToList();

                        foreach (var item in resumeList)
                        {
                            item.IsLocked = false;
                        }

                        db.TransactionSaveChanges();

                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 解锁简历
        /// </summary>
        private static void ResumeUnLock()
        {
            lock (lockObj)
            {
                using (var db = new ResumeMatchDBEntities())
                {
                    db.UsingTransaction(() =>
                    {
                        KeyValuePair<string, List<ResumeComplete>> hostResume;

                        while (resumeQueue.TryDequeue(out hostResume))
                        {
                            var resumeIdArr = hostResume.Value.Select(s=>s.Id).ToArray();

                            var resumes = db.ResumeComplete.Where(w => resumeIdArr.Any(a=>a == w.Id) && w.IsLocked).ToList();

                            foreach (var resume in resumes)
                            {
                                resume.IsLocked = false;
                            }
                        }

                        foreach (var item in hostResumeList)
                        {
                            var resumeIdArr = item.Value.Select(s => s.Id).ToArray();

                            var resumes = db.ResumeComplete.Where(w => resumeIdArr.Any(a => a == w.Id) && w.IsLocked).ToList();

                            foreach (var resume in resumes)
                            {
                                resume.IsLocked = false;
                            }
                        }

                        db.SaveChanges();
                    });
                }
            }
        }

        private void Download()
        {
            lock (lockObj)
            {
                var resumes = new List<ResumeComplete>();

                using (var db = new ResumeMatchDBEntities())
                {
                    db.UsingTransaction(() =>
                    {
                        var dateTime = DateTime.UtcNow.AddHours(-1);

                        resumes = db.ResumeComplete.Where(w => w.Status == 2 && (!w.IsLocked || w.IsLocked && w.LockedTime < dateTime)).OrderByDescending(o => o.MatchTime).Take(20).ToList();

                        if (Global.IsEnanbleProxy)
                        {
                            resumes = resumes.Where(w => !string.IsNullOrEmpty(w.Host)).ToList();
                        }

                        foreach (var resume in resumes)
                        {
                            resume.IsLocked = true;

                            resume.LockedTime = DateTime.UtcNow;
                        }

                        db.SaveChanges();
                    });
                }

                var hostResumes = resumes.GroupBy(g => g.Host).Select(s => new { Host = s.Key, ResumeIdList = s }).ToList();

                hostResumes.ForEach(f =>
                {
                    resumeQueue.Enqueue(new KeyValuePair<string, List<ResumeComplete>>(f.Host, f.ResumeIdList.ToList()));
                });
            }

            KeyValuePair<string, List<ResumeComplete>> hostResume;

            while (resumeQueue.TryDequeue(out hostResume))
            {
                var hostResumeTemp = hostResume;

                hostResumeList.Add(hostResumeTemp);

                if (!string.IsNullOrWhiteSpace(hostResumeTemp.Key))
                {
                    while (true)
                    {
                        if (GetProxy(hostResumeTemp.Key)) break;

                        Thread.Sleep(3000);
                    }
                }

                Work(hostResumeTemp.Key, hostResumeTemp.Value);

                if (isEnd)
                {
                    ResumeUnLock();
                }

                hostResumeList.Remove(hostResumeTemp);
            }
        }

        public override DataResult Init()
        {
            try
            {
                Download();
            }
            catch (Exception ex)
            {
                ResumeUnLock();

                return new DataResult($"匹配程序异常！异常信息：{ex.Message},堆栈信息：{ex.StackTrace}");
            }

            return new DataResult();
        }
    }
}