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
        }

        private static void LoadAndTestSubstrings()
        {
            var grammar = LoadGrammar("ProseSample.Substrings.grammar");
            if (grammar == null) return;

            ProgramNode p = grammar.ParseAST(@"SubStr(v, PosPair(AbsPos(v, -4), AbsPos(v, -1)))",
                                             ASTSerializationFormat.HumanReadable);
            StringRegion prose = StringRegion.Create("Microsoft Program Synthesis using Examples SDK");
            State input = State.Create(grammar.InputSymbol, prose);
            Console.WriteLine(p.Invoke(input));

            StringRegion sdk = prose.Slice(prose.End - 3, prose.End);
            Spec spec = ShouldConvert.Given(grammar).To(prose, sdk);
            Learn(grammar, spec);

            TestFlashFillBenchmark(grammar, "emails");
        }

        private static void TestFlashFillBenchmark(Grammar grammar, string benchmark, int exampleCount = 2)
        {
            string[] lines = File.ReadAllLines($"benchmarks/{benchmark}.tsv");
            Tuple<string, string>[] data = lines.Select(l =>
            {
                var parts = l.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                return Tuple.Create(parts[0], parts[1]);
            }).ToArray();
            var examples = data.Take(exampleCount)
                               .ToDictionary(t => State.Create(grammar.InputSymbol, StringRegion.Create(t.Item1)),
                                             t => (object) StringRegion.Create(t.Item2));
            var spec = new ExampleSpec(examples);
            ProgramNode program = Learn(grammar, spec);
            foreach (Tuple<string, string> row in data.Skip(exampleCount))
            {
                State input = State.Create(grammar.InputSymbol, StringRegion.Create(row.Item1));
                var output = (StringRegion) program.Invoke(input);
                WriteColored(ConsoleColor.DarkCyan, $"{row.Item1} => {output.Value}");
            }
        }

        private static void LoadAndTestTextExtraction()
        {
            var grammar = LoadGrammar("ProseSample.TextExtraction.grammar", "ProseSample.Substrings.grammar");
            if (grammar == null) return;

            TestExtractionBenchmark(grammar, "countries");
        }

        private static void TestExtractionBenchmark(Grammar grammar, string benchmark)
        {
            StringRegion document;
            List<StringRegion> examples = LoadBenchmark($"benchmarks/{benchmark}.txt", out document);
            var input = State.Create(grammar.InputSymbol, document);
            var spec = new PrefixSpec(input, examples);
            ProgramNode program = Learn(grammar, spec);
            string[] output = program.Invoke(input).ToEnumerable().Select(s => ((StringRegion) s).Value).ToArray();
            WriteColored(ConsoleColor.DarkCyan, output.DumpCollection(openDelim: "", closeDelim: "", separator: "\n"));
        }
    }
}
