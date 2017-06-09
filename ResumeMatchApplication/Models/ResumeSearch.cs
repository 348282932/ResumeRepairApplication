namespace ResumeMatchApplication.Models
{
    public class ResumeSearch
    {
        /// <summary>
        /// 简历编号
        /// </summary>
        public string ResumeId { get; set; }

        /// <summary>
        /// 用户编号
        /// </summary>
        public string UserMasterExtId { get; set; }

        /// <summary>
        /// 简历NO
        /// </summary>
        public string ResumeNumber { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 性别（0.男 1.女 -1.未知）
        /// </summary>
        public short Gender { get; set; }

        /// <summary>
        /// 最后一次工作的公司
        /// </summary>
        public string LastCompany { get; set; }

        /// <summary>
        /// 毕业院校
        /// </summary>
        public string University { get; set; }

        /// <summary>
        /// 自我介绍
        /// </summary>
        public string Introduction { get; set; }

        /// <summary>
        /// 学历
        /// </summary>
        public string Degree  { get; set; }

        /// <summary>
        /// 是否匹配（0.未匹配 1.匹配成功 2.匹配失败）
        /// </summary>
        public short ZhaoPinGouIsMatch { get; set; } = 0;

        /// <summary>
        /// 是否匹配（0.未匹配 1.匹配成功 2.匹配失败）
        /// </summary>
        public short FenJianLiIsMatch { get; set; } = 0;

        /// <summary>
        /// 是否搜索结束
        /// </summary>
        public bool IsEnd { get; set; } = false;

        public MessageSubjectEnum MatchPlatform { get; set; }
    }
}
