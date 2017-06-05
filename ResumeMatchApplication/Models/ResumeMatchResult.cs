using System;

namespace ResumeMatchApplication.Models
{
    [Serializable]
    public class ResumeMatchResult
    {
        /// <summary>
        /// 简历编号
        /// </summary>
        public string ResumeNumber { get; set; }

        /// <summary>
        /// 状态（0:未处理 1:处理中 2:有联系方式 3:无联系方式 4:更新完成）
        /// </summary>
        public short Status { get; set; }

        /// <summary>
        /// 电话号码
        /// </summary>
        public string Cellphone { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }
    }
}
