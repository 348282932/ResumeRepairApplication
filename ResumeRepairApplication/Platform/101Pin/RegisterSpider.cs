using ResumeRepairApplication.Common;
using ResumeRepairApplication.EntityFramework;
using System;
using System.Net;
using System.Linq;
using ResumeRepairApplication.Platform._101Pin;

namespace ResumeRepairApplication._101Pin
{
    /// <summary>
    /// 注册
    /// </summary>
    public class RegisterSpider : Pin101Spider
    {
        private static CookieContainer cookie = new CookieContainer();

        private static string userEmail = string.Empty; // 创建的随机邮箱

        /// <summary>
        /// 注册
        /// </summary>
        private void Register()
        {
            string inviteCode = "861666925";// string.Empty;

            using (var db = new ResumeRepairDBEntities())
            {
                var pin101 = db.Pin101.Where(w => w.IsEnable && w.IsActivation && !string.IsNullOrEmpty(w.InviteCode)).OrderBy(o => o.Integral).FirstOrDefault();

                if (pin101 != null)
                {
                    inviteCode = pin101.InviteCode;
                }
            }

            // GET 注册页面

            string password = DateTime.Now.ToString("yyyyMMddHHmmss");

            string username = password + Global.Email.Substring(Global.Email.IndexOf("@"));

            string url = "http://101pin.com/home/user/register";

            string param = $"username2={username}&password2={password}&showpassword=请设置密码&protocal2=on&reg_flag=2&invitecode={inviteCode}&invitefrom=1";

            // 发送注册请求

            var sources = RequestFactory.QueryRequest(url, param, RequestEnum.POST, cookie, referer: "http://101pin.com/Home/User/register/invite/" + inviteCode);

            if (!sources.Contains("激活邮件已发送至你的邮箱")) throw new RequestException($"注册请求失败！响应数据：{sources}");

            userEmail = username;

            using (var db = new ResumeRepairDBEntities())
            {
                db.Pin101.Add(new Pin101
                {
                    Email = userEmail,
                    PassWord = userEmail.Substring(0, 14),
                    CreateTime = DateTime.UtcNow,
                    IsEnable = true,
                    Integral = 0,
                    IsActivation = false,
                    IsVerification = false,
                    IsLocked = false,
                    InviteCode=""
                });

                db.SaveChanges();
            }

            Pin101Scheduling.ssf.SetText(Pin101Scheduling.ssf.fjl_tbx_RegisterActivation, $"注册成功！邮箱：{userEmail}");
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public override DataResult Init()
        {
            try
            {
                Register(); // 注册

                return new DataResult();
            }
            catch (RequestException ex)
            {
                return new DataResult("请求异常！" + ex.Message);
            }
            catch (Exception ex)
            {
                return new DataResult("程序异常！" + ex.Message);
            }
        }
    }
}
