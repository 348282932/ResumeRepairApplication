namespace ResumeRepairApplication.EntityFramework
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Pin101", Schema = "public")]
    public partial class Pin101
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PassWord { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public bool IsEnable { get; set; }
        public int Integral { get; set; }
        public bool IsActivation { get; set; }
        public bool IsVerification { get; set; }
        public string VerificationAccount { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? LockedTime { get; set; }
        public string InviteCode { get; set; }
    }
}
