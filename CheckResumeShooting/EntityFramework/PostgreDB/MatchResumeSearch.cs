using System.ComponentModel.DataAnnotations.Schema;

namespace CheckResumeShooting.EntityFramework.PostgreDB
{
	[Table("MatchResumeSearch", Schema = "public")]
    public class MatchResumeSearch
    {
        public int Id { get; set; }

        public long ResumeRecodeId { get; set; }

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
