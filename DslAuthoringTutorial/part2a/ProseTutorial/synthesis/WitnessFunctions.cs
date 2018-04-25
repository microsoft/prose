using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Learning;

namespace ProseTutorial
{
    public class WitnessFunctions : DomainLearningLogic
    {
        public WitnessFunctions(Grammar grammar) : base(grammar) { }

        // We will use this set of regular expressions in this tutorial 
        public static Regex[] UsefulRegexes = {
    new Regex(@"\w+"),  // Word
	new Regex(@"\d+"),  // Number
    new Regex(@"\s+"),  // Space
    new Regex(@".+"),  // Anything
    new Regex(@"$")  // End of line
};

        [WitnessFunction(nameof(Semantics.Substring), 1)]
        public DisjunctiveExamplesSpec WitnessStartPosition(GrammarRule rule, ExampleSpec spec) {
            var result = new Dictionary<State, IEnumerable<object>>();

            foreach (var example in spec.Examples) {
                State inputState = example.Key;
                var input = inputState[rule.Body[0]] as string;
                var output = example.Value as string;
                var occurrences = new List<int>();

                for (int i = input.IndexOf(output); i >= 0; i = input.IndexOf(output, i + 1)) {
                    occurrences.Add(i);
                }

                if (occurrences.Count == 0) return null;
                result[inputState] = occurrences.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);

        }

        [WitnessFunction(nameof(Semantics.Substring), 2, DependsOnParameters = new []{1})]
        public ExampleSpec WitnessEndPosition(GrammarRule rule, ExampleSpec spec, ExampleSpec startSpec) {
            var result = new Dictionary<State, object>();
            foreach (var example in spec.Examples) {
                State inputState = example.Key;
                var output = example.Value as string;
                var start = (int) startSpec.Examples[inputState];
                result[inputState] = start + output.Length;
            }
            return new ExampleSpec(result);
        }

        [WitnessFunction(nameof(Semantics.AbsPos), 1)]
        public DisjunctiveExamplesSpec WitnessK(GrammarRule rule, DisjunctiveExamplesSpec spec) {

            var kExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.DisjunctiveExamples) {
                State inputState = example.Key;
                var v = inputState[rule.Body[0]] as string;

                var positions = new List<int>();
                foreach (int pos in example.Value) {
                    positions.Add((int)pos + 1);
                    positions.Add((int)pos - v.Length - 1);
                }
                if (positions.Count == 0) return null;
                kExamples[inputState] = positions.Cast<object>();
            }
            return DisjunctiveExamplesSpec.From(kExamples);
        }

        /// <summary>
        /// This witness function deduces a spec on rr given the spec on its operator, RelPos
        /// To do so, we need to learn a list of regular expressions that match to the left and to the right of a given position. 
        /// There are many techniques for doing that; in this tutorial, we assume that we have a predefined list of 
        /// “common” regexes like /[0-9]+/, and enumerate them exhaustively at a given position.
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="spec">The spec on RelPos, which is a position in the input string</param>
        /// <returns></returns>
        [WitnessFunction(nameof(Semantics.RelPos), 1)]
        public DisjunctiveExamplesSpec WitnessRegexPair(GrammarRule rule, DisjunctiveExamplesSpec spec) {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.DisjunctiveExamples) {
                State inputState = example.Key;
                var input = inputState[rule.Body[0]] as string;

                var regexes = new List<Tuple<Regex, Regex>>();
                foreach (int output in example.Value) {
                    //TODO, complete the witness function for the rr parameter. 
                    //Given the position in the output variable, you need to generate 
                    //all pairs of regular expressions that match this position. 

                    //You can use the auxiliary function bellow to get the regular expressions 
                    //that match each position in the input string
                    //Uncomment the code about to do so. 
                    //List<Regex>[] leftMatches, rightMatches;
                    //BuildStringMatches(input, out leftMatches, out rightMatches);

                    //Get the list of regexes that match the position 'output' from the leftMatches and rightMatches
                    //by completing the next two lines. 
                    //var leftRegex = ...
                    //var rightRegex = ...

                    //if leftRegex or rightRegex is empty, we could not find a spec for this parameter in this input state    
                    //if (leftRegex.Count == 0 || rightRegex.Count == 0)
                    //    return null;

                    //generate the cross product of the left and right regexes and for each pair, add it to the regexes list.

                }

                if (regexes.Count == 0) return null; 
                result[inputState] = regexes;
            }
            return DisjunctiveExamplesSpec.From(result);
        }

        /// <summary>
        /// This method returns for each position in the input string the regular expressions that 
        /// match to the left and to the right of the position.  
        /// </summary>
        /// <param name="inp"></param>
        /// <param name="leftMatches"></param>
        /// <param name="rightMatches"></param>
        static void BuildStringMatches(string inp, out List<Regex>[] leftMatches,
                                       out List<Regex>[] rightMatches) {
            leftMatches = new List<Regex>[inp.Length + 1];
            rightMatches = new List<Regex>[inp.Length + 1];
            for (int p = 0; p <= inp.Length; ++p) {
                leftMatches[p] = new List<Regex>();
                rightMatches[p] = new List<Regex>();
            }
            foreach (Regex r in UsefulRegexes) {
                foreach (Match m in r.Matches(inp)) {
                    leftMatches[m.Index + m.Length].Add(r);
                    rightMatches[m.Index].Add(r);
                }
            }
        }
    }
}