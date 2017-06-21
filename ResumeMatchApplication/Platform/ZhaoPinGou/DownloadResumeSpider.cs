using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;
using ResumeMatchApplication.Api;

namespace ResumeMatchApplication.Platform.ZhaoPinGou
{
    public class DownloadResumeSpider : ZhaoPinGouSpider
    {
        protected static ConcurrentDictionary<User, CookieContainer> userDictionary = new ConcurrentDictionary<User, CookieContainer>();

        /// <summary>
        /// 获取简历ID
        /// </summary>
        /// <param name="data"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        [Loggable]
        public static DataResult DownloadResume(ResumeComplete data, string host)
        {
            if (Filer(data)) return new DataResult();

            var dataResult = new DataResult<string>();

            dataResult.IsSuccess = false;

            CookieContainer cookie;

            Next:

            var result = GetUser(userDictionary, host, false, MatchPlatform.ZhaoPinGou, Login, out cookie);

            if (!result.IsSuccess)
            {
                dataResult.IsSuccess = false;

                dataResult.Code = result.Code;

                dataResult.ErrorMsg = result.ErrorMsg;

                return dataResult;
            }

            var user = result.Data;

            var userToken = cookie.GetCookies(new Uri("http://qiye.zhaopingou.com/"))["hrkeepToken"];

            if (string.IsNullOrWhiteSpace(user.FolderCode))
            {
                dataResult = GetFolderId(cookie, user, userToken?.Value, data.MatchResumeId, host);

                if (!dataResult.IsSuccess) return dataResult;

                user.FolderCode = dataResult.Data;
            }

            var unLockResult = UnLockResume(cookie, user, userToken?.Value, data.MatchResumeId, host, user.FolderCode);

            using (var db = new ResumeMatchDBEntities())
            {
                var resumeEntity = db.ResumeComplete.FirstOrDefault(f => f.Id == data.Id);

                if (resumeEntity == null)
                {
                    LogFactory.Warn($"找不到简历！Id：{data.Id}",MessageSubjectEnum.ZhaoPinGou);

                    return dataResult;
                }

                var userEntity = db.User.FirstOrDefault(f => f.Id == user.Id);

                if (userEntity == null)
                {
                    LogFactory.Warn($"找不到用户！用户：{user.Email}", MessageSubjectEnum.ZhaoPinGou);

                    db.TransactionSaveChanges();

                    return dataResult;
                }

                userEntity.FolderCode = user.FolderCode;

                if (!unLockResult.IsSuccess)
                {
                    resumeEntity.Status = 5;

                    if (unLockResult.Code == ResultCodeEnum.NoDownloadNumber)
                    {
                        var userTemp = user;

                        var users = userDictionary.Keys.Where(f => f.Email == userTemp.Email);

                        foreach (var item in users)
                        {
                            userDictionary.TryRemove(item, out cookie);
                        }

                        user.DownloadNumber = 0;

                        userEntity.DownloadNumber = 0;

                        resumeEntity.Status = 2;

                        user.LastLoginTime = DateTime.UtcNow;

                        userEntity.LastLoginTime = DateTime.UtcNow;

                        db.TransactionSaveChanges();

                        goto Next;
                    }

                    db.TransactionSaveChanges();

                    return unLockResult;
                }

                resumeEntity.DownloadTime = DateTime.UtcNow;

                resumeEntity.Status = 4;

                resumeEntity.UserId = user.Id;

                user.DownloadNumber--;

                if(user.DownloadNumber == 0) userEntity.LastLoginTime = DateTime.UtcNow;

                userEntity.DownloadNumber = user.DownloadNumber;

                dataResult = ResumeDetailSpider(data.MatchResumeId, cookie, host, userToken?.Value);

                if (!dataResult.IsSuccess)
                {
                    resumeEntity.Status = 7;

                    db.TransactionSaveChanges();

                    return dataResult;
                }

                var resumeHtml = dataResult.Data;

                var cellphone = Regex.IsMatch(resumeHtml, "(?s)电话：</label>(\\d+)</p>") ? Regex.Match(resumeHtml, "(?s)电话：</label>(\\d+)</p>").Result("$1") : null;

                if (cellphone == null)
                {
                    resumeEntity.Status = 7;

                    LogFactory.Warn($"补全简历异常！电话为空,ResumeId：{data.ResumeId}",MessageSubjectEnum.ZhaoPinGou);

                    db.TransactionSaveChanges();

                    return dataResult;
                }

                var email = Regex.IsMatch(resumeHtml, "(?s)邮箱：</label>(.+?)</p>") ? Regex.Match(resumeHtml, "(?s)邮箱：</label>(.+?)</p>").Result("$1") : null;

                var name = Regex.IsMatch(resumeHtml, "(?s)'resumeb-head-top'><h2>(.+?)</h2><p>") ? Regex.Match(resumeHtml, "(?s)'resumeb-head-top'><h2>(.+?)</h2><p>").Result("$1") : null;

                resumeEntity.PostBackStatus = 2;

                resumeEntity.Status = 6;

                if (resumeEntity.Name.Trim() == name?.Trim())
                {
                    var matchedResult = new List<ResumeMatchResult>();

                    matchedResult.Add(new ResumeMatchResult
                    {
                        ResumeNumber = data.ResumeNumber,
                        Cellphone = cellphone,
                        Email = email,
                        Status = 2
                    });

                    if (ApiBase.PostResumes(matchedResult))
                    {
                        resumeEntity.PostBackStatus = 1;
                    }
                }
                else
                {
                    LogFactory.Warn($"姓名校验异常！库中简历姓名：{resumeEntity.Name}，下载简历姓名：{name}");

                    resumeEntity.Name += $"_{name}";

                    resumeEntity.PostBackStatus = 0;

                    resumeEntity.Status = 8;
                }

                resumeEntity.Email = email;

                resumeEntity.Cellphone = cellphone;

                db.TransactionSaveChanges();

                dataResult.IsSuccess = true;
            }

            return dataResult;
        }

