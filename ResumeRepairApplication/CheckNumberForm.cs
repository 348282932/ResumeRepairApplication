using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ResumeRepairApplication
{
    public partial class CheckNumberForm : Form
    {
        public CheckNumberForm()
        {
            InitializeComponent();
        }

        private void CheckNumberForm_Load(object sender, EventArgs e)
        {
            //pbx_checkNumber.Image = Image.FromFile("D:\\pic.jpg");
        }

        public delegate void ShowCheckNumberDeleage(PictureBox pbx, string fileName);

        /// <summary>
        /// 打印日志文本
        /// </summary>
        /// <param name="textbox"></param>
        /// <param name="text"></param>
        public void ShowCheckBox(PictureBox pbx, string fileName)
        {
            // 如果调用控件的线程和创建创建控件的线程不是同一个则为 True

            if (pbx.InvokeRequired)
            {
                while (!pbx.IsHandleCreated)
                {
                    // 解决窗体关闭时出现“访问已释放句柄“的异常

                    if (pbx.Disposing || pbx.IsDisposed) return;
                }

                ShowCheckNumberDeleage d = new ShowCheckNumberDeleage(ShowCheckBox);

                pbx.Invoke(d, new object[] { pbx, fileName });
            }
            else
            {
                try
                {
                    pbx.Image = Image.FromFile("D:\\pic.jpg");
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }
        }
    }
}
