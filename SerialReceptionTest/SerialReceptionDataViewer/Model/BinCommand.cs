using Fl.Net.Message;
using System;
using System.Collections.Generic;

namespace SerialReceptionDataViewer.Model
{
    public class BinCommand : BinMessageBase
    {
        public int MaxTryCount { get; set; }
        public int TryCount { get; set; }
        public int TryInterval { get; set; }
        public int ResponseWaitTimeout { get; set; }
        public List<DateTime> SendTimeHistory { get; set; }

        public BinCommand()
        {
        }

        public BinCommand(FlBinMessageCommand flComand) : base(flComand)
        {
            if (flComand != null)
            {
                MaxTryCount = flComand.MaxTryCount;
                TryCount = flComand.TryCount;
                TryInterval = flComand.TryInterval;
                ResponseWaitTimeout = flComand.ResponseWaitTimeout;
                SendTimeHistory = flComand.SendTimeHistory;
            }
        }
    }
}
