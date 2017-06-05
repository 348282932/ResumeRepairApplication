namespace ResumeRepairApplication
{
    partial class CheckNumberForm
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
            this.pbx_checkNumber = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_checkNumber)).BeginInit();
            this.SuspendLayout();
            // 
            // pbx_checkNumber
            // 
            this.pbx_checkNumber.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbx_checkNumber.Location = new System.Drawing.Point(0, 0);
            this.pbx_checkNumber.Name = "pbx_checkNumber";
            this.pbx_checkNumber.Size = new System.Drawing.Size(222, 121);
            this.pbx_checkNumber.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbx_checkNumber.TabIndex = 0;
            this.pbx_checkNumber.TabStop = false;
            // 
            // CheckNumberForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(222, 121);
            this.Controls.Add(this.pbx_checkNumber);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CheckNumberForm";
            this.ShowIcon = false;
            this.Text = "验证码";
            this.Load += new System.EventHandler(this.CheckNumberForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbx_checkNumber)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.PictureBox pbx_checkNumber;
    }
}