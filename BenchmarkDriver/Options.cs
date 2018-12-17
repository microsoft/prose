using System.CommandLine;

namespace BenchmarkDriver
{
    public enum Command
    {
        All,
        ExtractionText,
        SplitText,
        TransformationText,
        None
    }

    /// <summary>
    ///     Command line option definitions.
    /// </summary>
    public static class Options
    {
        public static Command Command = Command.None;
        public static string BenchmarkDirPath;
        public static string BenchmarkSuiteDirPath;

        private static void AddCommand(ArgumentSyntax syntax, string name, Command command, string help) =>
            syntax.DefineCommand(name, ref Command, command, help);

        public static void Parse(string[] args)
        {
            ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.ApplicationName = "BenchmarkDriver.exe";

                AddCommand(syntax, "all", Command.All, "Run driver against entire benchmark suite");
                syntax.DefineParameter("benchmarkSuiteDir", ref BenchmarkSuiteDirPath, "Benchmark suite root directory");

                AddCommand(syntax, "etext", Command.ExtractionText,
                    "Run driver against individual Extraction.Text benchmark");
                syntax.DefineParameter("benchmarkDir", ref BenchmarkDirPath, "Benchmark directory");

                AddCommand(syntax, "splitText", Command.SplitText,
                    "Run driver against individual Split.Text benchmark");
                syntax.DefineParameter("benchmarkDir", ref BenchmarkDirPath, "Benchmark directory");

                AddCommand(syntax, "ttext", Command.TransformationText,
                    "Run driver against individual Transformation.Text benchmark");
                syntax.DefineParameter("benchmarkDir", ref BenchmarkDirPath, "Benchmark directory");
            });
        }
    }
}