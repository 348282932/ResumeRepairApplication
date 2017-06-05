using System.Net;
using System.Web;
using ResumeRepairApplication.Common;

namespace ResumeRepairApplication.Platform.FenJianLi
{
    public abstract class FenJianLiSpider : BaseSpider
    {
        /// <summary>
        /// 登录获取 Cookie
        /// </summary>
        /// <param name="email"></param>
        /// <param name="passWord"></param>
        public static CookieContainer Login(string email, string passWord)
        {
            CookieContainer cookie = new CookieContainer();

            cookie.Add(new Cookie { Name = "username", Value = HttpUtility.UrlEncode(email), Domain = "www.fenjianli.com" });

            cookie.Add(new Cookie { Name = "password", Value = passWord.MD5(), Domain = "www.fenjianli.com" });

            //cookie.Add(new Cookie { Name = "JSESSIONID", Value = Guid.NewGuid().ToString().Replace("-", "").ToUpper(), Domain = "www.fenjianli.com" });

            //var param = $"username={email}&password={passWord}&rememberMe=1";

            //string sources = RequestFactory.QueryRequest("http://www.fenjianli.com/login/login.htm", param, RequestEnum.POST, cookie);

            //if (!sources.Contains("success"))
            //{
            //    throw new RequestException($"帐号：{email} 登录失败！{Environment.NewLine}响应源：{sources}");
            //}

            return cookie;
        }
    }
}
