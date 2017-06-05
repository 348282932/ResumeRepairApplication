using System;
using System.Configuration;

namespace ResumeMatchApplication
{
    public static class Global
    {
        #region API Host

        public static string HostZhao = ConfigurationManager.AppSettings["HostZhao"]; // 恢武 Host

        public static string HostChen = ConfigurationManager.AppSettings["HostChen"]; // 绍林 Host

        #endregion

        #region 拟人化配置

        public static bool IsEnanbleProxy = Convert.ToBoolean(ConfigurationManager.AppSettings["IsEnanbleProxy"]); // 是否启用代理

        public static short ThreadCount = Convert.ToInt16(ConfigurationManager.AppSettings["ThreadCount"]); // 线程数

        public static short PlatformHostCount = Convert.ToInt16(ConfigurationManager.AppSettings["PlatformHostCount"]); // 每个Host在每个匹配平台使用的人数

        public static short PlatformCount = Convert.ToInt16(ConfigurationManager.AppSettings["PlatformCount"]); // 匹配平台个数

        public static long TodayMaxRequestNumber = Convert.ToInt64(ConfigurationManager.AppSettings["TodayMaxRequestNumber"]); // 每个帐号每日请求上线次数

        #endregion

        #region 企业邮箱

        public static string Email = ConfigurationManager.AppSettings["Email"];                   // 企业邮箱帐号

        public static string PassWord = ConfigurationManager.AppSettings["PassWord"];             // 企业邮箱密码

        #endregion

        #region 绍林 API 授权信息

        public static string UserName = ConfigurationManager.AppSettings["UserName"];

        public static string UserPassword = ConfigurationManager.AppSettings["UserPassword"];

        #endregion

        #region 缓存计数器

        public static int TotalMatch = 0;

        public static int TotalMatchSuccess = 0;

        public static int TotalDownload = 0;

        #endregion
    }
}
