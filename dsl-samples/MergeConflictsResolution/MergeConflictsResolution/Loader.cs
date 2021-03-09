using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Wrangling;
using Microsoft.ProgramSynthesis.Wrangling.Tree;
using System.Collections.Generic;

namespace MergeConflictsResolution
{
    /// <summary>
    ///     Class for loading a <see cref="Program" /> from its deserialized string.
    /// </summary>
    public class Loader : SimpleProgramLoader<Program, MergeConflict, IReadOnlyList<Node>> {
        private Loader() { }

        public static Loader Instance { get; } = new Loader();

        protected override Grammar Grammar => LanguageGrammar.Instance.Grammar;

        public override Program Create(ProgramNode program) => new Program(program);
    }
}