        /// <summary>
        /// 获取下载文件夹ID
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="user"></param>
        /// <param name="token"></param>
        /// <param name="resumeId"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        private static DataResult<string> GetFolderId(CookieContainer cookie, User user, string token, string resumeId, string host)
        {
            var jumpsTimes = 0;

            Jumps:

            var param = $"type=1&keyStr=&clientNo=&userToken={token}&clientType=2";

            var dataResult = RequestFactory.QueryRequest("http://qiye.zhaopingou.com/zhaopingou_interface/get_candidate_folder_list?timestamp=" + BaseFanctory.GetUnixTimestamp(), param, RequestEnum.POST, cookie, "Referer: http://qiye.zhaopingou.com/resume/detail?resumeId=" + resumeId, host: host);

            if (!dataResult.IsSuccess) return dataResult;

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                if ((int)jObject["errorCode"] != 1)
                {
                    ++jumpsTimes;

                    LogFactory.Warn($"获取文件夹异常！异常用户：{user.Email} 异常信息：{(string)jObject["message"]}", MessageSubjectEnum.ZhaoPinGou);

                    if (jumpsTimes > 3) return new DataResult<string>();

                    goto Jumps;
                }

                var jArray = jObject["dataList"] as JArray;

                if (jArray != null)
                {
                    dataResult.Data = jArray[0]["id"].ToString();

                    return dataResult;
                }
            }

            return new DataResult<string>();
        }

        /// <summary>
        /// 解锁简历
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="user"></param>
        /// <param name="token"></param>
        /// <param name="resumeId"></param>
        /// <param name="host"></param>
        /// <param name="folderId"></param>
        /// <returns></returns>
        [Loggable]
        private static DataResult UnLockResume(CookieContainer cookie, User user, string token, string resumeId, string host, string folderId)
        {
            var dataResult = new DataResult();

            dataResult.IsSuccess = false;

            var jumpsTimes = 0;

            Jumps:

            var param = $"htmlCode={resumeId}&mFolderId={folderId}&notes=&clientNo=&userToken={token}&clientType=2";

            var result = RequestFactory.QueryRequest("http://qiye.zhaopingou.com/zhaopingou_interface/zpg_charge_example_unlock_new?timestamp=" + BaseFanctory.GetUnixTimestamp(), param, RequestEnum.POST, cookie, "Referer: http://qiye.zhaopingou.com/resume/detail?resumeId=" + resumeId, host: host);

            if (!result.IsSuccess) return result;

            var jObject = JsonConvert.DeserializeObject(result.Data) as JObject;

            if (jObject != null)
            {
                if (((string)jObject["message"]).Contains("未阅读过"))
                {
                    var readResult = ResumeDetailSpider(resumeId, cookie, host, token);

                    if (!readResult.IsSuccess) return readResult;

                    goto Jumps;
                }

                if (((string)jObject["message"]).Contains("余额不足"))
                {
                    dataResult.Code = ResultCodeEnum.NoDownloadNumber;

                    return dataResult;
                }

                if ((int)jObject["errorCode"] != 1 && (int)jObject["errorCode"] != 4)
                {
                    ++jumpsTimes;

                    LogFactory.Warn($"解锁简历失败！用户：{user.Email} 异常信息：{(string)jObject["message"]}", MessageSubjectEnum.ZhaoPinGou);

                    if (jumpsTimes > 1)
                    {
                        dataResult.ErrorMsg = $"解锁简历失败！用户：{user.Email} 异常信息：{(string)jObject["message"]}";

                        return dataResult;
                    }

                    goto Jumps;
                }

                dataResult.IsSuccess = true;
            }

            return dataResult;
        }

        /// <summary>
        /// 获取已解锁的简历详情
        /// </summary>
        /// <param name="resumeId"></param>
        /// <param name="cookie"></param>
        /// <param name="host"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static DataResult<string> ResumeDetailSpider(string resumeId, CookieContainer cookie, string host, string token)
        {
            var jumpsTimes = 0;

            Jumps:

            var param = $"resumeHtmlId={resumeId}&keyStr=&keyPositionName=&tradeId=&postionStr=&jobId=0&clientNo=&userToken={token}&clientType=2";

            var dataResult = RequestFactory.QueryRequest("http://qiye.zhaopingou.com/zhaopingou_interface/zpg_find_resume_html_details?timestamp=" + BaseFanctory.GetUnixTimestamp(), param, RequestEnum.POST, cookie, "http://qiye.zhaopingou.com/resume/detail?resumeId=" + resumeId, host: host);

            if (!dataResult.IsSuccess) return dataResult;

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                if ((int)jObject["errorCode"] != 1)
                {
                    ++jumpsTimes;

                    LogFactory.Warn($"获取简历详情异常！异常信息：{(string)jObject["message"]}", MessageSubjectEnum.ZhaoPinGou);

                    if (jumpsTimes > 1) return new DataResult<string>();

                    goto Jumps;
                }

                dataResult.Data = (string)jObject["jsonHtml"];

                return dataResult;
            }

            return new DataResult<string>();
        }
        
        /// <summary>
        /// 招聘狗旧库过滤
        /// </summary>
        /// <param name="resume"></param>
        /// <returns></returns>
        private static bool Filer(ResumeComplete resume)
        {
            var resumes = new List<ResumeComplete>();

            resumes.Add(resume);

            resumes = ResumeFiler.ZhaoPinGou(resumes); // 过滤已有的招聘狗简历

            if (!resumes.Any()) return true;

            return false;
        }
    }
}