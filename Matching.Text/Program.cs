using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.ProgramSynthesis.Matching.Text;
using Microsoft.ProgramSynthesis.Matching.Text.Semantics;
using Microsoft.ProgramSynthesis.Utils;

namespace Matching.Text
{
    internal static class Program {
        private static void Main(string[] args)
        {
            // Learn patterns from a set of input strings.  
            BasicUsage();

            // Controlling the generated clusters and regexes with constriants.
            ClusterConstraints();

            // Providing custom tokens to build regexes from.
            CustomTokens();
        }

        private static void BasicUsage()
        {
            // The inputs are a set of names in different formats.
            var session = new Session();
            session.Inputs.Add(Names);

            // Learn a set of patterns, one for each pattern.
            var patterns = session.LearnPatterns();
            Debug.Assert(patterns.Count == 3);
            var expectedRegexes = new[]
            {
                "^[A-Z][a-z]+[\\s][A-Z][a-z]+$",
                "^[A-Z][a-z]+,[\\s][A-Z][a-z]+$",
                "^[A-Z]\\.[\\s][A-Z][a-z]+$",
            };
            Debug.Assert(patterns.Select(x => x.Regex).ConvertToHashSet().SetEquals(expectedRegexes));

            // Each pattern object contains additional information.
            var pattern = patterns.First(p => p.Regex == "^[A-Z][a-z]+[\\s][A-Z][a-z]+$");
            // Regex that matches the pattern
            Console.WriteLine("Regex: " + pattern.Regex);
            // Human readable description of pattern.
            Console.WriteLine("Description: " + pattern.Description); 
            // A few sample input strings matching the pattern.
            Console.WriteLine("Examples: " + string.Join(", ", pattern.Examples.Select(x => $"\"{x}\"")));
            // What fraction of the input strings match this pattern?
            Console.WriteLine("Matching fraction: " + pattern.MatchingFraction);

            // In case it is known that there is only one format in the input, we can use LearnSinglePattern
            session = new Session();
            session.Inputs.Add(SingleFormatNames);
            var singlePattern = session.LearnSinglePattern();
            Debug.Assert(pattern.Regex == "^[A-Z][a-z]+[\\s][A-Z][a-z]+$");
        }

        private static void ClusterConstraints()
        {
            // The inputs are a set of dates in multiple different formats.
            var session = new Session();
            session.Inputs.Add(Dates);

            // Learn patterns produces 6 patterns, one each for every format. 
            // However, dates like "8-Oct-43" and "21-Feb-73" are categories separately.
            var patterns = session.LearnPatterns();
            var patternRegexes = patterns.Select(x => x.Regex).ConvertToHashSet();
            Debug.Assert(patterns.Count == 6);
            var expectedRegexes = new[] {
                "^1[0-9]{3}$",                                      // Like "1928"
                "^[0-9]{2}-[A-Z][a-z]+-[0-9]{2}$",                  // Like "21-Feb-73"
                "^[0-9]+[\\s][A-Z][a-z]+[\\s]1[0-9]{3}[\\s]$",      // Like "4 July 1767 "
                "^[0-9]-[A-Z][a-z]+-[0-9]{2}$",                     // Like "8-Oct-43"
                "^2\\ January\\ 1920a[\\s]$",                       // Outlier "2 January 1920a "
                "^Unknown[\\s]$"};                                 // Outlier "Unknown "
            Debug.Assert(patternRegexes.SetEquals(expectedRegexes));

            // To combine the two oversplit patterns (like "8-Oct-43" and like "21-Feb-73"), we can use an additional constraint.
            session.Constraints.Add(new InSameCluster(new List<string> { "21-Feb-73", "8-Oct-43" }));
            patterns = session.LearnPatterns();
            patternRegexes = patterns.Select(x => x.Regex).ConvertToHashSet();
            Debug.Assert(patterns.Count == 5);
            expectedRegexes = new[] {
                "^1[0-9]{3}$",                                      // Like "1928"
                "^[0-9]+-[A-Z][a-z]+-[0-9]{2}$",                    // Like "21-Feb-73" and "8-Oct-43"
                "^[0-9]+[\\s][A-Z][a-z]+[\\s]1[0-9]{3}[\\s]$",      // Like "4 July 1767 "
                "^2\\ January\\ 1920a[\\s]$",                       // Outlier "2 January 1920a "
                "^Unknown[\\s]$"};                                 // Outlier "Unknown "
            Debug.Assert(patternRegexes.SetEquals(expectedRegexes));
        }

