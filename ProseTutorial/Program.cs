using System;
using System.IO;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Specifications.Extensions;
using static ProseTutorial.Utils;

namespace ProseTutorial
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            LoadAndTestSubstrings();
        }

        private static void LoadAndTestSubstrings()
        {
            var grammar = ProseTutorial.Substrings.Language.Grammar;
            if (grammar == null) return;

            ProgramNode p = ProgramNode.Parse(@"SubStr(v, PosPair(AbsPos(v, -4), AbsPos(v, -1)))",
                                              grammar, ASTSerializationFormat.HumanReadable);
            const string data = "Microsoft PROSE SDK";
            State input = State.Create(grammar.InputSymbol, data);
            Console.WriteLine(p.Invoke(input));

            string sdk = data.Substring(data.Length - 3);
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
            var examples = data.Take(exampleCount)
                               .ToDictionary(t => State.Create(grammar.InputSymbol, t.Item1),
                                             t => (object) t.Item2);
            var spec = new ExampleSpec(examples);
            ProgramNode program = Learn(grammar, spec);
            foreach (Tuple<string, string> row in data.Skip(exampleCount))
            {
                var output = program.Invoke(State.Create(grammar.InputSymbol, row.Item1));
                WriteColored(ConsoleColor.DarkCyan, $"{row.Item1} => {output}");
            }
        }
    }
}
