using SyncScheduleManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sync_WMSData
{
    public class CombinedTaskSchedule
    {
        public SyncTask Task { get; set; }
        public SyncSchedule Schedule { get; set; }
    }
}
