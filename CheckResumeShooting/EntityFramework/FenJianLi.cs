//------------------------------------------------------------------------------
// <auto-generated>
//     此代码已从模板生成。
//
//     手动更改此文件可能导致应用程序出现意外的行为。
//     如果重新生成代码，将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace ResumeRepairApplication.EntityFramework
{
    using System;
    using System.Collections.Generic;
    
    public partial class FenJianLi
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PassWord { get; set; }
        public System.DateTime CreateDate { get; set; }
        public Nullable<System.DateTime> LastLoginDate { get; set; }
        public bool IsEnable { get; set; }
        public int Integral { get; set; }
        public bool IsActivation { get; set; }
        public bool IsVerification { get; set; }
        public string VerificationAccount { get; set; }
        public bool IsLocked { get; set; }
    }
}
