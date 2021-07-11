using Fl.Net.PInvoke;
using System;

namespace Fl.Net.Message
{
    public class FlBinMessageHeader
    {
        // Unique device ID(for RS-422, RS-485).
        public UInt32 device_id;

        // Message length.
        public ushort length;

        // Message(function) ID.
        public FlMessageId message_id;

        // Flag1
        public FlBinMsgFlag1Struct flag1;

        // Flag2
        public FlBinMsgFlag2Struct flag2;
    }
}
