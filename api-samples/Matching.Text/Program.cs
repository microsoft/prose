using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis.Matching.Text;
using Microsoft.ProgramSynthesis.Translation;
using Microsoft.ProgramSynthesis.Matching.Text.Translation.Python;
using Microsoft.ProgramSynthesis.Utils;
using Microsoft.ProgramSynthesis.Translation.Python;

namespace Split.Text
{
    internal static class Program {

        private static readonly string[] Data = new[] {
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
            "2-Apr-10"
        };

        private static void Main(string[] args) {
            // create a new Matching.Text session and add input data
            var session = new Session();
            session.Inputs.Add(Data);

            // Learn patterns
            var patterns = session.LearnPatterns();
            foreach ((var i, var pattern) in patterns.Enumerate()) {
                Console.WriteLine($"Pattern {i}: {pattern.Description}");
                Console.WriteLine($"  Regex: {pattern.Regex}");
            }

            // Python code
            var program = session.Learn();
            var module = new MatchingTextPythonModule(
                program, null, null, null,
                new MatchingTextPythonModule.TranslationOptions(PythonTarget.Library)
            );
            var pythonCode = module.GenerateClusteringCode(OptimizeFor.Nothing, "classify");
            Console.WriteLine("======================");
            Console.WriteLine($"{pythonCode}");
        }
    }
}
