using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using static Microsoft.ProgramSynthesis.Extraction.Text.Semantics.Semantics;

namespace ProseSample.TextExtraction
{
    public class WitnessFunctions : DomainLearningLogic
    {
        public WitnessFunctions(Grammar grammar) : base(grammar) {}

        [WitnessFunction("LinesMap", 1)]
        internal PrefixSpec WitnessLinesMap(GrammarRule rule, PrefixSpec spec)
        {
            var linesExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var document = ((StringRegion) input[rule.Grammar.InputSymbol]);
                var selectionPrefix = spec.PositiveExamples[input].Cast<StringRegion>();

                var linesContainingSelection = new List<StringRegion>();
                foreach (StringRegion example in selectionPrefix)
                {
                    var startLine = GetLine(document, example.Start);
                    var endLine = GetLine(document, example.End);
                    if (startLine == null || endLine == null || startLine != endLine)
                        return null;
                    linesContainingSelection.Add(startLine);
                }

                linesExamples[input] = linesContainingSelection;
            }
            return new PrefixSpec(linesExamples);
        }

        [ExternLearningLogicMapping("Selection")]
        public DomainLearningLogic ExternWitnessFunction
            => new Substrings.WitnessFunctions(Grammar.GrammarReferences["ProseSample.Substrings"]);
    }
}
