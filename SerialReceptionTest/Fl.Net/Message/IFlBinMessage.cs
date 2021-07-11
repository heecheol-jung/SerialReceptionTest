using Fl.Net.PInvoke;

namespace Fl.Net.Message
{
    public interface IFlBinMessage : IFlMessage
    {
        FlBinMessageHeader Header { get; set; }
    }
}
