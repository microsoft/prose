using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Learning;

namespace ProseTutorial {
    public class WitnessFunctions : DomainLearningLogic {
        public WitnessFunctions(Grammar grammar) : base(grammar) { }

        /// <summary>
        /// This witness function should deduce the first position for the Substring operator
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="spec"></param>
        /// <returns>Since there may be more than one occurrence of the output in the input string, 
        /// there may be more than one spec for the start position, which is specified using DisjunctiveExamplesSpec
        /// </returns>
        [WitnessFunction(nameof(Semantics.Substring), 1)]
        public DisjunctiveExamplesSpec WitnessStartPosition(GrammarRule rule, ExampleSpec spec) {
            //the spec on the first position for each input state will have type IEnumerable<object> since we may have 
            //more than one possible output
            var result = new Dictionary<State, IEnumerable<object>>();

            foreach (var example in spec.Examples) {
                State inputState = example.Key;
                var input = inputState[rule.Body[0]] as string;
                var output = example.Value as string;
                var occurrences = new List<int>();

                ///////////////////////////////////////////////////////////////////////
                //TODO replace the following line by the commented out for-loop bellow where we identify all start positions 
                //and add each one to the occurrences list. 
                ///////////////////////////////////////////////////////////////////////
                occurrences.Add(input.IndexOf(output));
                //for (int i = input.IndexOf(output); i >= 0; i = input.IndexOf(output, i + 1)) {
                //    occurrences.Add(i);
                //}

                if (occurrences.Count == 0) return null;
                result[inputState] = occurrences.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);

        }

        [WitnessFunction(nameof(Semantics.Substring), 2)]
        public DisjunctiveExamplesSpec WitnessEndPosition(GrammarRule rule, ExampleSpec spec) {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.Examples) {
                State inputState = example.Key;
                var input = inputState[rule.Body[0]] as string;
                var output = example.Value as string;
                var occurrences = new List<int>();
                ///////////////////////////////////////////////////////////////////////
                //TODO replace the following line by a for-loop where we identify all end positions 
                //as we did in the previous witness function
                ///////////////////////////////////////////////////////////////////////
                occurrences.Add(input.IndexOf(output) + output.Length);

                if (occurrences.Count == 0) return null;
                result[inputState] = occurrences.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }

        [WitnessFunction(nameof(Semantics.AbsPos), 1)]
        public DisjunctiveExamplesSpec WitnessK(GrammarRule rule, DisjunctiveExamplesSpec spec) {
            var kExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.DisjunctiveExamples) {
                State inputState = example.Key;
                var v = inputState[rule.Body[0]] as string;

                var positions = new List<int>();
                foreach (int pos in example.Value) {
                    positions.Add(pos + 1);
                }
                if (positions.Count == 0) return null;
                kExamples[inputState] = positions.Cast<object>();
            }
            return DisjunctiveExamplesSpec.From(kExamples);
        }
    }
}