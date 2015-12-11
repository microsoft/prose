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

namespace ProseSample.TextExtraction
{
    public static class WitnessFunctions
    {
        [WitnessFunction("LinesMap", 1)]
        public static PrefixSpec WitnessLinesMap(GrammarRule rule, int parameter,
                                                 PrefixSpec spec)
        {
            var linesExamples = new Dictionary<State, IEnumerable<object>>();
            foreach (State input in spec.ProvidedInputs)
            {
                var document = ((StringRegion) input[rule.Grammar.InputSymbol]);
                var selectionPrefix = spec.Examples[input].Cast<StringRegion>();

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
    }
}
