using System;
using System.Collections.Generic;
using FlashMeta.Core;
using FlashMeta.Core.Rules;
using FlashMeta.Core.Specifications;
using FlashMeta.Core.Synthesis;

namespace MOD2015.Substrings.RelativePositioning {
    public static class RightWitnessFunction {
        [WitnessFunction(0)]
        public static DisjunctiveExamplesSpecification WitnessRight(LetRule rule, int parameter,
                                                                    DisjunctiveExamplesSpecification spec) {
            var examples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs) {
                var rightPositions = new List<object>();
                foreach (Tuple<uint?, uint?> example in spec.DisjunctiveExamples[input]) {
                    rightPositions.Add(example.Item2);
                }
                examples[input] = rightPositions;
            }
            return DisjunctiveExamplesSpecification.From(examples);
        }
    }
}
