using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Extraction.Text;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Logging;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.VersionSpace;
using ProseTutorial.Substrings;

namespace ProseTutorial
{
    internal static class Utils
    {
        public static void Learn(Grammar grammar, Spec spec)
        {
            var engine =
                new SynthesisEngine(grammar,
                                    new SynthesisEngine.Config
                                    {
                                        UseThreads = false,
                                        Strategies = new ISynthesisStrategy[]
                                        {
                                            new EnumerativeSynthesis(), 
                                            new DeductiveSynthesis(new WitnessFunctions(grammar)),
                                        },
                                        LogListener = new LogListener(),
                                    });
            ProgramSet consistentPrograms = engine.LearnGrammar(spec);
            engine.Configuration.LogListener.SaveLogToXML("learning.log.xml");

            foreach (ProgramNode p in consistentPrograms.RealizedPrograms)
            {
                Console.WriteLine(p);
            }
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
            document = RegionSession.CreateStringRegion(content.Replace("}", "").Replace("{", ""));
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
