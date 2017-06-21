namespace ResumeMatchApplication.Models
{
    public enum ResultCodeEnum
    {
        /// <summary>
        /// 代理失效
        /// </summary>
        ProxyDisable = 1,

        /// <summary>
        /// 请求次数上限
        /// </summary>
        RequestUpperLimit = 2,

        /// <summary>
        /// 找不到用户
        /// </summary>
        NoUsers = 3,

        /// <summary>
        /// 没有下载数
        /// </summary>
        NoDownloadNumber = 4,

        /// <summary>
        /// Web 链接失效
        /// </summary>
        WebNoConnection = 5
    }
}