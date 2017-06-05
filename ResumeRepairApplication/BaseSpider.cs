using ResumeRepairApplication.Common;
using System;
using System.Net;

namespace ResumeRepairApplication
{
    /// <summary>
    /// 请求异常对象
    /// </summary>
    public class RequestException : Exception
    {
        public RequestException(string msg) : base(msg) { }
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
    }
}
