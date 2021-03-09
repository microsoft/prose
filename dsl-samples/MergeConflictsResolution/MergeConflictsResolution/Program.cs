using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Wrangling;
using Microsoft.ProgramSynthesis.Wrangling.Tree;
using static MergeConflictsResolution.Utils;

namespace MergeConflictsResolution
{
    /// <summary>
    ///     Represents a (learned) program that takes a <see cref="MergeConflict"/> and returns a merge resolution.
    /// </summary>
    [DebuggerDisplay("{ProgramNode}")]
    public class Program : TransformationProgram<Program, MergeConflict, IReadOnlyList<Node>>
    {
        internal Program(ProgramNode node) : base(node, node.GetFeatureValue(Learner.Instance.ScoreFeature)) { }

        /// <summary>
        ///     Executes the program on the <paramref name="input" /> to obtain the output.
        /// </summary>
        /// <param name="input">The input token.</param>
        /// <returns>The result output.</returns>
        public override IReadOnlyList<Node> Run(MergeConflict input)
        {
            State inputState = State.CreateForExecution(LanguageGrammar.Instance.Grammar.InputSymbol, input);
            return ProgramNode.Invoke(inputState) as IReadOnlyList<Node>;
        }

        /// <summary>
        ///     Executes the program on the <paramref name="input" /> to obtain the output.
        /// </summary>
        /// <param name="input">The input token.</param>
        /// <returns>The result string output.</returns>
        public string RunString(MergeConflict input)
        {
            return string.Join(
                System.Environment.NewLine,
                Run(input).Select(n => Semantics.NodeValue(n, Path))
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Select(path => $"{Include} \"{path}\""));
        }

        /// <summary>
        ///     Serializes a program to a string that can be loaded using <see cref="Loader.Load" />.
        /// </summary>
        /// <returns>A machine-readable string representation of this program.</returns>
        public string Serialize() => ProgramNode.PrintAST();

        /// <summary>
        ///     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        ///     true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Program other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(ProgramNode, other.ProgramNode);
        }

        /// <summary>
        ///     Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        ///     true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Program)obj);
        }

        /// <summary>
        ///     Serves as the default hash function.
        /// </summary>
        /// <returns>
        ///     A hash code for the current object.
        /// </returns>
        public override int GetHashCode() => ProgramNode?.GetHashCode() ?? 0;

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///     A string that represents the current object.
        /// </returns>
        public override string ToString() => ProgramNode.ToString();
    }
}