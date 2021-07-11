using Fl.Net;
using Fl.Net.Message;
using System.Collections.Generic;

namespace SerialReceptionDataViewer.Model
{
    public abstract class MessageBase
    {
        public uint DeviceId { get; set; }
        public FlMessageType MessageType { get; set; }
        public FlMessageCategory MessageCategory { get; set; }
        public FlMessageId MessageId { get; set; }
        public List<object> Arguments { get; set; }
        public byte[] Buffer { get; set; }

        public MessageBase()
        {
        }

        public MessageBase(IFlMessage msg)
        {
            if (msg != null)
            {
                MessageType = msg.MessageType;
                MessageCategory = msg.MessageCategory;
                MessageId = msg.MessageId;
                Arguments = msg.Arguments;
                Buffer = msg.Buffer;
            }
        }
    }
}
