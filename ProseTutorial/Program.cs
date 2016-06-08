using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Diagnostics;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Specifications.Extensions;
using Microsoft.ProgramSynthesis.Learning;
using static ProseTutorial.Utils;

namespace ProseTutorial
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            LoadAndTestSubstrings();
        }

        private static void LoadAndTestSubstrings()
        {
            var grammar = ProseTutorial.Substrings.Language.Grammar;
            if (grammar == null) return;

            ProgramNode p = ProgramNode.Parse(@"SubStr(v, PosPair(AbsPos(v, -4), AbsPos(v, -1)))",
                                              grammar, ASTSerializationFormat.HumanReadable);
            const string data = "Microsoft PROSE SDK";
            State input = State.Create(grammar.InputSymbol, data);
            Console.WriteLine(p.Invoke(input));
        }
    }
}
