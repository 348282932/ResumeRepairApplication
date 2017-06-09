using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeRepairApplication.Common;
using ResumeRepairApplication.EntityFramework;
using ResumeRepairApplication.Models;

namespace ResumeRepairApplication.Platform.FenJianLi
{
    public class ContactInformationSpider : FenJianLiSpider
    {
		/// <summary>
		/// 51 授权
		/// </summary>
		/// <param name="user"></param>
		/// <param name="cookie"></param>
		/// <param name="isFirst"></param>
		/// <param name="userName"></param>
		/// <returns></returns>
		public static bool Verification(EntityFramework.FenJianLi user, CookieContainer cookie, bool isFirst, string userName)
        {
            using (var db = new ResumeRepairDBEntities())
            {
                var account = db.AuthorizationAccount.FirstOrDefault(f => f.IsEnable);

                if (account == null)
                {
                    FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.system_tbx_Exception, "获取授权帐号失败！");

                    return false;
                } 

                var param = $"type=Job51&extendParam={account.ExtendParam}&username={userName}&password={account.PassWord}&_random={new Random().NextDouble()}";

                var sources = RequestFactory.QueryRequest("http://www.fenjianli.com/account/bindChannel.htm", param, RequestEnum.POST, cookie);

                if (!sources.Contains("已经绑定") && !sources.Contains("成功"))
                {
                    //FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.system_tbx_Exception, $"渠道授权失败！响应Json：{sources}");

                    return false;
                }

                var fjlUser = db.FenJianLi.FirstOrDefault(f => f.Email == user.Email);

                if(isFirst) if (fjlUser != null) fjlUser.Integral += 250;

	            if (fjlUser != null)
	            {
		            fjlUser.IsVerification = true;

		            fjlUser.VerificationAccount = userName;
	            }

	            db.SaveChanges();

                return true;
            }
        }

        private static ConcurrentQueue<EntityFramework.FenJianLi> queue = new ConcurrentQueue<EntityFramework.FenJianLi>();

        private static int count = 0;

        private static int errorCount = 0;

        /// <summary>
        /// 流水线
        /// </summary>
        public static ActionBlock<string> actionBlock = new ActionBlock<string>(i => 
        {
            try
            {

                var resumeId = i.Substring(0, i.IndexOf("-"));

                var resumeNo = i.Substring(i.IndexOf("-") + 1);

                Work(resumeId, resumeNo);
            }
            catch (RequestException ex)
            {
                FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.system_tbx_Exception, $"{ex.Message}");
            }
            catch (Exception ex)
            {
                FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.system_tbx_Exception, $"错误信息：{ex.Message}，堆栈信息：{ex.StackTrace}");
            }
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

		/// <summary>
		/// 工作单元
		/// </summary>
		/// <param name="resumeId"></param>
		/// <param name="resumeNo"></param>
		private static void Work(string resumeId, string resumeNo)
        {
            EntityFramework.FenJianLi user;

            var cookie = new CookieContainer();

            Next:

            while (!queue.TryDequeue(out user)) { Thread.Sleep(1000); };

            if (!user.IsVerification)
            {
                cookie = Login(user.Email, user.PassWord);

                var sources = RequestFactory.QueryRequest("http://192.168.1.100:15286/api/Fenjianli/GetAuthenticationAccount?token=g9Cp5O0l2ZENYI8J0PxMk7sZk624nkxY");

                string userName;

                if (Regex.IsMatch(sources, "\"(.+?)\"") && !sources.Contains("Cookie was invalid"))
                {
                    userName = Regex.Match(sources, "\"(.+?)\"").Result("$1");
                }
                else
                {
                    throw new RequestException($"解析51帐号失败！,返回信息：{sources}");
                }

                var tryCount = 0;

                while (!Verification(user, cookie, true, userName))
                {
                    if (tryCount > 1) goto Next;

                    tryCount++;
                }
            }

            cookie = Login(user.Email, user.PassWord);

            var id = resumeId.Substring(0, resumeId.IndexOf("/"));

            var param = $"id={id}&_random={new Random().NextDouble()}";

            Thread.Sleep(500);

            var html = RequestFactory.QueryRequest("http://www.fenjianli.com/search/getDetail.htm", param, RequestEnum.POST, cookie, "http://www.fenjianli.com/search/detail.htm");

            if (string.IsNullOrWhiteSpace(html) || html.Contains("很抱歉，出错了"))
            {
                using (var db = new ResumeRepairDBEntities())
                {
                    var fjl = db.FenJianLi.FirstOrDefault(w => w.Id == user.Id);

                    if (fjl != null)
                    {
                        fjl.IsLocked = false;

                        db.SaveChanges();
                    }
                    else
                    {
                        FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.system_tbx_Exception, $"用户{user.Email}解锁失败！");
                    }

                }

                errorCount++;

                FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.system_tbx_Exception, $"找不到简历：简历id:{id} 用户{user.Email}！ 数量：{errorCount}");

