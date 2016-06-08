using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using static ProseTutorial.Substrings.RegexUtils;
namespace ProseTutorial.Substrings
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class WitnessFunctions : DomainLearningLogic
    {
        public WitnessFunctions(Grammar grammar) : base(grammar) { }

        [WitnessFunction(nameof(Semantics.SubStr), 1)]
        public static DisjunctiveExamplesSpec WitnessPositionPair(GrammarRule rule, ExampleSpec spec)
        {
            var ppExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var v = (string) input[rule.Body[0]];
                var desiredOutput = (string) spec.Examples[input];

                var occurrences = new List<Tuple<uint?, uint?>>();
                for (int i = v.IndexOf(desiredOutput, StringComparison.Ordinal);
                     i >= 0;
                     i = v.IndexOf(desiredOutput, i + 1, StringComparison.Ordinal))
                {
                    occurrences.Add(Tuple.Create((uint?) i, (uint?) (i + desiredOutput.Length)));
                }

                ppExamples[input] = occurrences; // <== deduce examples for the position pair here
            }
            return DisjunctiveExamplesSpec.From(ppExamples);
        }

        [WitnessFunction(nameof(Semantics.AbsPos), 1)]
        public static DisjunctiveExamplesSpec WitnessK(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            var kExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var v = (string) input[rule.Body[0]];
                var positionVariants = spec.DisjunctiveExamples[input].Cast<uint?>();

                var positions = new List<object>();
                foreach (uint? pos in positionVariants)
                {
                    positions.Add((int) pos + 1);
                    positions.Add((int) pos - v.Length - 1);
                }

                kExamples[input] = positions; // <== deduce examples for the absolute index 'k' here
            }
            return DisjunctiveExamplesSpec.From(kExamples);
        }

        [WitnessFunction(nameof(Semantics.RegPos), 1)]
        public static DisjunctiveExamplesSpec WitnessRegexPair(GrammarRule rule,
                                                               DisjunctiveExamplesSpec spec)
        {
            var rrExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var v = (string) input[rule.Body[0]];
                var regexes = new List<Tuple<Regex, Regex>>();
                foreach (uint pos in spec.DisjunctiveExamples[input])
                {
                    Regex[] rightRegexes = 
                        Tokens.Where(t => t.Match(v, (int) pos).Index == pos).ToArray();
                    if (rightRegexes.Length == 0) continue;
                    Regex[] leftRegexes = 
                        LeftTokens.Where(t => t.Match(v, (int) pos).Index == pos).ToArray();
                    if (leftRegexes.Length == 0) continue;
                    regexes.AddRange(
                        leftRegexes.SelectMany(l => rightRegexes.Select(r => Tuple.Create(l, r))));
                }

                rrExamples[input] = regexes; // <== deduce examples for the regex pair here
            }
            return DisjunctiveExamplesSpec.From(rrExamples);
        }

        [WitnessFunction(nameof(Semantics.RegPos), 2, DependsOnParameters = new[] { 1 })]
        public static DisjunctiveExamplesSpec WitnessRegexCount(GrammarRule rule,
                                                                DisjunctiveExamplesSpec spec,
                                                                ExampleSpec regexBinding)
        {
            var kExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var v = (string) input[rule.Body[0]];
                var rr = (Tuple<Regex, Regex>) regexBinding.Examples[input];

                var r = new Regex($"(?<={rr.Item1}){rr.Item2}");
                Match[] ms = r.Matches(v).Cast<Match>().ToArray();
                var ks = new List<object>();
                foreach (uint pos in spec.DisjunctiveExamples[input])
                {
                    int index = ms.BinarySearchBy(m => m.Index.CompareTo((int) pos));
                    if (index < 0) return null;
                    ks.Add(index + 1);
                    ks.Add(index - ms.Length);
                }

                kExamples[input] = ks; // <== deduce examples for the regex count here
            }
            return DisjunctiveExamplesSpec.From(kExamples);
        }
    }
}
