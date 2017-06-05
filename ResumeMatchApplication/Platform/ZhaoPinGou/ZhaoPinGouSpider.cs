using System;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication.Platform.ZhaoPinGou
{
    public abstract class ZhaoPinGouSpider : BaseSpider
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

            var param = $"userName={email}&password={passWord}&code=&clientNo=&userToken=&clientType=2";

            var dataResult = RequestFactory.QueryRequest("http://qiye.zhaopingou.com/zhaopingou_interface/security_login?timestamp=" + BaseFanctory.GetUnixTimestamp(), param, RequestEnum.POST, cookie, host: host);

            if (!dataResult.IsSuccess)
            {
                result.IsSuccess = false;

                result.ErrorMsg = $"用户登录异常！异常用户：{email}";

                return result;
            }
            
            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                if ((int)jObject["errorCode"] != 1)
                {
                    ++jumpsTimes;
                   
                    if (jumpsTimes > 1)
                    {
                        result.IsSuccess = false;

                        result.ErrorMsg = $"用户登录异常！异常用户：{email},异常信息:{(string)jObject["message"]}";

                        return result;
                    }
                    
                    goto Jumps;
                }

                cookie.Add(new Cookie { Name = "hrkeepToken", Value = jObject["user"]?["user_token"].ToString(), Domain = ".zhaopingou.com" });

                cookie.Add(new Cookie { Name = "zhaopingou_select_city", Value = "-1", Domain = ".zhaopingou.com" });

                result.Data = cookie;

                RefreshFreeDownloadNumber(email, cookie);

                return result;
            }

            return result;
        }

        /// <summary>
        /// 刷新简历下载数
        /// </summary>
        /// <param name="email"></param>
        /// <param name="cookie"></param>
        private static void RefreshFreeDownloadNumber(string email, CookieContainer cookie)
        {
            var userToken = cookie.GetCookies(new Uri("http://qiye.zhaopingou.com/"))["hrkeepToken"];

            var param = $"isAjax=1&clientNo=&userToken={userToken?.Value}&clientType=2";

            var dataResult = RequestFactory.QueryRequest("http://qiye.zhaopingou.com/zhaopingou_interface/user_information?timestamp=" + BaseFanctory.GetUnixTimestamp(), param, RequestEnum.POST, cookie);

            if (!dataResult.IsSuccess)
            {
                LogFactory.Warn($"用户登录刷新下载数异常！异常用户：{email}",MessageSubjectEnum.ZhaoPinGou);

                return;
            }

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                if ((int)jObject["errorCode"] != 1)
                {
                    LogFactory.Warn($"用户登录异常！异常用户：{email},异常信息:{(string)jObject["message"]}", MessageSubjectEnum.ZhaoPinGou);

                    return;
                }

                using (var db = new ResumeMatchDBEntities())
                {
                    var user = db.User.FirstOrDefault(f => f.Email == email);

                    if (user == null) return;

                    var count = (int)jObject["memberEvents"]["free_count"];

                    user.DownloadNumber = count;

                    db.TransactionSaveChanges();
                }
            }
        }
    }
}
