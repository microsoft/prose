using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Utils;
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
                var v = (StringRegion) input[rule.Body[0]];
                var desiredOutput = (StringRegion) spec.Examples[input];

                var occurrences = new List<Tuple<uint?, uint?>>();
                for (int i = v.Value.IndexOf(desiredOutput.Value, StringComparison.Ordinal);
                     i >= 0;
                     i = v.Value.IndexOf(desiredOutput.Value, i + 1, StringComparison.Ordinal))
                {
                    occurrences.Add(Tuple.Create(v.Start + (uint?) i, v.Start + (uint?) i + desiredOutput.Length));
                }

                ppExamples[input] = occurrences;
            }
            return DisjunctiveExamplesSpec.From(ppExamples);
        }

        [WitnessFunction(nameof(Semantics.AbsPos), 1)]
        public static DisjunctiveExamplesSpec WitnessK(GrammarRule rule, DisjunctiveExamplesSpec spec)
        {
            var kExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var v = (StringRegion) input[rule.Body[0]];

                var positions = new List<object>();
                foreach (uint pos in spec.DisjunctiveExamples[input])
                {
                    positions.Add((int) pos + 1 - (int) v.Start);
                    positions.Add((int) pos - (int) v.End - 1);
                }

                kExamples[input] = positions;
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
                var v = (StringRegion) input[rule.Body[0]];
                var regexes = new List<Tuple<RegularExpression, RegularExpression>>();
                foreach (uint pos in spec.DisjunctiveExamples[input])
                {
                    if (!v.Cache.TryGetAllMatchesStartingAt(pos, out var rightMatches)) continue;
                    if (!v.Cache.TryGetAllMatchesEndingAt(pos, out var leftMatches)) continue;
                    var leftRegexes =
                        leftMatches.Keys.Select(t => RegularExpression.Create(new[] { t }))
                                        .Append(Epsilon);
                    var rightRegexes = 
                        rightMatches.Keys.Select(t => RegularExpression.Create(new[] { t }))
                                         .Append(Epsilon);
                    var regexPairs = from l in leftRegexes
                                     from r in rightRegexes
                                     select Tuple.Create(l, r);
                    regexes.AddRange(regexPairs);
                }
                rrExamples[input] = regexes;
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
                var v = (StringRegion) input[rule.Body[0]];
                var rr = (Tuple<RegularExpression, RegularExpression>) regexBinding.Examples[input];

                var ks = new List<object>();
                foreach (uint pos in spec.DisjunctiveExamples[input])
                {
                    var ms = rr.Item1.Run(v).Where(m => rr.Item2.MatchesAt(v, m.Right)).ToArray();
                    int index = ms.BinarySearchBy(m => m.Right.CompareTo(pos));
                    if (index < 0) return null;
                    ks.Add(index + 1);
                    ks.Add(index - ms.Length);
                }

                kExamples[input] = ks;
            }
            return DisjunctiveExamplesSpec.From(kExamples);
        }
    }
}
