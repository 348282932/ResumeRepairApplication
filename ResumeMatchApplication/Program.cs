using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EntityFramework.Extensions;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PostSharp.Aspects;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.EntityFramework.PostgreDB;
using ResumeMatchApplication.Models;
using ResumeMatchApplication.Platform.FenJianLi;
using ResumeMatchApplication.Platform.ZhaoPinGou;
using ResumeMatchApplication.Works;

namespace ResumeMatchApplication
{
    internal class Program
    {
        public delegate void ControlCtrlDelegate(int CtrlType);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ControlCtrlDelegate HandlerRoutine, bool Add);

        private static readonly ControlCtrlDelegate cancelHandler = HandlerRoutine;

        private static void Main(string[] args)
        {
            //FenJianLiScheduling.Run();

            //FenJianLiScheduling.Start();

            //ZhaoPinGouScheduling.Run();

            //ZhaoPinGouScheduling.Start();

            WorksScheduling.Run();

            WorksScheduling.Start();

            SetConsoleCtrlHandler(cancelHandler, true);

            SpinWait.SpinUntil(() =>{return false;}, -1);
        }

        /// <summary>
        /// 释放全部代理
        /// </summary>
        /// <returns></returns>
        public static void ReleaseProxy()
        {
            var dataResult = RequestFactory.QueryRequest($"{Global.HostZhao}/splider/proxy/SetFreeAll?UserTag=maxlong");

            if (!dataResult.IsSuccess)
            {
                LogFactory.Warn($"释放全部 Host 异常！异常信息：{dataResult.ErrorMsg}");
            }

            var jObject = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

            if (jObject != null)
            {
                if ((int)jObject["Code"] == 1)
                {
                    LogFactory.Info("释放全部 Host 成功！");

                    return;
                }

                LogFactory.Warn($"释放全部 Host 异常！异常信息：{jObject["Message"]}");
            }
        }

        public static void HandlerRoutine(int CtrlType)
        {
            using (var db = new ResumeMatchDBEntities())
            {
                var users = db.User.Where(w => w.IsLocked).ToList();

                foreach (var user in users)
                {
                    user.IsLocked = false;
                }

                var resumes = db.ResumeComplete.Where(w => w.IsLocked).ToList();

                foreach (var resume in resumes)
                {
                    resume.IsLocked = false;
                }

                db.TransactionSaveChanges();
            }

            ReleaseProxy();
        }
    }
}
