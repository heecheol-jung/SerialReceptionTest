using Fl.Net;
using Fl.Net.Message;
using Fw.Net;
using LiteDB;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using UsartReceptionTest.Model;

namespace UsartReceptionTest
{
    public class Mpu6050ReceptionTestManager
    {
        LiteDatabase _db;
        uint _deviceId = 1;
        byte _seqNum = 0;
        FlMessageManager _flMsgMgr = new FlMessageManager();
        MessageManagerSetting _msgMgrSetting = new MessageManagerSetting()
        {
            SerialPortSetting = new SerialPortSetting()
            {
                PortName = "COM9",
                //BaudRate = 115200 * 16,
                BaudRate = 115200 * 10,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One
            }
        };
        ILiteCollection<BinCommandResult> _cmdResultCollecction;
        ILiteCollection<BinEvent> _eventCollection;
        ILiteCollection<LastTestNumber> _lastTestNumCollection;
        ConcurrentQueue<IFlMessage> _agEventQ = new ConcurrentQueue<IFlMessage>();
        bool _litedbLoop = false;
        Thread _litedbThread = null;
        DateTime _lastAgEvtPrintTime = DateTime.MinValue;
        UInt64 _agEvtCount = 0;
        DateTime _lastAgLitedbPrintTime = DateTime.MinValue;
        UInt64 _agLitedbCount = 0;
        UInt64 _lastTestNum = 0;
        bool _isAccelGyroEventStarted = false;
        string _dbFileName = AppConstant.STR_LITEDB_FILENAME;

        public Mpu6050ReceptionTestManager()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                _dbFileName = AppConstant.STR_LINUX_LITEDB_FILENAME;
            }

