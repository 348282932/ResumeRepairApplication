using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication.Platform.FenJianLi
{
    /// <summary>
    /// 纷简历激活邮箱
    /// </summary>
    public class ActivationSpider : FenJianLiSpider
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

                        users = db.User.Where(w => w.Status == 0 && w.Platform == 1 && (!w.IsLocked || w.IsLocked && w.LockedTime < time)).ToList();

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

                    var message = messages.FirstOrDefault(f => f.message.Headers.To.FirstOrDefault()?.Address == user.Email);

                    if (message == null) continue;

                    var content = Encoding.Default.GetString(message.message.RawMessage);

                    if (!Regex.IsMatch(content, "(?s)code=(.+?)</a>"))
                    {
                        LogFactory.Error($"未匹配到激活码！响应源：{content}", MessageSubjectEnum.FenJianLi);

                        continue;
                    }
                    
                    var activationCode = Regex.Match(content, "(?s)code=(.+?)</a>").Result("$1").Substring(2);

                    var host = Global.IsEnanbleProxy ? GetProxy(true) : string.Empty;

                    var dataResult = Activation(activationCode, user.Email, host);

                    if (dataResult == null) continue;

                    if (!dataResult.IsSuccess)
                    {
                        LogFactory.Error(dataResult.ErrorMsg, MessageSubjectEnum.FenJianLi);

                        continue;
                    }

                    user.Status = 1;

                    LogFactory.Info($"激活成功！邮箱：{user.Email}", MessageSubjectEnum.FenJianLi);

                    if (!EmailFactory.DeleteMessageByMessageId("pop.exmail.qq.com", 995, true, Global.Email, Global.PassWord, message.message.Headers.MessageId))
                    {
                        LogFactory.Error($"删除激活邮件失败,邮箱地址：{user.Email}", MessageSubjectEnum.FenJianLi);
                    }
                }

                db.TransactionSaveChanges();
            }

            #endregion       
        }

        /// <summary>
        /// 激活蜘蛛
        /// </summary>
        /// <param name="activationCode"></param>
        /// <param name="email"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        [Loggable]
        private static DataResult Activation(string activationCode, string email, string host)
        {
            var url = "http://www.fenjianli.com/register/checkEmailOfCode.htm?code=" + activationCode;

            var dataResult = RequestFactory.QueryRequest(url, host: host);

            if(!dataResult.IsSuccess) throw new RequestException($"激活账户失败！邮箱地址：{email}");

            if (!dataResult.Data.Contains("成功"))
            {
                dataResult.IsSuccess = false;

                dataResult.ErrorMsg += $"激活失败！,邮箱地址：{email}{Environment.NewLine}";
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
                Activation(); // 激活

                return new DataResult();
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
