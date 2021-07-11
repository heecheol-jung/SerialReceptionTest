using Fl.Net.Message;
using System;

namespace SerialReceptionDataViewer.Model
{
    public class BinResponse : BinMessageBase
    {
        public DateTime ReceiveTime { get; set; }

        public BinResponse()
        {
        }

        public BinResponse(FlBinMessageResponse flResponse) : base(flResponse)
        {
            if (flResponse != null)
            {
                ReceiveTime = flResponse.ReceiveTime;
            }
        }
    }
}
