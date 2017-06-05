using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication.Platform.FenJianLi
{
    public class SearchResumeSpider
    {
        protected static ConcurrentQueue<User> userQueue = new ConcurrentQueue<User>();

        /// <summary>
        /// 获取简历ID
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cookie"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private string GetResumeId(ResumeSearch data, CookieContainer cookie, User user)
        {
            var keyWord = HttpUtility.UrlEncode(data.University);

            var companyName = HttpUtility.UrlEncode(data.LastCompany);

            if (!string.IsNullOrWhiteSpace(keyWord) || !string.IsNullOrWhiteSpace(companyName))
            {
                return GetResumeId(keyWord, companyName, data.Name, cookie, user);
            }

            return string.Empty;
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
        private static string GetResumeId(string keywords, string companyName, string name, CookieContainer cookie, User user, int pageIndex = 0)
        {
            Jumps:

            var param = $"keywords={keywords}&companyName={companyName}&rows=60&sortBy=1&sortType=1&offset={pageIndex * 30}&_random={new Random().NextDouble()}&name={name}";

            var dataResult = RequestFactory.QueryRequest("http://www.fenjianli.com/search/search.htm", param, RequestEnum.POST, cookie, isNeedSleep: true, host: user.Host);

            if (!dataResult.IsSuccess)
            {
                LogFactory.Warn($"搜索简历异常，返回结果为空！账户：{user.Email}",MessageSubjectEnum.FenJianLi);

                return string.Empty;
            }

            if (dataResult.Data.Contains("\"error\"") || string.IsNullOrWhiteSpace(dataResult.Data)) goto Jumps;

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                if((int)jObject["totalSize"] == 0) return string.Empty;

                var totalSize = Math.Ceiling((double)jObject["totalSize"] / 30);

                var jArray = jObject["list"] as JArray;

                var resume = jArray?.FirstOrDefault(f => (string)f["realName"] == name);

                if (resume != null) return $"{(string)resume["id"]}/{(string)resume["name"]}";

                if (pageIndex + 1 < totalSize && pageIndex < 3)
                {
                    return GetResumeId(keywords, companyName, name, cookie, user, ++pageIndex);
                }
            }

            return string.Empty;
        }
    }
}