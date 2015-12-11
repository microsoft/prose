using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;

namespace ProseSample.Substrings
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class RankingScore
    {
        public const double VariableScore = 0;

        [FeatureCalculator("SubStr")]
        public static double Score_SubStr(double x, double pp) => Math.Log(pp);

        [FeatureCalculator("PosPair")]
        public static double Score_PosPair(double pp1, double pp2) => pp1 * pp2;

        [FeatureCalculator("AbsPos")]
        public static double Score_AbsPos(double x, double k)
        {
            k = 1 / k - 1;
            // Prefer absolute positions to regex positions if k is small
            return Math.Max(10 * Token.MinScore - (k - 1) * 3 * Token.MinScore, 1 / k);
        }

        [FeatureCalculator(Method = CalculationMethod.FromLiteral)]
        public static double KScore(int k) => k >= 0 ? 1.0 / (k + 1.0) : 1.0 / (-k + 1.1);

        [FeatureCalculator("BoundaryPair")]
        public static double Score_BoundaryPair(double r1, double r2) => r1 + r2;

        [FeatureCalculator("RegPos")]
        public static double Score_RegPos(double x, double rr, double k) => rr * k;

        [FeatureCalculator(Method = CalculationMethod.FromLiteral)]
        public static double RegexScore(RegularExpression r) => r.Score;
    }
}
