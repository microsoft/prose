using System;
using System.IO;
using Microsoft.ProgramSynthesis;

namespace BenchmarkDriver
{
    /// <summary>
    ///     Abstract base class for runners.
    /// </summary>
    /// <typeparam name="TProgram">The type of program learned.</typeparam>
    internal abstract class AbstractRunner<TProgram> : IRunner where TProgram : IProgram
    {
        protected AbstractRunner(string reportDirPath)
        {
            ReportDirPath = Path.GetFullPath(reportDirPath);
        }

        /// <summary>
        ///     true iff the learned program computes the expected result.
        /// </summary>
        protected bool Success { get; set; }

        /// <summary>
        ///     The learned program.
        /// </summary>
        protected TProgram Program { get; set; }

        /// <summary>
        ///     Directory in which to output validation results.
        /// </summary>
        protected string ReportDirPath { get; }

        /// <summary>
        ///     If <see cref="Success" />, then this file will be created.
        /// </summary>
        protected string PassFilePath => Path.Combine(ReportDirPath, "pass.txt");

        /// <summary>
        ///     If not <see cref="Success" />, then this file will be created.
        /// </summary>
        protected string FailFilePath => Path.Combine(ReportDirPath, "fail.txt");

        /// <summary>
        ///     Path to file containing a serialized representation of the learned program.
        /// </summary>
        protected string ProgramFilePath => Path.Combine(ReportDirPath, "program.xml");

        /// <inheritdoc />
        public abstract void Run();

        /// <summary>
        ///     Record validation results to disk.
        /// </summary>
        protected virtual void RecordResult()
        {
            Console.WriteLine(Success ? "Pass" : "Fail");
            if (Success)
            {
                Utils.Write(ProgramFilePath, Program?.Serialize() ?? "");
                Utils.Write(PassFilePath, "");
                return;
            }
            Utils.Write(FailFilePath, "");
        }
    }
}