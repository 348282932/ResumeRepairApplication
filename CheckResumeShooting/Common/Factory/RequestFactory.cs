using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CheckResumeShooting.Common.Factory
{
    public class RequestFactory
    {
        private const string userAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.31 (KHTML, like Gecko) Chrome/26.0.1410.64 Safari/537.31";
        private const string contentType = "application/x-www-form-urlencoded";
        private static bool isEnanbleProxy = false;
        /// <summary>
        /// http 请求
        /// </summary>
        /// <param name="hostUrl"></param>
        /// <param name="action"></param>
        /// <param name="requestParams"></param>
        /// <param name="requestType"></param>
        /// <returns></returns>
        public static string QueryRequest(string hostUrl, string action, string requestParams = "", RequestEnum requestType = RequestEnum.GET, CookieContainer cookieContainer = null, string referer = "", string contentType = contentType)
        {
            return QueryRequest((hostUrl + action), requestParams, requestType, cookieContainer, referer, contentType);
        }

        /// <summary>
        /// http 请求
        /// </summary>
        /// <param name="hostUrl"></param>
        /// <param name="action"></param>
        /// <param name="requestParams"></param>
        /// <param name="requestType"></param>
        /// <returns></returns>
        public static string QueryRequest(string url, string requestParams = "", RequestEnum requestType = RequestEnum.GET, CookieContainer cookieContainer = null, string referer = "", string contentType = contentType)
        {
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
                        httpRequest = (HttpWebRequest)WebRequest.Create(url);
                    else
                        httpRequest = (HttpWebRequest)WebRequest.Create(url + "?" + requestParams.Trim());
                }


                httpRequest.Method = requestType.ToString();
                httpRequest.Timeout = 200000;
                httpRequest.ContentType = contentType;
                httpRequest.Accept = "application/json, text/javascript, */*; q=0.01";
                //httpRequest.Accept = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";
                httpRequest.Headers.Add("Accept-Encoding", "gzip,deflate,sdch");
                httpRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                httpRequest.UserAgent = userAgent;
                httpRequest.CookieContainer = cookieContainer;
                if (isEnanbleProxy) httpRequest.Proxy = NextProxy();
                if (!string.IsNullOrWhiteSpace(referer)) httpRequest.Referer = referer;

                if (requestType == RequestEnum.POST)
                {
                    Encoding encoding = Encoding.GetEncoding("utf-8");
                    byte[] bytesToPost = encoding.GetBytes(requestParams);

                    httpRequest.ContentLength = bytesToPost.Length;
                    Stream requestStream = httpRequest.GetRequestStream();
                    requestStream.Write(bytesToPost, 0, bytesToPost.Length);
                    requestStream.Close();
                }

                HttpWebResponse response = (HttpWebResponse)httpRequest.GetResponse();

                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8"));
                string reStr = sr.ReadToEnd();
                sr.Close();
                response.Close();
                return reStr;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// http 请求（异步）
        /// </summary>
        /// <param name="hostUrl"></param>
        /// <param name="action"></param>
        /// <param name="requestParams"></param>
        /// <param name="requestType"></param>
        /// <returns></returns>
        public static async Task<string> QueryRequestAsync(string url, string requestParams = "", RequestEnum requestType = RequestEnum.GET, CookieContainer cookieContainer = null)
        {
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
                        httpRequest = (HttpWebRequest)WebRequest.Create(url);
                    else
                        httpRequest = (HttpWebRequest)WebRequest.Create(url + "?" + requestParams.Trim());
                }


                httpRequest.Method = requestType.ToString();
                httpRequest.Timeout = 200000;
                httpRequest.ContentType = "application/x-www-form-urlencoded";
                httpRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                httpRequest.Headers.Add("Accept-Encoding", "gzip,deflate,sdch");
                httpRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                httpRequest.UserAgent = userAgent;
                httpRequest.CookieContainer = cookieContainer;
                if(isEnanbleProxy)httpRequest.Proxy = NextProxy();

                if (requestType == RequestEnum.POST)
                {
                    Encoding encoding = Encoding.GetEncoding("utf-8");
                    byte[] bytesToPost = encoding.GetBytes(requestParams);

                    httpRequest.ContentLength = bytesToPost.Length;
                    Stream requestStream = httpRequest.GetRequestStream();
                    requestStream.Write(bytesToPost, 0, bytesToPost.Length);
                    requestStream.Close();
                }

                var result = await httpRequest.GetResponseAsync();

                HttpWebResponse response = (HttpWebResponse)result;

                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8"));
                string reStr = sr.ReadToEnd();
                sr.Close();
                response.Close();
                return reStr;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// http 请求，并将请求结果序列化为指定实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hostUrl"></param>
        /// <param name="action"></param>
        /// <param name="requestParams"></param>
        /// <param name="requestType"></param>
        /// <returns></returns>
        public static T RequestToEntity<T>(string hostUrl, string action, string requestParams, RequestEnum requestType) where T : class
        {
            return JsonConvert.DeserializeObject<T>(QueryRequest(hostUrl, action, requestParams, requestType));
        }

        /// <summary>
        /// 直接下载对应的URL页面并读取其内容
        /// </summary>
        /// <param name="url"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string GetHtml(string url, Encoding encoding)
        {
            byte[] buf = new WebClient().DownloadData(url);
            if (encoding != null) return encoding.GetString(buf);
            string html = Encoding.UTF8.GetString(buf);
            encoding = GetEncoding(html);
            if (encoding == null || encoding == Encoding.UTF8) return html;
            return encoding.GetString(buf);
        }

        /// <summary>
        /// 判断读取下载页面的文本编码
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static Encoding GetEncoding(string html)
        {
            string pattern = @"(?i)\bcharset=(?<charset>[-a-zA-Z_0-9]+)";
            string charset = Regex.Match(html, pattern).Groups["charset"].Value;
            try { return Encoding.GetEncoding(charset); }
            catch (ArgumentException) { return null; }
        }

        private static Proxy proxyData = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private static WebProxy NextProxy()
        {
            if (proxyData == null || (proxyData != null && proxyData.ExpirationTime < DateTime.Now))
            {
                var prd =  ProxyResponseData.GetProxy();

                if (prd.Success)
                {
                    proxyData = prd.Proxy;

                    return new WebProxy(prd.Proxy.IP, Convert.ToInt32(prd.Proxy.Port));
                }
            }

            return new WebProxy(proxyData.IP, Convert.ToInt32(proxyData.Port));
        }
    }

    public enum RequestEnum
    {
        POST, GET
    }

    #region 配置代理

    public class ProxyResponseData
    {
        /// <summary>
        /// 
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public short Code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Proxy Proxy { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static ProxyResponseData GetProxy()
        {
            HttpResponseMessage response;

            HttpClient client = new HttpClient();

            try
            {
                response = client.SendAsync(new HttpRequestMessage
                {
                    Headers =
                    {
                        { "Accept", "application/json, text/javascript, */*; q=0.01" },
                        { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36" }
                    },
                    RequestUri = new Uri("http://www.xdaili.cn/ipagent//freeip/getFreeIps?page=1&rows=10")
                }).Result;
            }
            catch (Exception ex)
            {
                return new ProxyResponseData
                {
                    Code = 0x0001,
                    Message = $"Exception was happened when get the HttpResponseMessage from HttpRequest. Exception message:{ex.Message}"
                };
            }

            dynamic message;

            try
            {
                var encoding = response.Content.Headers.ContentType.CharSet;

                if (string.IsNullOrEmpty(encoding))
                {
                    encoding = "utf-8";
                }

                using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding(encoding)))
                {
                    message = JsonConvert.DeserializeObject<dynamic>(sr.ReadToEnd());
                }
            }
            catch (Exception ex)
            {
                return new ProxyResponseData
                {
                    Code = 0x0010,
                    Message = $"Exception was happened when get the content from HttpResponseMessage. Exception message:{ex.Message}"
                };
            }

            try
            {
                var proxy = new Proxy
                {
                    ResponseTime = double.MaxValue
                };

                foreach (var item in message.rows)
                {
                    if (proxy.ResponseTime <= double.Parse(item.responsetime.ToString()))
                    {
                        continue;
                    }

                    proxy.IP = item.ip.ToString();

                    proxy.Port = item.port.ToString();
                }

                return new ProxyResponseData
                {
                    Success = true,
                    Message = "Get proxy success.",
                    Proxy = proxy
                };
            }
            catch (Exception ex)
            {
                return new ProxyResponseData
                {
                    Code = 0x0100,
                    Message = $"Exception was happened when get the content from message. Exception message:{ex.Message}"
                };
            }
        }
    }
    /// <summary>
	/// 代理类
	/// </summary>
	public class Proxy
    {
        /// <summary>
        /// 
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Port { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double ResponseTime { get; set; } = double.MaxValue;

        /// <summary>
        /// 
        /// </summary>
        public DateTime ExpirationTime { get; } = DateTime.Now.AddSeconds(30);
    }

    #endregion
}

