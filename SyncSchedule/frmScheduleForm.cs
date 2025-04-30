using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SyncScheduleManager
{
    public partial class frmScheduleForm : Form
    {
        //public frmScheduleForm()
        //{
        //    InitializeComponent();
        //}
        private ComboBox cboScheduleType;
        private DateTimePicker dtpSpecificTime;
        private NumericUpDown numInterval;
        private CheckedListBox clbWeekDays;
        private Button btnSave, btnLoad;
        
        public frmScheduleForm()
        {
            InitializeComponent();

            cboScheduleType = new ComboBox() { Top = 20, Left = 20, Width = 200 };
            cboScheduleType.Items.AddRange(new string[] { "한 번 수행", "되풀이 수행", "일별 수행", "주별 수행" });
            cboScheduleType.SelectedIndexChanged += CboScheduleType_SelectedIndexChanged;

            dtpSpecificTime = new DateTimePicker() { Top = 60, Left = 20, Width = 200, Format = DateTimePickerFormat.Time, ShowUpDown = true };

            numInterval = new NumericUpDown() { Top = 100, Left = 20, Width = 200, Minimum = 1, Maximum = 1440, Value = 60 }; // 분 단위
            Label lblInterval = new Label() { Text = "주기 (분 단위)", Top = 80, Left = 20, Width = 200 };

            clbWeekDays = new CheckedListBox() { Top = 140, Left = 20, Width = 200, Height = 100 };
            clbWeekDays.Items.AddRange(Enum.GetNames(typeof(DayOfWeek)));

            btnSave = new Button() { Text = "저장", Top = 260, Left = 20, Width = 200 };
            btnSave.Click += BtnSave_Click;
            btnLoad = new Button() { Text = "스케줄 불러오기", Top = 300, Left = 20, Width = 200 };
            btnLoad.Click += BtnLoad_Click;

            Controls.Add(cboScheduleType);
            Controls.Add(dtpSpecificTime);
            Controls.Add(numInterval);
            Controls.Add(lblInterval);
            Controls.Add(clbWeekDays);
            Controls.Add(btnSave);
            Controls.Add(btnLoad);

            SetControlVisibility(false, false, false); // 초기에는 모두 숨김
        }

        // 스케줄 타입에 따라 필요한 컨트롤 보이기/숨기기
        private void CboScheduleType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedType = cboScheduleType.SelectedItem.ToString();

            switch (selectedType)
            {
                case "한 번 수행":
                    SetControlVisibility(true, false, false);
                    break;
                case "되풀이 수행":
                    SetControlVisibility(false, true, false);
                    break;
                case "일별 수행":
                    SetControlVisibility(true, false, false);
                    break;
                case "주별 수행":
                    SetControlVisibility(false, false, true);
                    break;
            }
        }

        private void SetControlVisibility(bool showSpecificTime, bool showInterval, bool showWeekDays)
        {
            dtpSpecificTime.Visible = showSpecificTime;
            numInterval.Visible = showInterval;
            clbWeekDays.Visible = showWeekDays;
        }

        // 설정 저장 버튼 클릭 시 처리
        private void BtnSave_Click(object sender, EventArgs e)
        {
            string selectedType = cboScheduleType.SelectedItem.ToString();
            SyncSchedule schedule = new SyncSchedule();

            switch (selectedType)
            {
                case "한 번 수행":
                    schedule.ScheduleType = "OneTime";
                    schedule.SpecificTime = dtpSpecificTime.Value;
                    break;
                case "되풀이 수행":
                    schedule.ScheduleType = "Recurring";
                    schedule.Interval = TimeSpan.FromMinutes((double)numInterval.Value);
                    break;
                case "일별 수행":
                    schedule.ScheduleType = "Daily";
                    schedule.SpecificTime = dtpSpecificTime.Value;
                    break;
                case "주별 수행":
                    schedule.ScheduleType = "Weekly";
                    schedule.WeekDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), clbWeekDays.CheckedItems[0].ToString()); // 하나의 요일만 선택한다고 가정
                    schedule.SpecificTime = dtpSpecificTime.Value;
                    break;
            }

            // schedule 데이터를 파일이나 DB에 저장하는 로직 추가
            SaveSchedule(schedule);
        }

        // 스케줄 불러오기 버튼 클릭 시 처리
        private void BtnLoad_Click(object sender, EventArgs e)
        {
            SyncSchedule loadedSchedule = ScheduleFileManager.LoadSchedule();
            if (loadedSchedule != null)
            {
                // 불러온 스케줄 데이터를 폼에 적용
                cboScheduleType.SelectedItem = GetScheduleTypeDisplayName(loadedSchedule.ScheduleType);
                if (loadedSchedule.ScheduleType == "OneTime" || loadedSchedule.ScheduleType == "Daily")
                {
                    dtpSpecificTime.Value = loadedSchedule.SpecificTime.Value;
                }
                else if (loadedSchedule.ScheduleType == "Recurring")
                {
                    numInterval.Value = (decimal)loadedSchedule.Interval.Value.TotalMinutes;
                }
                else if (loadedSchedule.ScheduleType == "Weekly")
                {
                    clbWeekDays.SetItemChecked((int)loadedSchedule.WeekDay.Value, true);
                    dtpSpecificTime.Value = loadedSchedule.SpecificTime.Value;
                }

                MessageBox.Show("스케줄이 불러와졌습니다.");
            }
            else
            {
                MessageBox.Show("저장된 스케줄이 없습니다.");
            }
        }
        // 스케줄 타입에 대한 표시 이름 반환
        private string GetScheduleTypeDisplayName(string scheduleType)
        {
            switch (scheduleType)
            {
                case "OneTime": return "한 번 수행";
                case "Recurring": return "되풀이 수행";
                case "Daily": return "일별 수행";
                case "Weekly": return "주별 수행";
                default: return string.Empty;
            }
        }
        private void SaveSchedule(SyncSchedule schedule)
        {
            ScheduleFileManager.SaveSchedule(schedule);
            // 스케줄 데이터를 저장하는 로직 (파일 또는 데이터베이스)
            MessageBox.Show($"스케줄이 저장되었습니다: {schedule.ScheduleType}");
        }
    }
}
