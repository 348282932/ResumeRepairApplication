using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeMatchApplication.Common;
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
            var host = string.Empty;

            Repeat:

            var dataResult = RequestFactory.QueryRequest($"{Global.HostZhao}/splider/proxy/GetFree?UserTag=maxlong_{tag}&IsRepeat={!isNewProxy}");

            if (!dataResult.IsSuccess)
            {
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
        public bool GetProxy(string tag, string host)
        {
            var ip = host.Substring(0, host.IndexOf(":", StringComparison.Ordinal));

            var port = host.Substring(host.IndexOf(":", StringComparison.Ordinal) + 1);

            dynamic param = new { UserTag = "maxlong_"+ tag, IP = ip, Port = port };

            Repeat:

            var dataResult = RequestFactory.QueryRequest($"{Global.HostZhao}/splider/proxy/Assigned", JsonConvert.SerializeObject(param),RequestEnum.POST,contentType:ContentTypeEnum.Json.Description());

            if (!dataResult.IsSuccess)
            {
                LogFactory.Warn($"获取 Host：{host} 异常！异常信息：{dataResult.ErrorMsg}");

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
        /// <param name="isAll"></param>
        /// <returns></returns>
        public void ReleaseProxy(string tag, string host, bool isAll = false)
        {
            if(string.IsNullOrWhiteSpace(host)) return;

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
        /// 获取可用的代理列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetEnableProxyList()
        {
            var ipList = new List<string>();

            // TODO:请求API获取可用的IP列表

            return ipList;
        }
    }
}
