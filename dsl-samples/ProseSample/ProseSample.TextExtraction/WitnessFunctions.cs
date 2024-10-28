using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.DslLibrary;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;

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


            // Helper function to select a line in string given a position on that line.
            static StringRegion GetLine(StringRegion s, uint position)
            {
                if ((position < s.Start) || (position > s.End)) return null;
                CachedList lineSeparators;
                Token lineSepToken = s.Cache.GetStaticTokenByName(Token.LineSeparatorName);
                if (!s.Cache.TryGetMatchPositionsFor(lineSepToken, out lineSeparators))
                {
                    return s;
                }
                int upperIndex = lineSeparators.BinarySearchForFirstGreaterOrEqual(position);
                if (upperIndex == -1) upperIndex = lineSeparators.Count;
                // Deal with position 0, where the length of line sep is 0.
                // We need to move to the next line sep
                if ((position == 0) && (upperIndex >= 0) && (upperIndex < lineSeparators.Count - 1))
                {
                    upperIndex++;
                }

                uint lowerPosition = s.Start;
                if (upperIndex > 0)
                {
                    uint prevNewLinePosition = lineSeparators[upperIndex - 1].Position;
                    uint prevNewLineRight = lineSeparators[upperIndex - 1].Right;
                    if ((prevNewLinePosition < position) && (position < prevNewLineRight))
                    {
                        return null; // position is in the middle of a new line
                    }
                    if (s.Start < prevNewLineRight)
                    {
                        lowerPosition = prevNewLineRight;
                    }
                }

                uint upperPosition = s.End;
                if ((upperIndex < lineSeparators.Count) && (lineSeparators[upperIndex].Position < s.End))
                {
                    upperPosition = lineSeparators[upperIndex].Position;
                }

                return s.Slice(lowerPosition, upperPosition);
            }
        }

        [ExternLearningLogicMapping("Selection")]
        public DomainLearningLogic ExternWitnessFunction
            => new Substrings.WitnessFunctions(Grammar.GrammarReferences["Substrings"]);
    }
}
