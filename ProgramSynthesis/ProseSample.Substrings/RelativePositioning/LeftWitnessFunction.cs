using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlashMeta.Core;
using FlashMeta.Core.Rules;
using FlashMeta.Core.Specifications;
using FlashMeta.Core.Synthesis;

namespace MOD2015.Substrings.RelativePositioning {
    public static class LeftWitnessFunction {
        [WitnessFunction(0)]
        public static DisjunctiveExamplesSpecification WitnessLeft(LetRule rule, int parameter,
                                                                   DisjunctiveExamplesSpecification spec) {
            var examples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs) {
                var leftPositions = new List<object>();
                foreach (Tuple<uint?, uint?> example in spec.DisjunctiveExamples[input]) {
                    leftPositions.Add(example.Item1);
                }
                examples[input] = leftPositions;
            }
            return DisjunctiveExamplesSpecification.From(examples);
        }
    }
}
