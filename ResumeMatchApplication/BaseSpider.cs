using System;
using System.Collections.Generic;
using System.Net;
using ResumeMatchApplication.Common;

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
        /// <param name="isNewProxy">是否获取新的</param>
        /// <returns></returns>
        public string GetProxy(bool isNewProxy = false)
        {
            var host = string.Empty;

            // TODO:请求API获取代理

            host = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString();

            return host;
        }

        /// <summary>
        /// 获取特定IP的使用权
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public bool GetProxy(string host)
        {
            var isSuccess = false;

            // TODO:请求AIP获取特定的IP的使用权

            return isSuccess;
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
