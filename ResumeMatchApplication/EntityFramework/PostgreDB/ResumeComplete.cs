using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeMatchApplication.EntityFramework.PostgreDB
{
    
    [Table("ResumeComplete", Schema = "public")]
    public class ResumeComplete
    {
        public int Id { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 性别（0.男 1.女 -1.未知）
        /// </summary>
        public short Gender { get; set; }

        /// <summary>
        /// 最近一家公司名称
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
        /// MasterExtId
        /// </summary>
        public string UserMasterExtId { get; set; }

        /// <summary>
        /// 简历编号
        /// </summary>
        public string ResumeNumber { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 简历ID
        /// </summary>
        public string ResumeId { get; set; }

        /// <summary>
        /// 简历平台（1.智联招聘）
        /// </summary>
        public short ResumePlatform { get; set; }

        /// <summary>
        /// 匹配平台（1.纷简历 2.101聘 3.简历咖）
        /// </summary>
        public short MatchPlatform { get; set; }

        /// <summary>
        /// 第三方平台简历ID
        /// </summary>
        public string MatchResumeId { get; set; }

        /// <summary>
        /// 匹配时间
        /// </summary>
        public DateTime? MatchTime { get; set; }

        /// <summary>
        /// 下载时间
        /// </summary>
        public DateTime? DownloadTime { get; set; }

        /// <summary>
        /// 库中是否存在（0.待定 1.存在 2.不存在 3.异常）
        /// </summary>
        public short LibraryExist { get; set; }

        /// <summary>
        /// 状态（0.待过滤 1.待匹配 2.匹配成功 3.匹配失败 4.下载成功 5.下载失败 6.补全成功, 7.补全失败 8.姓名检验失败）
        /// </summary>
        public short Status { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 电话号码
        /// </summary>
        public string Cellphone { get; set; }

        /// <summary>
        /// 回传状态（0.未回传 1.回传成功 2.回传失败）
        /// </summary>
        public short PostBackStatus { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Host 地址
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 学历
        /// </summary>
        public string Degree { get; set; }

        /// <summary>
        /// 是否匹配（0.未匹配 1.匹配成功 2.匹配失败）
        /// </summary>
        public short ZhaoPinGouIsMatch { get; set; } = 0;

        /// <summary>
        /// 是否匹配（0.未匹配 1.匹配成功 2.匹配失败）
        /// </summary>
        public short FenJianLiIsMatch { get; set; } = 0;

        /// <summary>
        /// 是否被锁定
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// 锁定时间
        /// </summary>
        public DateTime LockedTime { get; set; }

        /// <summary>
        /// 权重（0.正常 1.泽林条件的简历）
        /// </summary>
        public short Weights { get; set; }
    }
}