using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Extraction.Text;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Specifications.Extensions;
using Microsoft.ProgramSynthesis.Utils;
using Microsoft.ProgramSynthesis.VersionSpace;
using Microsoft.ProgramSynthesis.Wrangling.Constraints;
using Microsoft.ProgramSynthesis.Wrangling.Session;
using static ProseTutorial.Utils;

namespace ProseTutorial {
    internal static partial class Program {
        private static void Main(string[] args) {
            LoadAndTestSubstrings();
            //LoadAndTestTextExtraction();
        }

        private static void LoadAndTestSubstrings() {
            var grammar = Substrings.Language.Grammar;
            if (grammar == null) return;

            /*ProgramNode p = ProgramNode.Parse(@"SubStr(v, PosPair(AbsPos(v, -4), AbsPos(v, -1)))",
                                              grammar, ASTSerializationFormat.HumanReadable);
            StringRegion data = RegionSession.CreateStringRegion("Microsoft PROSE SDK");
            State input = State.Create(grammar.InputSymbol, data);
            Console.WriteLine(p.Invoke(input));

            StringRegion sdk = data.Slice(data.End - 3, data.End);
            Spec spec = ShouldConvert.Given(grammar).To(data, sdk);
            Learn(grammar, spec,
                  new Substrings.RankingScore(grammar), new Substrings.WitnessFunctions(grammar));*/

            TestFlashFillBenchmark("emails");
        }

        private static void TestFlashFillBenchmark(string benchmark, int exampleCount = 2) {
            string[] lines = File.ReadAllLines($"benchmarks/{benchmark}.tsv");
            (string, string)[] data = lines.Select(l => {
                var parts = l.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                return (parts[0], parts[1]);
            }).ToArray();

            using (var session = new DSLSession()) {
                var examples =
                    data.Take(exampleCount)
                        .Select(t => new Example<StringRegion, StringRegion>(
                                    RegionSession.CreateStringRegion(t.Item1),
                                    RegionSession.CreateStringRegion(t.Item2)));
                session.AddConstraints(examples);
                var program = session.LearnTopK(1)[0];

                /*var spec = new ExampleSpec(examples);
                ProgramNode program = Learn(grammar, spec,
                                            new Substrings.RankingScore(grammar),
                                            new Substrings.WitnessFunctions(grammar));*/
                foreach ((string, string) row in data.Skip(exampleCount)) {
                    var output = program.Run(RegionSession.CreateStringRegion(row.Item1));
                    WriteColored(ConsoleColor.DarkCyan, $"{row.Item1} => {output}");
                }
            }
        }

        private static void LoadAndTestTextExtraction() {
            var grammar = TextExtraction.Language.Grammar;
            if (grammar == null) return;

            TestTextExtractionBenchmark(grammar, "countries");
            TestTextExtractionBenchmark(grammar, "popl13-erc");
        }

        private static void TestTextExtractionBenchmark(Grammar grammar, string benchmark) {
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
