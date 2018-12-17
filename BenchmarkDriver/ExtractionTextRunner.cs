using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ProgramSynthesis.DslLibrary;
using Microsoft.ProgramSynthesis.Extraction.Text;
using Microsoft.ProgramSynthesis.Extraction.Text.Constraints;
using Microsoft.ProgramSynthesis.Utils;
using Microsoft.ProgramSynthesis.Utils.Interactive;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BenchmarkDriver
{
    /// <summary>
    ///     Runner for a single Extraction.Text benchmark. An Extraction.Text benchmark consists of one or more spec files
    ///     corresponding to individual Sequence and Field extractions. See <see cref="SequenceRunner" /> and
    ///     <see cref="FieldRunner" /> for their corresponding runners.
    /// </summary>
    internal class ExtractionTextRunner : IRunner
    {
        public ExtractionTextRunner(string benchmarkDirPath, string reportDirPath)
        {
            benchmarkDirPath = Path.GetFullPath(benchmarkDirPath);
            reportDirPath = Path.GetFullPath(reportDirPath);
            var inputFilePath = Path.Combine(benchmarkDirPath, "input.txt");
            var specFilePaths = Directory.EnumerateFiles(benchmarkDirPath, "*.spec.json").ToList();
            SubRunners = specFilePaths.Select(specFilePath =>
                    CreateSubRunner(inputFilePath, specFilePath,
                        Path.Combine(
                            reportDirPath,
                            Path.GetFileNameWithoutExtension(specFilePath))))
                .ToList();
        }

        private IReadOnlyList<IRunner> SubRunners { get; }

        public void Run() => SubRunners.ForEach(sr => sr.Run());

        private static IRunner CreateSubRunner(string inputFilePath, string specFilePath, string reportDirPath)
        {
            var inputString = File.ReadAllText(inputFilePath, Utils.Utf8WithoutBom);
            var jsonObject = JObject.Parse(File.ReadAllText(specFilePath, Utils.Utf8WithoutBom));

            var kind = jsonObject["Kind"].Value<string>();
            switch (kind)
            {
                case "Sequence":
                    var sequenceExamples = jsonObject["Examples"]
                        .ToObject<IEnumerable<SequenceExampleWrapper>>().ToList();
                    sequenceExamples.ForEach(ex => ex.ReifyFromString(inputString));
                    return new SequenceRunner(specFilePath, reportDirPath,
                        sequenceExamples.SelectMany(ex => ex.AsSessionConstraints()));
                case "Field":
                    var fieldExamples = jsonObject["Examples"]
                        .ToObject<IEnumerable<FieldExampleWrapper>>().ToList();
                    fieldExamples.ForEach(ex => ex.ReifyFromString(inputString));
                    return new FieldRunner(specFilePath, reportDirPath,
                        fieldExamples.SelectMany(ex => ex.AsSessionConstraints()));
                default:
                    throw new NotImplementedException($"Unexpected kind = {kind}");
            }
        }

        private static string GetSubBenchmarkName(string specFilePath) =>
            string.Join("/", Directory.GetParent(specFilePath).Name, Path.GetFileName(specFilePath).Replace(".spec.json", ""));

        /// <summary>
        ///     Runner for an Extraction.Text benchmark spec file with Field kind.
        /// </summary>
        private class FieldRunner : AbstractInteractiveRunner<RegionProgram, RegionExample>
        {
            public FieldRunner(string specFilePath, string reportDirPath, IEnumerable<RegionExample> examples)
                : base(specFilePath, reportDirPath)
            {
                AllExamples = examples.ToList();
                SubBenchmarkName = GetSubBenchmarkName(specFilePath);
            }

            private string SubBenchmarkName { get; }

            protected override string PrettyPrintedExamples()
            {
                var formattedConstraints =
                    UsedExamples.Select(constraint =>
                        new Dictionary<string, string>
                        {
                            ["Input"] = constraint.InputMember.Value,
                            ["Output"] = constraint.OutputMember.Value
                        });
                return JsonConvert.SerializeObject(formattedConstraints, Formatting.Indented);
            }

            public override void Run()
            {
                if (!Directory.Exists(ReportDirPath)) Directory.CreateDirectory(ReportDirPath);
                Console.WriteLine($"Learning Extraction.Text region program for {SubBenchmarkName}");

                Success = false;
                while (!Success && TryGetNextExample(out RegionExample nextConstraint))
                {
                    var session = new RegionSession();
                    UsedExamples.Add(nextConstraint);
                    session.Constraints.Add(UsedExamples);
                    Program = session.Learn();
                    Success = Program != null && AllExamples.All(e => Valid(e, Program));
                }
                RecordResult();
            }

            protected override bool Valid(RegionExample example, RegionProgram program)
            {
                var output = program.Run(example.InputMember);
                if (ReferenceEquals(output, example.OutputMember)) return true;
                return output?.Equals(example.OutputMember) ?? false;
            }
        }

        /// <summary>
        ///     Runner for an Extraction.Text benchmark spec file with Sequence kind.
        /// </summary>
        private class SequenceRunner : AbstractInteractiveRunner<SequenceProgram, SequenceExample>
        {
            public SequenceRunner(string specFilePath, string reportDirPath, IEnumerable<SequenceExample> examples)
                : base(specFilePath, reportDirPath)
            {
                AllExamples = examples.ToList();
                SubBenchmarkName = GetSubBenchmarkName(specFilePath);
            }

            private string SubBenchmarkName { get; }

            protected override string PrettyPrintedExamples()
            {
                var formattedConstraints =
                    UsedExamples.Select(constraint =>
                        new Dictionary<string, object>
                        {
                            ["Input"] = constraint.InputMember.Value,
                            ["Output"] =
                            constraint.OutputMember.Select(
                                stringRegion => stringRegion.Value)
                        });
                return JsonConvert.SerializeObject(formattedConstraints, Formatting.Indented);
            }

            public override void Run()
            {
                if (!Directory.Exists(ReportDirPath)) Directory.CreateDirectory(ReportDirPath);
                Console.WriteLine($"Learning Extraction.Text sequence program for {SubBenchmarkName}");

                while (!Success && TryGetNextExample(out SequenceExample nextConstraint))
                {
                    var session = new SequenceSession();
                    UsedExamples.Add(nextConstraint);
                    session.Constraints.Add(UsedExamples);
                    Program = session.Learn();
                    Success = Program != null && AllExamples.All(e => Valid(e, Program));
                }
                RecordResult();
            }

            protected override bool Valid(SequenceExample example, SequenceProgram program)
            {
                var output = program.Run(example.InputMember);
                if (output == null) return false;
                return ValueEquality.Comparer.Equals(output.Take(example.OutputMember.Count()),
                    example.OutputMember);
            }
        }

        /// <summary>
        ///     Wrapper to support deserialization of <see cref="SequenceExample" /> instances from spec files.
        /// </summary>
        private class SequenceExampleWrapper
        {
            public IReadOnlyList<StringRegionWrapper> Input { get; set; }

            public IReadOnlyList<IReadOnlyList<StringRegionWrapper>> Output { get; set; }

            internal IEnumerable<SequenceExample> AsSessionConstraints()
            {
                foreach (Record<StringRegionWrapper, IReadOnlyList<StringRegionWrapper>> pair in Input.ZipWith(Output))
                {
                    var input = pair.Item1.ReifiedStringRegion;
                    var outputSequence =
                        pair.Item2.Select(output => output.ReifiedStringRegion).ToList();
                    foreach (var prefixSize in Enumerable.Range(2, outputSequence.Count + 1))
                    {
                        yield return new SequenceExample(input, outputSequence.Take(prefixSize));
                    }
                }
            }

            internal void ReifyFromString(string inputString)
            {
                Input.ForEach(i => i.ReifyFromString(inputString));
                foreach (var i in Enumerable.Range(0, Input.Count))
                {
                    foreach (var output in Output[i])
                    {
                        output.ReifyFromStringRegion(Input[i].ReifiedStringRegion);
                    }
                }
            }
        }

        /// <summary>
        ///     Wrapper to support deserialization of <see cref="RegionExample" /> instances from spec files.
        /// </summary>
        private class FieldExampleWrapper
        {
            public IReadOnlyList<StringRegionWrapper> Input { get; set; }

            public IReadOnlyList<StringRegionWrapper> Output { get; set; }

            internal IEnumerable<RegionExample> AsSessionConstraints() =>
                Input.Zip(Output, (i, o) => new RegionExample(i.ReifiedStringRegion, o.ReifiedStringRegion));

            internal void ReifyFromString(string inputString)
            {
                Input.ForEach(i => i.ReifyFromString(inputString));
                foreach (var i in Enumerable.Range(0, Input.Count))
                {
                    Output[i].ReifyFromStringRegion(Input[i].ReifiedStringRegion);
                }
            }
        }

        /// <summary>
        ///     Wrapper to support deserialization of <see cref="StringRegion" /> instances from spec files.
        /// </summary>
        private class StringRegionWrapper
        {
            private StringRegion _stringRegion;

            public uint Start { get; set; }

            public uint End { get; set; }

            public string Value { get; set; }

            internal StringRegion ReifiedStringRegion => _stringRegion ??
                                                         throw new InvalidOperationException(
                                                             "Must reify string region first");

            internal void ReifyFromString(string inputString)
            {
                var s = inputString.Replace("\r\n", "\n");
                _stringRegion = RegionSession.CreateStringRegion(s).Slice(Start, End);
            }

            internal void ReifyFromStringRegion(StringRegion stringRegion)
            {
                _stringRegion = stringRegion.Slice(Start, End);
            }
        }
    }
}
