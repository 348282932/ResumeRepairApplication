using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeMatchApplication.EntityFramework.PostgreDB
{
    [Table("AuthorizationAccount", Schema ="public")]
    public class AuthorizationAccount
    {
        public int Id { get; set; }
        public string ExtendParam { get; set; }
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public System.DateTime CreateTime { get; set; }
        public bool IsEnable { get; set; }
    }
}
