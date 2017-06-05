using ResumeRepairApplication.EntityFramework;
using ResumeRepairApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using EntityFramework.Extensions;
using ResumeRepairApplication.Common;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using ResumeRepairApplication.Platform.FenJianLi;

namespace ResumeRepairApplication
{
    public class ResumeRepair
    {
        private static bool isStart = true;

        private static bool isFirst = true;

        /// <summary>
        /// 初始化启动
        /// </summary>
        public static void Start()
        {
            isStart = true;
  
            Task.Run(() =>
            {
                while (isStart)
                {
                    if (actionBlock.InputCount != 0)
                    {
                        Thread.Sleep(1000);

                        continue;
                    }

	                List<ResumeSearch> resumeSearch = new List<ResumeSearch>();

					if (isFirst)
                    {
                        using (var db = new ResumeRepairDBEntities())
                        {
                            var resumeQuery = from r in db.ResumeRecord
                                              join m in db.MatchResumeSearch on r.Id equals m.ResumeRecodeId
                                              where r.Status == 1
                                              select m ;
                            if (resumeQuery.Count() > 0)
                            {
                                resumeSearch = resumeQuery.Select(s => new ResumeSearch
                                {
                                    LastCompany = s.LastCompany,
                                    Name = s.Name,
                                    ResumeId = s.ResumeId,
                                    ResumeNumber = s.ResumeNumber,
                                    University = s.University,
                                    UserMasterExtId = s.UserMasterExtId
                                }).ToList();
                            }
                            else
                            {
                                resumeSearch = PullResumes();

                                isFirst = false;
                            }
                        }
                    }
                    else
                    {
                        resumeSearch = PullResumes();
                    }

                    var pullResumes = resumeSearch;

                    if (pullResumes.Count == 0) continue;

                    var filterResumes = FilterExist(pullResumes);

                    var query = from n in filterResumes.AsQueryable()
                                join l in pullResumes.AsQueryable() on new { n.ResumeId, n.ResumeNumber, n.UserMasterExtId } equals new { l.ResumeId, l.ResumeNumber, l.UserMasterExtId }
                                select new ResumeSearch
                                {
                                    LastCompany = l.LastCompany,
                                    Name = l.Name,
                                    ResumeId = n.ResumeId,
                                    ResumeNumber = n.ResumeNumber,
                                    University = l.University,
                                    UserMasterExtId = l.UserMasterExtId
                                };

                    var matchList = query.Distinct().ToList();

                    if (matchList != null)
                    {
                        foreach (var item in matchList)
                        {
                            actionBlock.Post(item);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 停止补全
        /// </summary>
        public static void Stop()
        {
            isStart = false;
        }

        /// <summary>
        /// 拉取没有联系方式的简历
        /// </summary>
        /// <returns></returns>
        private static List<ResumeSearch> PullResumes()
        {
            var resumesList = new List<ResumeSearch>();

            togo:

            var requestStr = RequestFactory.QueryRequest(Global.PullResumesUrl);

            if (string.IsNullOrWhiteSpace(requestStr)) return resumesList;

            var jObject = JsonConvert.DeserializeObject(requestStr) as JObject;

            var jArray = jObject["Resumes"] as JArray;

            if (jArray == null)
            {
                goto togo;
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

                resume.ResumeNumber = ((JArray)(item["Reference"]?["Mapping"]))?[2]["Value"].ToString();

                resume.UserMasterExtId = ((JArray)(item["Reference"]?["Mapping"]))?[1]["Value"].ToString();

                resume.University = item["Educations"]?[0]?["School"].ToString();

                resumesList.Add(resume);
            }

            return resumesList;
        }

        /// <summary>
        /// 工作任务
        /// </summary>
        /// <param name="i"></param>
        private static void Work(ResumeSearch i)
        {
            var dataResult = resumeSpider.ResumeRepair(i); // 纷简历平台

            using (var db = new ResumeRepairDBEntities())
            {
                if (dataResult.IsSuccess)
                {
                    ContactInformationSpider.actionBlock.Post(dataResult.Data + "-" + i.ResumeNumber.Substring(0, 10));

                    var resume = db.ResumeRecord.FirstOrDefault(w => w.ResumePlatform == 1 && w.ResumeId == i.ResumeId);

                    if (resume != null)
                    {
                        resume.Status = (int)ResumeRecordStatus.MatchSuccess;

                        resume.MatchPlatform = 1;

                        resume.MatchResumeId = dataResult.Data + "-" + i.ResumeNumber.Substring(0, 10);

                        resume.MatchTime = DateTime.UtcNow;

                        FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.tbx_ResumeRepair, $"匹配成功！平台：纷简历，简历ID:{i.ResumeId}");

                        Interlocked.Add(ref Global.TotalMatchSuccess, 1);
                    }
                    else
                    {
                        FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.system_tbx_Exception, $"记录简历匹配结果失败！简历ID:{i.ResumeId}，结果：匹配成功！");
                    }
                }
                else
                {
                    var resume = db.ResumeRecord.FirstOrDefault(w => w.ResumePlatform == 1 && w.ResumeId == i.ResumeId);

                    if (resume != null)
                    {
                        resume.Status = (int)ResumeRecordStatus.MatchFailure;

                        resume.MatchTime = DateTime.UtcNow;

                        FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.tbx_ResumeRepair, $"匹配失败！平台：纷简历，简历ID:{i.ResumeId}");
                    }
                    else
                    {
                        FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.system_tbx_Exception, $"记录简历匹配结果失败！简历ID:{i.ResumeId}，结果：匹配失败！");
                    }

                    var requestParam = new List<ResumeMatchResult>
                    {
                        new ResumeMatchResult
                        {
                            ResumeNumber = i.ResumeNumber.Substring(0,10),
                            Status = 3
                        }
                    };

                    var data = RequestFactory.QueryRequest(Global.PostResumesUrl, JsonConvert.SerializeObject(requestParam), RequestEnum.POST, contentType: "application/json");

                    resume.PostBackStatus = data.Contains("成功") ? (short)1 : (short)2;

                }

                db.SaveChanges();
            }
        }

        /// <summary>
        /// 流水线
        /// </summary>
        public static ActionBlock<ResumeSearch> actionBlock = new ActionBlock<ResumeSearch>(i => { Work(i); }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 6 });

        /// <summary>
        /// 过滤器
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static List<FilterResult> FilterExist(List<ResumeSearch> list)
        {
            var filterResult = new List<FilterResult>();

            filterResult.AddRange(list.Select(s => new FilterResult
            {
                ResumeId = s.ResumeId,
                ResumeNumber = s.ResumeNumber,
                UserMasterExtId = s.UserMasterExtId
            }));

            if (!isFirst)
            {
                using (var db = new ResumeRepairDBEntities())
                {
                    var filterResultArr = filterResult.Select(s => s.ResumeId).ToArray();

                    var resumeIdArr = db.ResumeRecord.Where(w => filterResultArr.Any(a => w.ResumeId == a)).Select(s => s.ResumeId).ToArray();

                    filterResult.RemoveAll(r => resumeIdArr.Any(a => r.ResumeId == a));

                    var resumeList = filterResult.Select(s => new ResumeRecord
                    {
                        LibraryExist = 2,
                        ResumePlatform = 1,
                        ResumeId = s.ResumeId,
                        Status = (short)ResumeRecordStatus.WaitMatch,
                        PostBackStatus = 0
                    }).ToList();

                    db.ResumeRecord.AddRange(resumeList);

                    db.SaveChanges();

                    var query = from n in resumeList.AsQueryable()
                                join l in list.AsQueryable() on new { n.ResumeId } equals new { l.ResumeId }
                                select new MatchResumeSearch
                                {
                                    LastCompany = l.LastCompany,
                                    Name = l.Name,
                                    ResumeId = n.ResumeId,
                                    ResumeNumber = l.ResumeNumber,
                                    University = l.University,
                                    UserMasterExtId = l.UserMasterExtId,
                                    ResumeRecodeId = n.Id
                                };

                    db.MatchResumeSearch.AddRange(query);

                    db.SaveChanges();
                }
            }

            isFirst = false;

            To:

            var result = RequestFactory.QueryRequest(Global.FilterAuthUrl, "{\"Username\": \"longzhijie\",\"Password\": \"NuRQe6kC\"}", RequestEnum.POST, contentType: EnumFactory.Codes(ContentTypeEnum.Json));

            if (!string.IsNullOrWhiteSpace(result))
            {
                var jObject = JsonConvert.DeserializeObject(result) as JObject;

                if ((int)jObject["Code"] == 0)
                {
                    dynamic param = new
                    {
                        Username = "longzhijie",
                        Signature = (string)jObject["Signature"],
                        ResumeSummaries = new List<object>()
                    };

                    param.ResumeSummaries.AddRange(list.Select(s => new { ResumeNumber = s.ResumeNumber, UserMasterExtendId = s.UserMasterExtId, ResumeId = s.ResumeId }).ToList());

                    result = RequestFactory.QueryRequest(Global.FilterUrl, JsonConvert.SerializeObject(param), RequestEnum.POST, contentType: EnumFactory.Codes(ContentTypeEnum.Json));

                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        jObject = JsonConvert.DeserializeObject(result) as JObject;

                        if ((int)jObject["Code"] == 3) goto To;

                        if ((int)jObject["Code"] == 0)
                        {
                            using (var db = new ResumeRepairDBEntities())
                            {
                                var jArray = jObject["ResumeSummaries"] as JArray;

                                var isSuccessBack = true;

                                if (jArray.Count > 0)
                                {
                                    Global.TotalMatchSuccess += jArray.Count;

                                    Global.TotalDownload += jArray.Count;

                                    var matchedResult = jArray.Select(s => new ResumeMatchResult
                                    {
                                        Cellphone = (string)s["Cellphone"],
                                        Email = (string)s["Email"],
                                        ResumeNumber = ((string)s["ResumeNumber"]).Substring(0, 10),
                                        Status = 2
                                    }).ToList();

                                    var data = RequestFactory.QueryRequest(Global.PostResumesUrl, JsonConvert.SerializeObject(matchedResult), RequestEnum.POST, contentType: "application/json");

                                    if (!data.Contains("成功")) isSuccessBack = false;
                                }

                                foreach (var item in jArray)
                                {
                                    var resumeId = (string)item["ResumeId"];

                                    var resume = db.ResumeRecord.FirstOrDefault(f => f.ResumeId == resumeId);

                                    filterResult.RemoveAll(f => f.ResumeNumber == (string)item["ResumeNumber"] && f.UserMasterExtId == (string)item["UserMasterExtendId"]);

                                    if (resume != null)
                                    {
                                        resume.LibraryExist = 1;
                                        resume.Status = (short)ResumeRecordStatus.DownLoadSuccess;
                                        resume.DownLoadTime = DateTime.UtcNow;
                                        resume.Cellphone = (string)item["Cellphone"];
                                        resume.Email = (string)item["Email"];
                                        resume.PostBackStatus = isSuccessBack ? (short)1 : (short)2;
                                    }
                                }

                                db.SaveChanges();
                            }
                        }
                    }
                }
            }

            return filterResult;
        }

        private static ResumeSpider resumeSpider = new ResumeSpider();

        private static List<ResumeMatchResult> resumeMatchResults = new List<ResumeMatchResult>();
    }
}
