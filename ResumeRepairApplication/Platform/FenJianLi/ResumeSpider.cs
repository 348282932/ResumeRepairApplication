using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeRepairApplication.Common;
using ResumeRepairApplication.Models;
using System.Collections.Concurrent;

namespace ResumeRepairApplication.Platform.FenJianLi
{
    /// <summary>
    /// 同步简历信息（1.登录获取Cookie 2.搜索对应简历, 3. 授权， 4.下载简历获取手机号并同步）
    /// </summary>
    public class ResumeSpider : FenJianLiSpider
    {
        /// <summary>
        /// 获取简历ID
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        private string GetResumeId(ResumeSearch data, CookieContainer cookie)
        {
            var keyWord = HttpUtility.UrlEncode(data.University);

            var companyName = HttpUtility.UrlEncode(data.LastCompany);

            if (!string.IsNullOrWhiteSpace(keyWord) || !string.IsNullOrWhiteSpace(companyName))
            {
                 return GetResumeId(keyWord, companyName, data.Name, cookie);
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取匹配到的简历ID
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="companyName"></param>
        /// <param name="name"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        private static string GetResumeId(string keywords, string companyName, string name, CookieContainer cookie, int pageIndex = 0)
        {
            Jumps:

            Thread.Sleep(2500);

            string url = "http://www.fenjianli.com/search/search.htm";

            string param = $"keywords={keywords}&companyName={companyName}&rows=60&sortBy=1&sortType=1&offset={pageIndex * 30}&_random={new Random().NextDouble()}&name={name}";

            string sources = RequestFactory.QueryRequest(url, param, RequestEnum.POST, cookie);

            if (sources.Contains("\"error\"") || string.IsNullOrWhiteSpace(sources)) goto Jumps;

            var jObject = JsonConvert.DeserializeObject(sources) as JObject;

            if ((int)jObject["totalSize"] == 0) return string.Empty;

            var totalSize = Math.Ceiling((double)jObject["totalSize"] / 30);

            var jArray = jObject["list"] as JArray;

            var resume = jArray.FirstOrDefault(f => (string)f["realName"] == name);

            if (resume != null) return $"{(string)resume["id"]}/{(string)resume["name"]}";

            if (pageIndex + 1 < totalSize && pageIndex < 3)
            {
                return GetResumeId(keywords, companyName, name, cookie, ++pageIndex);
            }

            return string.Empty;
        }

        private static ConcurrentQueue<EntityFramework.FenJianLi> queue = new ConcurrentQueue<EntityFramework.FenJianLi>();

        private static readonly object lockObj = new object();

        /// <summary>
        /// 简历搜索
        /// </summary>
        /// <param name="searchData"></param>
        /// <returns></returns>
        public DataResult<string> ResumeRepair(ResumeSearch searchData)
        {
            var dataRsult = new DataResult<string>();

            try
            {
                EntityFramework.FenJianLi user = new EntityFramework.FenJianLi();

                lock (lockObj)
                {
                    while (!queue.TryDequeue(out user))
                    {
                        using (var db = new EntityFramework.ResumeRepairDBEntities())
                        {
                            var users = db.FenJianLi.Where(w => w.IsEnable).ToList();

                            users.ForEach(f => { queue.Enqueue(f); });
                        }
                    }
                }

                var cookie = Login(user.Email, user.PassWord); // 设置 Cookie

                var resumeId = GetResumeId(searchData, cookie);

                if (string.IsNullOrWhiteSpace(resumeId))
                {
                    dataRsult.IsSuccess = false;
                }
                else
                {
                    dataRsult.Data = resumeId;
                }

                return dataRsult;
            }
            catch (Exception ex)
            {
                dataRsult.IsSuccess = false;

                dataRsult.ErrorMsg = $"程序异常，异常原因：{ex.Message}，堆栈信息：{ex.StackTrace}";

                return dataRsult;
            }
            
        }
    }
    //    /// <summary>
    //    /// 初始化
    //    /// </summary>
    //    /// <returns></returns>
    //    public override DataResult Init()
    //    {
    //        var dataResult = new DataResult();

    //        try
    //        {
    //            ResumeRepair();
    //        }
    //        catch (Exception ex)
    //        {
    //            dataResult.IsSuccess = false;

    //            dataResult.ErrorMsg = $"异常信息：{ex.Message}，堆栈信息：{ex.StackTrace}";
    //        }

    //        return dataResult;
    //    }
    //}

    public class DirectoryAllFiles
    {
        List<FileInformation> FileList = new List<FileInformation>();

        public List<FileInformation> GetAllFiles(DirectoryInfo dir)
        {
            FileInfo[] allFile = dir.GetFiles().Where(w => w.Name.Substring(0, 3) != "NO-").ToArray();

            if (allFile.Length > 0)
            {
                foreach (FileInfo fi in allFile)
                {
                    this.FileList.Add(new FileInformation { FileName = fi.Name, FilePath = fi.FullName });
                }

                return this.FileList;
            }

            DirectoryInfo[] allDir = dir.GetDirectories();

            foreach (var d in allDir)
            {
                this.GetAllFiles(d);

                if (this.FileList.Count > 0) return this.FileList;
            }

            return this.FileList;
        }
    }

    public class FileInformation
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }

    public class SearchModel
    {
        public string KeyWord { get; set; }

        public string CompanyName { get; set; }

        public string Name { get; set; }

        public CookieContainer Cookie { get; set; }
    }
}
