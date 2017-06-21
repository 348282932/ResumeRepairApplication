using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using JiebaNet.Segmenter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication.Platform.JianLika
{
    /// <summary>
    /// 搜索简历
    /// </summary>
    public class SearchResumeSpider : JianLikaSpider
    {
        protected static ConcurrentDictionary<User, CookieContainer> userDictionary = new ConcurrentDictionary<User, CookieContainer>();

        private static readonly JiebaSegmenter jbs = new JiebaSegmenter();

        private static readonly Random random = new Random();

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

            var result = GetUser(userDictionary, host, true, MatchPlatform.JianLiKa, Login, out cookie);

            if (!result.IsSuccess)
            {
                dataResult.IsSuccess = false;

                dataResult.Code = result.Code;

                dataResult.ErrorMsg = result.ErrorMsg;

                return dataResult;
            }

            var user = result.Data;

            var count = 0;

            var keyWord = string.Empty;

            if (!string.IsNullOrWhiteSpace(data.University)) keyWord += data.University + " "; ++count; 

            if (count == 0) return new DataResult<string>(); 

            if (!string.IsNullOrWhiteSpace(data.Introduction))
            {
                var strArr = jbs.Cut(data.Introduction.Replace("\r\n", "")).Where(w => w.Length > 1).ToArray();

                keyWord += string.Join(" ", strArr.Skip(strArr.Length / 2 - 1).Take(3 - count));
            }

            keyWord = HttpUtility.UrlEncode(keyWord)?.Replace("+", "%20");

            var gender = data.Gender;

            var degress = MatchDegree(data.Degree);

            dataResult = GetResumeId(keyWord, data.LastCompany, gender, degress, data.Introduction, cookie, user);

            using (var db = new ResumeMatchDBEntities())
            {
                var resume = db.ResumeComplete.FirstOrDefault(f => f.ResumeId == data.ResumeId);

                if (resume != null)
                {
                    resume.Host = host;

                    resume.MatchPlatform = (short)MatchPlatform.JianLiKa;

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
        /// 获取匹配到的简历ID
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="lastCompany"></param>
        /// <param name="gender"></param>
        /// <param name="degress"></param>
        /// <param name="introduction"></param>
        /// <param name="cookie"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private static DataResult<string> GetResumeId(string keywords, string lastCompany, int gender, int degress, string introduction, CookieContainer cookie, User user)
        {
            var jumpsTimes = 0; 

             Jumps:

            var param = $"keywords={keywords}&companyName={lastCompany}&searchNear=off&jobs=&trade=&areas=&hTrade=&hJobs=&degree={degress}-{degress}&workYearFrom=&workYearTo=&ageFrom=&ageTo=&sex={gender + 1}&updateDate=0";

            var dataResult = RequestFactory.QueryRequest("http://www.jianlika.com/Search/index.html", param, RequestEnum.POST, cookie, "http://www.jianlika.com/Search/index.html", host: user.Host);

            if (!dataResult.IsSuccess) return dataResult;

            if (string.IsNullOrWhiteSpace(dataResult.Data)) goto Jumps;

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                if ((int)jObject["status"] != 1)
                {
                    ++jumpsTimes;

                    LogFactory.Warn($"搜索简历异常！异常信息：{(string)jObject["message"]}",MessageSubjectEnum.JianLiKa);

                    if (jumpsTimes > 3) return new DataResult<string>();

                    goto Jumps;
                }

                var url = (string)jObject["url"];

                dataResult = RequestFactory.QueryRequest(url, host: user.Host);

                if (!dataResult.IsSuccess) return dataResult;

                var matches = Regex.Matches(dataResult.Data, "/Resume/view/token/(\\w+).html") ;

                var urlArray = (from Match match in matches select match.Result("$1")).ToArray();

                if (string.IsNullOrWhiteSpace(introduction)) return new DataResult<string>();

                dataResult = MatchResumes(urlArray, introduction, cookie, user.Host);

                if (!dataResult.IsSuccess)
                {
                    if (dataResult.Code == ResultCodeEnum.ProxyDisable) return dataResult;

                    return new DataResult<string>();
                }

                return dataResult;
            }

            return new DataResult<string>();
        }

        /// <summary>
        /// 过滤简历
        /// </summary>
        /// <param name="resumeIdArray"></param>
        /// <param name="introduction"></param>
        /// <param name="cookie"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        private static DataResult<string> MatchResumes(IEnumerable<string> resumeIdArray, string introduction, CookieContainer cookie, string host)
        {
            foreach (var resumeId in resumeIdArray)
            {
                var dataResult = RequestFactory.QueryRequest($"http://www.jianlika.com/Resume/view/token/{resumeId}.html", cookieContainer: cookie, host: host);

                if (!dataResult.IsSuccess)
                {
                    if (dataResult.Code == ResultCodeEnum.ProxyDisable) return dataResult;

                    continue;
                }

                if (string.IsNullOrWhiteSpace(dataResult.Data)) continue;

                if(!Regex.IsMatch(dataResult.Data, "(?s)自我评价.+?<p class=\"text-block\">(.+?)</p>")) continue;

                var selfEvaluation = Regex.Match(dataResult.Data, "(?s)自我评价.+?<p class=\"text-block\">(.+?)</p>").Result("$1");

                selfEvaluation = selfEvaluation.Replace("<br />", "\r\n").Replace("<em>", "").Replace("</em>", "").Replace("<BR>", "\r\n").Replace(" ","");

                if (selfEvaluation.Length <= 2) continue;

                var start = random.Next(0, selfEvaluation.Length / 2 - 1);

                var end = start + 6 > selfEvaluation.Length ? selfEvaluation.Length - start : 6;

                selfEvaluation = selfEvaluation.Substring(start, end);

                if (introduction.Replace(" ","").Contains(selfEvaluation))
                {
                    dataResult.Data = resumeId;

                    return dataResult;
                }
            }

            return new DataResult<string>();
        }

        /// <summary>
        /// 获取对应学历
        /// </summary>
        /// <param name="degree"></param>
        /// <returns></returns>
        private static int MatchDegree(string degree)
        {
            switch (degree.ToUpper())
            {
                case "初中":
                    return 1;
                case "高中":
                    return 2;
                case "中技":
                    return 3;
                case "中专":
                    return 4;
                case "大专":
                    return 5;
                case "本科":
                    return 6;
                case "MBA":
                case "EMBA":
                    return 7;
                case "硕士":
                    return 8;
                case "博士":
                    return 9;
                case "博士后":
                    return 10;
                default:
                    return 0;
            }
        }
    }
}