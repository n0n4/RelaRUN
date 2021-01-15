using RelanNet.Snapshots.PerfTest.Tests;
using System;

namespace RelanNet.Snapshots.PerfTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Reporter reporter = new Reporter();

            while (true)
            {
                Console.WriteLine("---");
                Console.WriteLine("Select a Category");
                Console.WriteLine("1) Basic2dSimple");
                while (true)
                {
                    string s = Console.ReadLine();
                    s = s.Trim().ToLower();

                    if (s == "1")
                    {
                        Basic2dSimple.ConsoleInteraction(reporter);
                        break;
                    }
                }
            }
        }
    }
}
