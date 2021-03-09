namespace BenchmarkDriver
{
    /// <summary>
    ///     Interface for classes that implement benchmark execution and validation logic.
    /// </summary>
    internal interface IRunner
    {
        /// <summary>
        ///     Execute the relevant learner(s) and validate their results.
        /// </summary>
        void Run();
    }
}