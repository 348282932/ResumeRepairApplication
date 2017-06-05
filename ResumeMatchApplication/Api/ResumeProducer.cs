using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication.Api
{
    public class ResumeProducer : ApiBase
    {
        private static readonly object lockObj = new object();

        /// <summary>
        /// 拉取没有联系方式的简历
        /// </summary>
        /// <returns></returns>
        public static List<ResumeSearch> PullResumes()
        {
            var resumesList = new List<ResumeSearch>();

            Retry:

            var dataResult = RequestFactory.QueryRequest(Global.HostZhao + "/splider/Resume/GetResumeWithNoDeal?rowcount=1");

            if (!dataResult.IsSuccess || string.IsNullOrWhiteSpace(dataResult.Data))
            {
                LogFactory.Error("获取简历 Api 调用异常,响应信息：" + dataResult.ErrorMsg, MessageSubjectEnum.API);

                goto Retry;
            }

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                var jArray = jObject["Resumes"] as JArray;

                if (jArray == null)
                {
                    goto Retry;
                }

                Global.TotalMatch += jArray.Count;

                foreach (var item in jArray)
                {
                    var resume = new ResumeSearch();

                    resume.Name = item["Name"].ToString();

                    var worksArr = (JArray)item["Works"];

                    resume.LastCompany = worksArr.Count > 0 ? worksArr[0]["Company"].ToString() : "";

                    var educationsArr = (JArray)item["Educations"];

                    resume.University = educationsArr.Count > 0 ? educationsArr[0]["School"].ToString() : "";

                    resume.ResumeId = item["Reference"]?["Id"].ToString();

                    resume.ResumeNumber = ((JArray)item["Reference"]?["Mapping"])?[2]["Value"].ToString();

                    resume.UserMasterExtId = ((JArray)item["Reference"]?["Mapping"])?[1]["Value"].ToString();

                    resume.University = item["Educations"]?[0]?["School"].ToString();

                    resume.Gender = item["Gender"].ToString() == "男" ? (short)0 : item["Gender"].ToString() == "女" ? (short)1 : (short)-1;

                    resume.Degree = item["Degree"].ToString();

                    resume.Introduction = item["Intention"]?["Evaluation"].ToString();

                    resumesList.Add(resume);
                }
            }

            using (var db = new ResumeMatchDBEntities())
            {
                db.ResumeComplete.AddRange(resumesList.Select(s => new ResumeComplete
                {
                    CreateTime = DateTime.UtcNow,
                    Gender = s.Gender,
                    Introduction = s.Introduction,
                    LastCompany = s.LastCompany,
                    LibraryExist = 0,
                    Name = s.Name,
                    PostBackStatus = 0,
                    ResumeId = s.ResumeId,
                    ResumeNumber = s.ResumeNumber,
                    ResumePlatform = 1,
                    Status = 0,
                    University = s.University,
                    UserMasterExtId = s.UserMasterExtId,
                    Degree = s.Degree
                }));

                db.TransactionSaveChanges();
            }

            return FilterExist(resumesList);
        }

        /// <summary>
        /// 过滤器
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static List<ResumeSearch> FilterExist(List<ResumeSearch> list)
        {
            Login:

            lock (lockObj)
            {
                if (string.IsNullOrWhiteSpace(signature))
                {
                    signature = Login();
                }
            }

            using (var db = new ResumeMatchDBEntities())
            {
                var resumeIdArr = list.Select(s => s.ResumeId).ToArray();

                var resumeList = db.ResumeComplete.Where(w => resumeIdArr.Any(a => a == w.ResumeId)).ToList();

                if (string.IsNullOrWhiteSpace(signature))
                {
                    foreach (var resume in resumeList)
                    {
                        resume.Status = 1;

                        resume.LibraryExist = 3;
                    }

                    db.TransactionSaveChanges();

                    LogFactory.Warn("简历过滤 API 登录异常！跳过过滤！",MessageSubjectEnum.API);

                    return list;
                }

                dynamic param = new
                {
                    Username = Global.UserName,
                    Signature = signature,
                    ResumeSummaries = new List<object>()
                };

                param.ResumeSummaries.AddRange(list.Select(s => new { s.ResumeNumber, s.ResumeId, UserMasterExtendId = s.UserMasterExtId }).ToList());

                for (var i = 0; i < 3; i++)
                {
                    var dataResult = RequestFactory.QueryRequest(Global.HostChen + "/api/queryresume/query", JsonConvert.SerializeObject(param), RequestEnum.POST, contentType: ContentTypeEnum.Json.Description());

                    if (dataResult.IsSuccess)
                    {
                        var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

                        if (jObject == null) continue;

                        if ((int)jObject["Code"] == 3)
                        {
                            signature = string.Empty;

                            goto Login;
                        }

                        if ((int)jObject["Code"] == 0)
                        {
                            var jArray = jObject["ResumeSummaries"] as JArray;

                            if (jArray == null) continue;

                            if (jArray.Count > 0)
                            {
                                Global.TotalMatchSuccess += jArray.Count;

                                Global.TotalDownload += jArray.Count;

                                var matchedResult = jArray.Select(s => new ResumeMatchResult
                                {
                                    Cellphone = (string)s["Cellphone"],
                                    Email = (string)s["Email"],
                                    ResumeNumber = (string)s["ResumeNumber"],
                                    Status = 2
                                }).ToList();

                                var isPostSuccess = PostResumes(matchedResult);

                                var arr = matchedResult.Select(s => s.ResumeNumber).ToArray();

                                var resumes = db.ResumeComplete.Where(w => arr.Any(a => a.Equals(w.ResumeNumber))).ToList();

                                foreach (var resume in resumes)
                                {
                                    resume.Status = 6;
                                    resume.Cellphone = matchedResult.FirstOrDefault(f => f.ResumeNumber == resume.ResumeNumber)?.Cellphone;
                                    resume.Email = matchedResult.FirstOrDefault(f => f.ResumeNumber == resume.ResumeNumber)?.Email;
                                    resume.DownloadTime = DateTime.UtcNow;
                                    resume.LibraryExist = 1;
                                    resume.PostBackStatus = isPostSuccess ? (short)1 : (short)2;
                                }

                                list.RemoveAll(r => arr.Any(a => a == r.ResumeNumber));
                            }

                            var resumesArr = list.Select(s => s.ResumeNumber).ToArray();

                            var resumeItems = db.ResumeComplete.Where(w => resumesArr.Any(a => a == w.ResumeNumber)).ToList();

                            foreach (var resume in resumeItems)
                            {
                                resume.Status = 1;
                                resume.LibraryExist = 2;
                            }

                            db.TransactionSaveChanges();

                            break;
                        }

                        LogFactory.Warn("简历过滤 API 筛选简历异常！异常信息：" + jObject["Message"], MessageSubjectEnum.API);
                    }
                }
            }

            return list;
        }
    }
}