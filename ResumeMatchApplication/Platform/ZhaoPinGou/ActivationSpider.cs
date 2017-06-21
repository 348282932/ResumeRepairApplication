using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication.Platform.ZhaoPinGou
{
    public class ActivationSpider : ZhaoPinGouSpider
    {
        private static readonly object lockObj = new object();

        /// <summary>
        /// 激活
        /// </summary>
        private void Activation()
        {
            var users = new List<User>();

            lock (lockObj)
            {
                using (var db = new ResumeMatchDBEntities())
                {
                    db.UsingTransaction(() =>
                    {
                        var time = DateTime.UtcNow.AddHours(-1);

                        users = db.User.Where(w => w.Status == 0 && w.Platform == 4 /*&& (!w.IsLocked || w.IsLocked && w.LockedTime < time)*/).ToList();

                        foreach (var user in users)
                        {
                            user.IsLocked = true;

                            user.LockedTime = DateTime.UtcNow;
                        }

                        db.SaveChanges();
                    });
                }
            }

            if (users.Count == 0) return;

            #region 获取未读邮件列表

            var seenUids = new List<string>();

            var messages = EmailFactory.FetchUnseenMessages("pop.exmail.qq.com", 995, true, Global.Email, Global.PassWord, seenUids);

            using (var db = new ResumeMatchDBEntities())
            {
                var userIdArr = users.Select(s => s.Id).ToArray();

                var userList = db.User.Where(w => userIdArr.Any(a => a == w.Id));

                foreach (var user in userList)
                {
                    user.IsLocked = false;

                    var message = messages.FirstOrDefault(f => f.message.Headers.To.FirstOrDefault()?.Address == user.Email && f.message.Headers.From.Address == "zhaopingou@info.zhaopingou.com");

                    if (message == null) continue;

                    var content = Encoding.UTF8.GetString(message.message.FindFirstHtmlVersion().Body);

                    if (!Regex.IsMatch(content, "(?s)完成验证.+?com/track/click/(.+?html)"))
                    {
                        LogFactory.Error($"未匹配到激活码！响应源：{content}", MessageSubjectEnum.ZhaoPinGou);

                        continue;
                    }

                    var url = "http://sctrack.info.zhaopingou.com/track/click/" + Regex.Match(content, "(?s)完成验证.+?com/track/click/(.+?html)\".style").Result("$1");

                    var host = string.Empty;

                    //if (Global.IsEnanbleProxy)
                    //{
                    //    if (!string.IsNullOrWhiteSpace(user.Host))
                    //    {
                    //        host = user.Host;

                    //        GetProxy("ZPG_Activation",user.Host);
                    //    }
                    //}
                    var dataResult = Activation(url, user.Email, host);

                    //ReleaseProxy("ZPG_Activation", host);

                    if (dataResult == null) continue;

                    if (!dataResult.IsSuccess)
                    {
                        LogFactory.Error(dataResult.ErrorMsg, MessageSubjectEnum.ZhaoPinGou);

                        continue;
                    }

                    user.Status = 1;

                    LogFactory.Info($"激活成功！邮箱：{user.Email}", MessageSubjectEnum.ZhaoPinGou);

                    if (!EmailFactory.DeleteMessageByMessageId("pop.exmail.qq.com", 995, true, Global.Email, Global.PassWord, message.message.Headers.MessageId))
                    {
                        LogFactory.Error($"删除激活邮件失败,邮箱地址：{user.Email}", MessageSubjectEnum.ZhaoPinGou);
                    }
                }

                db.TransactionSaveChanges();
            }

            #endregion       
        }

        /// <summary>
        /// 激活蜘蛛
        /// </summary>
        /// <param name="url"></param>
        /// <param name="email"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        private static DataResult Activation(string url, string email, string host)
        {
            var dataResult = RequestFactory.QueryRequest(url, host: host);

            if (!dataResult.IsSuccess)
            {
                dataResult.IsSuccess = false;

                dataResult.ErrorMsg += $"激活失败！,邮箱地址：{email}{Environment.NewLine}";
            }

            return dataResult;

            //if (!dataResult.Data.Contains("成功"))
            //{
            //    dataResult.IsSuccess = false;

            //    dataResult.ErrorMsg += $"激活失败！,邮箱地址：{email}{Environment.NewLine}";
            //}

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
                Activation(); // 激活

                return new DataResult();
            }
            catch (RequestException ex)
            {
                return new DataResult("请求异常！" + ex.Message);
            }
            catch (Exception ex)
            {
                return new DataResult($"程序异常！异常信息：{ex.Message}{Environment.NewLine}堆栈信息：{ex.StackTrace}");
            }
        }
    }
}
