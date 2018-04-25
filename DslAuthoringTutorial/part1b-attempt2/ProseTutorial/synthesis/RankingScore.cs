using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Features;

namespace ProseTutorial {
    public class RankingScore : Feature<double> {
        public RankingScore(Grammar grammar) : base(grammar, "Score") { }

        [FeatureCalculator(nameof(Semantics.Substring))]
        public static double Substring(double v, double start, double end) => start * end;

        [FeatureCalculator(nameof(Semantics.AbsPos))]
        public static double AbsPos(double v, double k) => k;

        [FeatureCalculator("k", Method = CalculationMethod.FromLiteral)]
        public static double K(int k) => 1.0;
    }
}