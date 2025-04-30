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
            this.lblStatus = new System.Windows.Forms.Label();
            this.lstSyncLog = new System.Windows.Forms.ListBox();
            this.btn_order_sync = new System.Windows.Forms.Button();
            this.SuspendLayout();
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
            // btn_order_sync
            // 
            this.btn_order_sync.Location = new System.Drawing.Point(37, 12);
            this.btn_order_sync.Name = "btn_order_sync";
            this.btn_order_sync.Size = new System.Drawing.Size(242, 71);
            this.btn_order_sync.TabIndex = 9;
            this.btn_order_sync.Text = "주문등록";
            this.btn_order_sync.UseVisualStyleBackColor = true;
            this.btn_order_sync.Click += new System.EventHandler(this.btn_order_sync_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btn_order_sync);
            this.Controls.Add(this.lstSyncLog);
            this.Controls.Add(this.lblStatus);
            this.Name = "MainForm";
            this.Text = "Sync_WMS-To-ERP";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ListBox lstSyncLog;
        private System.Windows.Forms.Button btn_order_sync;
    }
}

