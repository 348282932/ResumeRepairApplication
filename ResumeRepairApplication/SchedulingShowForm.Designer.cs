namespace ResumeRepairApplication
{
    partial class SchedulingShowForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.fenJianLi_groupBox = new System.Windows.Forms.GroupBox();
            this.fjl_btn_ContactInfomation = new System.Windows.Forms.Button();
            this.fjl_btn_RegisterActivation = new System.Windows.Forms.Button();
            this.fjl_btn_LoginCheckIn = new System.Windows.Forms.Button();
            this.fjl_tbx_RepairResume = new System.Windows.Forms.TextBox();
            this.fjl_tbx_LoginCheckIn = new System.Windows.Forms.TextBox();
            this.fjl_tbx_RegisterActivation = new System.Windows.Forms.TextBox();
            this.system_tbx_Exception = new System.Windows.Forms.TextBox();
            this.tbx_ResumeRepair = new System.Windows.Forms.TextBox();
            this.btn_ResumeRepair = new System.Windows.Forms.Button();
            this.lbl_TotalMatchTip = new System.Windows.Forms.Label();
            this.lbl_TotalMatch = new System.Windows.Forms.Label();
            this.lbl_TotalMatchSuccessTip = new System.Windows.Forms.Label();
            this.lbl_TotalMatchSuccess = new System.Windows.Forms.Label();
            this.lbl_TotalDownloadTip = new System.Windows.Forms.Label();
            this.lbl_TotalDownload = new System.Windows.Forms.Label();
            this.tim_RefreshSchedule = new System.Windows.Forms.Timer(this.components);
            this.fenJianLi_groupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // fenJianLi_groupBox
            // 
            this.fenJianLi_groupBox.Controls.Add(this.fjl_btn_ContactInfomation);
            this.fenJianLi_groupBox.Controls.Add(this.fjl_btn_RegisterActivation);
            this.fenJianLi_groupBox.Controls.Add(this.fjl_btn_LoginCheckIn);
            this.fenJianLi_groupBox.Controls.Add(this.fjl_tbx_RepairResume);
            this.fenJianLi_groupBox.Controls.Add(this.fjl_tbx_LoginCheckIn);
            this.fenJianLi_groupBox.Controls.Add(this.fjl_tbx_RegisterActivation);
            this.fenJianLi_groupBox.Location = new System.Drawing.Point(34, 79);
            this.fenJianLi_groupBox.Name = "fenJianLi_groupBox";
            this.fenJianLi_groupBox.Size = new System.Drawing.Size(1030, 430);
            this.fenJianLi_groupBox.TabIndex = 0;
            this.fenJianLi_groupBox.TabStop = false;
            this.fenJianLi_groupBox.Text = "纷简历";
            // 
            // fjl_btn_ContactInfomation
            // 
            this.fjl_btn_ContactInfomation.Location = new System.Drawing.Point(813, 24);
            this.fjl_btn_ContactInfomation.Name = "fjl_btn_ContactInfomation";
            this.fjl_btn_ContactInfomation.Size = new System.Drawing.Size(101, 33);
            this.fjl_btn_ContactInfomation.TabIndex = 2;
            this.fjl_btn_ContactInfomation.Text = "开始下载";
            this.fjl_btn_ContactInfomation.UseVisualStyleBackColor = true;
            this.fjl_btn_ContactInfomation.Click += new System.EventHandler(this.fjl_btn_ContactInfomation_Click);
            // 
            // fjl_btn_RegisterActivation
            // 
            this.fjl_btn_RegisterActivation.Location = new System.Drawing.Point(462, 24);
            this.fjl_btn_RegisterActivation.Name = "fjl_btn_RegisterActivation";
            this.fjl_btn_RegisterActivation.Size = new System.Drawing.Size(101, 33);
            this.fjl_btn_RegisterActivation.TabIndex = 2;
            this.fjl_btn_RegisterActivation.Text = "开始注册激活";
            this.fjl_btn_RegisterActivation.UseVisualStyleBackColor = true;
            this.fjl_btn_RegisterActivation.Click += new System.EventHandler(this.fjl_btn_RegisterActivation_Click);
            // 
            // fjl_btn_LoginCheckIn
            // 
            this.fjl_btn_LoginCheckIn.Location = new System.Drawing.Point(110, 24);
            this.fjl_btn_LoginCheckIn.Name = "fjl_btn_LoginCheckIn";
            this.fjl_btn_LoginCheckIn.Size = new System.Drawing.Size(88, 33);
            this.fjl_btn_LoginCheckIn.TabIndex = 1;
            this.fjl_btn_LoginCheckIn.Text = "开始签到";
            this.fjl_btn_LoginCheckIn.UseVisualStyleBackColor = true;
            this.fjl_btn_LoginCheckIn.Click += new System.EventHandler(this.fjl_btn_LoginCheckIn_Click);
            // 
            // fjl_tbx_RepairResume
            // 
            this.fjl_tbx_RepairResume.Location = new System.Drawing.Point(704, 68);
            this.fjl_tbx_RepairResume.Multiline = true;
            this.fjl_tbx_RepairResume.Name = "fjl_tbx_RepairResume";
            this.fjl_tbx_RepairResume.Size = new System.Drawing.Size(320, 356);
            this.fjl_tbx_RepairResume.TabIndex = 0;
            // 
            // fjl_tbx_LoginCheckIn
            // 
            this.fjl_tbx_LoginCheckIn.Location = new System.Drawing.Point(6, 68);
            this.fjl_tbx_LoginCheckIn.Multiline = true;
            this.fjl_tbx_LoginCheckIn.Name = "fjl_tbx_LoginCheckIn";
            this.fjl_tbx_LoginCheckIn.Size = new System.Drawing.Size(320, 356);
            this.fjl_tbx_LoginCheckIn.TabIndex = 0;
            // 
            // fjl_tbx_RegisterActivation
            // 
            this.fjl_tbx_RegisterActivation.Location = new System.Drawing.Point(359, 68);
            this.fjl_tbx_RegisterActivation.Multiline = true;
            this.fjl_tbx_RegisterActivation.Name = "fjl_tbx_RegisterActivation";
            this.fjl_tbx_RegisterActivation.Size = new System.Drawing.Size(320, 356);
            this.fjl_tbx_RegisterActivation.TabIndex = 0;
            // 
            // system_tbx_Exception
            // 
            this.system_tbx_Exception.Location = new System.Drawing.Point(555, 536);
            this.system_tbx_Exception.Multiline = true;
            this.system_tbx_Exception.Name = "system_tbx_Exception";
            this.system_tbx_Exception.Size = new System.Drawing.Size(519, 201);
            this.system_tbx_Exception.TabIndex = 1;
            // 
            // tbx_ResumeRepair
            // 
            this.tbx_ResumeRepair.Location = new System.Drawing.Point(34, 536);
            this.tbx_ResumeRepair.Multiline = true;
            this.tbx_ResumeRepair.Name = "tbx_ResumeRepair";
            this.tbx_ResumeRepair.Size = new System.Drawing.Size(515, 201);
            this.tbx_ResumeRepair.TabIndex = 1;
            // 
            // btn_ResumeRepair
            // 
            this.btn_ResumeRepair.Location = new System.Drawing.Point(847, 29);
            this.btn_ResumeRepair.Name = "btn_ResumeRepair";
            this.btn_ResumeRepair.Size = new System.Drawing.Size(101, 33);
            this.btn_ResumeRepair.TabIndex = 2;
            this.btn_ResumeRepair.Text = "开始补全简历";
            this.btn_ResumeRepair.UseVisualStyleBackColor = true;
            this.btn_ResumeRepair.Click += new System.EventHandler(this.btn_ResumeRepair_Click);
            // 
            // lbl_TotalMatchTip
            // 
            this.lbl_TotalMatchTip.AutoSize = true;
            this.lbl_TotalMatchTip.Location = new System.Drawing.Point(40, 29);
            this.lbl_TotalMatchTip.Name = "lbl_TotalMatchTip";
            this.lbl_TotalMatchTip.Size = new System.Drawing.Size(53, 12);
            this.lbl_TotalMatchTip.TabIndex = 3;
            this.lbl_TotalMatchTip.Text = "总匹配：";
            // 
            // lbl_TotalMatch
            // 
            this.lbl_TotalMatch.AutoSize = true;
            this.lbl_TotalMatch.Location = new System.Drawing.Point(99, 29);
            this.lbl_TotalMatch.Name = "lbl_TotalMatch";
            this.lbl_TotalMatch.Size = new System.Drawing.Size(11, 12);
            this.lbl_TotalMatch.TabIndex = 4;
            this.lbl_TotalMatch.Text = "0";
            // 
            // lbl_TotalMatchSuccessTip
            // 
            this.lbl_TotalMatchSuccessTip.AutoSize = true;
            this.lbl_TotalMatchSuccessTip.Location = new System.Drawing.Point(149, 29);
            this.lbl_TotalMatchSuccessTip.Name = "lbl_TotalMatchSuccessTip";
            this.lbl_TotalMatchSuccessTip.Size = new System.Drawing.Size(41, 12);
            this.lbl_TotalMatchSuccessTip.TabIndex = 5;
            this.lbl_TotalMatchSuccessTip.Text = "成功：";
            // 
            // lbl_TotalMatchSuccess
            // 
            this.lbl_TotalMatchSuccess.AutoSize = true;
            this.lbl_TotalMatchSuccess.Location = new System.Drawing.Point(189, 29);
            this.lbl_TotalMatchSuccess.Name = "lbl_TotalMatchSuccess";
            this.lbl_TotalMatchSuccess.Size = new System.Drawing.Size(11, 12);
            this.lbl_TotalMatchSuccess.TabIndex = 5;
            this.lbl_TotalMatchSuccess.Text = "0";
            // 
            // lbl_TotalDownloadTip
            // 
            this.lbl_TotalDownloadTip.AutoSize = true;
            this.lbl_TotalDownloadTip.Location = new System.Drawing.Point(259, 29);
            this.lbl_TotalDownloadTip.Name = "lbl_TotalDownloadTip";
            this.lbl_TotalDownloadTip.Size = new System.Drawing.Size(53, 12);
            this.lbl_TotalDownloadTip.TabIndex = 6;
            this.lbl_TotalDownloadTip.Text = "总下载：";
            // 
            // lbl_TotalDownload
            // 
            this.lbl_TotalDownload.AutoSize = true;
            this.lbl_TotalDownload.Location = new System.Drawing.Point(319, 29);
            this.lbl_TotalDownload.Name = "lbl_TotalDownload";
            this.lbl_TotalDownload.Size = new System.Drawing.Size(11, 12);
            this.lbl_TotalDownload.TabIndex = 7;
            this.lbl_TotalDownload.Text = "0";
            // 
            // tim_RefreshSchedule
            // 
            this.tim_RefreshSchedule.Tick += new System.EventHandler(this.tim_RefreshSchedule_Tick);
            // 
            // SchedulingShowForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1115, 783);
            this.Controls.Add(this.lbl_TotalDownload);
            this.Controls.Add(this.lbl_TotalDownloadTip);
            this.Controls.Add(this.lbl_TotalMatchSuccess);
            this.Controls.Add(this.lbl_TotalMatchSuccessTip);
            this.Controls.Add(this.lbl_TotalMatch);
            this.Controls.Add(this.lbl_TotalMatchTip);
            this.Controls.Add(this.btn_ResumeRepair);
            this.Controls.Add(this.tbx_ResumeRepair);
            this.Controls.Add(this.system_tbx_Exception);
            this.Controls.Add(this.fenJianLi_groupBox);
            this.MaximizeBox = false;
            this.Name = "SchedulingShowForm";
            this.Text = "调度监控";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SchedulingShowForm_FormClosing);
            this.Load += new System.EventHandler(this.SchedulingShowForm_Load);
            this.fenJianLi_groupBox.ResumeLayout(false);
            this.fenJianLi_groupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox fenJianLi_groupBox;
        public System.Windows.Forms.TextBox fjl_tbx_RegisterActivation;
        public System.Windows.Forms.TextBox system_tbx_Exception;
        public System.Windows.Forms.TextBox fjl_tbx_LoginCheckIn;
        public System.Windows.Forms.TextBox tbx_ResumeRepair;
        public System.Windows.Forms.TextBox fjl_tbx_RepairResume;
        private System.Windows.Forms.Button fjl_btn_RegisterActivation;
        private System.Windows.Forms.Button fjl_btn_LoginCheckIn;
        private System.Windows.Forms.Button btn_ResumeRepair;
        private System.Windows.Forms.Button fjl_btn_ContactInfomation;
        private System.Windows.Forms.Label lbl_TotalMatchTip;
        private System.Windows.Forms.Label lbl_TotalMatch;
        private System.Windows.Forms.Label lbl_TotalMatchSuccessTip;
        private System.Windows.Forms.Label lbl_TotalMatchSuccess;
        private System.Windows.Forms.Label lbl_TotalDownloadTip;
        private System.Windows.Forms.Label lbl_TotalDownload;
        private System.Windows.Forms.Timer tim_RefreshSchedule;
    }
}