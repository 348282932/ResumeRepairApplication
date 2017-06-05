using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeMatchApplication.EntityFramework.PostgreDB
{
    [Table("Proxy", Schema = "public")]
    public class Proxy
    {
        public int Id { get; set; }

        /// <summary>
        /// Host
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 平台（1.纷简历 2.101聘 3.简历咖）
        /// </summary>
        public short Platform { get; set; }

        /// <summary>
        /// 使用用户数
        /// </summary>
        public short Count { get; set; }
    }
    
}