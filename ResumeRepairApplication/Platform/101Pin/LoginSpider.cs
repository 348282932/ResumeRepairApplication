using ResumeRepairApplication._101Pin;
using ResumeRepairApplication.Common;
using ResumeRepairApplication.EntityFramework;
using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace ResumeRepairApplication.Platform._101Pin
{
    public class LoginSpider : Pin101Spider
    {
        public static CookieContainer cookie = new CookieContainer();

        /// <summary>
        /// 登录获取积分
        /// </summary>
        private DataResult Login()
        {
            var dataResult = new DataResult();

            using (var db = new ResumeRepairDBEntities())
            {
                var dateTime = DateTime.UtcNow.Date;

                var users = db.Pin101.Where(w => w.IsActivation && (w.LastLoginTime == null || w.LastLoginTime < dateTime)).Take(3).ToList();

                foreach (var user in users)
                {
                    cookie = Login(user.Email, user.PassWord);

                To:

                    Thread.Sleep(1000);

                    var sources = RequestFactory.QueryRequest("http://www.101pin.com/home/score/invite", cookieContainer: cookie);

                    if (string.IsNullOrWhiteSpace(sources)) goto To;

                    var integralStr = Regex.Match(sources, "剩余下载数：<strong>(\\d+)</strong>")?.Result("$1");

                    if (string.IsNullOrWhiteSpace(integralStr))
                    {
                        dataResult.IsSuccess = false;

                        dataResult.ErrorMsg += $"帐号：{user.Email} 获取积分失败！{Environment.NewLine}";

                        continue;
                    }

                    var inviteStr = Regex.Match(sources, "/invite/(\\d+)").Result("$1");

                    user.LastLoginTime = DateTime.UtcNow;

                    user.InviteCode = inviteStr;

                    user.Integral = Convert.ToInt32(integralStr);

                    Pin101Scheduling.ssf.SetText(Pin101Scheduling.ssf.fjl_tbx_LoginCheckIn, $"登录成功!账户:{user.Email},积分：{integralStr},邀请码：{inviteStr}");
                }

                db.SaveChanges();

            }

            return new DataResult();
        }

        public override DataResult Init()
        {
            try
            {
                return Login(); // 登录
            }
            catch (Exception ex)
            {
                return new DataResult("程序异常！" + ex.Message);
            }
        }
    }
}
