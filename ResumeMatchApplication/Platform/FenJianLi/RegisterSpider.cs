using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication.Platform.FenJianLi
{
    /// <summary>
    /// 注册
    /// </summary>
    public class RegisterSpider : FenJianLiSpider
    {
        private static readonly object lockObj = new object();
       
        /// <summary>
        /// 注册
        /// </summary>
        private void Register()
        {
            var proxyId = 0;

            var host = string.Empty;

            var inviteCode = string.Empty;

            var userId = 0;

            var isAddProxy = false;

            lock (lockObj)
            {
                using (var db = new ResumeMatchDBEntities())
                {
                    var data = db.UsingTransaction(() =>
                    {
                        var result = new DataResult();

                        if (!Global.IsEnanbleProxy)
                        {
                            var user = db.User.Where(w => string.IsNullOrEmpty(w.Host) && w.Platform == 1 && w.Status != 0 && !string.IsNullOrEmpty(w.InviteCode)).OrderBy(o => o.DownloadNumber).FirstOrDefault();

                            if (user != null)
                            {
                                inviteCode = user.InviteCode;

                                userId = user.Id;
                            }

                            return result;
                        }

                        var ipArr = db.Proxy.AsNoTracking()
                            .GroupBy(g => g.Host)
                            .Select(s => new { Host = s.Key, Count = s.Count() })
                            .Where(w => w.Count < Global.PlatformCount * Global.PlatformHostCount)
                            .Select(s => s.Host).ToArray();

                        if (ipArr.Length > 0)
                        {
                            var proxy = db.Proxy.FirstOrDefault(f => ipArr.Any(a => a == f.Host) && f.Platform == 1 && !string.IsNullOrEmpty(f.Host));

                            if (proxy != null && proxy.Count < Global.PlatformHostCount)
                            {
                                proxyId = proxy.Id;

                                host = proxy.Host;

                                proxy.Count++;

                                var user = db.User.Where(w => w.Host == host && w.Platform == 1).OrderBy(o => o.DownloadNumber).FirstOrDefault();

                                if (user == null)
                                {
                                    LogFactory.Error($"找不到用户，Host = {host}", MessageSubjectEnum.FenJianLi);

                                    result.IsSuccess = false;

                                    return result;
                                }

                                inviteCode = user.InviteCode;

                                userId = user.Id;

                                db.SaveChanges();

                                return result;
                            }
                        }

                        var proxyEntity = new Proxy { Count = 1, Platform = 1 };

                        db.Proxy.Add(proxyEntity);

                        db.SaveChanges();

                        isAddProxy = true;

                        proxyId = proxyEntity.Id;

                        return result;
                    });

                    if (!data.IsSuccess) return;
                }
            }

            if (string.IsNullOrWhiteSpace(host) && Global.IsEnanbleProxy) host = GetProxy(true);

            var dataResult = Register(host, inviteCode);

            using (var db = new ResumeMatchDBEntities())
            {
                var proxy = db.Proxy.FirstOrDefault(f => f.Id == proxyId);

                if (dataResult == null || !dataResult.IsSuccess)
                {
                    if (proxy != null)
                    {
                        if (isAddProxy)
                        {
                            db.Proxy.Remove(proxy);
                        }
                        else
                        {
                            proxy.Count--;
                        }
                    }
                }
                else
                {
                    db.User.Add(new User
                    {
                        Email = dataResult.Data.Email,
                        Password = dataResult.Data.Password,
                        CreateTime = DateTime.UtcNow,
                        IsEnable = true,
                        DownloadNumber = (255-2) / 3,
                        Host = host,
                        Platform = 1,
                        Status = 0
                    });

                    if (!string.IsNullOrWhiteSpace(inviteCode))
                    {
                        var user = db.User.FirstOrDefault(f => f.Id == userId);

                        if (user != null) user.DownloadNumber += 100;
                    }
                    if (!string.IsNullOrWhiteSpace(host) && proxy != null)
                    {
                        proxy.Host = host;
                    }
                }

                db.TransactionSaveChanges();
            }
        }

        /// <summary>
        /// 注册蜘蛛
        /// </summary>
        /// <param name="host">代理IP</param>
        /// <param name="inviteCode">邀请码</param>
        /// <returns></returns>
        [Loggable]
        private static DataResult<dynamic> Register(string host, string inviteCode)
        {
            var cookie = new CookieContainer();

            if (!string.IsNullOrWhiteSpace(inviteCode))
            {
                cookie.Add(new Cookie { Name = "vid", Value = inviteCode, Domain = "www.fenjianli.com" });
            }

            // GET 注册页面

            var dataResult = RequestFactory.QueryRequest("http://www.fenjianli.com/register/toRegisterByEmail.htm", cookieContainer: cookie, host: host);

            if (!dataResult.IsSuccess) return new DataResult<dynamic>(dataResult.ErrorMsg);

            var id = Regex.Match(dataResult.Data, "validate-id.+?\"(\\d+)").Result("$1");

            if (id == null) throw new RequestException("获取注册 ID 失败，失败原因：请求异常，导致解析HTML出错，源码："+ dataResult.Data);

            var password = DateTime.Now.ToString("yyyyMMddHHmmss");

            var username = password + Global.Email.Substring(Global.Email.IndexOf("@", StringComparison.Ordinal));

            #region 获取验证码

            var imgUrl = "http://www.fenjianli.com/register/getCheckCode.htm?" + new Random().NextDouble(); // 随机化图片验证码

            var loginRequest = (HttpWebRequest)WebRequest.Create(imgUrl);

            loginRequest.Accept = "image/png, image/svg+xml, image/*;q=0.8, */*;q=0.5"; // 图片类型

            loginRequest.CookieContainer = cookie;

            if (Global.IsEnanbleProxy)
            {
                var index = host.IndexOf(":", StringComparison.Ordinal);

                loginRequest.Proxy = new WebProxy(host.Substring(0, index), Convert.ToInt32(host.Substring(index + 1)));
            }

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

            //var sr = new StreamReader("D:\\pic.jpg");

            //FenJianLiScheduling.cnf.pbx_checkNumber.Image = MediaTypeNames.Image.FromStream(sr.BaseStream);

            //string checkCode = Interaction.InputBox("请输入验证码！", "验证码");

            //sr.Close();

            //sr.Dispose();

            Console.WriteLine("请输入验证码：");

            var checkCode = Console.ReadLine();

            #endregion

            var param = $"id={id}&regType=email&username={username}&password={password}&confirmPassword={password}&checkCode={checkCode}&agree=1&_random={new Random().NextDouble()}";

            // 发送注册请求
            
            dataResult = RequestFactory.QueryRequest("http://www.fenjianli.com/register/register.htm", param, RequestEnum.POST, cookie, host: host);

            if (!dataResult.IsSuccess) return new DataResult<dynamic>(dataResult.ErrorMsg);

            if (!dataResult.Data.Contains("成功")) throw new RequestException($"注册请求失败！响应数据：{dataResult.Data}");

            LogFactory.Info($"注册成功！邮箱：{username}",MessageSubjectEnum.FenJianLi);

            param = $"id={id}&checkSource={username}&_random={new Random().NextDouble()}";

            dataResult = RequestFactory.QueryRequest("http://www.fenjianli.com/register/sendCheckEmail.htm", param, RequestEnum.POST, cookie, host: host);

            // 发送激活邮件请求

            if (!dataResult.IsSuccess) return new DataResult<dynamic>(dataResult.ErrorMsg);

            if (!dataResult.Data.Contains("成功")) throw new RequestException($"发送激活邮件请求失败！响应数据：{dataResult.Data}");

            return new DataResult<dynamic>(new {Email = username, Password = password});
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
            catch (Exception ex)
            {
                return new DataResult("程序异常！" + ex.Message);
            }
        }
    }
}
