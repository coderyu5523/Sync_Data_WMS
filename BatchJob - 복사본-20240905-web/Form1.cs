using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DioImplant_batch
{
    public partial class Form1 : Form
    {
        private readonly DataSyncLogProcessor _dataSyncLogProcessor;
        public Form1()
        {
            InitializeComponent();
            //_dataSyncLogProcessor = dataSyncLogProcessor;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //DataSyncLogProcessor bp = new DataSyncLogProcessor();
            //bp.Batch_DataGet();
        }
    }
}
