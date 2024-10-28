﻿using System.Collections.Generic;
using Microsoft.ProgramSynthesis.DslLibrary;

namespace ProseSample.TextExtraction
{
    public static class Semantics
    {
        public static IEnumerable<StringRegion> SplitLines(StringRegion document)
        {
            Token lineBreak = document.Cache.GetStaticTokenByName(Token.LineSeparatorName);
            CachedList lineBreakPositions;
            if (!document.Cache.TryGetMatchPositionsFor(lineBreak, out lineBreakPositions))
                return new[] { document };
            var lines = new List<StringRegion>();
            for (int i = 0; i < lineBreakPositions.Count - 1; i++)
            {
                if (lineBreakPositions[i + 1].Length == 0) continue;
                lines.Add(document.Slice(lineBreakPositions[i].Right, lineBreakPositions[i + 1].Position));
            }
            return lines;
        }
    }
}
