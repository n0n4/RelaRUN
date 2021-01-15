using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RelanNet.Snapshots.PerfTest
{
    public class Reporter
    {
        public List<Record> Records = new List<Record>();
        public Record ActiveRecord = null;
        public Stopwatch Watch = new Stopwatch();

        public void StartRecord(string category, string name, int runs)
        {
            if (ActiveRecord != null)
                EndRecord();

            ActiveRecord = new Record(category, name, runs);
            Records.Add(ActiveRecord);
        }

        public void StartTimer()
        {
            Watch.Restart();
        }

        public void EndWarmup()
        {
            Watch.Stop();
            ActiveRecord.Warmup(Watch.Elapsed.TotalMilliseconds);
        }

        public void EndRun()
        {
            Watch.Stop();
            ActiveRecord.Add(Watch.Elapsed.TotalMilliseconds);
        }

        public void EndRecord()
        {
            ActiveRecord.Done();

            // display the record
            Console.WriteLine(ActiveRecord.TestCategory + " - " + ActiveRecord.TestName);
            Console.WriteLine(ActiveRecord.WarmupTime + "ms warmup");
            Console.WriteLine(ActiveRecord.RunTimes.Length + " runs");
            Console.WriteLine(ActiveRecord.RunAverage + "ms run avg");

            ActiveRecord = null;
        }
    }
}
