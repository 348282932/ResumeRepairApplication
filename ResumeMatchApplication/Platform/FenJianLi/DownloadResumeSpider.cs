using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;
using ResumeMatchApplication.Api;

namespace ResumeMatchApplication.Platform.FenJianLi
{
    public class DownloadResumeSpider : FenJianLiSpider
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
            var dataResult = new DataResult<string>();

            dataResult.IsSuccess = false;

            CookieContainer cookie;

            Next:

            var result = GetUser(userDictionary, host, false, MatchPlatform.FenJianLi, Login, out cookie);

            if (!result.IsSuccess)
            {
                dataResult.IsSuccess = false;

                dataResult.Code = result.Code;

                dataResult.ErrorMsg = result.ErrorMsg;

                return dataResult;
            }

            var user = result.Data;

            if (string.IsNullOrWhiteSpace(user.FolderCode))
            {
                dataResult = GetFolderId(cookie, host);

                if (!dataResult.IsSuccess) return dataResult;

                user.FolderCode = dataResult.Data;
            }

            var unLockResult = UnLockResume(cookie, user, data.MatchResumeId, host, user.FolderCode);

            using (var db = new ResumeMatchDBEntities())
            {
                var resumeEntity = db.ResumeComplete.FirstOrDefault(f => f.Id == data.Id);

                if (resumeEntity == null)
                {
                    LogFactory.Warn($"找不到简历！Id：{data.Id}",MessageSubjectEnum.FenJianLi);

                    return dataResult;
                }

                var userEntity = db.User.FirstOrDefault(f => f.Id == user.Id);

                if (userEntity == null)
                {
                    LogFactory.Warn($"找不到用户！用户：{user.Email}", MessageSubjectEnum.FenJianLi);

                    db.TransactionSaveChanges();

                    return dataResult;
                }

                userEntity.FolderCode = user.FolderCode;

                if (!unLockResult.IsSuccess)
                {
                    resumeEntity.Status = 5;

                    if (unLockResult.Code == ResultCodeEnum.NoDownloadNumber)
                    {
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

                var id = data.MatchResumeId.Substring(0, data.MatchResumeId.IndexOf("/", StringComparison.Ordinal));

                dataResult = ResumeDetailSpider(id, cookie, host);

                if (!dataResult.IsSuccess)
                {
                    resumeEntity.Status = 7;

                    dataResult.ErrorMsg = "获取简历详情异常！";

                    dataResult.IsSuccess = false;

                    db.TransactionSaveChanges();

                    return dataResult;
                }

                var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

                if (jObject == null)
                {
                    resumeEntity.Status = 7;

                    dataResult.ErrorMsg = "简历反序列化异常！";

                    dataResult.IsSuccess = false;

                    SaveFile(id, dataResult.Data, user.Email);

                    db.TransactionSaveChanges();

                    return dataResult;
                }

                var email = (string)jObject["contact"]?["email"];

                var cellphone = (string)jObject["contact"]?["mobile"];
                
                if (cellphone == null)
                {
                    resumeEntity.Status = 7;

                    LogFactory.Warn($"补全简历异常！电话为空,ResumeId：{data.ResumeId}",MessageSubjectEnum.FenJianLi);

                    SaveFile(id, dataResult.Data, user.Email);

                    db.TransactionSaveChanges();

                    dataResult.IsSuccess = false;

                    return dataResult;
                }

                resumeEntity.PostBackStatus = 2;

                resumeEntity.Status = 6;

                var matchedResult = new List<ResumeMatchResult>
                {
                    new ResumeMatchResult
                    {
                        ResumeNumber = data.ResumeNumber,
                        Cellphone = cellphone,
                        Email = email,
                        Status = 2
                    }
                };

                SaveFile(id, dataResult.Data, user.Email);

                if (ApiBase.PostResumes(matchedResult))
                {
                    resumeEntity.PostBackStatus = 1;
                }

                resumeEntity.Email = email;

                resumeEntity.Cellphone = cellphone;

                db.TransactionSaveChanges();

                dataResult.IsSuccess = true;

                
            }

            return dataResult;
        }

        /// <summary>
        /// 保存 Json 源文件
        /// </summary>
        /// <param name="id"></param>
        /// <param name="content"></param>
        /// <param name="email"></param>
        private static void SaveFile(string id, string content, string email)
        {
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
                File.WriteAllText($"{path}\\{id}.json", content);
            }
            catch (FileNotFoundException ex)
            {
                throw new RequestException($"找不到文件路径, 原因：{ex.Message}，帐号：{email}");
            }
        }

