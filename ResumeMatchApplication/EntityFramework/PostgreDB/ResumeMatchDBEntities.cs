using System.Data.Entity;

namespace ResumeMatchApplication.EntityFramework.PostgreDB
{
    public class ResumeMatchDBEntities : DbContext
    {
        public ResumeMatchDBEntities()
            : base("name=ResumeMatchDBEntities")
        {
        }

        public virtual DbSet<User> User { get; set; }
        
        public virtual DbSet<ResumeComplete> ResumeComplete { get; set; }
        public virtual DbSet<Proxy> Proxy { get; set; }

        //public virtual DbSet<AuthorizationAccount> AuthorizationAccount { get; set; }

    }
}
