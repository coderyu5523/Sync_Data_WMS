using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Forms;
using System.Timers;
using SyncCommon;
namespace SyncLibrary
{

    public class SyncManager
    {
        private readonly Timer _syncTimer;
        private readonly List<string> _selectedTargetData;
        //private readonly DataSyncLogProcessor _dataSyncLogProcessor;

        private IDataSyncProcessor _dataSyncProcessor;
        private readonly Logger _logger; // Logger 인스턴스 추가

        // 진행 상태 및 로그 업데이트를 위한 이벤트
        public event Action<string> StatusUpdated;
        public event Action<string> LogUpdated;

        public SyncManager(IDataSyncProcessor dataSyncLogProcessor, Logger logger)
        {
            _dataSyncProcessor = dataSyncLogProcessor;
            _logger = logger; // Logger 인스턴스 초기화
            _selectedTargetData = new List<string>();

            // Timer 초기화 및 Tick 이벤트 설정
            _syncTimer = new Timer();
            _syncTimer.Elapsed += OnTimerElapsed;
        }
        public void SetDataSyncProcessor(IDataSyncProcessor dataSyncProcessor)
        {
            _dataSyncProcessor = dataSyncProcessor; // 선택된 동기화 프로세서 설정
        }

        public void StartSync()
        {
            // 동기화 작업을 시작하는 메서드
            try
            {
                UpdateLog("동기화 작업이 시작되었습니다.");
                _logger.LogOperation("동기화 작업이 시작되었습니다.");

                // 비동기 동기화 작업을 동기적으로 대기
                _dataSyncProcessor.ProcessLogsAsync().Wait();

                // 동기화 작업이 성공적으로 완료된 경우
                UpdateLog("동기화 작업이 완료되었습니다.");
                _logger.LogOperation("동기화 작업이 완료되었습니다.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"동기화 작업 중 오류 발생: {ex.Message}");
                UpdateLog($"동기화 작업 중 오류 발생: {ex.Message}");
            }
        }

        public void SetSyncInterval(string interval)
        {
            switch (interval)
            {
                case "즉시":
                    _syncTimer.Stop(); // '즉시'는 타이머가 필요하지 않음, 수동으로 동기화 시작
                    break;
                case "5분":
                    _syncTimer.Interval = 5 * 60 * 1000; // 5분마다
                    _syncTimer.Start();
                    break;
                case "30분":
                    _syncTimer.Interval = 30 * 60 * 1000; // 30분마다
                    _syncTimer.Start();
                    break;
                case "1시간":
                    _syncTimer.Interval = 60 * 60 * 1000; // 1시간마다
                    _syncTimer.Start();
                    break;
                case "매일":
                    _syncTimer.Interval = 24 * 60 * 60 * 1000; // 하루마다
                    _syncTimer.Start();
                    break;
                default:
                    _syncTimer.Stop(); // 기본적으로 타이머 중지
                    break;
            }

            // 로그 기록
            _logger.LogOperation($"동기화 주기가 '{interval}'로 설정되었습니다.");
            UpdateLog($"동기화 주기가 '{interval}'로 설정되었습니다.");
        }

        //public void SetTargetData(CheckedListBox.CheckedItemCollection selectedItems)
        //{
        //    _selectedTargetData.Clear();

        //    foreach (var item in selectedItems)
        //    {
        //        _selectedTargetData.Add(item.ToString());
        //    }

        //    // 로그 기록
        //    _logger.LogOperation($"선택된 동기화 대상 데이터: {string.Join(", ", _selectedTargetData)}");
        //    UpdateLog($"선택된 동기화 대상 데이터: {string.Join(", ", _selectedTargetData)}");
        //}

        public void UpdateStatus(string message)
        {
            StatusUpdated?.Invoke(message);
        }

        private void UpdateLog(string message)
        {
            LogUpdated?.Invoke(message);
        }

        private void OnTimerElapsed(object sender, EventArgs e)
        {
            try
            {
                //_dataSyncLogProcessor.Batch_DataGet();
                _dataSyncProcessor.ProcessLogsAsync().Wait();
                UpdateLog("주기적 동기화 작업이 시작되었습니다.");
                _logger.LogOperation("주기적 동기화 작업이 시작되었습니다.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"동기화 작업 중 오류 발생: {ex.Message}");
                UpdateLog($"동기화 작업 중 오류 발생: {ex.Message}");
            }
        }
    }

}
