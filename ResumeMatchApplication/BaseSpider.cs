using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication
{
    /// <summary>
    /// 请求异常对象
    /// </summary>
    public class RequestException : Exception
    {
        public RequestException(string msg) : base(msg) { }
    }

    public class ProxyException : Exception
    {
        public ProxyException(string msg) : base(msg) { }
        public ProxyException(){ }
    }

    /// <summary>
    /// 基础爬虫
    /// </summary>
    public class BaseSpider
    {
        /// <summary>
        /// 执行逻辑
        /// </summary>
        /// <returns></returns>
        public virtual DataResult Init() { return new DataResult(); }

        /// <summary>
        /// 获取随机代理
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="isNewProxy">是否获取新的</param>
        /// <returns></returns>
        public string GetProxy(string tag, bool isNewProxy = false)
        {
            if (!Global.IsEnanbleProxy) return string.Empty;

            var host = string.Empty;

            Repeat:

            var dataResult = RequestFactory.QueryRequest($"{Global.HostZhao}/splider/proxy/GetFree?UserTag=maxlong_{tag}&IsRepeat={!isNewProxy}");

            if (!dataResult.IsSuccess)
            {
                ReleaseProxy();

                LogFactory.Warn("获取随机IP异常！异常信息：" + dataResult.ErrorMsg);

                Thread.Sleep(TimeSpan.FromSeconds(5));

                goto Repeat;
            }

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                if ((int)jObject["Code"] == 1)
                {
                    host = $"{jObject["Proxy"]["IP"]}:{jObject["Proxy"]["Port"]}";

                    LogFactory.Info($"获取 Host：{host} 成功！");

                    return host;
                }

                ReleaseProxy();

                LogFactory.Warn("获取随机IP异常！异常信息：" + jObject["Message"]);

                Thread.Sleep(TimeSpan.FromSeconds(5));

                goto Repeat;
            }

            return host;
        }

        /// <summary>
        /// 获取特定IP的使用权
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        [Loggable]
        public bool GetProxy(string tag, string host)
        {
            if (!Global.IsEnanbleProxy) return true;

            var ip = host.Substring(0, host.IndexOf(":", StringComparison.Ordinal));

            var port = host.Substring(host.IndexOf(":", StringComparison.Ordinal) + 1);

            dynamic param = new { UserTag = "maxlong_"+ tag, IP = ip, Port = port };

            Repeat:

            var dataResult = RequestFactory.QueryRequest($"{Global.HostZhao}/splider/proxy/Assigned", JsonConvert.SerializeObject(param),RequestEnum.POST,contentType:ContentTypeEnum.Json.Description());

            if (!dataResult.IsSuccess)
            {
                LogFactory.Warn($"获取 Host：{host} 异常！异常信息：{dataResult.ErrorMsg}");

                ReleaseProxy();

                Thread.Sleep(TimeSpan.FromSeconds(5));

                goto Repeat;
            }

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                if ((int)jObject["Code"] == 1)
                {
                    LogFactory.Info($"获取 Host：{host} 成功！");

                    return true;
                }

                LogFactory.Warn($"获取 Host：{host} 异常！异常信息：{jObject["Message"]}");

                ReleaseProxy();

                Thread.Sleep(TimeSpan.FromSeconds(5));

                goto Repeat;
            }

            return false;
        }

        /// <summary>
        /// 释放特定代理
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        [Loggable]
        public void ReleaseProxy(string tag, string host)
        {
            if (!Global.IsEnanbleProxy) return;

            if (string.IsNullOrWhiteSpace(host)) return;

            var ip = host.Substring(0, host.IndexOf(":", StringComparison.Ordinal));

            var port = host.Substring(host.IndexOf(":", StringComparison.Ordinal) + 1);

            dynamic param = new { UserTag = "maxlong_" + tag, IP = ip, Port = port };

            var dataResult = RequestFactory.QueryRequest($"{Global.HostZhao}/splider/proxy/SetFree", JsonConvert.SerializeObject(param), RequestEnum.POST, contentType: ContentTypeEnum.Json.Description());

            if (!dataResult.IsSuccess)
            {
                LogFactory.Warn($"释放 Host：{host} 异常！异常信息：{dataResult.ErrorMsg}");
            }

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                if ((int)jObject["Code"] == 1)
                {
                    LogFactory.Info($"释放 Host：{host} 成功！");

                    return;
                }

                LogFactory.Warn($"释放 Host：{host} 异常！异常信息：{jObject["Message"]}");
            }
        }

        /// <summary>
        /// 释放全部代理
        /// </summary>
        /// <returns></returns>
        public static void ReleaseProxy()
        {
            var dataResult = RequestFactory.QueryRequest($"{Global.HostZhao}/splider/proxy/SetFreeAll?UserTag=maxlong");

            if (!dataResult.IsSuccess)
            {
                LogFactory.Warn($"释放全部 Host 异常！异常信息：{dataResult.ErrorMsg}");
            }

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                if ((int)jObject["Code"] == 1)
                {
                    LogFactory.Info("释放全部 Host 成功！");

                    return;
                }

                LogFactory.Warn($"释放全部 Host 异常！异常信息：{jObject["Message"]}");
            }
        }

        /// <summary>
        /// 获取可用的代理列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetEnableProxyList()
        {
            var ipList = new List<string>();

            // TODO:请求API获取可用的IP列表

            return ipList;
        }

        /// <summary>
        /// 获取用户
        /// </summary>
        /// <param name="userDictionary"></param>
        /// <param name="host"></param>
        /// <param name="isMatch"></param>
        /// <param name="matchPlatform"></param>
        /// <param name="loginFuc"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public static DataResult<User> GetUser(ConcurrentDictionary<User, CookieContainer> userDictionary, string host, bool isMatch, MatchPlatform matchPlatform, Func<string, string, string, DataResult<CookieContainer>> loginFuc, out CookieContainer cookie)
        {
            var dataResult = new DataResult<User>();

            var messageSubjectEnum = EnumFactory<MessageSubjectEnum>.Parse(matchPlatform);

            User user;

            cookie = null;

            var dateTime = DateTime.UtcNow.Date.AddHours(-8);

            using (var db = new ResumeMatchDBEntities())
            {
                if (userDictionary.Keys.All(a => a.Host != host))
                {
                    List<User> users;

                    if (isMatch)
                    {
                        users = db.User.Where(w => w.IsEnable && w.Platform == (short)matchPlatform && w.Status == 1 && w.Host == host).ToList();
                    }
                    else
                    {
                        users = db.User.Where(w => w.IsEnable && w.Platform == (short)matchPlatform && w.Status == 1/* && w.Host == host*/ && (w.LastLoginTime == null || w.DownloadNumber > 0 || w.LastLoginTime < dateTime)).ToList();
                    }

                    if (!users.Any())
                    {
                        dataResult.IsSuccess = false;

                        dataResult.Code = ResultCodeEnum.NoUsers;

                        return dataResult;
                    }

                    foreach (var item in users)
                    {
                        for (var i = 0; i < 5; i++)
                        {
                            if (userDictionary.TryAdd(item, null)) break;

                            

                            if (i == 4) LogFactory.Warn($"向字典中添加用户 {item.Email} 失败！", messageSubjectEnum);
                        }
                    }
                }

                Next:

                if (isMatch)
                {
                    var userQuery = userDictionary.Keys.Where(f => f.IsEnable && f.Host == host);

                    if (!string.IsNullOrWhiteSpace(host)) userQuery = userQuery.Where(w => w.RequestDate == null || w.RequestDate.Value.Date < DateTime.UtcNow.Date || w.RequestDate.Value.Date == DateTime.UtcNow.Date && w.RequestNumber < Global.TodayMaxRequestNumber);

                    user = userQuery.OrderBy(o => o.RequestNumber).FirstOrDefault();
                }
                else
                {
                    user = userDictionary.Keys
                        .Where(f => f.IsEnable /*&& f.Host == host */&& (f.LastLoginTime == null || f.DownloadNumber > 0 || f.LastLoginTime < dateTime))
                        .OrderBy(o => o.Email)
                        .FirstOrDefault();
                }

                if (user == null)
                {
                    dataResult.IsSuccess = false;

                    if (isMatch)
                    {
                        dataResult.Code = ResultCodeEnum.RequestUpperLimit;
                    }
                    else
                    {
                        dataResult.Code = ResultCodeEnum.NoUsers;
                    }

                    LogFactory.Warn(JsonConvert.SerializeObject(userDictionary), messageSubjectEnum);

                    var list = userDictionary.Keys.Where(w => w.Host == host);

                    foreach (var item in list)
                    {
                        for (var i = 0; i < 5; i++)
                        {
                            if (userDictionary.TryRemove(item, out cookie)) break;

                            if (i == 4)
                            {
                                LogFactory.Warn($"从字典中移除用户 {item.Email} 失败！", messageSubjectEnum);

                                dataResult.ErrorMsg += $"向字典中移除用户 {item.Email} 失败！";

                                return dataResult;
                            }
                        }
                    }

                    return dataResult;
                }

                if (isMatch)
                {
                    if (user.RequestDate == null || user.RequestDate.Value.Date < DateTime.UtcNow.Date)
                    {
                        user.RequestDate = DateTime.UtcNow.Date;

                        user.RequestNumber = 0;
                    }

                    user.RequestNumber++;
                }

                for (var i = 0; i < 5; i++)
                {
                    if (userDictionary.TryGetValue(user, out cookie)) break;
                }

                if (cookie == null)
                {
                    var result = loginFuc(user.Email, user.Password, host);

                    if (!result.IsSuccess)
                    {
                        LogFactory.Warn(result.ErrorMsg, messageSubjectEnum);

                        userDictionary.TryRemove(user, out cookie);

                        dataResult.IsSuccess = false;

                        return dataResult;
                    }

                    cookie = result.Data;

                    if (cookie != null)
                    {
                        for (var i = 0; i < 5; i++)
                        {
                            if (userDictionary.TryUpdate(user, cookie, null)) break;
                        }
                    }
                }

                if (cookie == null)
                {
                    goto Next;
                }

                var userEntity = db.User.FirstOrDefault(f => f.Id == user.Id);

                if (userEntity != null)
                {
                    userEntity.RequestDate = user.RequestDate;

                    userEntity.RequestNumber = user.RequestNumber;
                }

                db.TransactionSaveChanges();
            }

            dataResult.Data = user;

            return dataResult;
        }
    }
}
