using Fl.Net.Message;
using System;

namespace Fw.Net
{
    public class FwCommandMessageResult
    {
        public IFlMessage Command { get; set; }
        public IFlMessage Response { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
