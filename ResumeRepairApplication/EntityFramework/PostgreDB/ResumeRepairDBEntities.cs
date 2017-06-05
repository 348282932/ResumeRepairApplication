namespace ResumeRepairApplication.EntityFramework
{
    using System.Data.Entity;

    public partial class ResumeRepairDBEntities : DbContext
    {
        public ResumeRepairDBEntities()
            : base("name=ResumeRepairDBEntities")
        {
        }

        public virtual DbSet<FenJianLi> FenJianLi { get; set; }
        public virtual DbSet<AuthorizationAccount> AuthorizationAccount { get; set; }
        public virtual DbSet<ResumeRecord> ResumeRecord { get; set; }
        public virtual DbSet<MatchResumeSearch> MatchResumeSearch { get; set; }
        public virtual DbSet<Pin101> Pin101 { get; set; }

    }
}
