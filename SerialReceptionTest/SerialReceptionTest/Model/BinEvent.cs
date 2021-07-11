using Fl.Net.Message;
using System;

namespace UsartReceptionTest.Model
{
    public class BinEvent : BinMessageBase
    {
        public Int64 Id { get; set; }
        public string TestName { get; set; }
        public DateTime ReceiveTime { get; set; }

        public BinEvent()
        {
        }

        public BinEvent(FlBinMessageEvent flEvent) : base(flEvent)
        {
            if (flEvent != null)
            {
                ReceiveTime = flEvent.ReceiveTime;
            }
        }
    }
}
