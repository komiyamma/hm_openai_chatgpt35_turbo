using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

interface IOutputWriter
{
    string Normalize(string? msg);

    int Write(string msg);

    int WriteLine(string msg);
}
