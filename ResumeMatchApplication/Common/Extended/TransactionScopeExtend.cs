using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;

namespace ResumeMatchApplication.Common
{
    public static class TransactionScopeExtend
    {
        /// <summary>
        /// EF在事务中提交
        /// </summary>
        /// <param name="db"></param>
        public static void TransactionSaveChanges(this DbContext db)
        {
            using (var ts = db.Database.BeginTransaction())
            {
                try
                {
                    db.SaveChanges();

                    ts.Commit();
                }
                catch (DbEntityValidationException ex)
                {
                    var ve = ex.EntityValidationErrors.First().ValidationErrors.First();

                    throw new Exception($"数据库存储异常，异常字段：{ve.PropertyName}，{Environment.NewLine}异常信息：{ve.ErrorMessage}");
                }
            }
        }

        /// <summary>
        /// 在EF事务中执行代码，若执行 SaveChanges 建议使用 TransactionSaveChanges()
        /// </summary>
        /// <param name="db"></param>
        /// <param name="work"></param>
        /// <param name="level"></param>
        public static void UsingTransaction(this DbContext db, Action work, IsolationLevel level = IsolationLevel.RepeatableRead)
        {
            using (var ts = db.Database.BeginTransaction(level))
            {
                try
                {
                    work();

                    ts.Commit();
                }
                catch (DbEntityValidationException ex)
                {
                    var ve = ex.EntityValidationErrors.First().ValidationErrors.First();

                    throw new Exception($"数据库存储异常，异常字段：{ve.PropertyName}，{Environment.NewLine}异常信息：{ve.ErrorMessage}");
                }
            }
        }

        /// <summary>
        /// 在EF事务中执行代码，若执行 SaveChanges 建议使用 TransactionSaveChanges()
        /// </summary>
        /// <param name="db"></param>
        /// <param name="work"></param>
        /// <param name="level"></param>
        public static T UsingTransaction<T>(this DbContext db, Func<T> work, IsolationLevel level = IsolationLevel.RepeatableRead)
        {
            using (var ts = db.Database.BeginTransaction(level))
            {
                var r = work();

                ts.Commit();

                return r;
            }
        }
    }
}