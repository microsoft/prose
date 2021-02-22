using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Features;
using static MergeConflictsResolution.Utils;

namespace MergeConflictsResolution
{
    public class RankingScore : Feature<double>
    {
        public RankingScore(Grammar grammar) : base(grammar, "Score") { }

        protected override double GetFeatureValueForVariable(VariableNode variable) => 0;

        [FeatureCalculator(nameof(Semantics.Apply))]
        public static double ApplyScore(double pattern, double action) => pattern + action;

        [FeatureCalculator(nameof(Semantics.Remove))]
        public static double RemoveScore(double input, double selected) => input;

        [FeatureCalculator(nameof(Semantics.Concat))]
        public static double ConcatScore(double input1, double input2) => input1 + input2;

        [FeatureCalculator(nameof(Semantics.SelectUpstreamIdx))]
        public static double SelectUpstreamIdxScore(double x, double k) => x + k - 20;

        [FeatureCalculator(nameof(Semantics.SelectDownstreamIdx))]
        public static double SelectDownstreamIdxScore(double x, double k) => x + k - 20;

        [FeatureCalculator(nameof(Semantics.SelectUpstream))]
        public static double SelectUpstreamScore(double x) => x;

        [FeatureCalculator(nameof(Semantics.SelectDownstream))]
        public static double SelectDownstreamScore(double x) => x;

        [FeatureCalculator(nameof(Semantics.SelectUpstreamPath))]
        public static double SelectUpstreamPathScore(double x, double k) => x + k;

        [FeatureCalculator(nameof(Semantics.SelectDownstreamPath))]
        public static double SelectDownstreamPathScore(double x, double k) => x + k;

        [FeatureCalculator(nameof(Semantics.FindMatch))]
        public static double FindMatchScore(double x, double k) => x + k;

        [FeatureCalculator(nameof(Semantics.Check))]
        public static double CheckScore(double x, double k) => x + k;

        [FeatureCalculator("SelectDup")]
        public static double SelectDupScore(double x, double k) => x + k;

        [FeatureCalculator(Path, Method = CalculationMethod.FromLiteral)]
        public static double ScorePath(string path) => 0;

        [FeatureCalculator("enabledPredicate", Method = CalculationMethod.FromLiteral)]
        public static double ScoreEnabledPredicate(int[] enabledPredicate) => 0;

        [FeatureCalculator("paths", Method = CalculationMethod.FromLiteral)]
        public static double ScorePaths(string[] paths) => 0;

        [FeatureCalculator("k", Method = CalculationMethod.FromLiteral)]
        public static double ScoreK(int k) => 0;
    }
}