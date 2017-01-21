using System;
using System.IO;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Extraction.Text;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Specifications.Extensions;
using Microsoft.ProgramSynthesis.Utils;
using static ProseTutorial.Utils;

namespace ProseTutorial
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
            var grammar = ProseTutorial.Substrings.Language.Grammar;
            if (grammar == null) return;

            ProgramNode p = ProgramNode.Parse(@"SubStr(v, PosPair(AbsPos(v, -4), AbsPos(v, -1)))",
                                              grammar, ASTSerializationFormat.HumanReadable);
            StringRegion data = RegionSession.CreateStringRegion("Microsoft PROSE SDK");
            State input = State.Create(grammar.InputSymbol, data);
            Console.WriteLine(p.Invoke(input));

            StringRegion sdk = data.Slice(data.End - 3, data.End);
            Spec spec = ShouldConvert.Given(grammar).To(data, sdk);
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
            var examples =
                data.Take(exampleCount)
                    .ToDictionary(
                        t => State.Create(grammar.InputSymbol, RegionSession.CreateStringRegion(t.Item1)),
                        t => (object)RegionSession.CreateStringRegion(t.Item2));
            var spec = new ExampleSpec(examples);
            ProgramNode program = Learn(grammar, spec);
            foreach (Tuple<string, string> row in data.Skip(exampleCount))
            {
                State input = State.Create(grammar.InputSymbol,
                                           RegionSession.CreateStringRegion(row.Item1));
                var output = program.Invoke(input);
                WriteColored(ConsoleColor.DarkCyan, $"{row.Item1} => {output}");
            }
        }

        private static void LoadAndTestTextExtraction()
        {
            var grammar = ProseTutorial.TextExtraction.Language.Grammar;

            StringRegion document;
            LoadBenchmark("benchmarks/countries.txt", out document);
            var input = State.Create(grammar.InputSymbol, document);
            ProgramNode p = ProgramNode.Parse(
                "LinesMap(SubStr(line, PosPair(AbsPos(line, 1), AbsPos(line, 5))), SplitLines(document))",
                grammar, ASTSerializationFormat.HumanReadable);
            var output = p.Invoke(input).ToEnumerable();
            WriteColored(ConsoleColor.DarkCyan,
                         output.DumpCollection(openDelim: "", closeDelim: "", separator: "\n"));
        }
    }
}
