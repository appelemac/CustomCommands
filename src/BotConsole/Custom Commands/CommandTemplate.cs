using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotConsole.Custom_Commands
{
    public class CommandTemplate
    {
        public string CommandName { get; set; }
        public string Name { get; set; }
        public List<string> Aliases { get; set; } = new List<string>();
        public string Description { get; set; }
        /// <summary>
        /// 1st value: param type: string, int....
        /// 2nd value: param name
        /// 3rd value: param attribute (can be null)
        /// </summary>
        public List<Tuple<string, string, string>> Parameters { get; set; } = new List<Tuple<string, string, string>>();
        public string FunctionString { get; set; }
    }
}
