﻿using HmNetCOM;

namespace HmOpenAIChatGpt;

internal class HmOutputWriter : IOutputWriter
{
    public const string NewLine = "\r\n";

    public HmOutputWriter() { }

    public string Normalize(string? msg)
    {
        if (msg == null)
        {
            return "";
        }

        var norm = msg.Replace("\n", NewLine);
        norm = norm.Replace("\r\r", "\r");
        return norm;
    }

    public int Write(string msg)
    {
        var norm = Normalize(msg);
        int status = Hm.OutputPane.Output(norm);
        return status;
    }

    public int WriteLine(string msg)
    {
        var norm = Normalize(msg);
        int status = Hm.OutputPane.Output(norm + NewLine);
        return status;
    }
}
