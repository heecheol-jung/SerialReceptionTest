
namespace AppCommon.Net
{
    public enum StartStatus
    {
        Stopped,

        Starting,

        Started,

        Stopping
    }

    public static class AppCommonConstant
    {
        public const string STR_COMMANDS = "commands : ";
        public const string STR_PROMPT = "> ";
        public const string STR_QUIT = "quit";
        public const string STR_UNKNOWN_COMMAND = "!!! Unknown command.";
    }
}