            _db = new LiteDatabase(_dbFileName);
        }

        ~Mpu6050ReceptionTestManager()
        {
            if (_db != null)
            {
                _db.Commit();
                _db.Dispose();
                Console.WriteLine("Commit done.");
                _db = null;
            }
        }

        public void Start()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                _msgMgrSetting.SerialPortSetting.PortName = "/dev/ttyACM1";
                Log.Debug("Linux OS");
            }

            _msgMgrSetting.MessageType = FwMessageType.Binary;
            if (_db == null)
            {
                _db = new LiteDatabase(_dbFileName);
            }
            _cmdResultCollecction = _db.GetCollection<BinCommandResult>(AppConstant.STR_COMMAND_RESULT_COL_NAME);
            _eventCollection = _db.GetCollection<BinEvent>(AppConstant.STR_ACCELGYRO_EVT_COL_NAME);
            _lastTestNumCollection = _db.GetCollection<LastTestNumber>(AppConstant.STR_LAST_TEST_NUM_COL_NAME);

            var lastTestNums = _lastTestNumCollection.FindAll().ToList();
            if (lastTestNums?.Count == 1)
            {
                _lastTestNum = lastTestNums[0].Number + 1;
                lastTestNums[0].Number = _lastTestNum;
                _lastTestNumCollection.Update(lastTestNums[0]);

                Log.Debug($"Last test number(update) : {_lastTestNum}");
            }
            else
            {
                _lastTestNum = 1;
                LastTestNumber lastTestNum = new LastTestNumber()
                {
                    Number = _lastTestNum
                };
                _lastTestNumCollection.Insert(lastTestNum);
                Log.Debug($"Last test number(insert) : {_lastTestNum}");
            }

            if (_flMsgMgr.StartStatus != FwStartStatus.Stopped)
            {
                Console.WriteLine("Message manager is not stopped");
                return;
            }

            if (_flMsgMgr.OnCommandResultReady == null)
            {
                _flMsgMgr.OnCommandResultReady = OnCommandResultReady;
            }
            if (_flMsgMgr.OnEventMessageReady == null)
            {
                _flMsgMgr.OnEventMessageReady = OnEventMessageReady;
            }

            _agEvtCount = 0;
            _lastAgEvtPrintTime = DateTime.MinValue;

            LitedbWriteStart();

            _flMsgMgr.Start(_msgMgrSetting);
        }

        public void Stop()
        {
            if (_flMsgMgr.StartStatus == FwStartStatus.Started)
            {
                _flMsgMgr.Stop();
            }

            LitedbWriteStop();
        }

        public void Mpu6050EventStart()
        {
            if (CommandEmpty() != true)
            {
                return;
            }

            if (_flMsgMgr.GetCommandCount() > 0)
            {
                Log.Warning("Prevous command is NOT DONE.");
                return;
            }

            _agEvtCount = 0;
            _lastAgEvtPrintTime = DateTime.MinValue;
            IFlBinMessage binMsg = new FlBinMessageCommand()
            {
                MaxTryCount = 1,
                ResponseWaitTimeout = 200,
                TryInterval = 300,
                MessageId = FlMessageId.StartAccelGyro,
                Arguments = new List<object>()
                {
                    (byte)1,
                    (byte)FlConstant.FL_MSG_ACCELGYRO_START
                }
            };
            binMsg.Header.device_id = _deviceId;
            binMsg.Header.flag1.sequence_num = GetNextSeqNum();

            IFlMessage message = binMsg;
            FlBinPacketBuilder.BuildMessagePacket(ref message);

            _flMsgMgr.EnqueueCommand(message);
        }

        public void Mpu6050EventStop()
        {
            if (CommandEmpty() != true)
            {
                return;
            }

            IFlBinMessage binMsg = new FlBinMessageCommand()
            {
                MaxTryCount = 2,
                ResponseWaitTimeout = 500,
                TryInterval = 1000,
                MessageId = FlMessageId.StartAccelGyro,
                Arguments = new List<object>()
                {
                    (byte)1,
                    (byte)FlConstant.FL_MSG_ACCELGYRO_STOP
                }
            };
            binMsg.Header.device_id = _deviceId;
            binMsg.Header.flag1.sequence_num = GetNextSeqNum();

            IFlMessage message = binMsg;
            FlBinPacketBuilder.BuildMessagePacket(ref message);

            _flMsgMgr.EnqueueCommand(message);
        }

        public bool IsAccelGyroEventStarted()
        {
            return _isAccelGyroEventStarted;
        }

        public void Commit()
        {
            if (_db != null)
            {
                _db.Commit();
                _db.Dispose();
                Console.WriteLine("Commit done.");
                _db = null;
            }
        }

        public void PrintLastTestInfo()
        {
            UInt64 lastTestNum = GetLastTestNum();

            if (lastTestNum > 0)
            {
                string strLastTestNum = lastTestNum.ToString();
                var eventList = _eventCollection.Find(evt => evt.TestName == strLastTestNum).ToArray();

                double minSpan = double.MaxValue;
                double maxSpan = double.MinValue;
                TimeSpan span;
                double milSec = 0.0;
                double totalMilSec = 0.0;

                for (int i = (eventList.Length-1); i > 1; i--)
                {
                    span = eventList[i].ReceiveTime - eventList[i - 1].ReceiveTime;
                    milSec = span.TotalMilliseconds;
                    totalMilSec += milSec;
                    if (milSec < minSpan)
                    {
                        minSpan = milSec;
                    }
                    if (milSec > maxSpan)
                    {
                        maxSpan = milSec;
                    }
                }

                Console.WriteLine($"Min interval : {(uint)minSpan}, Max interval : {(uint)maxSpan}, Average : {totalMilSec / (eventList.Length - 1)}");
            }
            else
            {
                Console.WriteLine("N events");
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

        private void LitedbWriteStop()
        {
            int count = 0;

            while (_agEventQ?.Count > 0)
            {
                Thread.Sleep(50);

                count++;
                if (count >= 3)
                {
                    break;
                }
            }

            _litedbLoop = false;
            if (_litedbThread != null)
            {
                if (_litedbThread.IsAlive)
                {
                    _litedbThread.Join();
                }
                _litedbThread = null;
            }

            StatusReport();
        }

        private void LitedbWriteStart()
        {
            LitedbWriteStop();

            _agLitedbCount = 0;
            _lastAgLitedbPrintTime = DateTime.MinValue;
            _litedbLoop = true;
            _litedbThread = new Thread(new ThreadStart(LitedbProc));
            _litedbThread.Start();
        }

        private void StatusReport()
        {
            Log.Debug($"Event count : {_agEvtCount}, LiteDB write count : {_agLitedbCount}");
        }

        public byte GetNextSeqNum()
        {
            _seqNum++;
            if (_seqNum > FlConstant.FL_BIN_MSG_MAX_SEQUENCE)
            {
                _seqNum = 1;
            }

            return _seqNum;
        }

        private bool CommandEmpty()
        {
            int respCount = _flMsgMgr.GetResponseCount();

            if (respCount > 0)
            {
                Log.Debug($"Response count : {respCount}");
            }

            if (_flMsgMgr.GetCommandCount() > 0)
            {
                Log.Warning("Previous command is NOT DONE.");
                return false;
            }

            return true;
        }

        private void OnEventMessageReady(IFlMessage evt)
        {
            if (evt.MessageId == FlMessageId.AccelGyroEvent)
            {
                _agEvtCount++;

                _agEventQ.Enqueue(evt);

                if (_lastAgEvtPrintTime == DateTime.MinValue)
                {
                    _lastAgEvtPrintTime = DateTime.UtcNow;
                }
                else
                {
                    TimeSpan span = DateTime.UtcNow - _lastAgEvtPrintTime;
                    if (span.TotalSeconds >= 2)
                    {
                        Log.Debug($"Accel/Gyro event count : {_agEvtCount}");
                        _lastAgEvtPrintTime = DateTime.UtcNow;
                    }
                }

                //string str = $"Sensor num : {sensorNum}, Sample count : {sampleCount}\r\n";
                //foreach (var item in rawAccelGyroList)
                //{
                //    str += $"Ax : {item.Ax}, Ay : {item.Ay}, Az : {item.Az}, Gx : {item.Gx}, Gy : {item.Gy}, Gz : {item.Gz}";
                //}
                //Log.Debug(str);
            }
        }

        private void OnCommandResultReady(FwCommandMessageResult cmdResult)
        {
            string strDisplay = cmdResult.Command.MessageId.ToString() + " : ";

            if (cmdResult.Command.MessageId == FlMessageId.StartAccelGyro)
            {
                DateTime now = DateTime.UtcNow;
                BinCommandResult binCmdResult = new BinCommandResult()
                {
                    Command = new BinCommand((FlBinMessageCommand)cmdResult.Command),
                    Response = new BinResponse((FlBinMessageResponse)cmdResult.Response),
                    CreatedDate = cmdResult.CreatedDate,
                    LogName = $"{now.Year}{now.Month.ToString("00")}{now.Day.ToString("00")} {now.Hour.ToString("00")}:{now.Minute.ToString("00")}:{now.Second.ToString("00")}.{now.Millisecond.ToString("000")}"
                };
                _cmdResultCollecction.Insert(binCmdResult);

                if (cmdResult.Response != null)
                {
                    byte startStop = (byte)cmdResult.Command.Arguments[1];

                    FlBinMessageResponse resp = (FlBinMessageResponse)cmdResult.Response;

                    if (startStop == FlConstant.FL_MSG_ACCELGYRO_START)
                    {
                        if (cmdResult.Response != null)
                        {
                            _isAccelGyroEventStarted = true;
                            strDisplay += "Started";
                            var startTime = binCmdResult.Response.ReceiveTime.ToString("yyyy-MM-ddTHH:mm:ssz");
                            Log.Debug($"Start time : {startTime}");
                        }
                        else
                        {
                            strDisplay += "No response";
                        }
                    }
                    else if (startStop == FlConstant.FL_MSG_ACCELGYRO_STOP)
                    {
                        if (cmdResult.Response != null)
                        {
                            _isAccelGyroEventStarted = false;
                            LitedbWriteStop();
                            strDisplay += "Stopped";
                            var stopTime = binCmdResult.Response.ReceiveTime.ToString("yyyy-MM-ddTHH:mm:ssz");
                            Log.Debug($"Stop time : {stopTime}");
                        }
                        else
                        {
                            strDisplay += "No response";
                        }
                    }
                }
                else
                {
                    strDisplay += "No response";
                }
            }

            Log.Debug(strDisplay);
        }

        private void LitedbProc()
        {
            List<BinEvent> events = new List<BinEvent>();

            while (_litedbLoop)
            {
                if (_agEventQ?.Count <= 0)
                {
                    continue;
                }

                if (_agEventQ.TryDequeue(out IFlMessage evt))
                {
                    _agLitedbCount++;
                    BinEvent binEvt = new BinEvent((FlBinMessageEvent)evt);
                    binEvt.TestName = _lastTestNum.ToString();
                    events.Add(binEvt);
                }

                if (events.Count > 10)
                {
                    _eventCollection.Insert(events);
                    events.Clear();
                }

                if (_lastAgLitedbPrintTime == DateTime.MinValue)
                {
                    _lastAgLitedbPrintTime = DateTime.UtcNow;
                }
                else
                {
                    TimeSpan span = DateTime.UtcNow - _lastAgLitedbPrintTime;
                    if (span.TotalSeconds >= 2)
                    {
                        Log.Debug($"Accel/Gyro influx send count : {_agLitedbCount}");
                        _lastAgLitedbPrintTime = DateTime.UtcNow;
                    }
                }
            }
        }
    }
}

