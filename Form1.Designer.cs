namespace ChromeUpdater
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rdo32 = new System.Windows.Forms.RadioButton();
            this.rdo64 = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.cmbChannel = new System.Windows.Forms.ComboBox();
            this.rdoDefaultNo = new System.Windows.Forms.RadioButton();
            this.rdoDefaultYes = new System.Windows.Forms.RadioButton();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.rdoShortcutYes = new System.Windows.Forms.RadioButton();
            this.rdoShortcutNo = new System.Windows.Forms.RadioButton();
            this.btnLaunch = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lnkCopyright = new System.Windows.Forms.LinkLabel();
            this.LabelCopyright = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rdo32);
            this.groupBox1.Controls.Add(this.rdo64);
            this.groupBox1.Location = new System.Drawing.Point(28, 26);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(5);
            this.groupBox1.Size = new System.Drawing.Size(374, 90);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "系统架构";
            // 
            // rdo32
            // 
            this.rdo32.AutoSize = true;
            this.rdo32.Location = new System.Drawing.Point(255, 36);
            this.rdo32.Margin = new System.Windows.Forms.Padding(5);
            this.rdo32.Name = "rdo32";
            this.rdo32.Size = new System.Drawing.Size(62, 25);
            this.rdo32.TabIndex = 1;
            this.rdo32.TabStop = true;
            this.rdo32.Text = "32位";
            this.rdo32.UseVisualStyleBackColor = true;
            // 
            // rdo64
            // 
            this.rdo64.AutoSize = true;
            this.rdo64.Location = new System.Drawing.Point(43, 32);
            this.rdo64.Margin = new System.Windows.Forms.Padding(5);
            this.rdo64.Name = "rdo64";
            this.rdo64.Size = new System.Drawing.Size(62, 25);
            this.rdo64.TabIndex = 0;
            this.rdo64.TabStop = true;
            this.rdo64.Text = "64位";
            this.rdo64.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.cmbChannel);
            this.groupBox2.Location = new System.Drawing.Point(509, 26);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(5);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(5);
            this.groupBox2.Size = new System.Drawing.Size(374, 90);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "版本类型";
            // 
            // cmbChannel
            // 
            this.cmbChannel.FormattingEnabled = true;
            this.cmbChannel.Items.AddRange(new object[] {
            "稳定版 (Stable)--绝大多数用户的选择",
            "测试版 (Beta)--比稳定版早一个版本",
            "开发版 (Dev)--每周更新，面向开发者",
            "金丝雀版 (Canary)--每天更新，最新最前沿"});
            this.cmbChannel.Location = new System.Drawing.Point(19, 32);
            this.cmbChannel.Margin = new System.Windows.Forms.Padding(5);
            this.cmbChannel.Name = "cmbChannel";
            this.cmbChannel.Size = new System.Drawing.Size(345, 29);
            this.cmbChannel.TabIndex = 0;
            // 
            // rdoDefaultNo
            // 
            this.rdoDefaultNo.AutoSize = true;
            this.rdoDefaultNo.Location = new System.Drawing.Point(255, 45);
            this.rdoDefaultNo.Margin = new System.Windows.Forms.Padding(5);
            this.rdoDefaultNo.Name = "rdoDefaultNo";
            this.rdoDefaultNo.Size = new System.Drawing.Size(44, 25);
            this.rdoDefaultNo.TabIndex = 1;
            this.rdoDefaultNo.TabStop = true;
            this.rdoDefaultNo.Text = "否";
            this.rdoDefaultNo.UseVisualStyleBackColor = true;
            // 
            // rdoDefaultYes
            // 
            this.rdoDefaultYes.AutoSize = true;
            this.rdoDefaultYes.Location = new System.Drawing.Point(43, 45);
            this.rdoDefaultYes.Margin = new System.Windows.Forms.Padding(5);
            this.rdoDefaultYes.Name = "rdoDefaultYes";
            this.rdoDefaultYes.Size = new System.Drawing.Size(140, 25);
            this.rdoDefaultYes.TabIndex = 0;
            this.rdoDefaultYes.TabStop = true;
            this.rdoDefaultYes.Text = "设为默认浏览器";
            this.rdoDefaultYes.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.rdoDefaultYes);
            this.groupBox3.Controls.Add(this.rdoDefaultNo);
            this.groupBox3.Location = new System.Drawing.Point(28, 141);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(5);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(5);
            this.groupBox3.Size = new System.Drawing.Size(374, 90);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "是否设为默认浏览器";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.rdoShortcutYes);
            this.groupBox4.Controls.Add(this.rdoShortcutNo);
            this.groupBox4.Location = new System.Drawing.Point(509, 141);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(5);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(5);
            this.groupBox4.Size = new System.Drawing.Size(374, 90);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "是否创建桌面快捷方式";
            // 
            // rdoShortcutYes
            // 
            this.rdoShortcutYes.AutoSize = true;
            this.rdoShortcutYes.Location = new System.Drawing.Point(36, 45);
            this.rdoShortcutYes.Margin = new System.Windows.Forms.Padding(5);
            this.rdoShortcutYes.Name = "rdoShortcutYes";
            this.rdoShortcutYes.Size = new System.Drawing.Size(156, 25);
            this.rdoShortcutYes.TabIndex = 0;
            this.rdoShortcutYes.TabStop = true;
            this.rdoShortcutYes.Text = "创建桌面快捷方式";
            this.rdoShortcutYes.UseVisualStyleBackColor = true;
            // 
            // rdoShortcutNo
            // 
            this.rdoShortcutNo.AutoSize = true;
            this.rdoShortcutNo.Location = new System.Drawing.Point(278, 45);
            this.rdoShortcutNo.Margin = new System.Windows.Forms.Padding(5);
            this.rdoShortcutNo.Name = "rdoShortcutNo";
            this.rdoShortcutNo.Size = new System.Drawing.Size(44, 25);
            this.rdoShortcutNo.TabIndex = 1;
            this.rdoShortcutNo.TabStop = true;
            this.rdoShortcutNo.Text = "否";
            this.rdoShortcutNo.UseVisualStyleBackColor = true;
            // 
            // btnLaunch
            // 
            this.btnLaunch.Location = new System.Drawing.Point(157, 312);
            this.btnLaunch.Margin = new System.Windows.Forms.Padding(5);
            this.btnLaunch.Name = "btnLaunch";
            this.btnLaunch.Size = new System.Drawing.Size(280, 42);
            this.btnLaunch.TabIndex = 4;
            this.btnLaunch.Text = "启动浏览器";
            this.btnLaunch.UseVisualStyleBackColor = true;
            this.btnLaunch.Click += new System.EventHandler(this.btnLaunch_Click);
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(520, 312);
            this.btnUpdate.Margin = new System.Windows.Forms.Padding(5);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(280, 42);
            this.btnUpdate.TabIndex = 5;
            this.btnUpdate.Text = "检查并更新";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(260, 257);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(266, 21);
            this.lblStatus.TabIndex = 6;
            this.lblStatus.Text = "本地版本: 未检测 | 最新版本: 未检测";
            // 
            // lnkCopyright
            // 
            this.lnkCopyright.AutoSize = true;
            this.lnkCopyright.Location = new System.Drawing.Point(598, 414);
            this.lnkCopyright.Name = "lnkCopyright";
            this.lnkCopyright.Size = new System.Drawing.Size(94, 21);
            this.lnkCopyright.TabIndex = 7;
            this.lnkCopyright.TabStop = true;
            this.lnkCopyright.Text = "@YuC2027";
            // 
            // LabelCopyright
            // 
            this.LabelCopyright.AutoSize = true;
            this.LabelCopyright.Location = new System.Drawing.Point(205, 414);
            this.LabelCopyright.Name = "LabelCopyright";
            this.LabelCopyright.Size = new System.Drawing.Size(398, 21);
            this.LabelCopyright.TabIndex = 8;
            this.LabelCopyright.Text = "版本: 8.13   技术支持: 微信: jiujiujiayi666   Telegram: ";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(926, 499);
            this.Controls.Add(this.LabelCopyright);
            this.Controls.Add(this.lnkCopyright);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnUpdate);
            this.Controls.Add(this.btnLaunch);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(5);
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Opacity = 0.92D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Chrome 智能升级器";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rdo32;
        private System.Windows.Forms.RadioButton rdo64;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ComboBox cmbChannel;
        private System.Windows.Forms.RadioButton rdoDefaultNo;
        private System.Windows.Forms.RadioButton rdoDefaultYes;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.RadioButton rdoShortcutYes;
        private System.Windows.Forms.RadioButton rdoShortcutNo;
        private System.Windows.Forms.Button btnLaunch;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.LinkLabel lnkCopyright;
        private System.Windows.Forms.Label LabelCopyright;
    }
}

