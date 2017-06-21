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

namespace ResumeMatchApplication.Platform.JianLika
{
    public abstract class JianLikaSpider : BaseSpider
    {
        /// <summary>
        /// 登录获取 Cookie
        /// </summary>
        /// <param name="email"></param>
        /// <param name="passWord"></param>
        /// <param name="host"></param>
        public static DataResult<CookieContainer> Login(string email, string passWord, string host)
        {
            var cookie = new CookieContainer();

            var result = new DataResult<CookieContainer>();

            var jumpsTimes = 0;

            Jumps:

            var param = $"username={email}&password={passWord}&remember=on";

            var dataResult = RequestFactory.QueryRequest("http://www.jianlika.com/Index/login.html", param, RequestEnum.POST, cookie, host: host);

            if (!dataResult.IsSuccess)
            {
                result.IsSuccess = false;

                result.ErrorMsg = $"用户登录异常！异常用户：{email}";

                return result;
            }

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                if ((int)jObject["status"] != 1)
                {
                    ++jumpsTimes;

                    if (jumpsTimes < 2)
                    {
                        using (var db = new ResumeMatchDBEntities())
                        {
                            var user = db.User.FirstOrDefault(f => f.Email == email);

                            if (user != null)
                            {
                                user.IsEnable = false;

                                db.TransactionSaveChanges();
                            }
                        }

                        result.IsSuccess = false;

                        result.ErrorMsg = $"用户登录异常！异常用户：{email},异常信息:{(string)jObject["message"]}";

                        return result;
                    }

                    goto Jumps;
                }

                result.Data = cookie;

                var retryCount = 0;

                Retry:

                try
                {
                    RefreshFreeDownloadNumber(email, cookie, host);
                }
                catch (Exception ex)
                {
                    retryCount++;

                    if (retryCount < 2) goto Retry;

                    LogFactory.Error($"刷新简历下载数异常！异常信息：{ex.Message} 堆栈信息：{ex.TargetSite}", MessageSubjectEnum.JianLiKa);
                }


                return result;
            }

            return result;
        }

        /// <summary>
        /// 刷新简历下载数
        /// </summary>
        /// <param name="email"></param>
        /// <param name="cookie"></param>
        /// <param name="host"></param>
        private static void RefreshFreeDownloadNumber(string email, CookieContainer cookie, string host)
        {
            var dataResult = RequestFactory.QueryRequest("http://www.jianlika.com/Search", cookieContainer: cookie, host: host);

            if (!dataResult.IsSuccess)
            {
                LogFactory.Warn($"用户登录刷新下载数异常！异常用户：{email}", MessageSubjectEnum.JianLiKa);

                return;
            }

            var html = dataResult.Data;

            if (Regex.IsMatch(html, "class=\"ico-png-money\"></i><span>(\\d+)"))
            {
                var count = Convert.ToInt32(Regex.Match(html, "class=\"ico-png-money\"></i><span>(\\d+)").Result("$1"));

                using (var db = new ResumeMatchDBEntities())
                {
                    var user = db.User.FirstOrDefault(f => f.Email == email);

                    if (user == null) return;

                    user.DownloadNumber = count;

                    db.TransactionSaveChanges();
                }
            }
        }
    }
}
