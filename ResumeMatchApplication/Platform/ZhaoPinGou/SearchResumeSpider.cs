﻿using System;
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

namespace ResumeMatchApplication.Platform.ZhaoPinGou
{
    /// <summary>
    /// 搜索简历
    /// </summary>
    public class SearchResumeSpider : ZhaoPinGouSpider
    {
        protected static ConcurrentDictionary<User, CookieContainer> userDictionary = new ConcurrentDictionary<User, CookieContainer>();

        private static readonly JiebaSegmenter jbs = new JiebaSegmenter();

        private static readonly Random random = new Random();

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
                        var users = db.User.Where(w => w.IsEnable && w.Platform == 4 && w.Status == 1 && w.Host == host).ToList();

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
                        .Where(f => f.IsEnable && f.Host == host && ( f.RequestDate == null || f.RequestDate.Value.Date < DateTime.UtcNow.Date || f.RequestDate.Value.Date == DateTime.UtcNow.Date && f.RequestNumber < Global.TodayMaxRequestNumber))
                        .OrderBy(o=>o.RequestNumber)
                        .FirstOrDefault();

                    if (user == null)
                    {
                        dataResult.IsSuccess = false;

                        dataResult.Code = ResultCodeEnum.RequestUpperLimit;

                        LogFactory.Warn(JsonConvert.SerializeObject(userDictionary),MessageSubjectEnum.ZhaoPinGou);

                        var list = userDictionary.Keys.Where(w => w.Host == host);

                        foreach (var item in list)
                        {
                            for (var i = 0; i < 5; i++)
                            {
                                if (userDictionary.TryRemove(item, out cookie)) break;

                                if (i == 4)
                                {
                                    LogFactory.Warn($"从字典中移除用户 {item.Email} 失败！", MessageSubjectEnum.ZhaoPinGou);

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
                        var result = Login(user.Email, user.Password, host);

                        if (!result.IsSuccess)
                        {
                            LogFactory.Warn(result.ErrorMsg, MessageSubjectEnum.ZhaoPinGou);

                            dataResult.IsSuccess = false;

                            return dataResult;
                        }

                        cookie = result.Data;

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

                    var userEntity = db.User.FirstOrDefault(f=>f.Id == user.Id);

                    if (userEntity != null)
                    {
                        userEntity.RequestDate = user.RequestDate;

                        userEntity.RequestNumber = user.RequestNumber;
                    }

                    db.TransactionSaveChanges();
                }
            }

            var count = 0;

            var keyWord = string.Empty;

            if (!string.IsNullOrWhiteSpace(data.University)) keyWord += data.University + " "; ++count; 

            if (!string.IsNullOrWhiteSpace(data.LastCompany)) keyWord += data.LastCompany + " "; ++count;

            if (count == 0) return new DataResult<string>(); 

            if (!string.IsNullOrWhiteSpace(data.Introduction))
            {
                var strArr = jbs.Cut(data.Introduction.Replace("\r\n", "")).Where(w => w.Length > 1).ToArray();

                keyWord += string.Join(" ", strArr.Skip(strArr.Length / 2 - 1).Take(3 - count));
            }

            keyWord = HttpUtility.UrlEncode(keyWord)?.Replace("+", "%20");

            var gender = data.Gender;

            var degress = MatchDegree(data.Degree);

            var userToken = cookie.GetCookies(new Uri("http://qiye.zhaopingou.com/"))["hrkeepToken"];

            dataResult = GetResumeId(keyWord, gender, degress, data.Introduction, cookie, user, userToken?.Value, data.Name);

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

                            resume.MatchPlatform = (short)MatchPlatform.ZhaoPinGou;

                            resume.MatchTime = DateTime.UtcNow;

                            resume.MatchResumeId = dataResult.Data;

                            resume.ZhaoPinGouIsMatch = 1;

                            resume.UserId = user.Id;
                        }
                        else
                        {
                            resume.Host = host;

                            resume.MatchTime = DateTime.UtcNow;

                            resume.ZhaoPinGouIsMatch = 2;
                        }

                        db.TransactionSaveChanges();
                    }
                }
            }

            return dataResult;
        }

        /// <summary>
        /// 获取匹配到的简历ID
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="degress"></param>
        /// <param name="introduction"></param>
        /// <param name="cookie"></param>
        /// <param name="token"></param>
        /// <param name="name"></param>
        /// <param name="pageIndex"></param>
        /// <param name="user"></param>
        /// <param name="gender"></param>
        /// <returns></returns>
        private static DataResult<string> GetResumeId(string keywords, int gender, int degress, string introduction, CookieContainer cookie, User user, string token, string name, int pageIndex = 0)
        {
            var jumpsTimes = 0; 

             Jumps:

            var param = $"pageSize={pageIndex}&pageNo=25&keyStr={keywords}&keyStrPostion=&postionStr=&startDegrees={degress}&endDegress={degress}&startAge=0&endAge=0&gender={gender}&region=&timeType=-1&startWorkYear=-1&endWorkYear=-1&beginTime=&endTime=&isMember=0&hopeAdressStr=&cityId=-1&updateTime=&tradeId=&clientNo=&userToken={token}&clientType=2";

            var referer = $"http://qiye.zhaopingou.com/resume?key={keywords}&beginDegreesType={degress}&endDegreesType={degress}&gender={gender}";

            var dataResult = RequestFactory.QueryRequest("http://qiye.zhaopingou.com/zhaopingou_interface/find_warehouse_by_position_new?timestamp=" + BaseFanctory.GetUnixTimestamp(), param, RequestEnum.POST, cookie, referer, host: user.Host);

            if (!dataResult.IsSuccess)
            {
                if (dataResult.Code == ResultCodeEnum.ProxyDisable) return dataResult;

                ++jumpsTimes;

                LogFactory.Warn($"搜索简历异常，返回结果为空！账户：{user.Email}",MessageSubjectEnum.ZhaoPinGou);

                if (jumpsTimes > 3) return new DataResult<string>();

                goto Jumps;
            }

            if (string.IsNullOrWhiteSpace(dataResult.Data)) goto Jumps;

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                if ((int)jObject["errorCode"] != 1)
                {
                    ++jumpsTimes;

                    LogFactory.Warn($"搜索简历异常！异常信息：{(string)jObject["message"]}",MessageSubjectEnum.ZhaoPinGou);

                    if (jumpsTimes > 3) return new DataResult<string>();

                    goto Jumps;
                }

                if ((int)jObject["total"] == 0) return new DataResult<string>();

                var totalSize = Math.Ceiling((double)jObject["total"] / 25);

                var jArray = jObject["warehouseList"] as JArray;

                var resumeIdArray = jArray?.Where(w=>((string)w["name"]).Substring(0,1) == name.Substring(0,1)).Select(s=>(string)s["resumeHtmlId"]).ToArray();

                if (string.IsNullOrWhiteSpace(introduction)) return new DataResult<string>();

                dataResult = MatchResumes(resumeIdArray, introduction, cookie, user.Host, token, keywords);

                if (!dataResult.IsSuccess)
                {
                    if (dataResult.Code == ResultCodeEnum.ProxyDisable) return dataResult;

                    return new DataResult<string>();
                }

                if (!string.IsNullOrWhiteSpace(dataResult.Data)) return dataResult;

                if (pageIndex + 1 < totalSize && pageIndex < 1)
                {
                    return GetResumeId(keywords, gender, degress, introduction, cookie, user, token, name, ++pageIndex);
                }
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
        /// <param name="token"></param>
        /// <param name="keyStr"></param>
        /// <returns></returns>
        private static DataResult<string> MatchResumes(IEnumerable<string> resumeIdArray, string introduction, CookieContainer cookie, string host, string token, string keyStr)
        {
            cookie.Add(new Cookie { Name = "fangWenNumber1", Value = "2", Domain = "qiye.zhaopingou.com" });

            cookie.Add(new Cookie { Name = "fangWenIp", Value = $"{random.Next(100, 1000)}.{random.Next(100, 1000)}.{random.Next(1, 1000)}.{random.Next(1, 1000)}", Domain = "qiye.zhaopingou.com" });

            cookie.Add(new Cookie { Name = "fanwenTime1", Value = $"\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"", Domain = "qiye.zhaopingou.com" });

            foreach (var resumeId in resumeIdArray)
            {
                var dataResult = ResumeDetailSpider(resumeId, cookie, host, token, keyStr);

                if (!dataResult.IsSuccess)
                {
                    if (dataResult.Code == ResultCodeEnum.ProxyDisable) return dataResult;

                    continue;
                }

                if (string.IsNullOrWhiteSpace(dataResult.Data)) continue;

                if(!Regex.IsMatch(dataResult.Data, "(?s)自我评价.+?<p class='ptxt'>(.+?)</p>")) continue;

                var selfEvaluation = Regex.Match(dataResult.Data, "(?s)自我评价.+?<p class='ptxt'>(.+?)</p>").Result("$1");

                selfEvaluation = selfEvaluation.Replace("<br>", "\r\n").Replace("<span class='search_check'>","").Replace("</span>","").Replace("<BR>", "\r\n").Replace(" ","");

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
        /// 获取简历详情
        /// </summary>
        /// <param name="resumeId"></param>
        /// <param name="cookie"></param>
        /// <param name="host"></param>
        /// <param name="token"></param>
        /// <param name="keyStr"></param>
        /// <returns></returns>
        private static DataResult<string> ResumeDetailSpider(string resumeId, CookieContainer cookie, string host, string token, string keyStr)
        {
            var jumpsTimes = 0;

            Jumps:

            var param = $"resumeHtmlId={resumeId}&keyStr={keyStr}&keyPositionName=&tradeId=&postionStr=&jobId=0&clientNo=&userToken={token}&clientType=2";

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
        /// 获取对应学历
        /// </summary>
        /// <param name="degree"></param>
        /// <returns></returns>
        private static int MatchDegree(string degree)
        {
            switch (degree)
            {
                case "高中":
                case "中技":
                case "中专":
                    return 1;
                case "大专":
                    return 2;
                case "本科":
                    return 3;
                case "硕士":
                    return 4;
                case "MBA":
                case "mba":
                    return 5;
                case "博士":
                    return 6;
                default:
                    return -1;
            }
        }
    }
}