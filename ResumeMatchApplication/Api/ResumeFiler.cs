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
    public class ResumeFiler: ApiBase
    {
        private static readonly object lockObj = new object();

        public static List<ResumeComplete> ZhaoPinGou(List<ResumeComplete> list)
        {
            Login:

            lock (lockObj)
            {
                if (string.IsNullOrWhiteSpace(signature))
                {
                    signature = Login();
                }
            }

            dynamic param = new
            {
                Username = Global.UserName,
                Signature = signature,
                ResumeSummaries = new List<object>()
            };

            param.ResumeSummaries.AddRange(list.Select(s => new { ResumeId = s.MatchResumeId, s.ResumeNumber }));

            for (var i = 0; i < 3; i++)
            {
                var dataResult = RequestFactory.QueryRequest(Global.HostChen + "/api/zhaopingou/query", JsonConvert.SerializeObject(param), RequestEnum.POST, contentType: ContentTypeEnum.Json.Description());

                using (var db = new ResumeMatchDBEntities())
                {
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

                                    LogFactory.Info($"简历库匹配成功！ResumeId：{resume.ResumeId}",MessageSubjectEnum.ZhaoPinGou);
                                }

                                list.RemoveAll(r => arr.Any(a => a == r.ResumeNumber));
                            }

                            var resumesArr = list.Select(s => s.ResumeNumber).ToArray();

                            var resumeItems = db.ResumeComplete.Where(w => resumesArr.Any(a => a == w.ResumeNumber)).ToList();

                            foreach (var resume in resumeItems)
                            {
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