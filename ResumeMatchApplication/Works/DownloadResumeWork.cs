using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// 工作流
        /// </summary>
        /// <param name="host"></param>
        /// <param name="resumes"></param>
        /// <returns></returns>
        private static void Work(string host, IReadOnlyCollection<ResumeComplete> resumes)
        {
            using (var db = new ResumeMatchDBEntities())
            {
                if (resumes.Any(resume => !DownloadResume(host, resume)))
                {
                    return;
                }

                var resumeIdArr = resumes.Select(s => s.Id).ToArray();

                var resumeList = db.ResumeComplete.Where(w => resumeIdArr.Any(a => a == w.Id) && w.IsLocked).ToList();

                foreach (var item in resumeList)
                {
                    item.IsLocked = false;
                }

                db.TransactionSaveChanges();
                
            }
        }

        /// <summary>
        /// 下载简历
        /// </summary>
        /// <param name="host"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static bool DownloadResume(string host, ResumeComplete data)
        {
            var dataResult = new DataResult();

            MessageSubjectEnum messageSubjectEnum = 0;

            if (data.MatchPlatform == (short)MatchPlatform.FenJianLi)
            {
                messageSubjectEnum = MessageSubjectEnum.FenJianLi;

                dataResult = Platform.FenJianLi.DownloadResumeSpider.DownloadResume(data, host);
            }

            if (data.MatchPlatform == (short)MatchPlatform.ZhaoPinGou)
            {
                messageSubjectEnum = MessageSubjectEnum.ZhaoPinGou;

                dataResult = Platform.ZhaoPinGou.DownloadResumeSpider.DownloadResume(data, host);
            }

            if (dataResult == null) return false;

            if (!dataResult.IsSuccess)
            {
                switch (dataResult.Code)
                {
                    case ResultCodeEnum.ProxyDisable:

                        LogFactory.Info($"Host:{host} 代理失效！", messageSubjectEnum);

                        return false; 

                    case ResultCodeEnum.NoUsers:

                        LogFactory.Info($"Host:{host} 对应的Host没有可用用户用于下载简历！{dataResult.ErrorMsg}", messageSubjectEnum);

                        return false;

                    case ResultCodeEnum.WebNoConnection:

                        LogFactory.Warn("网站无法建立链接！", MessageSubjectEnum.FenJianLi);

                        return true;

                    default:

                        LogFactory.Warn($"下载简历异常！异常消息：{dataResult.ErrorMsg} ", messageSubjectEnum);

                        return false;
                }
            }

            LogFactory.Info($"简历补全成功！ResumeId：{data.ResumeId}", messageSubjectEnum);

            return true;
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

                        var today = DateTime.UtcNow.Date.AddHours(-8);

                        var downloadUserArr = db.User
                                            .Where(w => w.IsEnable && (string.IsNullOrEmpty(w.Host) || !string.IsNullOrEmpty(w.Host) == Global.IsEnanbleProxy) && (w.DownloadNumber > 0 || w.LastLoginTime < today || w.LastLoginTime == null))
                                            .GroupBy(g => new { g.Host, g.Platform })
                                            .Select(s=> s.Key)
                                            .Distinct()
                                            .ToList();

                        var query = db.ResumeComplete
                            .Where(w => w.Status == 2 && (!w.IsLocked || w.IsLocked && w.LockedTime < dateTime));

                        var platformList = downloadUserArr.GroupBy(g => g.Platform).Select(s => s.Key).ToList();

                        var hostList = downloadUserArr.GroupBy(g => g.Host)/*.Where(w=>!string.IsNullOrEmpty(w.Key))*/.Select(s => s.Key).ToList(); // 排除本地HOST

                        foreach (var item in platformList)
                        {
                            resumes.AddRange(query
                                .Where(w => w.MatchPlatform == item)
                                .OrderByDescending(o => o.Weights)
                                .ThenByDescending(o => o.MatchTime)
                                .Take(20));
                        }

                        resumes = resumes.Where(w=> w.Weights == 1 && w.MatchPlatform == 4) // Todo：当前只优先下载泽林的简历
                            .OrderByDescending(o => o.Weights)
                            .ThenByDescending(o => o.MatchTime)
                            .Take(20).ToList();

                        foreach (var resume in resumes)
                        {
                            resume.IsLocked = true;

                            resume.LockedTime = DateTime.UtcNow;
                        }

                        db.SaveChanges();

                        var count = resumes.Count / hostList.Count + 1;

                        for (var i = 0; i < hostList.Count; i++)
                        {
                            var temp = resumes.Skip(i*count).Take(count).ToList();

                            if (temp.Any()) resumeQueue.Enqueue(new KeyValuePair<string, List<ResumeComplete>>(hostList[i], temp));
                        }

                    });
                }

                
            }

            KeyValuePair<string, List<ResumeComplete>> hostResume;

            while (resumeQueue.TryDequeue(out hostResume))
            {
                var hostResumeTemp = hostResume;

                hostResumeList.Add(hostResumeTemp);

                if (!string.IsNullOrWhiteSpace(hostResumeTemp.Key))
                {
                    GetProxy("Download", hostResumeTemp.Key);
                }

                Work(hostResumeTemp.Key, hostResumeTemp.Value);

                ReleaseProxy("Download", hostResumeTemp.Key);

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