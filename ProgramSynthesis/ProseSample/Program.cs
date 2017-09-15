using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Specifications.Extensions;
using Microsoft.ProgramSynthesis.Utils;
using static ProseSample.Utils;

namespace ProseSample
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            LoadAndTestSubstrings();
            LoadAndTestTextExtraction();
            Console.WriteLine("\n\nDone.");
        }

        private static void LoadAndTestSubstrings()
        {
            var grammar = LoadGrammar("ProseSample.Substrings.grammar");
            if (grammar == null) return;

            StringRegion prose = new StringRegion("Microsoft Program Synthesis using Examples SDK", Semantics.Tokens);
            StringRegion sdk = prose.Slice(prose.End - 3, prose.End);
            Spec spec = ShouldConvert.Given(grammar).To(prose, sdk);
            Learn(grammar, spec, new Substrings.RankingScore(grammar), new Substrings.WitnessFunctions(grammar));

            TestFlashFillBenchmark(grammar, "emails");
        }

        private static void TestFlashFillBenchmark(Grammar grammar, string benchmark, int exampleCount = 2)
        {
            string[] lines = File.ReadAllLines($"benchmarks/{benchmark}.tsv");
            ValueTuple<string, string>[] data = lines.Select(l =>
            {
                var parts = l.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                return ValueTuple.Create(parts[0], parts[1]);
            }).ToArray();
            var examples = data.Take(exampleCount)
                               .ToDictionary(
                                   t => State.CreateForLearning(grammar.InputSymbol, new StringRegion(t.Item1, Semantics.Tokens)),
                                   t => (object) new StringRegion(t.Item2, Semantics.Tokens));
            var spec = new ExampleSpec(examples);
            ProgramNode program = Learn(grammar, spec, new Substrings.RankingScore(grammar),
                                        new Substrings.WitnessFunctions(grammar));
            foreach (ValueTuple<string, string> row in data.Skip(exampleCount))
            {
                State input = State.CreateForExecution(grammar.InputSymbol, new StringRegion(row.Item1, Semantics.Tokens));
                var output = (StringRegion) program.Invoke(input);
                WriteColored(ConsoleColor.DarkCyan, $"{row.Item1} => {output.Value}");
            }
        }

        private static void LoadAndTestTextExtraction()
        {
            var grammar = LoadGrammar("ProseSample.TextExtraction.grammar");
            if (grammar == null)
            {
                WriteColored(ConsoleColor.Magenta,
                             "Compilation of ProseSample.TextExtraction.grammar failed: skipping tests");
                return;
            }

            TestExtractionBenchmark(grammar, "areas");
            TestExtractionBenchmark(grammar, "popl13-erc");
        }

        private static void TestExtractionBenchmark(Grammar grammar, string benchmark)
        {
            StringRegion document;
            List<StringRegion> examples = LoadBenchmark($"benchmarks/{benchmark}.txt", out document);
            var input = State.CreateForLearning(grammar.InputSymbol, document);
            var spec = new PrefixSpec(input, examples);
            ProgramNode program = Learn(grammar, spec, new TextExtraction.RankingScore(grammar),
                                        new TextExtraction.WitnessFunctions(grammar));
            string[] output = program.Invoke(input).ToEnumerable().Select(s => ((StringRegion) s).Value).ToArray();
            WriteColored(ConsoleColor.DarkCyan, output.DumpCollection(openDelim: "", closeDelim: "", separator: "\n"));
        }
    }
}