        /// <summary>
        /// 获取下载文件夹ID
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        private static DataResult<string> GetFolderId(CookieContainer cookie, string host)
        {
            var dataResult = new DataResult<string>();

            var param = $"folderCatalogType=Download&_random={new Random().NextDouble()}";

            var result = RequestFactory.QueryRequest("http://www.fenjianli.com/folderCatalog/treeOfFolderCatalog.htm", param, RequestEnum.POST, cookie, host : host);

            if (!result.IsSuccess) return result;

            dataResult.Data = Regex.Match(result.Data, "(?s)id:(\\d+)").Result("$1");

            return dataResult;
        }

        /// <summary>
        /// 解锁简历
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="user"></param>
        /// <param name="resumeId"></param>
        /// <param name="host"></param>
        /// <param name="folderId"></param>
        /// <returns></returns>
        [Loggable]
        private static DataResult UnLockResume(CookieContainer cookie, User user, string resumeId, string host, string folderId)
        {
            var dataResult = new DataResult();

            dataResult.IsSuccess = false;

            var jumpsTimes = 0;

            Jumps:

            var param = $"ids={resumeId}&folderCatalogId={folderId}&folderCatalogType=Download&type=add&isResumeId=true&_random={new Random().NextDouble()}";

            var result = RequestFactory.QueryRequest("http://www.fenjianli.com/userResumeDetail/addToFolderCatalog.htm", param, RequestEnum.POST, cookie, host: host);

            if (!result.IsSuccess) return result;

            if (result.Data.Contains("用户可用下载点不足"))
            {
                dataResult.Code = ResultCodeEnum.NoDownloadNumber;

                LogFactory.Warn($"解锁简历失败！用户：{user.Email} 可用下载点不足", MessageSubjectEnum.FenJianLi);

                return dataResult;
            }

            if (!result.Data.Contains("成功"))
            {
                ++jumpsTimes;

                LogFactory.Warn($"解锁简历失败！用户：{user.Email} 异常信息：{result.Data}", MessageSubjectEnum.FenJianLi);

                if (jumpsTimes > 1)
                {
                    dataResult.ErrorMsg = $"解锁简历失败！用户：{user.Email} 异常信息：{result.Data}";

                    return dataResult;
                }

                goto Jumps;
            }

            dataResult.IsSuccess = true;

            return dataResult;
        }

        /// <summary>
        /// 获取已解锁的简历详情
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cookie"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        private static DataResult<string> ResumeDetailSpider(string id, CookieContainer cookie, string host)
        {
            var jumpsTimes = 0;

            Jumps:

            var param = $"id={id}&_random={new Random().NextDouble()}";

            var dataResult = RequestFactory.QueryRequest("http://www.fenjianli.com/search/getDetail.htm", param, RequestEnum.POST, cookie, "http://www.fenjianli.com/search/detail.htm", host: host);

            if (!dataResult.IsSuccess) return dataResult;

            if (string.IsNullOrWhiteSpace(dataResult.Data) || dataResult.Data.Contains("非法") && dataResult.Data.Length < 20)
            {
                ++jumpsTimes;

                LogFactory.Warn($"获取简历详情异常！异常信息：{dataResult.Data}", MessageSubjectEnum.FenJianLi);

                if (jumpsTimes > 2) return new DataResult<string>();

                goto Jumps;

            }

            return dataResult;
        }
    }
}