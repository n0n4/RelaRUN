using System;
using System.Collections.Generic;
using System.Text;

namespace RelanNet.Snapshots.PerfTest
{
    public class Record
    {
        public string TestCategory;
        public string TestName;
        public double WarmupTime;
        public double[] RunTimes;
        private int Runs = 0;

        public double RunAverage;

        public Record(string category, string name, int runs)
        {
            TestCategory = category;
            TestName = name;
            WarmupTime = 0;
            RunTimes = new double[runs];
        }

        public void Warmup(double warmuptime)
        {
            WarmupTime = warmuptime;
        }

        public void Add(double runtime)
        {
            RunTimes[Runs] = runtime;
            Runs++;
        }

        public void Done()
        {
            double total = 0;
            for (int i = 0; i < Runs; i++)
                total += RunTimes[i];
            if (Runs > 0)
                RunAverage = total / Runs;
        }
    }
}
