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

            CookieContainer cookie;

            var result = GetUser(userDictionary, host, true, MatchPlatform.FenJianLi, Login, out cookie);

            if (!result.IsSuccess)
            {
                dataResult.IsSuccess = false;

                dataResult.Code = result.Code;

                dataResult.ErrorMsg = result.ErrorMsg;

                return dataResult;
            }

            var user = result.Data;

            dataResult = GetResumeId(data, cookie, user);

            using (var db = new ResumeMatchDBEntities())
            {
                var resume = db.ResumeComplete.FirstOrDefault(f => f.ResumeId == data.ResumeId);

                if (resume != null)
                {
                    resume.Host = host;

                    resume.MatchPlatform = (short)MatchPlatform.FenJianLi;

                    resume.MatchTime = DateTime.UtcNow;

                    resume.UserId = user.Id;

                    if (!string.IsNullOrWhiteSpace(dataResult.Data))
                    {
                        resume.Status = 2;

                        resume.MatchResumeId = dataResult.Data;
                    }

                    db.TransactionSaveChanges();
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
            var num = 0;

            Jumps:

            var param = $"keywords={keywords}&companyName={companyName}&rows=60&sortBy=1&sortType=1&offset={pageIndex * 30}&_random={new Random().NextDouble()}&name={name}";

            var dataResult = RequestFactory.QueryRequest("http://www.fenjianli.com/search/search.htm", param, RequestEnum.POST, cookie, isNeedSleep: true, host: user.Host);

            if (!dataResult.IsSuccess)return dataResult;

            if (dataResult.Data.Contains("\"error\"") || string.IsNullOrWhiteSpace(dataResult.Data))
            {
                Thread.Sleep(1000);

                if (num < 2)
                {
                    num++;

                    goto Jumps;
                }

                return new DataResult<string>();
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

                if (pageIndex + 1 < totalSize && pageIndex < 0)
                {
                    Thread.Sleep(2000);

                    return GetResumeId(keywords, companyName, name, cookie, user, ++pageIndex);
                }
            }

            return new DataResult<string>(); 
        }
    }
}