using Fl.Net.PInvoke;
using System;
using System.Collections.Generic;

namespace Fl.Net.Message
{
    public class FlBinMessageResponse : IFlBinMessage
    {
        public FlMessageType MessageType => FlMessageType.Binary;

        public FlMessageCategory MessageCategory { get => (FlMessageCategory)_header.flag1.message_type; }

        public FlMessageId MessageId
        {
            get => _header.message_id;
            set => _header.message_id = value;
        }

        private List<object> _arguments = null;
        public List<object> Arguments { get => _arguments; set => _arguments = value; }

        private byte[] _buffer = null;
        public byte[] Buffer { get => _buffer; set => _buffer = value; }

        private DateTime _receiveTime = DateTime.MinValue;
        public DateTime ReceiveTime { get => _receiveTime; set => _receiveTime = value; }

        private FlBinMessageHeader _header = new FlBinMessageHeader()
        {
            device_id = FlConstant.FL_DEVICE_ID_UNKNOWN,
            message_id = (byte)FlMessageId.Unknown,
            length = 0,
            flag1 = new FlBinMsgFlag1Struct()
            {
                message_type = (byte)FlMessageCategory.Response,
                return_expected = 0,
                sequence_num = 0,
                reserved = 0
            },
            flag2 = new FlBinMsgFlag2Struct()
            {
                error = 0,
                reserved = 0
            }
        };
        public FlBinMessageHeader Header { get => _header; set => _header = value; }

        public FlBinMessageResponse()
        {
        }
    }
}
