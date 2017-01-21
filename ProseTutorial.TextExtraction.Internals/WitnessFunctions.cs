using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using static Microsoft.ProgramSynthesis.Extraction.Text.Semantics.Semantics;

namespace ProseTutorial.TextExtraction
{
    public class WitnessFunctions : DomainLearningLogic
    {
        public WitnessFunctions(Grammar grammar) : base(grammar) { }

        [WitnessFunction("LinesMap", 1)]
        public static PrefixSpec WitnessLinesMap(GrammarRule rule, PrefixSpec spec)
        {
            var linesExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var document = ((StringRegion)input[rule.Grammar.InputSymbol]);
                var selectionPrefix = spec.PositiveExamples[input].Cast<StringRegion>();

                linesExamples[input] = null; // <== deduce a prefix of line examples here
            }
            return new PrefixSpec(linesExamples);
        }

        [ExternLearningLogicMapping("Selection")]
        public DomainLearningLogic ExternWitnessFunction
            => new Substrings.WitnessFunctions(Substrings.Language.Grammar);
    }
}
