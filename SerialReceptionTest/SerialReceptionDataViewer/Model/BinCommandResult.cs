using System;

namespace SerialReceptionDataViewer.Model
{
    public class BinCommandResult
    {
        public Int64 Id { get; set; }
        public string LogName { get; set; }
        public BinCommand Command { get; set; }
        public BinResponse Response { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
