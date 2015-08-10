using System;
using System.Diagnostics.CodeAnalysis;

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
    }
}
