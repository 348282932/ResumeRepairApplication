using ResumeRepairApplication.Common;
using ResumeRepairApplication.Common.Factory;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using ResumeRepairApplication.Platform.FenJianLi;
using ResumeRepairApplication.Platform._101Pin;

namespace ResumeRepairApplication
{
	public partial class SchedulingShowForm : Form
    {
        public SchedulingShowForm()
        {
            InitializeComponent();
        }

	    private static CheckNumberForm cnf = new CheckNumberForm();

        private void SchedulingShowForm_Load(object sender, EventArgs e)
        {

            CheckForIllegalCrossThreadCalls = false;

            var cache = new CacheFactory<Models.Schedule>();

            var schedule = cache.GetCache("Schedule").Data;

            Global.TotalMatch = schedule?.TotalMatch ?? 0;
            Global.TotalMatchSuccess = schedule?.TotalMatchSuccess ?? 0;
            Global.TotalDownload = schedule?.TotalDownload ?? 0;

            tim_RefreshSchedule.Start();

            FenJianLiScheduling.Start(this, cnf); // 启动纷简历调度           
            //Pin101Scheduling.Start(this, cnf); // 启动101pin调度
        }



        private void fjl_btn_LoginCheckIn_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;

            if (btn.Text.Contains("开始"))
            {
                FenJianLiScheduling.Start("签到");
                btn.Text = "停止签到";
            }
            else
            {
                FenJianLiScheduling.Stop("签到");
                btn.Text = "开始签到";
            }
            
        }
        public delegate void SetTextCallBack(TextBox textbox, string text);

        /// <summary>
        /// 打印日志文本
        /// </summary>
        /// <param name="textbox"></param>
        /// <param name="text"></param>
        public void SetText(TextBox textbox, string text)
        {
            try
            {
                // 如果调用控件的线程和创建创建控件的线程不是同一个则为 True

                if (textbox.InvokeRequired)
                {
                    while (!textbox.IsHandleCreated)
                    {
                        // 解决窗体关闭时出现“访问已释放句柄“的异常

                        if (textbox.Disposing || textbox.IsDisposed) return;
                    }

                    var d = new SetTextCallBack(SetText);

                    textbox.Invoke(d, textbox, text);
                }
                else
                {

                    if (textbox.Name == "system_tbx_Exception")
                    {
                        LogFactory.SetErrorLog(text);
                    }
                    else
                    {
                        LogFactory.SetInfoLog(text);
                    }

                    if (textbox.Text.Length > 2000)
                    {
                        textbox.Text = text + Environment.NewLine;
                    }
                    else
                    {
                        textbox.AppendText(text + Environment.NewLine);
                    }
                }

            }
            catch (ObjectDisposedException){}
            catch (InvalidAsynchronousStateException){}
        }

        private void btn_ResumeRepair_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;

            if (btn.Text.Contains("开始"))
            {
                btn.Text = "停止补全";
                ResumeRepair.Start();
            }
            else
            {
                ResumeRepair.Stop();
                btn.Text = "开始补全";
            }
        }

        private void fjl_btn_RegisterActivation_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;

            if (btn.Text.Contains("开始"))
            {
                cnf.Show();
                FenJianLiScheduling.Start("注册");
                FenJianLiScheduling.Start("激活");
                btn.Text = "停止注册激活";
            }
            else
            {
                cnf.Close();
                FenJianLiScheduling.Stop("注册");
                FenJianLiScheduling.Stop("激活");
                btn.Text = "开始注册激活";
            }
        }

        /// <summary>
        /// 窗体关闭时释放用户锁
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SchedulingShowForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            IsDisposedUserLock();

            var cache = new CacheFactory<Models.Schedule>();

            cache.SetCache("Schedule", new Models.Schedule
            {
                TotalDownload = Global.TotalDownload,
                TotalMatch = Global.TotalMatch,
                TotalMatchSuccess = Global.TotalMatchSuccess
            });
        }

        private void fjl_btn_ContactInfomation_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;

            if (btn.Text.Contains("开始"))
            {
                FenJianLiScheduling.Start("下载");
                btn.Text = "停止下载";
            }
            else
            {
                FenJianLiScheduling.Stop("下载");
                IsDisposedUserLock();
                btn.Text = "开始下载";
            }
        }

        /// <summary>
        /// 释放用户锁
        /// </summary>
        private void IsDisposedUserLock()
        {
            using (var db = new EntityFramework.ResumeRepairDBEntities())
            {
                var list = db.FenJianLi.Where(w => w.IsLocked).ToList();

                if (list.Count > 0)
                {
                    foreach (var item in list)
                    {
                        item.IsLocked = false;
                    }

                    db.SaveChanges();
                }
            }
        }

        private void tim_RefreshSchedule_Tick(object sender, EventArgs e)
        {
            lbl_TotalMatch.Text = Global.TotalMatch.ToString();
            lbl_TotalMatchSuccess.Text = Global.TotalMatchSuccess.ToString();
            lbl_TotalDownload.Text = Global.TotalDownload.ToString();
        }
    }
}
