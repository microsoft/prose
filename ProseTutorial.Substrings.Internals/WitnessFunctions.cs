using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;

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

                rrExamples[input] = null; // <== deduce examples for the regex pair here
            }
            return DisjunctiveExamplesSpec.From(rrExamples);
        }
    }
}
