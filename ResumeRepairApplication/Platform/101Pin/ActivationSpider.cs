using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ResumeRepairApplication.Common;
using ResumeRepairApplication.EntityFramework;
using ResumeRepairApplication._101Pin;

namespace ResumeRepairApplication.Platform._101Pin
{
	public class ActivationSpider : Pin101Spider
	{
		private DataResult Activation()
		{
			var dataResult = new DataResult();

			using (var db = new ResumeRepairDBEntities())
			{
				var list = db.Pin101.Where(w => !w.IsActivation).ToList();

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

					var content = Encoding.UTF8.GetString(message.message.FindFirstHtmlVersion().Body);

					var url = string.Empty;
                    //var referer = string.Empty;

                    //if (Regex.IsMatch(content, "(?s)(http://sctrack.+?html)"))
                    //{
                    //    url = Regex.Match(content, "(?s)(http://sctrack.+?html)").Result("$1");
                    //}

                    if (Regex.IsMatch(content, "(?s)php\\?s=(.+?/istype/\\d+)"))
                    {
                        url = "http://www.101pin.com/index.php?s=" + Regex.Match(content, "(?s)php\\?s=(.+?/istype/\\d+)").Result("$1");
                    }

                    var html = RequestFactory.QueryRequest(url, accept: "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");

					if (!html.Contains("成功"))
					{
						dataResult.IsSuccess = false;

						dataResult.ErrorMsg += $"激活失败！,邮箱地址：{userEmail.Email}{Environment.NewLine}";

						continue;
					}

					userEmail.IsActivation = true;

					Pin101Scheduling.ssf.SetText(Pin101Scheduling.ssf.fjl_tbx_RegisterActivation, $"激活成功！邮箱：{userEmail.Email}");

					if (!EmailFactory.DeleteMessageByMessageId("pop.exmail.qq.com", 995, true, Global.Email, Global.PassWord, message.message.Headers.MessageId))
					{
						Pin101Scheduling.ssf.SetText(Pin101Scheduling.ssf.fjl_tbx_RegisterActivation, $"删除激活邮件失败,邮箱地址：{userEmail.Email}");
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
                return Activation(); // 激活
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
