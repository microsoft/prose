using System;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis.Features;

namespace ProseTutorial {
    public class RankingScore : Feature<double> {
        public RankingScore(Grammar grammar) : base(grammar, "Score") { }

        protected override double GetFeatureValueForVariable(VariableNode variable) => 0;

        [FeatureCalculator(nameof(Semantics.Substring))]
        public static double Substring(double v, double start, double end) => start * end;

        [FeatureCalculator(nameof(Semantics.AbsPos))]
        public static double AbsPos(double v, double k) => k;

        [FeatureCalculator("k", Method = CalculationMethod.FromLiteral)]
        public static double K(int k) => 0;
    }
}