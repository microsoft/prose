using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;

namespace ProseSample.Substrings
{
    static class RegexUtils
    {
        public static readonly Regex[] Tokens =
        {
            new Regex(@"", RegexOptions.Compiled), // Epsilon
            new Regex(@"\p{Lu}(\p{Ll})+", RegexOptions.Compiled), // Camel Case
            new Regex(@"\p{Ll}+", RegexOptions.Compiled), // Lowercase word
            new Regex(@"\p{Lu}(\p{Lu})+", RegexOptions.Compiled), // ALL CAPS
            new Regex(@"[0-9]+(\,[0-9]{3})*(\.[0-9]+)?", RegexOptions.Compiled), // Number
            new Regex(@"[-.\p{Lu}\p{Ll}]+", RegexOptions.Compiled), // Words/dots/hyphens
            new Regex(@"[-.\p{Lu}\p{Ll}0-9]+", RegexOptions.Compiled), // Alphanumeric
            new Regex(@"\p{Zs}+", RegexOptions.Compiled), // WhiteSpace
            new Regex(@"\t", RegexOptions.Compiled), // Tab
            new Regex(@",", RegexOptions.Compiled), // Comma
            new Regex(@"\.", RegexOptions.Compiled), // Dot
            new Regex(@":", RegexOptions.Compiled), // Colon
            new Regex(@";", RegexOptions.Compiled), // Semicolon
            new Regex(@"!", RegexOptions.Compiled), // Exclamation
            new Regex(@"\)", RegexOptions.Compiled), // Right Parenthesis
            new Regex(@"\(", RegexOptions.Compiled), // Left Parenthesis
            new Regex(@"""", RegexOptions.Compiled), // Double Quote
            new Regex(@"'", RegexOptions.Compiled), // Single Quote
            new Regex(@"/", RegexOptions.Compiled), // Forward Slash
            new Regex(@"\\", RegexOptions.Compiled), // Backward Slash
            new Regex(@"-", RegexOptions.Compiled), // Hyphen
            new Regex(@"\*", RegexOptions.Compiled), // Star
            new Regex(@"\+", RegexOptions.Compiled), // Plus
            new Regex(@"_", RegexOptions.Compiled), // Underscore
            new Regex(@"=", RegexOptions.Compiled), // Equal
            new Regex(@">", RegexOptions.Compiled), // Greater-than
            new Regex(@"<", RegexOptions.Compiled), // Left-than
            new Regex(@"\]", RegexOptions.Compiled), // Right Bracket
            new Regex(@"\[", RegexOptions.Compiled), // Left Bracket
            new Regex(@"}", RegexOptions.Compiled), // Right Brace
            new Regex(@"{", RegexOptions.Compiled), // Left Brace
            new Regex(@"\|", RegexOptions.Compiled), // Bar
            new Regex(@"&", RegexOptions.Compiled), // Ampersand
            new Regex(@"#", RegexOptions.Compiled), // Hash
            new Regex(@"\$", RegexOptions.Compiled), // Dollar
            new Regex(@"\^", RegexOptions.Compiled), // Hat
            new Regex(@"@", RegexOptions.Compiled), // At
            new Regex(@"%", RegexOptions.Compiled), // Percentage
            new Regex(@"\?", RegexOptions.Compiled), // Question Mark
            new Regex(@"~", RegexOptions.Compiled), // Tilde
            new Regex(@"`", RegexOptions.Compiled), // Back Prime
            new Regex(@"\u2192", RegexOptions.Compiled), // RightArrow
            new Regex(@"\u2190", RegexOptions.Compiled), // LeftArrow
            new Regex(@"(?<=\n)[\p{Zs}\t]*(\r)?\n", RegexOptions.Compiled), // Empty Line
            new Regex(@"[\p{Zs}\t]*((\r)?\n|^|$)", RegexOptions.Compiled), // Line separator
        };

        private static Regex[] _leftTokens;
        public static readonly RegularExpression Epsilon = RegularExpression.Create(new Token[0]);

        public static Regex[] LeftTokens =>
            _leftTokens ?? (_leftTokens = Tokens.Select(t => new Regex($"(?<={t})", RegexOptions.Compiled)).ToArray());

        public static PositionMatch[] Run(this RegularExpression r, StringRegion v) => r.Run(v);

        public static bool MatchesAt(this RegularExpression r, StringRegion v, uint pos)
            => r.MatchesAt(v, pos);

        public static int BinarySearchBy<T>(this IList<T> list, Func<T, int> comparer)
        {
            int min = 0;
            int max = list.Count - 1;

            while (min <= max)
            {
                int mid = min + (max - min) / 2;
                int comparison = comparer(list[mid]);
                if (comparison == 0)
                {
                    return mid;
                }
                if (comparison < 0)
                {
                    min = mid + 1;
                }
                else
                {
                    max = mid - 1;
                }
            }
            return ~min;
        }
    }
}
