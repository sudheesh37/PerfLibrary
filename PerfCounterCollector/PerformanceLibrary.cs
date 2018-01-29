using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace PerfCounterCollector
{
    class PerformanceLibrary
    {
        /// This is a dll for collecting performance counters for any process.
        /// Returns : None, writes a stats.csv file under folder that was input
        /// Inputs :
        ///		string processname, Name of the exe of the process that is to be monitored e.g. "DatabasePurger.exe"
        ///		string foldername, Folder where stats will be logged
        ///		string buildnum, build number of the current install version of the process e.g. "2.1.45"
        ///		string description, just a description for the test run e.g. "DB Purge Action"
        ///		string stage, two possibly values "start and "end"
        ///	Usage: 
        ///		Reference logPerfCounters.dll
        ///		Call PerfProgram.getPerfCounters with desired inputs - last parameter should be "start"
        ///		Run desired test actions - e.g. run anaysis
        ///		Call PerfProgram.getPerfCounters with same inputs - last parameter should be "end"

        public static void GetPerfCounters(string processname, string foldername, string buildnum, string group, string label, string stage)
        {
            string[] counterNames = { "% Processor Time", "Working Set", "Working Set Peak" };
            long[] counterValues = new long[4];

            Settings settings = new Settings(foldername, buildnum, label);
            //Settings.statsfolder = foldername;
            //Settings.buildnum = buildnum;
            //Settings.label = label;

            //For the system counters 
            PerformanceCounter PC = new PerformanceCounter();
            PC.CategoryName = "Process";
            PC.InstanceName = processname;

            try
            {
                for (int i = 0; i < counterNames.Length; i++)
                {
                    PC.CounterName = counterNames[i];
                    counterValues[i] = PC.RawValue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception seen while collecting data: {0}", ex);
            }

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-CA", false);
            ProcessCounters(counterValues, stage, settings);
        }

        public static void ProcessCounters(long[] counterValues, string stage, Settings settings)
        {
            if (stage == "start")
            {
                StatsStruct.startclocktime = DateTime.Now;

                for (int i = 0; i < 3; i++)
                {
                    switch (i)
                    {
                        case 0:
                            TimeSpan startspan = new TimeSpan(counterValues[i]);
                            Storedcounters.cputimecounter = counterValues[i];
                            StatsStruct.starttime = String.Concat(startspan.Hours, ":", startspan.Minutes, ":", startspan.Seconds);
                            StatsStruct.deltatime = StatsStruct.starttime;
                            break;
                        case 1:
                            StatsStruct.startworkingkbytes = counterValues[i] / 1024;
                            Storedcounters.workingkbytescounter = counterValues[i];
                            break;
                        case 2:
                            StatsStruct.startpeakkbytes = counterValues[i] / 1024;
                            Storedcounters.peakkbytescounter = counterValues[i];
                            break;
                    }
                }
            }
            else if (stage == "end")
            {
                StatsStruct.endclocktime = DateTime.Now;

                for (int i = 0; i < 3; i++)
                {
                    switch (i)
                    {
                        case 0:
                            TimeSpan endspan = new TimeSpan(counterValues[i]);
                            StatsStruct.endtime = String.Concat(endspan.Hours, ":", endspan.Minutes, ":", endspan.Seconds);
                            TimeSpan startspan = new TimeSpan(Storedcounters.cputimecounter);
                            StatsStruct.starttime = String.Concat(startspan.Hours, ":", startspan.Minutes, ":", startspan.Seconds);
                            long deltacountervalue = counterValues[i] - Storedcounters.cputimecounter;
                            TimeSpan deltaspan = new TimeSpan(deltacountervalue);
                            StatsStruct.deltatime = String.Concat(deltaspan.Hours, ":", deltaspan.Minutes, ":", deltaspan.Seconds);

                            break;
                        case 1:
                            StatsStruct.endworkingkbytes = counterValues[i] / 1024;
                            StatsStruct.startworkingkbytes = Storedcounters.workingkbytescounter / 1024;
                            StatsStruct.deltaworkingkbytes = StatsStruct.endworkingkbytes - StatsStruct.startworkingkbytes;
                            break;
                        case 2:
                            StatsStruct.endpeakkbytes = counterValues[i] / 1024;
                            StatsStruct.startpeakkbytes = Storedcounters.peakkbytescounter / 1024;
                            StatsStruct.deltapeakkbytes = StatsStruct.endpeakkbytes - StatsStruct.startpeakkbytes;
                            break;
                    }
                }
                LogCounters(settings);
            }
        }

        public static void LogCounters(Settings settings)
        {
            //Write data to csv
            StreamWriter log;

            if (!Directory.Exists(settings.Statsfolder))
            {
                Directory.CreateDirectory(settings.Statsfolder);
            }

            string statisticsFile = settings.Statsfolder + "\\stats.csv";
            string buildnum = settings.Buildnum;
            string Label = settings.Label;

            if (!File.Exists(statisticsFile))
            {
                log = new StreamWriter(statisticsFile);
                string statsheader =
                    "Build Number,Label,Start Clock Time,End Clock Time,Start CPU Time,Start Working memory(KB),Start Peak Working memory(KB),End CPU Time,End Working memory(KB),End Peak Working memory(KB),Delta CPU time,Delta Working memory(KB),Delta Peak Working memory(KB)";
                log.WriteLine(statsheader);
                string statstring = String.Concat(buildnum, ",", Label, ",", StatsStruct.startclocktime, ",", StatsStruct.endclocktime, ",", StatsStruct.starttime, ",", StatsStruct.startworkingkbytes, ",", StatsStruct.startpeakkbytes, ",", StatsStruct.endtime,
                              ",", StatsStruct.endworkingkbytes, ",", StatsStruct.endpeakkbytes, ",", StatsStruct.deltatime, ",", StatsStruct.deltaworkingkbytes, ",", StatsStruct.deltapeakkbytes);
                log.WriteLine(statstring);
            }
            else
            {
                log = File.AppendText(statisticsFile);
                string statstring = String.Concat(buildnum, ",", Label, ",", StatsStruct.startclocktime, ",", StatsStruct.endclocktime, ",", StatsStruct.starttime, ",", StatsStruct.startworkingkbytes, ",", StatsStruct.startpeakkbytes, ",", StatsStruct.endtime,
                              ",", StatsStruct.endworkingkbytes, ",", StatsStruct.endpeakkbytes, ",", StatsStruct.deltatime, ",", StatsStruct.deltaworkingkbytes, ",", StatsStruct.deltapeakkbytes);
                log.WriteLine(statstring);
            }
            log.Flush();
            log.Close();
        }

        // Classes required for handling data
        #region 

        public struct StatsStruct
        {
            public static DateTime startclocktime;
            public static DateTime endclocktime;
            public static string starttime;
            public static string endtime;
            public static string deltatime;
            public static long startworkingkbytes;
            public static long endworkingkbytes;
            public static long deltaworkingkbytes;
            public static long startpeakkbytes;
            public static long endpeakkbytes;
            public static long deltapeakkbytes;
        }

        public class Storedcounters
        {
            public static long cputimecounter;
            public static long workingkbytescounter;
            public static long peakkbytescounter;
        }

        public class Settings
        {
            private string statsfolder;
            private string buildnum;
            private string label;

            public string Statsfolder { get => statsfolder; set => statsfolder = value; }
            public string Buildnum { get => buildnum; set => buildnum = value; }
            public string Label { get => label; set => label = value; }

            public Settings(string foldername, string buildnum, string label)
            {
                this.Statsfolder = foldername;
                this.Buildnum = buildnum;
                this.Label = label;
            }
        }

        #endregion
    }
}
