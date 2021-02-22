using System;
using Microsoft.ProgramSynthesis.Wrangling.Tree;

namespace MergeConflictsResolution {
    internal static class Utils {
        internal const string Include = "#include";

        internal const string Path = "path";

        internal static string NormalizeInclude(this string include) 
            => include.Replace("\n", "")
                .Replace("\\n", "")
                .Replace("\r", "")
                .Replace(Include, "")
                .Replace(" ", "")
                .Replace("\"", "")
                .Replace("'", "");

        internal static string[] SplitLines(this string s) 
            => s?.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None) ?? new string[0];

        internal static string GetPath(this Node node) => node.Attributes.TryGetValue(Path, out string path) ? path : null;
    }
}
