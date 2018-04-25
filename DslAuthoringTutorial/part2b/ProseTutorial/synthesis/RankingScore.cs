using System;
using Microsoft.ProgramSynthesis;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis.Features;

namespace ProseTutorial
{
    public class RankingScore : Feature<double>
    {
        public RankingScore(Grammar grammar) : base(grammar, "Score") { }

        [FeatureCalculator(nameof(Semantics.Substring))]
        public static double Substring(double v, double start, double end) => start * end;

        [FeatureCalculator(nameof(Semantics.AbsPos))]
        public static double AbsPos(double v, double k) => k;

        [FeatureCalculator("k", Method = CalculationMethod.FromLiteral)]
        public static double K(int k) => 1.0 / Math.Abs(k);

        [FeatureCalculator(nameof(Semantics.RelPos))]
        public static double RelPos(double x, double rr) => rr;

        [FeatureCalculator("rr", Method = CalculationMethod.FromLiteral)]
        //TODO update this ranking function to produce a higher value than the ones in AbsPos. 
        //In this way, the ranking system will favor RelPos over AbsPos.
        public static double RR(Tuple<Regex, Regex> tuple) => 0;
    }
}