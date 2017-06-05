using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeMatchApplication.EntityFramework.PostgreDB
{
    [Table("User", Schema = "public")]
    public class User
    {
        public int Id { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 邀请码
        /// </summary>
        public string InviteCode { get; set; }

        /// <summary>
        /// 平台（1.纷简历 2.101聘 3.简历咖 4.招聘狗）
        /// </summary>
        public short Platform { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        public DateTime? LastLoginTime { get; set; }

        /// <summary>
        /// IP 地址
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnable { get; set; }

        /// <summary>
        /// 剩余下载数
        /// </summary>
        public int DownloadNumber { get; set; }

        /// <summary>
        /// 状态（0.待激活 1.已激活 2.已授权）
        /// </summary>
        public short Status { get; set; }

        /// <summary>
        /// 公司帐号
        /// </summary>
        public string CompanyAccount { get; set; }
        
        /// <summary>
        /// 是否锁定
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// 锁定时间
        /// </summary>
        public DateTime? LockedTime { get; set; }

        /// <summary>
        /// 请求数
        /// </summary>
        public int RequestNumber { get; set; }

        /// <summary>
        /// 请求时间
        /// </summary>
        public DateTime? RequestDate { get; set; }

        /// <summary>
        /// 文件夹编号
        /// </summary>
        public string FolderCode { get; set; }
    }
}