using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

                ppExamples[input] = null; // <== deduce examples for the position pair here
            }
            return DisjunctiveExamplesSpec.From(ppExamples);
        }
    }
}
