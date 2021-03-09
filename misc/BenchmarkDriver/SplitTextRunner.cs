using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ProgramSynthesis.DslLibrary;
using Microsoft.ProgramSynthesis.Split.Text;
using Microsoft.ProgramSynthesis.Utils;
using Newtonsoft.Json;

namespace BenchmarkDriver
{
    /// <summary>
    ///     Runner for a single Split.Text benchmark.
    /// </summary>
    internal class SplitTextRunner : AbstractRunner<SplitProgram>
    {
        public SplitTextRunner(string benchmarkDirPath, string reportDirPath) : base(reportDirPath)
        {
            BenchmarkDirPath = Path.GetFullPath(benchmarkDirPath);
            BenchmarkName = new DirectoryInfo(BenchmarkDirPath).Name;
        }

        private string BenchmarkDirPath { get; }
        private string BenchmarkName { get; }
        private string InputFilePath => Path.Combine(BenchmarkDirPath, "input.txt");
        private string OutputFilePath => Path.Combine(BenchmarkDirPath, "output.json");

        private static IReadOnlyList<StringRegion> LinesAsStringRegions(string filePath) =>
            File.ReadAllText(filePath, Utils.Utf8WithoutBom)
                .TrimEnd('\r', '\n')
                .Split(new[] {"\r\n", "\n", "\r"}, StringSplitOptions.None)
                .Select(SplitSession.CreateStringRegion)
                .ToList();

        public override void Run()
        {
            if (!Directory.Exists(ReportDirPath)) Directory.CreateDirectory(ReportDirPath);
            Console.WriteLine($"Learning Split.Text program for {BenchmarkName}");

            Success = RunImpl(fixedWidth: false) || RunImpl(fixedWidth: true);
            RecordResult();
        }

        private bool RunImpl(bool fixedWidth)
        {
            var session = new SplitSession();
            if (fixedWidth) session.Constraints.Add(new FixedWidthConstraint());
            var stringRegions = LinesAsStringRegions(InputFilePath);
            session.Inputs.Add(stringRegions);
            Program = session.Learn();

            if (Program == null)
            {
                return false;
            }

            IReadOnlyList<IReadOnlyList<string>> actualRows =
                stringRegions.Select(Program.Run)
                    .Select(cells => cells.Select(cell => cell.CellValue?.Value).ToList())
                    .ToList();
            var expectedRows =
                JsonConvert.DeserializeObject<OutputFileFormat>(
                        File.ReadAllText(OutputFilePath, Utils.Utf8WithoutBom))
                    .Rows;

            if (ValueEquality.Comparer.Equals(actualRows, expectedRows))
            {
                Utils.Write(ProgramFilePath, Program.Serialize());
                Utils.Write(PassFilePath, "");
                return true;
            }

            return false;
        }

        private class OutputFileFormat
        {
            public IReadOnlyList<IReadOnlyList<string>> Rows { get; set; }
        }
    }
}