﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncCommon
{
    public class SyncTaskJob
    {
        public int TaskId { get; set; }                  // 작업 단위 ID
        public string TaskName { get; set; }             // 작업 단위 명칭
        public List<string> ReferenceTables { get; set; } // 참조 테이블 목록 (여러 개일 수 있음)
        public string TargetTable { get; set; }             // 작업 단위 명칭
        public string ProcedureName { get; set; }        // 호출할 프로시저명
        public string SourceDB { get; set; }             // 원본 DB
        public string TargetDB { get; set; }             // 대상 DB
        public string SyncDirection { get; set; }        // 동기화 방향 (DB1 -> DB2, DB2 -> DB1, 양방향)
        public bool IsActive { get; set; }               // 작업 활성화 여부
        
        public string ScheduleType { get; set; }    // 스케줄 타입 (OneTime, Recurring, Daily, Weekly)
        
    }
    /*
     * int taskId = context.MergedJobDataMap.GetInt("TaskId");
               string taskName = context.MergedJobDataMap.GetString("TaskName");
               string scheduleType = context.MergedJobDataMap.GetString("ScheduleType");
               string sourceDB = context.MergedJobDataMap.GetString("SourceDB");
               string targetDB = context.MergedJobDataMap.GetString("TargetDB");
               string SyncDirection = context.MergedJobDataMap.GetString("SyncDirection");

               string procedureName = context.MergedJobDataMap.GetString("ProcedureName");
               string referenceTables = context.MergedJobDataMap.GetString("ReferenceTables");


     * */
}
