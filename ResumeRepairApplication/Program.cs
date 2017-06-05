using ResumeRepairApplication.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using ResumeRepairApplication.EntityFramework;
using System.Data.Entity;
using JiebaNet.Segmenter;
using ResumeRepairApplication.Models;

namespace ResumeRepairApplication
{
    
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
			////DropCreateDatabaseIfModelChanges 此处只有修改模型运行才会创建新的数据库
			////CreateDatabaseIfNotExists 如果数据库不存在才会创建新的数据库
			////DropCreateDatabaseAlways 每次运行都会创建新的数据库
			//var Initializes = new DropCreateDatabaseIfModelChanges<ResumeRepairDBEntities>();

			//using (var db = new ResumeRepairDBEntities())
			//{
			//    Initializes.InitializeDatabase(db);
			//}

			Application.EnableVisualStyles();

			Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new SchedulingShowForm());

			//SpinWait.SpinUntil(() => { return false; }, -1); 
		}
    }
}
