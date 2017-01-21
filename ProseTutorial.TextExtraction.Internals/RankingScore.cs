using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;

namespace ProseTutorial.TextExtraction
{
    public class RankingScore : Feature<double>
    {
        public RankingScore(Grammar grammar) : base(grammar, "Score") { }

        protected override double GetFeatureValueForVariable(VariableNode variable) => 0;

        [ExternFeatureMapping("Selection", 0)]
        public IFeature ExternScore => new Substrings.RankingScore(Substrings.Language.Grammar);

        [FeatureCalculator("SplitLines")]
        public static double Score_SplitLines(double document) => document;

        [FeatureCalculator("LinesMap")]
        public static double Score_LinesMap(double selection, double lines) => selection + lines + Token.MinScore * 20;

        [FeatureCalculator("selection", Method = CalculationMethod.FromProgramNode)]
        public static double Dummy(ProgramNode p) => 0;
    }
}
