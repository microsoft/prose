using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.DslLibrary;
using Microsoft.ProgramSynthesis.Features;

namespace ProseSample.TextExtraction
{
    public class RankingScore : Feature<double>
    {
        public RankingScore(Grammar grammar) : base(grammar, "Score") {}

        protected override double GetFeatureValueForVariable(VariableNode variable) => 0;

        [ExternFeatureMapping("Selection", 0)]
        public IFeature ExternScore => new Substrings.RankingScore(Grammar.GrammarReferences["Substrings"]);

        [FeatureCalculator(nameof(Semantics.SplitLines))]
        public static double Score_SplitLines(double document) => document;

        [FeatureCalculator("LinesMap")]
        public static double Score_LinesMap(double selection, double lines) => selection + lines + Token.MinScore * 20;
    }
}
