using LiteDB;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using SerialReceptionDataViewer.Model;

namespace SerialReceptionDataViewer.AppWnd
{
    /// <summary>
    /// Interaction logic for WndSerialReceptionDataViewer.xaml
    /// </summary>
    public partial class WndSerialReceptionDataViewer : Window
    {
        LiteDatabase _db;
        ILiteCollection<BinEvent> _eventCollection;
        ILiteCollection<LastTestNumber> _lastTestNumCollection;

        public WndSerialReceptionDataViewer()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_db != null)
            {
                _db.Dispose();
                _db = null;
            }
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "LiteDB files (*.db)|*.db|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                _db = new LiteDatabase(openFileDialog.FileName);
                _eventCollection = _db.GetCollection<BinEvent>(AppConstant.STR_ACCELGYRO_EVT_COL_NAME);
                _lastTestNumCollection = _db.GetCollection<LastTestNumber>(AppConstant.STR_LAST_TEST_NUM_COL_NAME);

                UInt64 lastTestNum = GetLastTestNum();

                LbTests.Items.Clear();
                for (ulong i = 0; i < lastTestNum; i++)
                {
                    LbTests.Items.Add(i + 1);
                }
            }
        }

        private void LbTests_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            wpfPlot1.Plot.Clear();

            if (LbTests.SelectedIndex >= 0)
            {
                string strLastTestNum = LbTests.SelectedItem.ToString();
                var eventList = _eventCollection.Find(evt => evt.TestName == strLastTestNum).ToArray();

                if (eventList?.Length > 0)
                {
                    TbCount.Text = "" + eventList.Length;

                    TimeSpan span;
                    double[] intervals = new double[eventList.Length - 1];
                    int idx = 0;

                    for (int i = (eventList.Length - 1); i > 1; i--)
                    {
                        span = eventList[i].ReceiveTime - eventList[i - 1].ReceiveTime;
                        intervals[idx++] = span.TotalMilliseconds;
                    }

                    wpfPlot1.Plot.AddSignal(intervals);
                }
                else
                {
                    TbCount.Text = "0";
                }                
            }
        }

        private ulong GetLastTestNum()
        {
            var lastTestNums = _lastTestNumCollection.FindAll().ToList();
            if (lastTestNums?.Count == 1)
            {
                return lastTestNums[0].Number;
            }

            return 0;
        }
    }
}
