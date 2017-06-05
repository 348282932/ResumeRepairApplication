using System.Configuration;

namespace ResumeRepairApplication
{
    public sealed class Global
    {
        #region API URL

        public static string PullResumesUrl = ConfigurationManager.AppSettings["PullResumesUrl"]; // 获取没有联系方式简历

        public static string PostResumesUrl = ConfigurationManager.AppSettings["PostResumesUrl"]; // 回传匹配结果

        public static string FilterAuthUrl = ConfigurationManager.AppSettings["FilterAuthUrl"]; // 过滤简历授权

        public static string FilterUrl = ConfigurationManager.AppSettings["FilterUrl"]; // 过滤简历

        #endregion

        #region 企业邮箱

        public static string Email = ConfigurationManager.AppSettings["Email"];                   // 企业邮箱帐号

        public static string PassWord = ConfigurationManager.AppSettings["PassWord"];             // 企业邮箱密码

        #endregion


        #region 缓存计数器

        public static int TotalMatch = 0;

        public static int TotalMatchSuccess = 0;

        public static int TotalDownload = 0;

        #endregion
    }
}
