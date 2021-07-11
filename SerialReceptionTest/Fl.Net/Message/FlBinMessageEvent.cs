using Fl.Net.PInvoke;
using System;
using System.Collections.Generic;

namespace Fl.Net.Message
{
    public class FlBinMessageEvent : IFlBinMessage
    {
        public FlMessageType MessageType => FlMessageType.Binary;

        public FlMessageCategory MessageCategory => FlMessageCategory.Event;

        //private FlMessageId _messageId = FlMessageId.Unknown;
        public FlMessageId MessageId 
        {
            get
            {
                if (_header != null)
                {
                    return _header.message_id;
                }
                else
                {
                    return FlMessageId.Unknown;
                }
            }
            set
            {
                if (_header != null)
                {
                    _header.message_id = value;
                }
            }
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
                message_type = (byte)FlMessageCategory.Event,
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

        public FlBinMessageEvent()
        {
        }
    }
}
