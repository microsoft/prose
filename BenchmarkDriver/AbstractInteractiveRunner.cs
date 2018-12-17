using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Utils;

namespace BenchmarkDriver
{
    /// <summary>
    ///     Abstract base class for learners that accept examples interactively during learning.
    /// </summary>
    /// <typeparam name="TProgram">The type of program learned.</typeparam>
    /// <typeparam name="TExample">The type of example accepted.</typeparam>
    internal abstract class AbstractInteractiveRunner<TProgram, TExample> : AbstractRunner<TProgram>
        where TExample : class
        where TProgram : IProgram
    {
        protected AbstractInteractiveRunner(string specFilePath, string reportDirPath) : base(reportDirPath)
        {
            SpecFilePath = Path.GetFullPath(specFilePath);
        }

        /// <summary>
        ///     All examples contained in the spec file.
        /// </summary>
        protected List<TExample> AllExamples { get; set; }

        /// <summary>
        ///     All examples used during learning.
        /// </summary>
        protected List<TExample> UsedExamples { get; } = new List<TExample>();

        /// <summary>
        ///     Path to the spec file.
        /// </summary>
        protected string SpecFilePath { get; }

        /// <summary>
        ///     Path to the file in which to write examples used during learning.
        /// </summary>
        protected string UsedExamplesFilePath => Path.Combine(ReportDirPath, "used-examples.json");

        /// <summary>
        ///     Pretty print used examples.
        /// </summary>
        /// <returns>Pretty-printed string containing examples used during learning.</returns>
        protected virtual string PrettyPrintedExamples() =>
            string.Join(Environment.NewLine, UsedExamples.Select(c => c.ToString()));

        /// <inheritdoc />
        protected override void RecordResult()
        {
            Utils.Write(UsedExamplesFilePath, PrettyPrintedExamples());
            base.RecordResult();
        }

        /// <summary>
        ///     Get the next example on which to learn, if one exists. The next example is the first
        ///     unused example on which the currently learned program fails. If no program has been found yet,
        ///     the next example is simply the first unused example.
        /// </summary>
        /// <param name="nextExample">The next example.</param>
        /// <returns>true if there exists a next example, otherwise false.</returns>
        protected bool TryGetNextExample(out TExample nextExample)
        {
            if (UsedExamples.Count == AllExamples.Count)
            {
                nextExample = null;
                return false;
            }

            if (Program == null)
            {
                nextExample = AllExamples.Except(UsedExamples).FirstOrDefault();
                return nextExample != null;
            }

            var firstFailedIndex = AllExamples.Select(e => Valid(e, Program)).IndexOf(false);
            if (firstFailedIndex.HasValue)
            {
                nextExample = AllExamples[firstFailedIndex.Value];
                return true;
            }

            nextExample = null;
            return false;
        }

        /// <summary>
        ///     Check if the given program computes the expected result with respect to the given example.
        /// </summary>
        /// <param name="example">The example to check.</param>
        /// <param name="program">The program to check.</param>
        /// <returns>
        ///     true if <see cref="program" /> computes the expected result with respect to <see cref="example" />, otherwise
        ///     false.
        /// </returns>
        protected abstract bool Valid(TExample example, TProgram program);
    }
}