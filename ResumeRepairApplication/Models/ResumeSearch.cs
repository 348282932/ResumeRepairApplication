namespace ResumeRepairApplication.Models
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
        /// 最后一次工作的公司
        /// </summary>
        public string LastCompany { get; set; }

        /// <summary>
        /// 毕业院校
        /// </summary>
        public string University { get; set; }
    }
}
