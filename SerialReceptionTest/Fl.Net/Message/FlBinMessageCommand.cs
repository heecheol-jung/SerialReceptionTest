using Fl.Net.PInvoke;
using System;
using System.Collections.Generic;

namespace Fl.Net.Message
{
    public class FlBinMessageCommand : IFlBinMessage, IFlMessageCommand
    {
        public FlMessageType MessageType { get => FlMessageType.Binary; }

        public FlMessageCategory MessageCategory { get => (FlMessageCategory)_header.flag1.message_type; }

        //private FlMessageId _messageId = FlMessageId.Unknown;
        public FlMessageId MessageId 
        { 
            get => _header.message_id; 
            set => _header.message_id = value; 
        }

        private List<object> _arguments = null;
        public List<object> Arguments { get => _arguments; set => _arguments = value; }

        private byte[] _buffer = null;
        public byte[] Buffer { get => _buffer; set => _buffer = value; }

        private int _maxTryCount = FlConstant.FL_DEF_CMD_MAX_TRY_COUNT;
        public int MaxTryCount { get => _maxTryCount; set => _maxTryCount = value; }

        private int _tryCount = 0;
        public int TryCount { get => _tryCount; set => _tryCount = value; }

        private int _tryInterval = FlConstant.FL_DEF_CMD_TRY_INTERVAL;
        public int TryInterval { get => _tryInterval; set => _tryInterval = value; }

        private int _responseWaitTimeout = FlConstant.FL_DEF_CMD_RESPONSE_TIMEOUT;
        public int ResponseWaitTimeout { get => _responseWaitTimeout; set => _responseWaitTimeout = value; }

        private bool _responseExpected = true;
        public bool ResponseExpected { get => _responseExpected; set => _responseExpected = value; }

        private List<DateTime> _sendTimeHistory = null;
        public List<DateTime> SendTimeHistory { get => _sendTimeHistory; set => _sendTimeHistory = value; }

        private FlBinMessageHeader _header = new FlBinMessageHeader()
        {
            device_id = FlConstant.FL_DEVICE_ID_UNKNOWN,
            message_id = (byte)FlMessageId.Unknown,
            length = 0,
            flag1 = new FlBinMsgFlag1Struct()
            {
                message_type = (byte)FlMessageCategory.Command,
                return_expected = 1,
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

        public FlBinMessageCommand()
        {
        }
    }
}
