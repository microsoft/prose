using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace ProseTutorial.Substrings
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class Semantics
    {
        public static string SubStr(string v, Tuple<uint?, uint?> posPair)
        {
            uint? start = posPair.Item1, end = posPair.Item2;
            if (start == null || end == null || start < 0 || start >= v.Length || end <= 0 || end > v.Length)
                return null;
            return v.Substring((int) start, (int) (end - start));
        }

        public static uint? AbsPos(string v, int k)
        {
            if (Math.Abs(k) > v.Length + 1) return null;
            return (uint?) (k > 0 ? k - 1 : (v.Length + k + 1));
        }

        public static uint? RegPos(string v, Tuple<Regex, Regex> rr, int k)
        {
            var r = new Regex($"(?<={rr.Item1}){rr.Item2}");
            MatchCollection ms = r.Matches(v);
            int index = k > 0 ? (k - 1) : (ms.Count + k);
            return (index < 0 || index >= ms.Count) ? null : (uint?) ms[index].Index;
        }
    }
}
