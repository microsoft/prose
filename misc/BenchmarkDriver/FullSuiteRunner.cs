using System.IO;
using System.Linq;

namespace BenchmarkDriver
{
    /// <summary>
    ///     Runner for the full benchmark suite.
    /// </summary>
    internal class FullSuiteRunner : IRunner
    {
        public FullSuiteRunner(string benchmarkSuiteDirPath)
        {
            BenchmarkSuiteDirPath = benchmarkSuiteDirPath;
            SplitTextDirPath = Path.Combine(BenchmarkSuiteDirPath, "Split.Text");
            TransformationTextDirPath = Path.Combine(BenchmarkSuiteDirPath, "Transformation.Text");
        }

        private string BenchmarkSuiteDirPath { get; }
        private string SplitTextDirPath { get; }
        private string TransformationTextDirPath { get; }

        public void Run()
        {
            RunSplitText();
            RunTransformationText();
        }

        public void RunSplitText()
        {
            foreach (var benchmarkDirPath in Directory.EnumerateDirectories(SplitTextDirPath).OrderBy(x => x))
            {
                var runner = new SplitTextRunner(benchmarkDirPath, Path.Combine(benchmarkDirPath, "report"));
                runner.Run();
            }
        }

        public void RunTransformationText()
        {
            foreach (var benchmarkDirPath in Directory.EnumerateDirectories(TransformationTextDirPath).OrderBy(x => x))
            {
                var runner = new TransformationTextRunner(benchmarkDirPath, Path.Combine(benchmarkDirPath, "report"));
                runner.Run();
            }
        }
    }
}