using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
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
        protected static ConcurrentDictionary<Test, int> userDictionary = new ConcurrentDictionary<Test, int>();

        private static void Main(string[] args)
        {
            //FenJianLiScheduling.Run();

            //FenJianLiScheduling.Start();

            //ZhaoPinGouScheduling.Run();

            //ZhaoPinGouScheduling.Start();

            WorksScheduling.Run();

            WorksScheduling.Start();

            SpinWait.SpinUntil(() =>{return false;}, -1);
        }

        

        [Loggable]
        private DataResult Wirte()
        {
            var tests = new List<Test>();

            var dataResult = new DataResult();

            tests.Add(new Test {Id = 1, Name = "Max"});

            return dataResult;
        }
    }

    public class Test
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
