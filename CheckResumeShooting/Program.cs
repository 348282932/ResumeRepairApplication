using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CheckResumeShooting.Common.Factory;
using CheckResumeShooting.EntityFramework.PostgreDB;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JiebaNet.Segmenter;
using Npgsql;
using System.Data;

namespace CheckResumeShooting
{
	class Program
	{
        [STAThread]
		static void Main(string[] args)
		{
            FilterExist();
        }

        private static void FilterExist()
        {
            int total = 0;

            int filter = 0;

            int success = 0;

            using (var db = new ResumeRepairDBEntities())
            {
                var resumeQuery = from r in db.ResumeRecord.AsNoTracking()
                                  join m in db.MatchResumeSearch.AsNoTracking() on r.ResumeId equals m.ResumeId
                                  where r.Status == 2 orderby r.Id
                                  select new { r, m };
                
                var count = 8526;

               // var count = db.Database.SqlQuery<int>("SELECT count(*) FROM \"ResumeRecord\" AS R INNER JOIN  \"MatchResumeSearch\" AS M ON R.\"ResumeId\" = M.\"ResumeId\"  WHERE r.\"Status\" = 3");

                total = count;

                To:
                
                var result = RequestFactory.QueryRequest("http://192.168.1.100:15286/api/queryresume/login", "{\"Username\": \"longzhijie\",\"Password\": \"NuRQe6kC\"}", RequestEnum.POST, contentType: "application/json");

                if (!string.IsNullOrWhiteSpace(result))
                {
                    var jObject = JsonConvert.DeserializeObject(result) as JObject;

                    if ((int)jObject["Code"] == 3) goto To;

                    if ((int)jObject["Code"] == 0)
                    {
                        var signature = (string)jObject["Signature"];

                        var index = 0;

                        NpgsqlConnection conn = new NpgsqlConnection("Server=localhost;Database=ResumeRepairDB;Uid=postgres;Pwd=a123456;");

                        conn.Open();

                        while (true)
                        {
                            if (index > 8526) break;

                            dynamic param = new
                            {
                                Username = "longzhijie",
                                Signature = signature,
                                ResumeSummaries = new List<object>()
                            };

                            NpgsqlDataAdapter ndap = new NpgsqlDataAdapter($"SELECT M.* FROM \"ResumeRecord\" AS R INNER JOIN \"MatchResumeSearch\" AS M ON R.\"ResumeId\" = M.\"ResumeId\" WHERE r.\"Status\" = 2 LIMIT 20 OFFSET {index}", conn);

                            DataSet ds = new DataSet();

                            ndap.Fill(ds);

                            var tab = ds.Tables[0];

                            //var list = resumeQuery.Skip(index).Take(20).ToList();

                            foreach (DataRow item in tab.Rows)
                            {
                                param.ResumeSummaries.Add(new { ResumeNumber = (string)item["ResumeNumber"], UserMasterExtendId = (string)item["UserMasterExtId"], ResumeId = (string)item["ResumeId"] });
                            }

                            index += 20;

                            //if (list == null || list.Count == 0) break;

                            filter += 20;

                            //param.ResumeSummaries.AddRange(list.Select(s => new { ResumeNumber = s.m.ResumeNumber, UserMasterExtendId = s.m.UserMasterExtId, ResumeId = s.m.ResumeId }).ToList());

                            result = RequestFactory.QueryRequest("http://192.168.1.100:15286/api/queryresume/query", JsonConvert.SerializeObject(param), RequestEnum.POST, contentType: "application/json");

                            if (!string.IsNullOrWhiteSpace(result))
                            {
                                jObject = JsonConvert.DeserializeObject(result) as JObject;

                                if ((int)jObject["Code"] == 0)
                                {

                                    var jArray = jObject["ResumeSummaries"] as JArray;

                                    var isSuccessBack = true;

                                    if (jArray.Count > 0)
                                    {
                                        var matchedResult = jArray.Select(s => new
                                        {
                                            Cellphone = (string)s["Cellphone"],
                                            Email = (string)s["Email"],
                                            ResumeNumber = ((string)s["ResumeNumber"]).Substring(0, 10),
                                            Status = 2
                                        }).ToList();

                                        var data = RequestFactory.QueryRequest("http://192.168.1.38:8085/splider/Resume/ModifyContact", JsonConvert.SerializeObject(matchedResult), RequestEnum.POST, contentType: "application/json");

                                        if (!data.Contains("成功")) isSuccessBack = false;
                                    }

                                    foreach (var item in jArray)
                                    {
                                        var resumeId = (string)item["ResumeId"];

                                        var resume = db.ResumeRecord.FirstOrDefault(f => f.ResumeId == resumeId);

                                        if (resume != null)
                                        {
                                            resume.LibraryExist = 1;
                                            resume.Status = 4;
                                            resume.DownLoadTime = DateTime.UtcNow;
                                            resume.Cellphone = (string)item["Cellphone"];
                                            resume.Email = (string)item["Email"];
                                            resume.PostBackStatus = isSuccessBack ? (short)1 : (short)2;
                                        }
                                    }

                                    

                                    success += jArray.Count;
                                }
                            }

                            Console.WriteLine($"共 {total} 条，已过滤 {filter} 条，匹配成功 {success} 条， 命中率 {Math.Round((decimal)success / filter, 2) * 100}%  进度 {Math.Round((decimal)filter / total,2) * 100}%");
                        }

                        db.SaveChanges();

                        conn.Close();
                    }
                }
            }
        }

