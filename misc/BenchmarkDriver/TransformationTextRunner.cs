using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ProgramSynthesis.Transformation.Text;
using Newtonsoft.Json;
using TTextProgram = Microsoft.ProgramSynthesis.Transformation.Text.Program;


namespace BenchmarkDriver
{
    /// <summary>
    ///     Runner for a single Transformation.Text benchmark.
    /// </summary>
    internal class TransformationTextRunner : AbstractInteractiveRunner<TTextProgram, Example>
    {
        public TransformationTextRunner(string benchmarkDirPath, string reportDirPath) : base(
            Path.Combine(benchmarkDirPath, "spec.json"), reportDirPath)
        {
            AllExamples = ExamplesFrom(SpecFilePath);
            BenchmarkName = new DirectoryInfo(benchmarkDirPath).Name;
        }

        private string BenchmarkName { get; }

        private static List<Example> ExamplesFrom(string specFilePath)
        {
            var spec = JsonConvert.DeserializeObject<SpecFileFormat>(
                File.ReadAllText(specFilePath, Utils.Utf8WithoutBom));
            return spec.Examples.Select(e => e.ToExample()).ToList();
        }

        protected override string PrettyPrintedExamples()
        {
            var formattedConstraints =
                UsedExamples.Select(SpecFileExampleFormat.FromExample);
            return JsonConvert.SerializeObject(formattedConstraints, Formatting.Indented);
        }

        public override void Run()
        {
            if (!Directory.Exists(ReportDirPath)) Directory.CreateDirectory(ReportDirPath);
            Console.WriteLine($"Learning Transformation.Text program for {BenchmarkName}");

            Success = false;
            while (!Success && TryGetNextExample(out var nextConstraint))
            {
                var session = new Session();
                UsedExamples.Add(nextConstraint);
                session.Constraints.Add(UsedExamples);
                Program = session.Learn();
                Success = Program != null && AllExamples.All(e => Valid(e, Program));
            }
            RecordResult();
        }

        protected override bool Valid(Example example, TTextProgram program) => example.Valid(Program);

        /// <summary>
        ///     Wrapper to support deserialization of spec files.
        /// </summary>
        private class SpecFileFormat
        {
            public SpecFileExampleFormat[] Examples { get; set; }
        }

        /// <summary>
        ///     Wrapper to support deserialization of <see cref="Example" /> instances from spec files.
        /// </summary>
        private class SpecFileExampleFormat
        {
            public string[] Input { get; set; }

            public string Output { get; set; }

            public static SpecFileExampleFormat FromExample(Example example)
            {
                return new SpecFileExampleFormat()
                {
                    Input = (example.Input as InputRow)?.InputStrings,
                    Output = example.Output as string
                };
            }

            public Example ToExample() => new Example(new InputRow(Input), Output);
        }
    }
}