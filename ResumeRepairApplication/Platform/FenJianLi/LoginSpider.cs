using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using ResumeRepairApplication.Common;
using ResumeRepairApplication.EntityFramework;

namespace ResumeRepairApplication.Platform.FenJianLi
{
    public class LoginSpider : FenJianLiSpider
    {
        public static CookieContainer cookie = new CookieContainer();

		/// <summary>
		/// 登录获取积分
		/// </summary>
		/// <returns></returns>
		private DataResult Login()
        {
            var dataResult = new DataResult();

            using (var db = new ResumeRepairDBEntities())
            {
                var dateTime = DateTime.UtcNow.Date;

                var users = db.FenJianLi.Where(w => w.IsActivation && (w.LastLoginTime == null || w.LastLoginTime < dateTime)).Take(3).ToList();

                foreach (var user in users)
                {
                    var param = $"username={user.Email}&password={user.PassWord}&rememberMe=1";

                    var sources = RequestFactory.QueryRequest("http://www.fenjianli.com/login/login.htm", param, RequestEnum.POST, cookie);

                    if (!sources.Contains("success"))
                    {
                        dataResult.IsSuccess = false;

                        dataResult.ErrorMsg += $"帐号：{user.Email} 登录失败！{Environment.NewLine}响应源：{sources}";

                        continue;
                    }

                    To:

                    Thread.Sleep(1000);

                    sources = RequestFactory.QueryRequest("http://www.fenjianli.com/search/home.htm", cookieContainer: cookie);

                    if (string.IsNullOrWhiteSpace(sources)) goto To;

                    var integralStr = Regex.Match(sources, ">.(\\d+)</a>个积分").Result("$1");

	                user.LastLoginTime = DateTime.UtcNow;

                    user.Integral = Convert.ToInt32(integralStr);

                    FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.fjl_tbx_LoginCheckIn, $"登录成功!账户:{user.Email},积分：{integralStr}");
                }

                db.SaveChanges();

            }

            //FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.fjl_tbx_LoginCheckIn, $"刷新积分成功！");

            return new DataResult();
        }

        public override DataResult Init()
        {
            try
            {
                return this.Login(); // 登录
            }
            catch (Exception ex)
            {
                return new DataResult("程序异常！" + ex.Message);
            }
        }
    }
}
