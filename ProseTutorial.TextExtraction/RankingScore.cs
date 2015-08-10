using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis;

namespace ProseTutorial.TextExtraction
{
    public static class RankingScore
    {
        public const double VariableScore = 0;

        [FeatureCalculator("SplitLines")]
        public static double Score_SplitLines(double document) => document;

        [FeatureCalculator("LinesMap")]
        public static double Score_LinesMap(double selection, double lines) => selection + lines + Token.MinScore * 20;
    }
}
