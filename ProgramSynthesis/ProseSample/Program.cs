using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Diagnostics;
using Microsoft.ProgramSynthesis.Extraction.Text;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Specifications.Extensions;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Utils;
using static ProseSample.Utils;

namespace ProseSample
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            //LoadAndTestSubstrings();
            LoadAndTestTextExtraction();
        }

        private static void LoadAndTestSubstrings()
        {
            var grammar = LoadGrammar("ProseSample.Substrings.grammar");
            if (grammar == null) return;

            ProgramNode p = ProgramNode.Parse(@"SubStr(v, PosPair(AbsPos(v, -4), AbsPos(v, -1)))",
                                              grammar, ASTSerializationFormat.HumanReadable);
            StringRegion data = RegionLearner.CreateStringRegion("Microsoft PROSE SDK");
            State input = State.Create(grammar.InputSymbol, data);
            Console.WriteLine(p.Invoke(input));

            StringRegion sdk = data.Slice(data.End - 3, data.End);
            Spec spec = ShouldConvert.Given(grammar).To(data, sdk);
            Learn(grammar, spec,
                  new Substrings.RankingScore(grammar), new Substrings.WitnessFunctions(grammar));

            TestTransformation.TextBenchmark(grammar, "emails");
        }

        private static void TestTransformation.TextBenchmark(Grammar grammar, string benchmark, int exampleCount = 2)
        {
            string[] lines = File.ReadAllLines($"benchmarks/{benchmark}.tsv");
            Tuple<string, string>[] data = lines.Select(l =>
            {
                var parts = l.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                return Tuple.Create(parts[0], parts[1]);
            }).ToArray();
            var examples =
                data.Take(exampleCount)
                    .ToDictionary(
                        t => State.Create(grammar.InputSymbol, RegionLearner.CreateStringRegion(t.Item1)),
                        t => (object) RegionLearner.CreateStringRegion(t.Item2));
            var spec = new ExampleSpec(examples);
            ProgramNode program = Learn(grammar, spec,
                                        new Substrings.RankingScore(grammar),
                                        new Substrings.WitnessFunctions(grammar));
            foreach (Tuple<string, string> row in data.Skip(exampleCount))
            {
                State input = State.Create(grammar.InputSymbol,
                                           RegionLearner.CreateStringRegion(row.Item1));
                var output = program.Invoke(input);
                WriteColored(ConsoleColor.DarkCyan, $"{row.Item1} => {output}");
            }
        }

        private static void LoadAndTestTextExtraction()
        {
            var grammar = LoadGrammar("ProseSample.TextExtraction.grammar", "ProseSample.Substrings.grammar");
            if (grammar == null) return;

            TestTextExtractionBenchmark(grammar, "countries");
            TestTextExtractionBenchmark(grammar, "popl13-erc");
        }

        private static void TestTextExtractionBenchmark(Grammar grammar, string benchmark)
        {
            StringRegion document;
            List<StringRegion> examples = LoadBenchmark($"benchmarks/{benchmark}.txt", out document);
            var input = State.Create(grammar.InputSymbol, document);
            var spec = new PrefixSpec(input, examples);
            ProgramNode program = Learn(grammar, spec,
                                        new TextExtraction.RankingScore(grammar),
                                        new TextExtraction.WitnessFunctions(grammar));
            string[] output =
                program.Invoke(input).ToEnumerable().Select(s => ((StringRegion) s).Value).ToArray();
            WriteColored(ConsoleColor.DarkCyan,
                         output.DumpCollection(openDelim: "", closeDelim: "", separator: "\n"));
        }
    }
}
