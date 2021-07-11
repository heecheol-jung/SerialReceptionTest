
using Fl.Net.Message;

namespace UsartReceptionTest.Model
{
    public abstract class BinMessageBase : MessageBase
    {
        public byte SequenceNum { get; set; }
        public bool ReturnExpected { get; set; }
        public byte Error { get; set; }

        public BinMessageBase()
        {
        }

        public BinMessageBase(IFlBinMessage flBinMsg) : base(flBinMsg)
        {
            if (flBinMsg != null)
            {
                DeviceId = flBinMsg.Header.device_id;

                SequenceNum = flBinMsg.Header.flag1.sequence_num;
                ReturnExpected = flBinMsg.Header.flag1.return_expected == 0 ? false : true;
                Error = flBinMsg.Header.flag2.error;
            }
        }
    }
}
