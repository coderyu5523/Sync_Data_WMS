using System.Windows.Forms;

namespace SyncScheduleManager
{
    partial class frmScheduleForm
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.dgvTasks = new System.Windows.Forms.DataGridView();
            this.label3 = new System.Windows.Forms.Label();
            this.pnl_re = new System.Windows.Forms.Panel();
            this.lblInterval1 = new System.Windows.Forms.Label();
            this.numInterval1 = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.dtsrtdt = new System.Windows.Forms.DateTimePicker();
            this.pnl_one = new System.Windows.Forms.Panel();
            this.lblSpecificTime1 = new System.Windows.Forms.Label();
            this.dtpSpecificTime1 = new System.Windows.Forms.DateTimePicker();
            this.cboScheduleType1 = new System.Windows.Forms.ComboBox();
            this.lblTaskId = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btnSave1 = new System.Windows.Forms.Button();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnSaveTasks = new System.Windows.Forms.Button();
            this.cboSourceDB = new System.Windows.Forms.ComboBox();
            this.btnLoadTasks = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.TaskId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TaskName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SourceDB = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.TargetDB = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.ReferenceTables = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProcedureName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SyncDirection = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.IsActive = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTasks)).BeginInit();
            this.pnl_re.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numInterval1)).BeginInit();
            this.pnl_one.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupBox2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.groupBox1);
            this.splitContainer1.Size = new System.Drawing.Size(1214, 479);
            this.splitContainer1.SplitterDistance = 964;
            this.splitContainer1.TabIndex = 0;
            // 
            // dgvTasks
            // 
            this.dgvTasks.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTasks.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.TaskId,
            this.TaskName,
            this.SourceDB,
            this.TargetDB,
            this.ReferenceTables,
            this.ProcedureName,
            this.SyncDirection,
            this.IsActive});
            this.dgvTasks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTasks.Location = new System.Drawing.Point(3, 17);
            this.dgvTasks.Name = "dgvTasks";
            this.dgvTasks.RowTemplate.Height = 23;
            this.dgvTasks.Size = new System.Drawing.Size(958, 459);
            this.dgvTasks.TabIndex = 0;
            this.dgvTasks.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvTasks_CellClick);
            this.dgvTasks.DefaultValuesNeeded += new System.Windows.Forms.DataGridViewRowEventHandler(this.dgvTasks_DefaultValuesNeeded);
            this.dgvTasks.RowEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvTasks_RowEnter);
            this.dgvTasks.SelectionChanged += new System.EventHandler(this.dgvTasks_SelectionChanged_1);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(123, 32);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(17, 12);
            this.label3.TabIndex = 11;
            this.label3.Text = "번";
            // 
            // pnl_re
            // 
            this.pnl_re.Controls.Add(this.lblInterval1);
            this.pnl_re.Controls.Add(this.numInterval1);
            this.pnl_re.Location = new System.Drawing.Point(17, 149);
            this.pnl_re.Name = "pnl_re";
            this.pnl_re.Size = new System.Drawing.Size(145, 70);
            this.pnl_re.TabIndex = 10;
            // 
            // lblInterval1
            // 
            this.lblInterval1.AutoSize = true;
            this.lblInterval1.Location = new System.Drawing.Point(3, 15);
            this.lblInterval1.Name = "lblInterval1";
            this.lblInterval1.Size = new System.Drawing.Size(95, 12);
            this.lblInterval1.TabIndex = 6;
            this.lblInterval1.Text = "되풀이 (분 단위)";
            // 
            // numInterval1
            // 
            this.numInterval1.Location = new System.Drawing.Point(6, 41);
            this.numInterval1.Maximum = new decimal(new int[] {
            1440,
            0,
            0,
            0});
            this.numInterval1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numInterval1.Name = "numInterval1";
            this.numInterval1.Size = new System.Drawing.Size(121, 21);
            this.numInterval1.TabIndex = 5;
            this.numInterval1.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(21, 94);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 12);
            this.label2.TabIndex = 9;
            this.label2.Text = "시작 날짜";
            // 
            // dtsrtdt
            // 
            this.dtsrtdt.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtsrtdt.Location = new System.Drawing.Point(22, 112);
            this.dtsrtdt.Name = "dtsrtdt";
            this.dtsrtdt.ShowUpDown = true;
            this.dtsrtdt.Size = new System.Drawing.Size(121, 21);
            this.dtsrtdt.TabIndex = 8;
            // 
            // pnl_one
            // 
            this.pnl_one.Controls.Add(this.lblSpecificTime1);
            this.pnl_one.Controls.Add(this.dtpSpecificTime1);
            this.pnl_one.Location = new System.Drawing.Point(17, 149);
            this.pnl_one.Name = "pnl_one";
            this.pnl_one.Size = new System.Drawing.Size(145, 70);
            this.pnl_one.TabIndex = 7;
            // 
            // lblSpecificTime1
            // 
            this.lblSpecificTime1.AutoSize = true;
            this.lblSpecificTime1.Location = new System.Drawing.Point(3, 14);
            this.lblSpecificTime1.Name = "lblSpecificTime1";
            this.lblSpecificTime1.Size = new System.Drawing.Size(61, 12);
            this.lblSpecificTime1.TabIndex = 6;
            this.lblSpecificTime1.Text = "한 번 수행";
            // 
            // dtpSpecificTime1
            // 
            this.dtpSpecificTime1.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpSpecificTime1.Location = new System.Drawing.Point(2, 42);
            this.dtpSpecificTime1.Name = "dtpSpecificTime1";
            this.dtpSpecificTime1.ShowUpDown = true;
            this.dtpSpecificTime1.Size = new System.Drawing.Size(121, 21);
            this.dtpSpecificTime1.TabIndex = 4;
            // 
            // cboScheduleType1
            // 
            this.cboScheduleType1.FormattingEnabled = true;
            this.cboScheduleType1.Items.AddRange(new object[] {
            "되풀이 수행",
            "일별 수행"});
            this.cboScheduleType1.Location = new System.Drawing.Point(23, 57);
            this.cboScheduleType1.Name = "cboScheduleType1";
            this.cboScheduleType1.Size = new System.Drawing.Size(121, 20);
            this.cboScheduleType1.TabIndex = 3;
            this.cboScheduleType1.SelectedIndexChanged += new System.EventHandler(this.cboScheduleType1_SelectedIndexChanged);
            // 
            // lblTaskId
            // 
            this.lblTaskId.AutoSize = true;
            this.lblTaskId.Location = new System.Drawing.Point(103, 32);
            this.lblTaskId.Name = "lblTaskId";
            this.lblTaskId.Size = new System.Drawing.Size(17, 12);
            this.lblTaskId.TabIndex = 2;
            this.lblTaskId.Text = "00";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "선택 TaskId";
            // 
            // btnSave1
            // 
            this.btnSave1.Location = new System.Drawing.Point(19, 249);
            this.btnSave1.Name = "btnSave1";
            this.btnSave1.Size = new System.Drawing.Size(145, 36);
            this.btnSave1.TabIndex = 0;
            this.btnSave1.Text = "일정 저장";
            this.btnSave1.UseVisualStyleBackColor = true;
            this.btnSave1.Click += new System.EventHandler(this.btnSave1_Click);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.label4);
            this.splitContainer2.Panel1.Controls.Add(this.btnSaveTasks);
            this.splitContainer2.Panel1.Controls.Add(this.cboSourceDB);
            this.splitContainer2.Panel1.Controls.Add(this.btnLoadTasks);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.statusStrip1);
            this.splitContainer2.Panel2.Controls.Add(this.splitContainer1);
            this.splitContainer2.Size = new System.Drawing.Size(1214, 546);
            this.splitContainer2.SplitterDistance = 63;
            this.splitContainer2.TabIndex = 1;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 457);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1214, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(121, 17);
            this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // btnSaveTasks
            // 
            this.btnSaveTasks.Location = new System.Drawing.Point(314, 12);
            this.btnSaveTasks.Name = "btnSaveTasks";
            this.btnSaveTasks.Size = new System.Drawing.Size(90, 39);
            this.btnSaveTasks.TabIndex = 3;
            this.btnSaveTasks.Text = "저장";
            this.btnSaveTasks.UseVisualStyleBackColor = true;
            this.btnSaveTasks.Click += new System.EventHandler(this.btnSaveTasks_Click);
            // 
            // cboSourceDB
            // 
            this.cboSourceDB.FormattingEnabled = true;
            this.cboSourceDB.Items.AddRange(new object[] {
            "CRM",
            "KR"});
            this.cboSourceDB.Location = new System.Drawing.Point(72, 22);
            this.cboSourceDB.Name = "cboSourceDB";
            this.cboSourceDB.Size = new System.Drawing.Size(121, 20);
            this.cboSourceDB.TabIndex = 2;
            // 
            // btnLoadTasks
            // 
            this.btnLoadTasks.Location = new System.Drawing.Point(215, 11);
            this.btnLoadTasks.Name = "btnLoadTasks";
            this.btnLoadTasks.Size = new System.Drawing.Size(93, 40);
            this.btnLoadTasks.TabIndex = 1;
            this.btnLoadTasks.Text = "조회";
            this.btnLoadTasks.UseVisualStyleBackColor = true;
            this.btnLoadTasks.Click += new System.EventHandler(this.btnLoadTasks_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.btnSave1);
            this.groupBox1.Controls.Add(this.pnl_re);
            this.groupBox1.Controls.Add(this.lblTaskId);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.cboScheduleType1);
            this.groupBox1.Controls.Add(this.dtsrtdt);
            this.groupBox1.Controls.Add(this.pnl_one);
            this.groupBox1.Location = new System.Drawing.Point(15, 14);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(197, 323);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "동기화 일정";
            // 
            // TaskId
            // 
            this.TaskId.HeaderText = "TaskId";
            this.TaskId.Name = "TaskId";
            this.TaskId.Width = 50;
            // 
            // TaskName
            // 
            this.TaskName.HeaderText = "작업명칭";
            this.TaskName.Name = "TaskName";
            this.TaskName.Width = 200;
            // 
            // SourceDB
            // 
            this.SourceDB.HeaderText = "원본DB";
            this.SourceDB.Items.AddRange(new object[] {
            "CRM",
            "KR"});
            this.SourceDB.Name = "SourceDB";
            this.SourceDB.Width = 60;
            // 
            // TargetDB
            // 
            this.TargetDB.HeaderText = "대상 DB";
            this.TargetDB.Items.AddRange(new object[] {
            "CRM",
            "KR"});
            this.TargetDB.Name = "TargetDB";
            this.TargetDB.Width = 60;
            // 
            // ReferenceTables
            // 
            this.ReferenceTables.HeaderText = "참조 테이블";
            this.ReferenceTables.Name = "ReferenceTables";
            // 
            // ProcedureName
            // 
            this.ProcedureName.HeaderText = "프로시저명";
            this.ProcedureName.Name = "ProcedureName";
            this.ProcedureName.Width = 150;
            // 
            // SyncDirection
            // 
            this.SyncDirection.HeaderText = "동기화 방향";
            this.SyncDirection.Items.AddRange(new object[] {
            "CTE",
            "ETC"});
            this.SyncDirection.Name = "SyncDirection";
            this.SyncDirection.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.SyncDirection.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.SyncDirection.Width = 120;
            // 
            // IsActive
            // 
            this.IsActive.HeaderText = "활성화 여부";
            this.IsActive.Name = "IsActive";
            this.IsActive.Width = 80;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.dgvTasks);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(964, 479);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Sync Task List";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 30);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(45, 12);
            this.label4.TabIndex = 4;
            this.label4.Text = "원본DB";
            // 
            // frmScheduleForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1214, 546);
            this.Controls.Add(this.splitContainer2);
            this.Name = "frmScheduleForm";
            this.Text = "SyncSchedule Manager";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTasks)).EndInit();
            this.pnl_re.ResumeLayout(false);
            this.pnl_re.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numInterval1)).EndInit();
            this.pnl_one.ResumeLayout(false);
            this.pnl_one.PerformLayout();
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btnSave1;
        private System.Windows.Forms.DataGridView dgvTasks;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.Button btnLoadTasks;
        private System.Windows.Forms.ComboBox cboSourceDB;
        private System.Windows.Forms.Button btnSaveTasks;
        private System.Windows.Forms.Label lblTaskId;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cboScheduleType1;
        private System.Windows.Forms.DateTimePicker dtpSpecificTime1;
        private Label lblInterval1;
        private NumericUpDown numInterval1;
        private Panel pnl_re;
        private Label label2;
        private DateTimePicker dtsrtdt;
        private Panel pnl_one;
        private Label lblSpecificTime1;
        private Label label3;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private DataGridViewTextBoxColumn TaskId;
        private DataGridViewTextBoxColumn TaskName;
        private DataGridViewComboBoxColumn SourceDB;
        private DataGridViewComboBoxColumn TargetDB;
        private DataGridViewTextBoxColumn ReferenceTables;
        private DataGridViewTextBoxColumn ProcedureName;
        private DataGridViewComboBoxColumn SyncDirection;
        private DataGridViewCheckBoxColumn IsActive;
        private Label label4;
    }
}