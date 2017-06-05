using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using ResumeRepairApplication.Common;
using ResumeRepairApplication.EntityFramework;

namespace ResumeRepairApplication.Platform.FenJianLi
{
    /// <summary>
    /// 注册
    /// </summary>
    public class RegisterSpider : FenJianLiSpider
    {
        private static readonly CookieContainer cookie = new CookieContainer();

        private static string userEmail = string.Empty; // 创建的随机邮箱

        /// <summary>
        /// 注册
        /// </summary>
        private void Register()
        {
            // GET 注册页面

            string sources = RequestFactory.QueryRequest("http://www.fenjianli.com/register/toRegisterByEmail.htm", cookieContainer: cookie);

            string id = Regex.Match(sources, "validate-id.+?\"(\\d+)")?.Result("$1");

            if (id == null) throw new RequestException("获取注册 ID 失败，失败原因：请求异常，导致解析HTML出错，源码："+ sources);

            string password = DateTime.Now.ToString("yyyyMMddHHmmss");

            string username = password + Global.Email.Substring(Global.Email.IndexOf("@"));

            string url = "http://www.fenjianli.com/register/register.htm";

            #region 获取验证码

            var imgUrl = "http://www.fenjianli.com/register/getCheckCode.htm?" + new Random().NextDouble(); // 随机化图片验证码

            var loginRequest = (HttpWebRequest)WebRequest.Create(imgUrl);

            loginRequest.Accept = "image/png, image/svg+xml, image/*;q=0.8, */*;q=0.5"; // 图片类型

            loginRequest.CookieContainer = cookie;

            using (var imgresponse = loginRequest.GetResponse())
            {
                using (var imgreader = imgresponse.GetResponseStream())
                {
                    var writer = new FileStream("D:\\pic.jpg", FileMode.OpenOrCreate, FileAccess.Write);

                    var buff = new byte[512];

                    int read;

                    while (imgreader != null && (read = imgreader.Read(buff, 0, buff.Length)) > 0)
                    {
                        writer.Write(buff, 0, read);
                    }

                    writer.Close();

                    writer.Dispose();
                }
            }

            StreamReader sr = new StreamReader("D:\\pic.jpg");

            FenJianLiScheduling.cnf.pbx_checkNumber.Image = Image.FromStream(sr.BaseStream);
            
            string checkCode = Interaction.InputBox("请输入验证码！", "验证码");

            sr.Close();
            sr.Dispose();

            //checkForm.Close();

            #endregion

            string param = $"id={id}&regType=email&username={username}&password={password}&confirmPassword={password}&checkCode={checkCode}&agree=1&_random={new Random().NextDouble()}";

            // 发送注册请求
            
            sources = RequestFactory.QueryRequest(url, param, RequestEnum.POST, cookie);

            if (!sources.Contains("成功")) throw new RequestException($"注册请求失败！响应数据：{sources}");

            userEmail = username;

            using (var db = new ResumeRepairDBEntities())
            {
                db.FenJianLi.Add(new EntityFramework.FenJianLi
                {
                    Email = userEmail,
                    PassWord = userEmail.Substring(0, 14),
                    CreateTime = DateTime.UtcNow,
                    IsEnable = true,
                    Integral = 255,
                    IsActivation = false,
                    IsVerification = false,
                    IsLocked = false
                });

                db.SaveChanges();
            }

            FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.fjl_tbx_RegisterActivation, $"注册成功！邮箱：{userEmail}");

            url = "http://www.fenjianli.com/register/sendCheckEmail.htm";

            param = $"id={id}&checkSource={username}&_random={new Random().NextDouble()}";

            sources = RequestFactory.QueryRequest(url, param, RequestEnum.POST, cookie);

            // 发送激活邮件请求

            if (!sources.Contains("成功")) throw new RequestException($"发送激活邮件请求失败！响应数据：{sources}");

            FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.fjl_tbx_RegisterActivation, $"发送激活邮件成功！邮箱：{userEmail}");
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public override DataResult Init()
        {
            try
            {
                this.Register(); // 注册

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
