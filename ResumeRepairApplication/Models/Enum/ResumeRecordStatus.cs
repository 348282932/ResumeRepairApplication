
namespace ResumeRepairApplication.Models
{
    public enum ResumeRecordStatus : short
    {
        /// <summary>
        /// 等待匹配
        /// </summary>
        WaitMatch = 1,

        /// <summary>
        /// 匹配成功
        /// </summary>
        MatchSuccess = 2,

        /// <summary>
        /// 匹配失败
        /// </summary>
        MatchFailure = 3,

        /// <summary>
        /// 下载成功
        /// </summary>
        DownLoadSuccess = 4,

        /// <summary>
        /// 下载失败
        /// </summary>
        DownLoadFailure = 5
    }
}
