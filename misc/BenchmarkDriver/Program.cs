using System;
using System.IO;

namespace BenchmarkDriver
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            Options.Parse(args);

            try
            {
                IRunner runner;
                if (Options.Command == Command.All)
                {
                    runner = new FullSuiteRunner(Options.BenchmarkSuiteDirPath);
                }
                else
                {
                    var benchmarkDirPath = Options.BenchmarkDirPath;
                    var reportDirPath = Path.Combine(benchmarkDirPath, "report");
                    switch (Options.Command)
                    {
                        case Command.SplitText:
                            runner = new SplitTextRunner(benchmarkDirPath, reportDirPath);
                            break;
                        case Command.TransformationText:
                            runner = new TransformationTextRunner(benchmarkDirPath, reportDirPath);
                            break;
                        default:
                            Console.Error.WriteLine("No command specified. Exiting.");
                            return -1;
                    }
                }

                runner.Run();
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine(exc.ToString());
                return -1;
            }

            return 0;
        }
    }
}