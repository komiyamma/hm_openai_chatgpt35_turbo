using HmNetCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HmOpenAIChatGpt35Turbo
{
    internal class HmInputReader : IInputReader
    {
        public string ReadText()
        {
            string? text = (String)Hm.Macro.Var["$HmSelectedText"];
            if (String.IsNullOrEmpty(text))
            {
                return "";
            }

            return text;
        }
    }
}
