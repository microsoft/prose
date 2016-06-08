using System;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis;

namespace ProseTutorial.Substrings
{
    public static class RankingScore
    {
        public const double VariableScore = 0;

        [FeatureCalculator("SubStr")]
        public static double Score_SubStr(double x, double pp) => Math.Log(pp);

        [FeatureCalculator("PosPair")]
        [FeatureCalculator("Add")]
        [FeatureCalculator("GetSuffix")]
        public static double Score_PosPair(double pp1, double pp2) => pp1 * pp2;

        [FeatureCalculator("RelativePosPair")]
        public static double Score_RelativePosPair(double pp1, double pp2) => Score_PosPair(pp1, pp2) / 1000.0;

        [FeatureCalculator("AbsPos")]
        public static double Score_AbsPos(double x, double k) => 0.01 / (Math.Abs(k) + 1);

        [FeatureCalculator("k", Method = CalculationMethod.FromLiteral)]
        public static double KScore(int k) => k >= 0 ? 1.0 / (k + 1.0) : 1.0 / (-k + 1.1);

        [FeatureCalculator("BoundaryPair")]
        public static double Score_BoundaryPair(double r1, double r2) => r1 + r2;

        [FeatureCalculator("RegPos")]
        public static double Score_RegPos(double x, double rr, double k) => rr * k;

        //[FeatureCalculator(Method = CalculationMethod.FromLiteral)]
        //public static double RegexScore(Regex r) => 0.1 / (1 + r.ToString().Length);

        [FeatureCalculator("r", Method = CalculationMethod.FromLiteral)]
        public static double RegexScore(RegularExpression r) => r.Score;
    }
}