        private static void CustomTokens() {
            var session = new Session();
            var allowedTokens = new AllowedTokens<string, bool>(DefaultTokens.AllTokens);
             // All tokens
            session.Constraints.Add(allowedTokens);
            session.Inputs.Add(SingleFormatNames);
            var pattern = session.LearnSinglePattern();
            Debug.Assert(pattern.Regex == @"^[A-Z][a-z]+[\s][A-Z][a-z]+$");
            Debug.Assert(pattern.Description == "TitleWord & [Space]{1} & TitleWord");

            session = new Session();
            // Only alpha (no lower and upper separately) and space
            allowedTokens = new AllowedTokens<string, bool>(DefaultTokens.Alpha, DefaultTokens.Space);
            session.Constraints.Add(allowedTokens);
            session.Inputs.Add(SingleFormatNames);
            pattern = session.LearnSinglePattern();
            Debug.Assert(pattern.Regex == @"^[a-zA-Z]+[\s][a-zA-Z]+$");
            Debug.Assert(pattern.Description == "[Alpha]+ & [Space]{1} & [Alpha]+");

            session = new Session();
            // You can also define your own custom tokens out of regexes.
            var dateToken = new RegexToken("Date", @"\d\d-\d\d-\d\d\d\d", score: -2);
            var tokens = DefaultTokens.AllTokens.ToList();
            tokens.Add(dateToken);
            allowedTokens = new AllowedTokens<string, bool>(tokens);
            session.Constraints.Add(allowedTokens);
            session.Inputs.Add(new[] {
                "versions/08-07-2020/file1.xml",
                "versions/24-06-2019/file2.xml",
                "versions/30-11-2019/statistics.xml",
            });
            pattern = session.LearnSinglePattern();
            Debug.Assert(pattern.Regex == @"^versions/\d\d-\d\d-\d\d\d\d/[0-9a-zA-Z]+\.xml$");
            Debug.Assert(pattern.Description == "Const[versions/] & Date & Const[/] & [AlphaDigit]+ & Const[.xml]");
        }

        // Sample data
        private static string[] Names = new[]
        {
            "Rowan Murphy",
            "Miguel Reyes",
            "Felix Henderson",
            "C. Baker",
            "E. Lopez",
            "R. Kline",
            "Edwards, Logan",
            "Harris, Michelle",
            "Sullivan, Maria",
            "Wagner, Nicole",
        };

        private static string[] Dates = new[]
        {
            "21-Feb-73",
            "2 January 1920a ",
            "4 July 1767 ",
            "1892",
            "11 August 1897 ",
            "11 November 1889 ",
            "9-Jul-01",
            "17-Sep-08",
            "10-May-35",
            "7-Jun-52",
            "24 July 1802 ",
            "25 April 1873 ",
            "24 August 1850 ",
            "Unknown ",
            "1058",
            "8 August 1876 ",
            "26 July 1165 ",
            "28 December 1843 ",
            "22-Jul-46",
            "17 January 1871 ",
            "17-Apr-38",
            "28 February 1812 ",
            "1903",
            "1915",
            "1854",
            "9 May 1828 ",
            "28-Jul-32",
            "25-Feb-16",
            "19-Feb-40",
            "10-Oct-50",
            "5 November 1880 ",
            "1928",
            "13-Feb-03",
            "8-Oct-43",
            "1445",
            "8 July 1859 ",
            "25-Apr-27",
            "25 November 1562 ",
            "2-Apr-10",
        };

        private static string[] SingleFormatNames = new[]
        {
            "Rowan Murphy",
            "Miguel Reyes",
            "Felix Henderson",
            "Cameron Baker",
            "Eugenia Lopez",
            "Robin Kline",
            "Logan Edwards",
            "Michelle Harris",
            "Maria Sullivan",
            "Nicole Wagner",
        };
    }
}
