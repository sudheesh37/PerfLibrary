using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfCounterCollector
{
    class PerfCountCaller
    {
        static void Main(string[] args)
        {
            string Processname = "notepad++";
            const string Buildnum = "333";
            PerformanceLibrary.GetPerfCounters(Processname, @"C:\Temp11\", Buildnum, "tag", "tag", "start");

            // Do some performance intensive action with notepad++ here - maybe open a huge file

            PerformanceLibrary.GetPerfCounters(Processname, @"C:\Temp11\", Buildnum, "tag", "tag", "end");
        }
    }
}
