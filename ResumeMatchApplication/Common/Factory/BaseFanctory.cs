using System;

namespace ResumeMatchApplication.Common
{
    public class BaseFanctory
    {
        private static readonly string[] telStarts = "134,135,136,137,138,139,150,151,152,157,158,159,130,131,132,155,156,133,153,180,181,182,183,185,186,176,187,188,189,177,178".Split(',');

        private static readonly Random random = new Random();

        /// <summary>
        /// 随机生成电话号码
        /// </summary>
        /// <returns></returns>
        public static string GetRandomTel()
        {
            var index = random.Next(0, telStarts.Length - 1);

            var first = telStarts[index];

            var second = (random.Next(100, 888) + 10000).ToString().Substring(1);

            var thrid = (random.Next(1, 9100) + 10000).ToString().Substring(1);

            return first + second + thrid;
        }

        /// <summary>
        /// 获取 Unix 时间戳
        /// </summary>
        /// <returns></returns>
        public static string GetUnixTimestamp()
        {
            return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000 + random.Next(0,1000).ToString("000");
        }
    }
}