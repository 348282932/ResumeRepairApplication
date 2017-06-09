using System;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication.Platform.ZhaoPinGou
{
    /// <summary>
    /// 注册
    /// </summary>
    public class RegisterSpider : ZhaoPinGouSpider
    {
        private static readonly object lockObj = new object();
       
        /// <summary>
        /// 注册
        /// </summary>
        private void Register()
        {
            var proxyId = 0;

            var host = string.Empty;

            var isAddProxy = false;

            lock (lockObj)
            {
                using (var db = new ResumeMatchDBEntities())
                {
                    var data = db.UsingTransaction(() =>
                    {
                        var result = new DataResult();

                        if (!Global.IsEnanbleProxy) return result;

                        var ipArr = db.Proxy.AsNoTracking()
                            .GroupBy(g => g.Host)
                            .Select(s => new { Host = s.Key, Count = s.Count() })
                            .Where(w => w.Count < Global.PlatformCount * Global.PlatformHostCount)
                            .Select(s => s.Host).ToArray();

                        if (ipArr.Length > 0)
                        {
                            var proxy = db.Proxy.FirstOrDefault(f => ipArr.Any(a => a == f.Host) && f.Platform == 4 && !string.IsNullOrEmpty(f.Host) && f.Count < Global.PlatformHostCount);

                            if (proxy != null)
                            {
                                proxyId = proxy.Id;

                                host = proxy.Host;

                                proxy.Count++;

                                var user = db.User.Where(w => w.Host == host && w.Platform == 4).OrderBy(o => o.DownloadNumber).FirstOrDefault();

                                if (user == null)
                                {
                                    LogFactory.Error($"找不到用户，Host = {host}", MessageSubjectEnum.ZhaoPinGou);

                                    result.IsSuccess = false;

                                    return result;
                                }

                                db.SaveChanges();

                                return result;
                            }
                        }

                        var proxyEntity = new Proxy { Count = 1, Platform = 4};

                        db.Proxy.Add(proxyEntity);

                        db.SaveChanges();

                        isAddProxy = true;

                        proxyId = proxyEntity.Id;

                        return result;
                    });

                    if (!data.IsSuccess) return;
                }
            }

            if (Global.IsEnanbleProxy)
            {
                if (string.IsNullOrWhiteSpace(host))
                {
                    host = GetProxy("ZPG_Register",true);
                }
                else
                {
                    GetProxy("ZPG_Register",host);
                }
            }

            var dataResult = Register(host);

            ReleaseProxy("ZPG_Register",host);

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
                        InviteCode = dataResult.Data.InviteCode,
                        CreateTime = DateTime.UtcNow,
                        IsEnable = true,
                        DownloadNumber = 10,
                        Host = host,
                        Platform = 4,
                        Status = 0,
                        IsLocked = false,
                        LockedTime = new DateTime(1900,1,1),
                        RequestNumber = 0
                    });

                    if (proxy != null)
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
        /// <returns></returns>
        [Loggable]
        private static DataResult<dynamic> Register(string host)
        {
            var cookie = new CookieContainer();

            var password = BaseFanctory.GetRandomTel();

            var userName = password + Global.Email.Substring(Global.Email.IndexOf("@", StringComparison.Ordinal));

            RequestFactory.QueryRequest("http://qiye.zhaopingou.com/signup", cookieContainer: cookie, host: host);

            var retryTimes = 2;

            Retry:

            #region 获取验证码

            var loginRequest = (HttpWebRequest)WebRequest.Create("http://qiye.zhaopingou.com/zhaopingou_interface/verification_code");

            loginRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";

            loginRequest.Accept = "image/webp,image/*,*/*;q=0.8"; // 图片类型

            loginRequest.Referer = "http://qiye.zhaopingou.com/signup";

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

            //if (--retryTimes > 0)
            //{
            //    goto Retry;
            //}

            //var sr = new StreamReader("D:\\pic.jpg");

            //FenJianLiScheduling.cnf.pbx_checkNumber.Image = MediaTypeNames.Image.FromStream(sr.BaseStream);

            //string checkCode = Interaction.InputBox("请输入验证码！", "验证码");

            //sr.Close();

            //sr.Dispose();

            Console.WriteLine("请输入验证码：");

            var checkCode = Console.ReadLine();

            #endregion

            var param = $"userName={userName}&password={password}&code={checkCode}&type=2&invitationNumber=&clientNo=&userToken=&clientType=2";

            // 发送注册请求

            var dataResult = RequestFactory.QueryRequest("http://qiye.zhaopingou.com/zhaopingou_interface/register?timestamp=" + BaseFanctory.GetUnixTimestamp(), param, RequestEnum.POST, cookie, "http://qiye.zhaopingou.com/signup", host: host);

            if (!dataResult.IsSuccess) return new DataResult<dynamic>(dataResult.ErrorMsg);

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject == null || (int)jObject["errorCode"] != 1)
            {
                if (--retryTimes > 0)
                {
                    goto Retry;
                }

                throw new RequestException($"注册请求失败！响应数据：{dataResult.Data}");
            }

            var inviteCode = jObject["user"]["invitationCode"].ToString();

            LogFactory.Info($"注册成功！邮箱：{userName}",MessageSubjectEnum.ZhaoPinGou);

            var userToken = jObject["user"]["user_token"].ToString();

            param = $"email={userName}&emailType=1&clientNo=&userToken={userToken}&clientType=2";

            cookie.Add(new Cookie { Name = "zhaopingou_account", Value = userName, Domain = "qiye.zhaopingou.com" });

            cookie.Add(new Cookie { Name = "zhaopingou_login_callback", Value = "http%3A//qiye.zhaopingou.com/resume", Domain = "qiye.zhaopingou.com" });

            var cookieData = cookie.GetCookies(new Uri("http://qiye.zhaopingou.com/zhaopingou_interface"))["fanwenTime1"];

            if (cookieData != null) cookieData.Expires = DateTime.Now.AddDays(-1);

            dataResult = RequestFactory.QueryRequest("http://qiye.zhaopingou.com/zhaopingou_interface/Binding_email?timestamp=" + BaseFanctory.GetUnixTimestamp(), param, RequestEnum.POST, cookie, "http://qiye.zhaopingou.com/user/email/verification", host: host, accept: "multipart/form-data");

            // 发送激活邮件请求

            if (!dataResult.IsSuccess) return new DataResult<dynamic>(dataResult.ErrorMsg);

            jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject == null || (int)jObject["errorCode"] != 1) throw new RequestException($"发送激活邮件请求失败！响应数据：{dataResult.Data}");

            LogFactory.Info($"发送激活邮件成功！邮箱：{userName}", MessageSubjectEnum.ZhaoPinGou);

            return new DataResult<dynamic>(new { Email = userName, Password = password, InviteCode = inviteCode });
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
