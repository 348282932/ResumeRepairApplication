
namespace ResumeRepairApplication.EntityFramework
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("ResumeRecord", Schema ="public")]
    public partial class ResumeRecord
    {
        public long Id { get; set; }
        public short ResumePlatform { get; set; }
        public string ResumeId { get; set; }
        public short? MatchPlatform { get; set; }
        public string MatchResumeId { get; set; }
        public DateTime? MatchTime { get; set; }
        public DateTime? DownLoadTime { get; set; }
        public short LibraryExist { get; set; }
        public short Status { get; set; }
        public string Email { get; set; }
        public string Cellphone { get; set; }
        public short PostBackStatus { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
