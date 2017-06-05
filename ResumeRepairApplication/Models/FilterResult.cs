using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeRepairApplication.Models
{
    public class FilterResult
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
        /// 电话号码
        /// </summary>
        public string Cellphone { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }
    }
}
