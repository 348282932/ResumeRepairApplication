using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication.Common
{
    public class RequestFactory
    {
        private const string userAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.31 (KHTML, like Gecko) Chrome/26.0.1410.64 Safari/537.31";

        private const string defaultAccept = "application/json, text/javascript, */*; q=0.01";

        private static readonly string defaultContentType = ContentTypeEnum.Form.Description();

        private static readonly bool isEnanbleProxy = Global.IsEnanbleProxy;

        /// <summary>
        /// Http请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestParams"></param>
        /// <param name="requestType"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="referer"></param>
        /// <param name="contentType"></param>
        /// <param name="accept"></param>
        /// <param name="isNeedSleep"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public static DataResult<string> QueryRequest(string url, string requestParams = "", RequestEnum requestType = RequestEnum.GET, CookieContainer cookieContainer = null, string referer = "", string contentType = "", string accept = "", bool isNeedSleep = true, string host = "")
        {
            var dataResult = new DataResult<string>();

            if (!string.IsNullOrWhiteSpace(host) && isEnanbleProxy)
            {
                if (!WebProxyIsEnable(host))
                {
                    dataResult.IsSuccess = false;

                    dataResult.Code = ResultCodeEnum.ProxyDisable;

                    dataResult.ErrorMsg = host;

                    return dataResult;
                }

                if (isNeedSleep) SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(new Random().Next(2, 3)));
            }

            if(!url.IsInnerIP()) SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(new Random().Next(2, 3)));

            try
            {
                HttpWebRequest httpRequest;

                if (requestType == RequestEnum.POST)
                {
                    httpRequest = (HttpWebRequest)WebRequest.Create(url);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(requestParams))
                    {
                        httpRequest = (HttpWebRequest)WebRequest.Create(url);
                    }
                    else
                    {
                        httpRequest = (HttpWebRequest)WebRequest.Create(url + "?" + requestParams.Trim());
                    }
                }

                httpRequest.Method = requestType.ToString();

                httpRequest.Timeout = 30 * 1000;

                httpRequest.ContentType = string.IsNullOrWhiteSpace(contentType) ? defaultContentType : contentType;

                httpRequest.Accept = string.IsNullOrWhiteSpace(accept) ? defaultAccept : accept;

                httpRequest.Headers.Add("Accept-Encoding", "gzip,deflate,sdch");

                httpRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

                httpRequest.UserAgent = userAgent;

                httpRequest.CookieContainer = cookieContainer;

                if (isEnanbleProxy && !url.IsInnerIP() && !string.IsNullOrEmpty(host))
                {
                    var index = host.IndexOf(":", StringComparison.Ordinal);

                    httpRequest.Proxy = new WebProxy(host.Substring(0, index), Convert.ToInt32(host.Substring(index + 1)));
                }
                if (!string.IsNullOrWhiteSpace(referer)) httpRequest.Referer = referer;

                if (requestType == RequestEnum.POST && !string.IsNullOrWhiteSpace(requestParams))
                {
                    var encoding = Encoding.GetEncoding("utf-8");

                    var bytesToPost = encoding.GetBytes(requestParams);

                    httpRequest.ContentLength = bytesToPost.Length;

                    var requestStream = httpRequest.GetRequestStream();

                    requestStream.Write(bytesToPost, 0, bytesToPost.Length);

                    requestStream.Close();
                }

                var response = (HttpWebResponse)httpRequest.GetResponse();

                var stream = response.GetResponseStream();

                var reStr = string.Empty;

                if (stream != null)
                {
                    var sr = new StreamReader(stream, Encoding.GetEncoding("utf-8"));

                    reStr = sr.ReadToEnd();

                    sr.Close();
                }
                else
                {
                    dataResult.IsSuccess = false;
                }
                
                response.Close();

                dataResult.Data = reStr;

                return dataResult;
            }
            catch (WebException ex)
            {
                LogFactory.Warn($"Web响应异常，请求Url:{url},请求参数:{requestParams}代理:{host},异常信息：{ex.Message}");

                dataResult.IsSuccess = false;

                dataResult.ErrorMsg = $"Web响应异常，请求Url:{url},Host:{host},异常信息：{ex.Message}";

                return dataResult;
            }
            catch(Exception ex)
            {
                LogFactory.Error($"请求异常，请求Url:{url},请求参数:{requestParams},Host:{host},{Environment.NewLine}异常信息：{ex.Message}{Environment.NewLine}{ex.StackTrace}");

                dataResult.IsSuccess = false;

                dataResult.ErrorMsg = $"请求异常，请求Url:{url},Host:{host},异常信息：{ex.Message}，详情请错误看日志！";

                return dataResult;
            }
        }

        /// <summary>
        /// 检查服务器代理IP是否可用
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private static bool WebProxyIsEnable(string host)
        {
            var ip = host.Substring(0, host.IndexOf(":", StringComparison.Ordinal));

            var port = host.Substring(host.IndexOf(":", StringComparison.Ordinal) + 1);

            var dataResult = QueryRequest($"{Global.HostZhao}/splider/proxy/Check?IP={ip}&Port={port}");

            if (dataResult.IsSuccess)
            {
                var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

                if((int)jObject?["Code"] == 1) return true;
            }

            return false;
        }
    }
}

