using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Specifications;

namespace Microsoft.ProgramSynthesis.FlashFill.Sample
{
    /// <summary>
    ///     Simplified version of <see cref="FlashFillProgram" /> to demonstrate lower level API usage.
    /// </summary>
    public class FlashFillProg
    {
        /// <summary>
        ///     Constructor for a FlashFill Program.
        /// </summary>
        /// <param name="program">The learnt program.</param>
        private FlashFillProg(ProgramNode program)
        {
            ProgramNode = program;
        }

        public ProgramNode ProgramNode { get; set; }

        /// <summary>
        ///     Learn <paramref name="k" /> top-ranked FlashFill programs for a given set of input-output examples.
        /// </summary>
        /// <param name="trainingExamples">
        ///     The set of input-output examples as a Tuple of the input and the output.
        /// </param>
        /// <param name="additionalInputs">
        ///     The set of additional inputs that do not have output examples, which helps rank learnt programs.
        /// </param>
        /// <param name="k">the number of top programs</param>
        /// <returns>The top-k ranked programs as <see cref="FlashFillProg" />s</returns>
        public static IEnumerable<FlashFillProg> LearnTopK(IDictionary<string, string> trainingExamples,
            IEnumerable<string> additionalInputs = null, int k = 1)
        {
            if (trainingExamples == null) throw new ArgumentNullException("trainingExamples");
            // Load FlashFill grammar
            Grammar grammar = FlashFillGrammar.Grammar;

            // Setup configuration of synthesis process.
            var engine = new SynthesisEngine(grammar, new SynthesisEngine.Config
            {
                // Strategies perform the actual logic of the synthesis process.
                Strategies = new[] {typeof (DeductiveSynthesis)},
                UseThreads = false,
                CacheSize = int.MaxValue
            });
            // Convert the examples in the format expected by Microsoft.ProgramSynthesis.
            // Internally, FlashFill represents strings as StringRegions to save on
            //  allocating new strings for each substring.
            // Could also use FlashFillInput.AsState() to construct the input state.
            Dictionary<State, object> trainExamples = trainingExamples.ToDictionary(
                t => State.Create(grammar.InputSymbol, new[] {StringRegion.Create(t.Key)}),
                t => (object) StringRegion.Create(t.Value));
            var spec = new ExampleSpec(trainExamples);
            // Learn an entire FlashFill program (i.e. start at the grammar's start symbol)
            //  for the specificiation consisting of the examples.
            // Learn the top-k programs according to the score feature used by FlashFill by default.
            // You could define your own feature on the FlashFill grammar to rank programs differently.
            var task = new LearningTask(grammar.StartSymbol, spec, k, FlashFillGrammar.ScoreFeature);
            if (additionalInputs != null)
            {
                task.AdditionalInputs =
                    additionalInputs.Select(
                        input => State.Create(grammar.InputSymbol, new[] {StringRegion.Create(input)})).ToList();
            }
            IEnumerable<ProgramNode> topk = engine.LearnSymbol(task).RealizedPrograms;
            // Return the generated programs wraped in a FlashFillProg object.
            return topk.Select(prog => new FlashFillProg(prog));
        }

        /// <summary>
        ///     Run the program on a given input
        /// </summary>
        /// <param name="input">The input</param>
        /// <returns></returns>
        public string Run(string input)
        {
            Grammar grammar = FlashFillGrammar.Grammar;
            State inputState = new FlashFillInput(input).AsState();
            // Same as above without using the FlashFillInput class:
            inputState = State.Create(grammar.InputSymbol, new[] {StringRegion.Create(input)});
            var result = (StringRegion) ProgramNode.Invoke(inputState);
            return result == null ? null : result.Value;
        }
    }
}