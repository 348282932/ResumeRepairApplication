using System;
using System.Collections.Concurrent;
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

        private static readonly ConcurrentQueue<List<ResumeSearch>> resumeQueue = new ConcurrentQueue<List<ResumeSearch>>();

        private static bool isFirst = true;
        
        public static List<ResumeSearch> PullResumes()
        {
            List<ResumeSearch> list;

            lock (lockObj)
            {
                while (resumeQueue.IsEmpty || !resumeQueue.TryDequeue(out list))
                {
                    if (resumeQueue.IsEmpty)
                    {
                        List<ResumeSearch> resumeList;

                        if (isFirst)
                        {
                            var time = DateTime.UtcNow.AddHours(-1);

                            using (var db = new ResumeMatchDBEntities())
                            {
                                resumeList = db.ResumeComplete.Where(w => w.Status == 1 && (!w.IsLocked || w.IsLocked && w.LockedTime < time)).Select(s => new ResumeSearch
                                {
                                    Degree = s.Degree,
                                    Gender = s.Gender,
                                    Introduction = s.Introduction,
                                    LastCompany = s.LastCompany,
                                    Name = s.Name,
                                    ResumeId = s.ResumeId,
                                    ResumeNumber = s.ResumeNumber,
                                    University = s.University,
                                    UserMasterExtId = s.UserMasterExtId
                                }).ToList();
                            }

                            isFirst = false;
                        }
                        else
                        {
                            resumeList = PullAllResumes();
                        }

                        const int taskCount = 10;

                        for (var i = 0; i < resumeList.Count + taskCount; i += taskCount)
                        {
                            var temp = resumeList.Skip(i).Take(taskCount).ToList();

                            if(temp.Any()) resumeQueue.Enqueue(temp);
                        }
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// 拉取没有联系方式的简历
        /// </summary>
        /// <returns></returns>
        [Loggable]
        private static List<ResumeSearch> PullAllResumes()
        {
            var list = PullResumesByZeLin();

            if (list != null) return list;

            var resumesList = new List<ResumeSearch>();

            Retry:

            var dataResult = RequestFactory.QueryRequest(Global.HostZhao + "/splider/Resume/GetResumeWithNoDeal?rowcount=100");

            if (!dataResult.IsSuccess || string.IsNullOrWhiteSpace(dataResult.Data)) goto Retry;

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

            if (resumesList.Count == 0) goto Retry;

            using (var db = new ResumeMatchDBEntities())
            {
                var resumeIdArr = resumesList.Select(s => s.ResumeId).ToArray();

                var resumes = db.ResumeComplete.Where(w => resumeIdArr.Any(a => a == w.ResumeId)).ToList();

                var removeArr = resumes.Select(s => s.ResumeId).ToArray();

                if (removeArr.Any()) LogFactory.Warn("获取到重复简历！ResumeID：" + string.Join("，", removeArr));

                var idArr = resumes.Where(a => a.Status == 6 || a.Status == 2).Select(s => s.ResumeId).ToArray();

                if (idArr.Length > 0)
                {
                    LogFactory.Warn("过滤掉已有联系方式的重复简历！" + string.Join("，", idArr));

                    resumes.RemoveAll(r => idArr.Any(a => a == r.ResumeId));

                    resumesList.RemoveAll(r => idArr.Any(a => a == r.ResumeId));
                }

                db.ResumeComplete.RemoveRange(resumes);

                db.TransactionSaveChanges();

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
                    Degree = s.Degree,
                    Weights = 0
                }));

                db.TransactionSaveChanges();
            }

            return FilterExist(resumesList);
        }

        private static List<ResumeSearch> PullResumesByZeLin()
        {
            var resumesList = new List<ResumeSearch>();

            Retry:

            var dataResult = RequestFactory.QueryRequest(Global.HostZhao + "/splider/Resume/GetResumeWithNoDeal_ZL?rowcount=100");

            if (!dataResult.IsSuccess || string.IsNullOrWhiteSpace(dataResult.Data)) goto Retry;

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                var jArray = jObject["Resumes"] as JArray;

                if (jArray == null || jArray.Count == 0)
                {
                    return null;
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

            if (resumesList.Count == 0) return null;

            using (var db = new ResumeMatchDBEntities())
            {
                var resumeIdArr = resumesList.Select(s => s.ResumeId).ToArray();

                var resumes = db.ResumeComplete.Where(w => resumeIdArr.Any(a => a == w.ResumeId)).ToList();

                var removeArr = resumes.Select(s => s.ResumeId).ToArray();

                if(removeArr.Any()) LogFactory.Warn("获取到重复简历！ResumeID：" + string.Join("，", removeArr));

                var idArr = resumes.Where(a => a.Status == 6 || a.Status == 2).Select(s => s.ResumeId).ToArray();

                if (idArr.Length > 0)
                {
                    LogFactory.Warn("过滤掉已匹配到的重复简历！ResumeID：" + string.Join("，", idArr));

                    resumes.RemoveAll(r => idArr.Any(a => a == r.ResumeId));

                    resumesList.RemoveAll(r => idArr.Any(a => a == r.ResumeId));
                }

                db.ResumeComplete.RemoveRange(resumes);

                db.TransactionSaveChanges();

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
                    Degree = s.Degree,
                    Weights = 1
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

                foreach (var resume in resumeList)
                {
                    resume.Status = 1;

                    resume.LibraryExist = 3;
                }

                if (string.IsNullOrWhiteSpace(signature))
                {
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