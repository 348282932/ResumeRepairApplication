using System.ComponentModel;

namespace ResumeMatchApplication.Models
{
    public enum MessageSubjectEnum
    {
        /// <summary>
        /// 纷简历
        /// </summary>
        [Description("纷简历")]
        FenJianLi = 1,

        /// <summary>
        /// 101聘
        /// </summary>
        [Description("101聘")]
        Pin101 = 2,

        /// <summary>
        /// 简历咖
        /// </summary>
        [Description("简历咖")]
        JianLiKa = 3,

        /// <summary>
        /// 招聘狗
        /// </summary>
        [Description("招聘狗")]
        ZhaoPinGou = 4,

        /// <summary>
        /// API
        /// </summary>
        [Description("API")]
        API = 5,

        /// <summary>
        /// System
        /// </summary>
        [Description("System")]
        System = 6
    }
}