        /// <summary>
        /// 检查命中率
        /// </summary>
        private static void Check()
        {
            using (var db = new ResumeRepairDBEntities())
            {
                var successList = new List<string>();

                var deSuccessList = new List<string>();

                var jbs = new JiebaSegmenter();

                var list = db.ResumeRecord.Where(w => w.PostBackStatus == 1 && w.Status != 3).ToList();

                var fileList = new DirectoryAllFiles().GetAllFiles(new DirectoryInfo("D:\\Resumes\\Complete\\"));

                var count = 0d;

                var success = 0d;

                var countNull = 0;

                foreach (var item in list)
                {
                    count++;

                    var resumeNumber = item.MatchResumeId.Substring(item.MatchResumeId.IndexOf("-") + 1);

                    var resumeJson = RequestFactory.QueryRequest("http://192.168.1.38:8085/splider/Resume/GetResumeByNumber?resumenumber=" + resumeNumber);

                    if (string.IsNullOrWhiteSpace(resumeJson) || resumeJson.Contains("null"))
                    {
                        Console.WriteLine("请求响应为空！resumenumber：" + resumeNumber);

                        count--;

                        countNull++;

                        continue;
                    }

                    var fileName = item.MatchResumeId.Substring(0, item.MatchResumeId.IndexOf("/")) + ".json";

                    var file = fileList.FirstOrDefault(f => f.FileName == fileName);

                    try
                    {
                        var json = File.ReadAllText(file.FilePath);

                        var jObjectA = JsonConvert.DeserializeObject(resumeJson) as JObject;

                        var jObjectB = JsonConvert.DeserializeObject(json) as JObject;

                        var evaluation = ((JArray)jObjectA["Resumes"])[0]["Intention"]["Evaluation"].ToString();

                        if (string.IsNullOrWhiteSpace(evaluation))
                        {
                            Console.WriteLine("自我介绍为空！简历ID：" + resumeNumber);

                            count--;

                            continue;
                        }

                        var arrSelf = ((JArray)jObjectB["selfIntroduction"]);

                        if (arrSelf.Count == 0)
                        {
                            Console.WriteLine("自我介绍为空！Path：" + file.FilePath);

                            count--;

                            continue;
                        }

                        var selfIntroduction = arrSelf[0]?["content"].ToString();

                        if (string.IsNullOrWhiteSpace(selfIntroduction))
                        {
                            Console.WriteLine("自我介绍为空！Path：" + file.FilePath);

                            count--;

                            continue;
                        }

                        var arrList = jbs.Cut(selfIntroduction).Where(w => w.Length > 1).Take(3);

                        if (arrList.All(a => evaluation.Contains(a)))
                        {
                            success++;

                            successList.Add($"{resumeJson}{Environment.NewLine}--------------------------------{json}");

                            Console.WriteLine($"命中成功！命中率：{Math.Round(success / count, 3) * 100}%");
                        }
                        else
                        {
                            deSuccessList.Add($"{resumeJson}{Environment.NewLine}--------------------------------{json}");
                        }

                    }
                    catch (FileNotFoundException)
                    {
                        Console.WriteLine("找不到文件！Path：" + file.FilePath);

                        continue;
                    }
                }

            }

            Console.WriteLine("End");
        }
	}
}
