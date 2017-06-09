using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication.Platform.FenJianLi
{
    public class SearchResumeSpider : FenJianLiSpider
    {
        protected static ConcurrentDictionary<User, CookieContainer> userDictionary = new ConcurrentDictionary<User, CookieContainer>();

        private static readonly object lockObj = new object();

        /// <summary>
        /// 获取简历ID
        /// </summary>
        /// <param name="data"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        [Loggable]
        public static DataResult<string> GetResumeId(ResumeSearch data, string host)
        {
            var dataResult = new DataResult<string>();

            var cookie = new CookieContainer();

            User user;

            lock (lockObj)
            {
                using (var db = new ResumeMatchDBEntities())
                {
                    if (userDictionary.Keys.All(a => a.Host != host))
                    {
                        var users = db.User.Where(w => w.IsEnable && w.Platform == 1 && w.Status == 1 && w.Host == host).ToList();

                        if (!users.Any())
                        {
                            dataResult.IsSuccess = false;

                            dataResult.Code = ResultCodeEnum.NoUsers;

                            return dataResult;
                        }

                        foreach (var item in users)
                        {
                            for (var i = 0; i < 5; i++)
                            {
                                if (userDictionary.TryAdd(item, null)) break;

                                if (i == 4) LogFactory.Warn($"向字典中添加用户 {item.Email} 失败！", MessageSubjectEnum.ZhaoPinGou);
                            }
                        }
                    }

                    Next:

                    user = userDictionary.Keys
                        .Where(f => f.IsEnable && f.Host == host && (f.RequestDate == null || f.RequestDate.Value.Date < DateTime.UtcNow.Date || f.RequestDate.Value.Date == DateTime.UtcNow.Date && f.RequestNumber < Global.TodayMaxRequestNumber))
                        .OrderBy(o => o.RequestNumber)
                        .FirstOrDefault();

                    if (user == null)
                    {
                        dataResult.IsSuccess = false;

                        dataResult.Code = ResultCodeEnum.RequestUpperLimit;

                        var list = userDictionary.Keys.Where(w => w.Host == host);

                        foreach (var item in list)
                        {
                            for (var i = 0; i < 5; i++)
                            {
                                if (userDictionary.TryRemove(item, out cookie)) break;

                                if (i == 4)
                                {
                                    LogFactory.Warn($"从字典中移除用户 {item.Email} 失败！", MessageSubjectEnum.FenJianLi);

                                    dataResult.ErrorMsg += $"向字典中移除用户 {item.Email} 失败！";

                                    return dataResult;
                                }
                            }
                        }

                        return dataResult;
                    }

                    if (user.RequestDate == null || user.RequestDate.Value.Date < DateTime.UtcNow.Date)
                    {
                        user.RequestDate = DateTime.UtcNow.Date;

                        user.RequestNumber = 0;
                    }

                    user.RequestNumber++;

                    for (var i = 0; i < 5; i++)
                    {
                        if (userDictionary.TryGetValue(user, out cookie)) break;
                    }

                    if (cookie == null)
                    {
                        cookie = Login(user.Email, user.Password);

                        if (cookie != null)
                        {
                            for (var i = 0; i < 5; i++)
                            {
                                if (userDictionary.TryUpdate(user, cookie, null)) break;
                            }
                        }
                    }

                    if (cookie == null)
                    {
                        goto Next;
                    }

                    var userEntity = db.User.FirstOrDefault(f => f.Id == user.Id);

                    if (userEntity != null)
                    {
                        userEntity.RequestDate = user.RequestDate;

                        userEntity.RequestNumber = user.RequestNumber;
                    }

                    db.TransactionSaveChanges();
                }
            }

            dataResult = GetResumeId(data, cookie, user);

            using (var db = new ResumeMatchDBEntities())
            {
                var resume = db.ResumeComplete.FirstOrDefault(f => f.ResumeId == data.ResumeId);

                if (resume != null)
                {
                    if (dataResult.IsSuccess)
                    {
                        if (!string.IsNullOrWhiteSpace(dataResult.Data))
                        {
                            resume.Host = host;

                            resume.Status = 2;

                            resume.MatchPlatform = (short)MatchPlatform.FenJianLi;

                            resume.MatchTime = DateTime.UtcNow;

                            resume.MatchResumeId = dataResult.Data;

                            resume.FenJianLiIsMatch = 1;

                            resume.UserId = user.Id;
                        }
                        else
                        {
                            resume.Host = host;

                            resume.MatchTime = DateTime.UtcNow;

                            resume.FenJianLiIsMatch = 2;
                        }

                        db.TransactionSaveChanges();
                    }
                }
            }

            return dataResult;
        }

        /// <summary>
        /// 获取简历ID
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cookie"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private static DataResult<string> GetResumeId(ResumeSearch data, CookieContainer cookie, User user)
        {
            var keyWord = HttpUtility.UrlEncode(data.University);

            var companyName = HttpUtility.UrlEncode(data.LastCompany);

            if (!string.IsNullOrWhiteSpace(keyWord) || !string.IsNullOrWhiteSpace(companyName))
            {
                return GetResumeId(keyWord, companyName, data.Name, cookie, user);
            }

            return new DataResult<string>();
        }

        /// <summary>
        /// 获取匹配到的简历ID
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="companyName"></param>
        /// <param name="name"></param>
        /// <param name="cookie"></param>
        /// <param name="pageIndex"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private static DataResult<string> GetResumeId(string keywords, string companyName, string name, CookieContainer cookie, User user, int pageIndex = 0)
        {
            Jumps:

            var param = $"keywords={keywords}&companyName={companyName}&rows=60&sortBy=1&sortType=1&offset={pageIndex * 30}&_random={new Random().NextDouble()}&name={name}";

            var dataResult = RequestFactory.QueryRequest("http://www.fenjianli.com/search/search.htm", param, RequestEnum.POST, cookie, isNeedSleep: true, host: user.Host);

            if (!dataResult.IsSuccess)
            {
                LogFactory.Warn($"搜索简历异常，返回结果为空！账户：{user.Email}",MessageSubjectEnum.FenJianLi);

                return dataResult;
            }

            if (dataResult.Data.Contains("\"error\"") || string.IsNullOrWhiteSpace(dataResult.Data))
            {
                Thread.Sleep(1500);

                goto Jumps;
            }

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                if ((int)jObject["totalSize"] == 0)
                {
                    return new DataResult<string>();
                }

                var totalSize = Math.Ceiling((double)jObject["totalSize"] / 30);

                var jArray = jObject["list"] as JArray;

                var resume = jArray?.FirstOrDefault(f => (string)f["realName"] == name);

                if (resume != null)
                {
                    dataResult.Data = $"{(string)resume["id"]}/{(string)resume["name"]}";

                    return dataResult;
                }

                if (pageIndex + 1 < totalSize && pageIndex < 3)
                {
                    return GetResumeId(keywords, companyName, name, cookie, user, ++pageIndex);
                }
            }

            return new DataResult<string>(); 
        }
    }
}