using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Extraction.Text;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Logging;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.VersionSpace;

namespace ProseSample
{
    internal static class Utils
    {
        public static Grammar LoadGrammar(string grammarFile, params string[] prerequisiteGrammars)
        {
            foreach (string prerequisite in prerequisiteGrammars) {
                var options = new CompilerOptions {
                    InputGrammar = prerequisite,
                    Output = Path.GetFileNameWithoutExtension(prerequisite) + ".Language.dll",
                    Verbosity = Verbosity.Normal
                };
                var buildResult = DSLCompiler.Compile(options);
                if (!buildResult.HasErrors) continue;
                WriteColored(ConsoleColor.Magenta, buildResult.TraceDiagnostics);
                return null;
            }

            var compilationResult = DSLCompiler.ParseGrammarFromFile(grammarFile);
            if (compilationResult.HasErrors) {
                WriteColored(ConsoleColor.Magenta, compilationResult.TraceDiagnostics);
                return null;
            }
            if (compilationResult.Diagnostics.Count > 0) {
                WriteColored(ConsoleColor.Yellow, compilationResult.TraceDiagnostics);
            }

            return compilationResult.Value;
        }

        public static ProgramNode Learn(Grammar grammar, Spec spec,
                                        Feature<double> scoreFeature, DomainLearningLogic witnessFunctions)
        {
            var engine =
                new SynthesisEngine(grammar,
                                    new SynthesisEngine.Config
                                    {
                                        UseThreads = false,
                                        Strategies = new ISynthesisStrategy[]
                                        {
                                            new EnumerativeSynthesis(), 
                                            new DeductiveSynthesis(witnessFunctions),
                                        },
                                        LogListener = new LogListener(),
                                    });
            ProgramSet consistentPrograms = engine.LearnGrammar(spec);
            engine.Configuration.LogListener.SaveLogToXML("learning.log.xml");

            /*foreach (ProgramNode p in consistentPrograms.RealizedPrograms)
            {
                Console.WriteLine(p);
            }*/

            ProgramNode bestProgram = consistentPrograms.TopK(scoreFeature).FirstOrDefault();
            if (bestProgram == null)
            {
                WriteColored(ConsoleColor.Red, "No program :(");
                return null;
            }
            var score = bestProgram.GetFeatureValue(scoreFeature);
            WriteColored(ConsoleColor.Cyan, $"[score = {score:F3}] {bestProgram}");
            return bestProgram;
        }

        #region Auxiliary methods

        public static void WriteColored(ConsoleColor color, object obj)
            => WriteColored(color, () => Console.WriteLine(obj));

        public static void WriteColored(ConsoleColor color, Action write)
        {
            ConsoleColor currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            write();
            Console.ForegroundColor = currentColor;
        }

        private static readonly Regex ExampleRegex = new Regex(@"(?<=\{)[^}]+(?=\})", RegexOptions.Compiled);

        public static List<StringRegion> LoadBenchmark(string filename, out StringRegion document)
        {
            string content = File.ReadAllText(filename);
            Match[] examples = ExampleRegex.Matches(content).Cast<Match>().ToArray();
            document = RegionLearner.CreateStringRegion(content.Replace("}", "").Replace("{", ""));
            var result = new List<StringRegion>();
            for (int i = 0, shift = -1; i < examples.Length; i++, shift -= 2)
            {
                int start = shift + examples[i].Index;
                int end = start + examples[i].Length;
                result.Add(document.Slice((uint) start, (uint) end));
            }
            return result;
        }

        #endregion Auxiliary methods
    }
}
