﻿using System.Net;
using System.Web;
using ResumeMatchApplication.Common;

namespace ResumeMatchApplication.Platform.FenJianLi
{
    public abstract class FenJianLiSpider : BaseSpider
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

            cookie.Add(new Cookie { Name = "username", Value = HttpUtility.UrlEncode(email), Domain = "www.fenjianli.com" });

            cookie.Add(new Cookie { Name = "password", Value = passWord.MD5(), Domain = "www.fenjianli.com" });

            return new DataResult<CookieContainer>(cookie);
        }
    }
}
