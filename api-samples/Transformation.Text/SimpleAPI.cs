using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Transformation.Text;
using Microsoft.ProgramSynthesis.Transformation.Text.Semantics;
using Microsoft.ProgramSynthesis.Wrangling;

namespace Transformation.Text
{
    /// <summary>
    ///     Simplified version of <see cref="Program" /> to demonstrate lower level API usage.
    /// </summary>
    public class TextTransformationProgram
    {
        /// <summary>
        ///     Constructor for a Transformation.Text Program.
        /// </summary>
        /// <param name="program">The learnt program.</param>
        private TextTransformationProgram(ProgramNode program)
        {
            ProgramNode = program;
        }

        public ProgramNode ProgramNode { get; set; }

        /// <summary>
        ///     Learn <paramref name="k" /> top-ranked Transformation.Text programs for a given set of input-output examples.
        /// </summary>
        /// <param name="trainingExamples">
        ///     The set of input-output examples as a Tuple of the input and the output.
        /// </param>
        /// <param name="additionalInputs">
        ///     The set of additional inputs that do not have output examples, which helps rank learnt programs.
        /// </param>
        /// <param name="k">the number of top programs</param>
        /// <returns>The top-k ranked programs as <see cref="TextTransformationProgram" />s</returns>
        public static IEnumerable<TextTransformationProgram> LearnTopK(IDictionary<string, string> trainingExamples,
                                                                       IEnumerable<string> additionalInputs = null,
                                                                       int k = 1)
        {
            if (trainingExamples == null) throw new ArgumentNullException(nameof(trainingExamples));
            // Load Transformation.Text grammar
            Grammar grammar = Language.Grammar;
            DomainLearningLogic learningLogic = new Witnesses(grammar,
                // This is currently required as a workaround for a bug.
                ((RankingScore)Learner.Instance.ScoreFeature).Clone());

            // Setup configuration of synthesis process.
            var engine = new SynthesisEngine(grammar, new SynthesisEngine.Config
            {
                // Strategies perform the actual logic of the synthesis process.
                Strategies = new[] { new DeductiveSynthesis(learningLogic) },
                UseThreads = false,
                CacheSize = int.MaxValue
            });
            // Convert the examples in the format expected by Microsoft.ProgramSynthesis.
            // Internally, Transformation.Text represents strings as ValueSubstrings to save on
            //  allocating new strings for each substring.
            // Could also use InputRow.AsStateForLearning() to construct the input state.
            Dictionary<State, object> trainExamples = trainingExamples.ToDictionary(
                t => State.CreateForLearning(grammar.InputSymbol, new[] { ValueSubstring.Create(t.Key) }),
                t => (object) ValueSubstring.Create(t.Value));
            var spec = new ExampleSpec(trainExamples);
            // Learn an entire Transformation.Text program (i.e. start at the grammar's start symbol)
            //  for the specification consisting of the examples.
            // Learn the top-k programs according to the score feature used by Transformation.Text by default.
            // You could define your own feature on the Transformation.Text grammar to rank programs differently.
            var task = new LearningTask(grammar.StartSymbol, spec, k, Learner.Instance.ScoreFeature);
            if (additionalInputs != null)
            {
                task.AdditionalInputs =
                    additionalInputs.Select(
                                        input =>
                                            State.CreateForLearning(grammar.InputSymbol, new[] { ValueSubstring.Create(input) }))
                                    .ToList();
            }
            IEnumerable<ProgramNode> topk = engine.Learn(task).RealizedPrograms;
            // Return the generated programs wraped in a TextTransformationProgram object.
            return topk.Select(prog => new TextTransformationProgram(prog));
        }

        /// <summary>
        ///     Run the program on a given input
        /// </summary>
        /// <param name="input">The input</param>
        /// <returns></returns>
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public string Run(string input)
        {
            Grammar grammar = Language.Grammar;
            State inputState = new InputRow(input).AsStateForExecution();
            // Same as above without using the InputRow class:
            inputState = State.CreateForExecution(grammar.InputSymbol, new[] { ValueSubstring.Create(input) });
            var result = (ValueSubstring) ProgramNode.Invoke(inputState);
            return result?.Value;
        }
    }
}
