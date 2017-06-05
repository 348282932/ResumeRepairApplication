using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication.Api
{
    public class ApiBase
    {
        protected static string signature;

        /// <summary>
        /// 登录授权
        /// </summary>
        /// <returns></returns>
        protected static string Login()
        {
            dynamic param = new { Username = Global.UserName, Password = Global.UserPassword };

            for (var i = 0; i < 3; i++)
            {
                var dataResult = RequestFactory.QueryRequest(Global.HostChen + "/api/queryresume/login", JsonConvert.SerializeObject(param), RequestEnum.POST, contentType: ContentTypeEnum.Json.Description());

                if (dataResult.IsSuccess)
                {
                    var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

                    if (jObject == null) continue;

                    if ((int)jObject["Code"] == 0)
                    {
                        signature = jObject["Signature"].ToString();

                        break;
                    }

                    LogFactory.Warn("简历过滤 API 登录异常！异常信息：" + jObject["Message"], MessageSubjectEnum.API);
                }
            }

            return signature;
        }

        /// <summary>
        /// 返回匹配结果
        /// </summary>
        /// <param name="list"></param>
        public static bool PostResumes(IReadOnlyCollection<ResumeMatchResult> list)
        {
            var dataResult = new DataResult<string>();

            var deepCopyList = list.Clone<ResumeMatchResult>();

            foreach (var resume in deepCopyList)
            {
                resume.ResumeNumber = resume.ResumeNumber.Substring(0, 10);
            }

            for (var i = 0; i < 3; i++)
            {
                dataResult = RequestFactory.QueryRequest(Global.HostZhao + "/splider/Resume/ModifyContact", JsonConvert.SerializeObject(deepCopyList), RequestEnum.POST, contentType: ContentTypeEnum.Json.Description());

                if (dataResult.IsSuccess && dataResult.Data.Contains("成功"))
                {
                    LogFactory.Info($"简历已成功回传！JSON：{JsonConvert.SerializeObject(deepCopyList)}", MessageSubjectEnum.API);

                    return true;
                }
            }

            LogFactory.Warn("简历回传 API 异常！响应信息：" + dataResult.Data, MessageSubjectEnum.API);

            return false;
        }
    }
}