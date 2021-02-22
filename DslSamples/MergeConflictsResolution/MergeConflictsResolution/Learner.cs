using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.ProgramSynthesis.Features;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Wrangling.Tree;
using Microsoft.ProgramSynthesis.VersionSpace;
using Microsoft.ProgramSynthesis.Wrangling;
using Microsoft.ProgramSynthesis.Wrangling.Constraints;
using Microsoft.ProgramSynthesis;

namespace MergeConflictsResolution
{
    /// <summary>
    ///     Class for learning <see cref="Program" />.
    /// </summary>
    public class Learner : ProgramLearner<Program, MergeConflict, IReadOnlyList<Node>>
    {
        private Learner() : base(supportsProgramSampling: false, supportsArbitraryFeatures: false) { }

        public override Feature<double> ScoreFeature { get; } = new RankingScore(LanguageGrammar.Instance.Grammar);

        public static Learner Instance { get; } = new Learner();

        protected override ProgramCollection<Program, MergeConflict, IReadOnlyList<Node>, TFeatureValue> LearnTopKUnchecked<TFeatureValue>(
            IEnumerable<Constraint<MergeConflict, IReadOnlyList<Node>>> constraints,
            Feature<TFeatureValue> feature,
            int k,
            int? numRandomProgramsToInclude,
            ProgramSamplingStrategy samplingStrategy,
            IEnumerable<MergeConflict> additionalInputs = null,
            CancellationToken? cancel = null,
            Guid? guid = null)
        {
            var result = LearnImpl(constraints, feature, k, numRandomProgramsToInclude, samplingStrategy, cancel);
            if (result == null || result.IsEmpty)
            {
                return ProgramCollection<Program, MergeConflict, IReadOnlyList<Node>, TFeatureValue>.Empty;
            }

            var pruned = result as PrunedProgramSet;
            return ProgramCollection<Program, MergeConflict, IReadOnlyList<Node>, TFeatureValue>.From(pruned, p => new Program(p), feature);
        }

        private ProgramSet LearnImpl<TFeatureValue>(IEnumerable<Constraint<MergeConflict, IReadOnlyList<Node>>> constraints,
                                                    Feature<TFeatureValue> feature,
                                                    int? k,
                                                    int? numRandomProgramsToInclude,
                                                    ProgramSamplingStrategy samplingStrategy,
                                                    CancellationToken? cancel = null)
        {
            Grammar grammar = LanguageGrammar.Instance.Grammar;
            Dictionary<State, object> examples =
                constraints.OfType<Example<MergeConflict, IReadOnlyList<Node>>>()
                           .ToDictionary(
                                e => State.CreateForLearning(grammar.InputSymbol, e.Input),
                                e => (object)e.Output);
            var spec = new ExampleSpec(examples);
            var witnesses = new WitnessFunctions(grammar);
            var engine = new SynthesisEngine(
                grammar, 
                new SynthesisEngine.Config
                {
                    Strategies = new ISynthesisStrategy[] { new DeductiveSynthesis(witnesses) },
                    UseThreads = false
                });

            LearningTask task = k.HasValue
                ? LearningTask.Create(grammar.StartSymbol, spec, numRandomProgramsToInclude, samplingStrategy, k.Value, feature)
                : new LearningTask(grammar.StartSymbol, spec);

            ProgramSet set = engine.Learn(task, cancel);
            engine.Configuration.LogListener?.SaveLogToXML("log.xml");
            if (k.HasValue)
            {
                return set.Prune(k.Value, numRandomProgramsToInclude, feature, null,
                                 task.FeatureCalculationContext,
                                 samplingStrategy, engine.RandomNumberGenerator, engine.Configuration.LogListener);
            }

            return set;
        }

        public override ProgramSet LearnAll(IEnumerable<Constraint<MergeConflict, IReadOnlyList<Node>>> constraints,
                                            IEnumerable<MergeConflict> additionalInputs = null,
                                            CancellationToken? cancel = null)
            => LearnImpl(constraints, ScoreFeature, null, null, default, cancel);
    }
}