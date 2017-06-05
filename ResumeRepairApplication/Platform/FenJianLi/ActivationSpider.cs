using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ResumeRepairApplication.Common;
using ResumeRepairApplication.EntityFramework;

namespace ResumeRepairApplication.Platform.FenJianLi
{
    public class ActivationSpider : FenJianLiSpider
    {

        /// <summary>
        /// 激活
        /// </summary>
        /// <returns></returns>
        private DataResult Activation()
        {
            var dataResult = new DataResult();

            using (var db = new ResumeRepairDBEntities())
            {
                var list = db.FenJianLi.Where(w => !w.IsActivation).ToList();

                if (list.Count == 0) return dataResult;

                #region 获取未读邮件列表

                var seenUids = new List<string>();

                var messages = EmailFactory.FetchUnseenMessages("pop.exmail.qq.com", 995, true, Global.Email, Global.PassWord, seenUids);

                foreach (var userEmail in list)
                {
                    var message = messages.FirstOrDefault(f => f.message.Headers.To.FirstOrDefault()?.Address == userEmail.Email);

                    if (message == null)
                    {
                        //dataResult.IsSuccess = false;

                        //dataResult.ErrorMsg += $"获取激活邮件失败！,找不到邮件！邮箱地址：{userEmail.Email}";

                        continue;
                    } 

                    var content = Encoding.Default.GetString(message.message.RawMessage);

                    var url = string.Empty;

                    if (Regex.IsMatch(content, "(?s)code=(.+?)</a>"))
                    {
                        url = "http://www.fenjianli.com/register/checkEmailOfCode.htm?code=" + Regex.Match(content, "(?s)code=(.+?)</a>").Result("$1").Substring(2);
                    }

                    var html = RequestFactory.QueryRequest(url);

                    if (!html.Contains("成功"))
                    {
                        dataResult.IsSuccess = false;

                        dataResult.ErrorMsg += $"激活失败！,邮箱地址：{userEmail.Email}{Environment.NewLine}";

                        continue;
                    }

                    userEmail.IsActivation = true;

                    FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.fjl_tbx_RegisterActivation, $"激活成功！邮箱：{userEmail.Email}");

                    if (!EmailFactory.DeleteMessageByMessageId("pop.exmail.qq.com", 995, true, Global.Email, Global.PassWord, message.message.Headers.MessageId))
                    {
                        FenJianLiScheduling.ssf.SetText(FenJianLiScheduling.ssf.fjl_tbx_RegisterActivation, $"删除激活邮件失败,邮箱地址：{userEmail.Email}");
                    }
                }

                #endregion

                db.SaveChanges();
            }

            return dataResult;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public override DataResult Init()
        {
            try
            {
                return this.Activation(); // 激活
            }
            catch (RequestException ex)
            {
                return new DataResult("请求异常！" + ex.Message);
            }
            catch (Exception ex)
            {
                return new DataResult("程序异常！" + ex.Message);
            }
        }
    }
}
