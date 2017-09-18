using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Matching.Text;
using Microsoft.ProgramSynthesis.Matching.Text.Learning;
using Microsoft.ProgramSynthesis.Matching.Text.Semantics;
using Microsoft.ProgramSynthesis.Utils;
using MTLearner = Microsoft.ProgramSynthesis.Matching.Text.Learner;
using MTProgram = Microsoft.ProgramSynthesis.Matching.Text.Program;
using MTSession = Microsoft.ProgramSynthesis.Matching.Text.Session;

namespace ProseDemo {
    public static class MatchingUtils {
        public static List<string[]> LoadData(string filename, int limit = 1000) {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
                return LoadData(stream, limit);
            }
        }

        public static List<string[]> LoadData(Stream stream, int limit = 1000) {
            var data = new List<string[]>();
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader)) {
                while (csv.Read()) data.Add(csv.CurrentRecord);
            }
            return data.DeterministicallySample(limit).ToList();
        }

        private static string PrettyDescription(this ProgramNode pattern) =>
            PrettyDescription(pattern.AcceptVisitor(new TokensCollector()));

        public static string PrettyDescription(this IEnumerable<IToken> tokens) {
            string ReadableName(IToken t) => t.Name.Contains("|") ? $"[{t.Name}]" : t.Name;
            string TokenDescription(IToken t) {
                switch (t) {
                    case CharClassToken cct when cct.RequiredLength == null:
                        return $"{ReadableName(cct)}+";
                    case CharClassToken cct:
                        string name = ReadableName(cct);
                        return cct.RequiredLength > 1 ? $"{cct.RequiredLength.Value}×{name}" : name;
                    case ConstantToken ct:
                        return $"“{ct.Constant}”";
                    default:
                        return t.Description;
                }
            }

            return tokens == null ? "<UNKNOWN>" : string.Join(" ", tokens.Select(TokenDescription));
        }

        public static string[] GetPatterns(this MTProgram program) => program
            .ProgramNode.GetFeatureValue(MTLearner.Instance.DisjunctsFeature)
            .Select(PrettyDescription)
            .ToArray();
    }

    public static class ColumnMatching {
        private const int MaxDisjuncts = 6;

        public static void Main(string[] args) {
            Console.OutputEncoding = Encoding.Unicode;
            List<string[]> data = MatchingUtils.LoadData("911.csv");
            List<string> descColumn = data.Select(r => r[2]).ToList();
            ProfileText(descColumn);
        }

        private static MTProgram Profile(IReadOnlyCollection<string> data, int? sampleSize = 1000) {
            IEnumerable<string> sample =
                sampleSize.HasValue ? data.DeterministicallySample(sampleSize.Value) : data;
            using (var session = new MTSession()) {
                session.AddInputs(sample);
                session.AddConstraints(new DisjunctionsLimit<string, bool>(1, MaxDisjuncts));
                return session.Learn();
            }
        }

        private static List<int>[] ProfileText(List<string> column) {
            Console.WriteLine("Profiling the 'desc' column...\n");
            Program discriminator = Profile(column);
            string[] patterns = discriminator.GetPatterns();
            for (var i = 0; i < patterns.Length; i++) {
                string pattern = patterns[i];
                Console.WriteLine($"#{i + 1}: {pattern}\n");
            }

            Dictionary<string, int> patternIndex = patterns.Select((s, i) => new KeyValuePair<string, int>(s, i))
                                                           .ToDictionary();

            var clustering = new List<int>[patterns.Length];
            for (var i = 0; i < clustering.Length; i++) clustering[i] = new List<int>();

            Console.WriteLine("Clustering the 'desc' column based on the learnt patterns...");

            var others = 0;
            for (var i = 0; i < column.Count; i++) {
                string descValue = column[i];
                string pattern = discriminator.GetMatchingTokens(descValue).PrettyDescription();
                if (patternIndex.TryGetValue(pattern, out var index)) clustering[index].Add(i);
                else ++others;
            }

            for (var i = 0; i < clustering.Length; i++) {
                Console.WriteLine($"Cluster #{i + 1}: {clustering[i].Count} rows");
            }

            Console.WriteLine($"Others: {others}");
            return clustering;
        }
    }
}