                return;
            }

            var folderCatalogId = GetFolderCatalogId(user);

            if (string.IsNullOrWhiteSpace(folderCatalogId)) throw new RequestException($"获取文件夹编号失败！,帐号：{user.Email}");

            Download:

            var result = DownloadResume(resumeId, folderCatalogId, user);

            if (result == 1)
            {
                using (var db = new ResumeRepairDBEntities())
                {
                    var fjl = db.FenJianLi.FirstOrDefault(w => w.Id == user.Id);

                    if (fjl != null)
                    {
                        fjl.IsLocked = false;

                        db.SaveChanges();
                    }
                    else
                    {
                        FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.system_tbx_Exception, $"用户{user.Email}解锁失败！");
                    }

                }

                while (!queue.TryDequeue(out user)) { Thread.Sleep(1000); };

                cookie = Login(user.Email, user.PassWord);

                goto Download;
            }

            if (result == 2)
            {
                var sources = RequestFactory.QueryRequest("http://192.168.1.100:15286/api/Fenjianli/GetAuthenticationAccount?token=g9Cp5O0l2ZENYI8J0PxMk7sZk624nkxY");

                string userName;

                if (Regex.IsMatch(sources, "\"(.+?)\""))
                {
                    userName = Regex.Match(sources, "\"(.+?)\"").Result("$1");
                }
                else
                {
                    throw new RequestException($"解析51帐号失败！,返回信息：{sources}");
                }

                var tryCount = 0;

                while (!Verification(user, cookie, false, userName))
                {
                    if (tryCount > 1) goto Next;

                    tryCount++;
                }
            }

            AdditionalInformation(resumeId, user, resumeNo);
        }

        /// <summary>
        /// 获取文件夹ID
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private static string GetFolderCatalogId(EntityFramework.FenJianLi user)
        {
            var cookie = Login(user.Email, user.PassWord);

            var param = $"folderCatalogType=Download&_random={new Random().NextDouble()}";

            var html = RequestFactory.QueryRequest("http://www.fenjianli.com/folderCatalog/treeOfFolderCatalog.htm", param, RequestEnum.POST, cookie);

            return Regex.Match(html, "(?s)id:(\\d+)").Result("$1");
        }

        /// <summary>
        /// 下载简历
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="folderCatalogId"></param>
        /// <returns></returns>
        private static int DownloadResume(string ids, string folderCatalogId, EntityFramework.FenJianLi user)
        {
            var cookie = Login(user.Email, user.PassWord);

            var param = $"ids={ids}&folderCatalogId={folderCatalogId}&folderCatalogType=Download&type=add&isResumeId=true&_random={new Random().NextDouble()}";

            string html = RequestFactory.QueryRequest("http://www.fenjianli.com/userResumeDetail/addToFolderCatalog.htm", param, RequestEnum.POST, cookie);

            if (string.IsNullOrEmpty(html)) throw new RequestException($"下载简历失败！响应为空！,帐号：{user.Email}");

            if (html.Contains("用户可用下载点不足")) return 1;

            if (html.Contains("渠道账号"))
            {
                return 2;
            }

            if (!html.Contains("成功")) throw new RequestException($"下载简历失败！响应：{html}，帐号：{user.Email}");

            using (var db = new ResumeRepairDBEntities())
            {
                var fenJianLi = db.FenJianLi.FirstOrDefault(f => f.Email == user.Email);

                if (fenJianLi != null) fenJianLi.Integral -= 3;

	            if (fenJianLi != null) fenJianLi.IsLocked = false;

	            db.SaveChanges();
            }

            return 0;
        }

		/// <summary>
		/// 补全信息
		/// </summary>
		/// <param name="resumeId"></param>
		/// <param name="user"></param>
		/// <param name="resumeNo"></param>
		private static void AdditionalInformation(string resumeId, EntityFramework.FenJianLi user, string resumeNo)
        {
            var cookie = Login(user.Email, user.PassWord);

            var id = resumeId.Substring(0, resumeId.IndexOf("/"));

            var param = $"id={id}&_random={new Random().NextDouble()}";

            To:

            Thread.Sleep(500);

            string html = RequestFactory.QueryRequest("http://www.fenjianli.com/search/getDetail.htm", param, RequestEnum.POST, cookie, "http://www.fenjianli.com/search/detail.htm");

            if (string.IsNullOrWhiteSpace(html) || (html.Contains("非法")&& html.Length<20)) goto To;

            var jObject = JsonConvert.DeserializeObject(html) as JObject;

            var email = (string)jObject["contact"]?["email"];

            var mobile = (string)jObject["contact"]?["mobile"];

            string path;

            if (id.Length > 4)
            {
                path = $"D:\\Resumes\\Complete\\{DateTime.Now:yyyy-MM-dd}\\{id.Remove(2)}\\{id.Substring(2, 2)}";
            }
            else
            {
                path = $"D:\\Resumes\\Complete\\{DateTime.Now:yyyy-MM-dd}\\other";
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            try
            {
                File.WriteAllText($"{path}\\{id}.json", html);

                var requestParam = new List<ResumeMatchResult>
                {
                    new ResumeMatchResult
                    {
                        Cellphone = mobile,
                        Email = email,
                        ResumeNumber = resumeNo,
                        Status = 2
                    }
                };

                var dataResult = RequestFactory.QueryRequest(Global.PostResumesUrl, JsonConvert.SerializeObject(requestParam), RequestEnum.POST, contentType: "application/json");

                using (var db = new ResumeRepairDBEntities())
                {
                    var matchResumeId = $"{resumeId}-{resumeNo}";

                    var record = db.ResumeRecord.FirstOrDefault(w => w.MatchPlatform == 1 && w.MatchResumeId == matchResumeId);

                    if (record != null)
                    {
                        record.Email = email;
                        record.Cellphone = mobile;
                        record.Status = (int)ResumeRecordStatus.DownLoadSuccess;
                        record.PostBackStatus = dataResult.Contains("成功") ? (short)1 : (short)2;
                        record.DownLoadTime = DateTime.UtcNow;
                        db.SaveChanges();
                    }
                    else
                    {
                        FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.system_tbx_Exception, $"更新补全简历信息失败！纷简历ID:{id}");
                    }
                }

                Interlocked.Add(ref count, 1);

                Interlocked.Add(ref Global.TotalDownload, 1);

                FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.fjl_tbx_RepairResume, $"补全简历成功！{user.Email} id:{id}，补全简历份数：{count}");
            }
            catch (FileNotFoundException ex) { throw new RequestException($"找不到文件路径, 原因：{ex.Message}，帐号：{user.Email}"); }
        }

        private static bool isFirst = true;

        public override DataResult Init()
        {
            var dataResult = new DataResult();

            try
            {
                using (var db = new ResumeRepairDBEntities())
                {
                    var dateNow = DateTime.UtcNow.AddDays(-1);

                    if (isFirst)
                    {
                        if (actionBlock.InputCount == 0)
                        {
                            var resumes = db.ResumeRecord.Where(w => (w.PostBackStatus == 0 || w.PostBackStatus == 2) && w.Status == (short)ResumeRecordStatus.MatchSuccess).OrderByDescending(o=>o.CreateTime).Take(50).ToList();

                            foreach (var item in resumes)
                            {
                                actionBlock.Post(item.MatchResumeId);
                            }
                        }

                        isFirst = false;
                    }

                    var users = db.FenJianLi.Where(w => w.IsEnable && w.IsActivation &&  w.IsVerification  &&  w.Integral > 5 && (!w.IsLocked || w.LockedTime < dateNow)).OrderBy(o => o.VerificationAccount).ToList();
                    //var users = db.FenJianLi.Where(w => w.IsEnable && w.IsActivation && w.Integral > 5 && w.IsVerification && (!w.IsLocked || w.LockedTime < dateNow)).ToList();

                    if (users != null)
                    {
                        users.ForEach(f => 
                        {
                            f.IsLocked = true;
                            f.LockedTime = DateTime.UtcNow;
                            queue.Enqueue(f);
                        });
                    }

                    db.SaveChanges();
                }
                
            }
            catch (Exception ex)
            {
                dataResult.IsSuccess = false;

                dataResult.ErrorMsg = $"异常信息：{ex.Message}，堆栈信息：{ex.StackTrace}";
            }

            return dataResult;
        }
    }
}
