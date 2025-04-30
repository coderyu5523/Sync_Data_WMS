namespace DioImplant_batch
{
    partial class MainForm
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lstSyncLog = new System.Windows.Forms.ListBox();
            this.progressBarSync = new System.Windows.Forms.ProgressBar();
            this.comboBoxSyncInterval = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnSetSyncPeriod = new System.Windows.Forms.Button();
            this.checkedListBoxTargetData = new System.Windows.Forms.CheckedListBox();
            this.btnSelectTargetData = new System.Windows.Forms.Button();
            this.btn_order_sync = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(35, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(131, 62);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(35, 97);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(38, 12);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "label1";
            // 
            // lstSyncLog
            // 
            this.lstSyncLog.FormattingEnabled = true;
            this.lstSyncLog.ItemHeight = 12;
            this.lstSyncLog.Location = new System.Drawing.Point(37, 134);
            this.lstSyncLog.Name = "lstSyncLog";
            this.lstSyncLog.Size = new System.Drawing.Size(242, 292);
            this.lstSyncLog.TabIndex = 2;
            // 
            // progressBarSync
            // 
            this.progressBarSync.Location = new System.Drawing.Point(206, 32);
            this.progressBarSync.Name = "progressBarSync";
            this.progressBarSync.Size = new System.Drawing.Size(438, 23);
            this.progressBarSync.TabIndex = 3;
            // 
            // comboBoxSyncInterval
            // 
            this.comboBoxSyncInterval.FormattingEnabled = true;
            this.comboBoxSyncInterval.Items.AddRange(new object[] {
            "즉시",
            "5분",
            "30분",
            "1시간",
            "매일"});
            this.comboBoxSyncInterval.Location = new System.Drawing.Point(323, 103);
            this.comboBoxSyncInterval.Name = "comboBoxSyncInterval";
            this.comboBoxSyncInterval.Size = new System.Drawing.Size(195, 20);
            this.comboBoxSyncInterval.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(323, 85);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 12);
            this.label1.TabIndex = 5;
            this.label1.Text = "주기";
            // 
            // btnSetSyncPeriod
            // 
            this.btnSetSyncPeriod.Location = new System.Drawing.Point(544, 72);
            this.btnSetSyncPeriod.Name = "btnSetSyncPeriod";
            this.btnSetSyncPeriod.Size = new System.Drawing.Size(131, 62);
            this.btnSetSyncPeriod.TabIndex = 6;
            this.btnSetSyncPeriod.Text = "주기설정";
            this.btnSetSyncPeriod.UseVisualStyleBackColor = true;
            this.btnSetSyncPeriod.Click += new System.EventHandler(this.btnSetSyncPeriod_Click);
            // 
            // checkedListBoxTargetData
            // 
            this.checkedListBoxTargetData.FormattingEnabled = true;
            this.checkedListBoxTargetData.Location = new System.Drawing.Point(323, 148);
            this.checkedListBoxTargetData.Name = "checkedListBoxTargetData";
            this.checkedListBoxTargetData.Size = new System.Drawing.Size(185, 228);
            this.checkedListBoxTargetData.TabIndex = 7;
            // 
            // btnSelectTargetData
            // 
            this.btnSelectTargetData.Location = new System.Drawing.Point(544, 174);
            this.btnSelectTargetData.Name = "btnSelectTargetData";
            this.btnSelectTargetData.Size = new System.Drawing.Size(131, 62);
            this.btnSelectTargetData.TabIndex = 8;
            this.btnSelectTargetData.Text = "대상선택";
            this.btnSelectTargetData.UseVisualStyleBackColor = true;
            this.btnSelectTargetData.Click += new System.EventHandler(this.btnSelectTargetData_Click);
            // 
            // btn_order_sync
            // 
            this.btn_order_sync.Location = new System.Drawing.Point(544, 263);
            this.btn_order_sync.Name = "btn_order_sync";
            this.btn_order_sync.Size = new System.Drawing.Size(160, 71);
            this.btn_order_sync.TabIndex = 9;
            this.btn_order_sync.Text = "button2";
            this.btn_order_sync.UseVisualStyleBackColor = true;
            this.btn_order_sync.Click += new System.EventHandler(this.btn_order_sync_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btn_order_sync);
            this.Controls.Add(this.btnSelectTargetData);
            this.Controls.Add(this.checkedListBoxTargetData);
            this.Controls.Add(this.btnSetSyncPeriod);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBoxSyncInterval);
            this.Controls.Add(this.progressBarSync);
            this.Controls.Add(this.lstSyncLog);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.button1);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ListBox lstSyncLog;
        private System.Windows.Forms.ProgressBar progressBarSync;
        private System.Windows.Forms.ComboBox comboBoxSyncInterval;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSetSyncPeriod;
        private System.Windows.Forms.CheckedListBox checkedListBoxTargetData;
        private System.Windows.Forms.Button btnSelectTargetData;
        private System.Windows.Forms.Button btn_order_sync;
    }
}

