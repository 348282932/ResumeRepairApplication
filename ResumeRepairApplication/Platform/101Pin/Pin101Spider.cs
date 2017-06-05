using ResumeRepairApplication.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks.Dataflow;
using System.Web;
using System.Windows.Forms;

namespace ResumeRepairApplication._101Pin
{
    public abstract class Pin101Spider : BaseSpider
    {
        /// <summary>
        /// 登录获取 Cookie
        /// </summary>
        /// <param name="email"></param>
        /// <param name="passWord"></param>
        protected static CookieContainer Login(string email, string passWord)
        {
            var cookie = new CookieContainer();

            var param = $"username={email}&password={passWord}&showpassword=请输入密码&protocol=on";

            var sources = RequestFactory.QueryRequest("http://www.101pin.com/Home/User/login.html", param, RequestEnum.POST, cookie);

            if (!sources.Contains("success"))
            {
                throw new RequestException($"帐号：{email} 登录失败！{Environment.NewLine}响应源：{sources}");
            }

            return cookie;
        }
    }
}
