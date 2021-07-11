using System;
using System.Collections.Generic;
using System.Text;

namespace AppCommon.Net
{
    public static class AppCommonUtil
    {
        public static void PrintExampleMenu(Dictionary<string, IExample> exampleTable)
        {
            Console.WriteLine(AppCommonConstant.STR_COMMANDS);
            foreach (var item in exampleTable)
            {
                Console.WriteLine(item.Key);
            }
            Console.Write(AppCommonConstant.STR_PROMPT);
        }

        public static void PrintMenu(Dictionary<string, Action> menuTable)
        {
            Console.WriteLine(AppCommonConstant.STR_COMMANDS);
            foreach (var item in menuTable)
            {
                Console.WriteLine(item.Key);
            }
            Console.Write(AppCommonConstant.STR_PROMPT);
        }
    }
}
