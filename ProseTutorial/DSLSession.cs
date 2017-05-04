using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Logging;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.VersionSpace;
using Microsoft.ProgramSynthesis.Wrangling.Constraints;
using Microsoft.ProgramSynthesis.Wrangling.Logging;
using Microsoft.ProgramSynthesis.Wrangling.Session;
using Newtonsoft.Json;

namespace ProseTutorial {
    public class DSLProgram : Program<StringRegion, StringRegion> {
        public DSLProgram(ProgramNode programNode, double score) : base(programNode, score) { }

        public override StringRegion Run(StringRegion input) => ProgramNode.Invoke(
            State.Create(Substrings.Language.Grammar.InputSymbol, input)) as StringRegion;
    }

    public class DSLProgramSetWrapper : Session<DSLProgram, StringRegion, StringRegion>.IProgramSetWrapper {
        public DSLProgramSetWrapper(ProgramSet programSet, Feature<double> score) {
            ProgramSet = programSet;
            Score = score;
        }

        public IEnumerable<DSLProgram> RealizedPrograms => ProgramSet.RealizedPrograms.Select(
            p => new DSLProgram(
                p, p.GetFeatureValue(Score)));

        public ProgramSet ProgramSet { get; }
        public Feature<double> Score { get; }
    }

    public class DSLSession : Session<DSLProgram, StringRegion, StringRegion> {
        private List<DSLProgram> _lastTopK;
        private DSLProgramSetWrapper _lastSet;
        public static Grammar Grammar => Substrings.Language.Grammar;
        public Feature<double> RankingScore { get; } = new Substrings.RankingScore(Grammar);
        public DomainLearningLogic LearningLogic { get; } = new Substrings.WitnessFunctions(Grammar);

        public DSLSession(IJournalStorage journalStorage = null,
                          CultureInfo culture = null,
                          ILogger logger = null) : base(
            journalStorage, culture ?? CultureInfo.InvariantCulture, logger) { }

        private SynthesisEngine CreateEngine(bool log = false) =>
            new SynthesisEngine(Grammar,
                                new SynthesisEngine.Config {
                                    UseThreads = false,
                                    Strategies = new ISynthesisStrategy[] {
                                        new EnumerativeSynthesis(),
                                        new DeductiveSynthesis(LearningLogic),
                                    },
                                    LogListener = log ? new LogListener() : null,
                                });

        private static ExampleSpec CreateSpec(LearnProgramRequest<DSLProgram, StringRegion, StringRegion> request) {
            var examples = request.Constraints.OfType<Example<StringRegion, StringRegion>>();
            var spec = new ExampleSpec(examples.ToDictionary(e => State.Create(Grammar.InputSymbol, e.Input),
                                                             e => (object) e.Output));
            return spec;
        }

        protected override IReadOnlyList<DSLProgram> LearnTopKCached(
            LearnProgramRequest<DSLProgram, StringRegion, StringRegion> request, RankingMode rankingMode, int k,
            CancellationToken cancel) {
            var engine = CreateEngine();
            var spec = CreateSpec(request);
            var learned = engine.LearnGrammarTopK(spec, RankingScore, k, cancel);
            _lastTopK = learned.Select(p => new DSLProgram(p, p.GetFeatureValue(RankingScore))).ToList();
            return _lastTopK;
        }

        protected override IProgramSetWrapper LearnAllCached(
            LearnProgramRequest<DSLProgram, StringRegion, StringRegion> request, CancellationToken cancel) {
            var engine = CreateEngine();
            var spec = CreateSpec(request);
            var learned = engine.LearnGrammar(spec, cancel);
            _lastSet = new DSLProgramSetWrapper(learned, RankingScore);
            return _lastSet;
        }

        public override Task<IReadOnlyList<IQuestion>> GetTopKQuestionsAsync(
            int? k = null, double? confidenceThreshold = null, IEnumerable<Type> allowedTypes = null,
            CancellationToken cancel = new CancellationToken()) {
            throw new NotImplementedException();
        }

        public override Task<IReadOnlyList<SignificantInputCluster<StringRegion>>> GetSignificantInputClustersAsync(
            double? confidenceThreshold = null,
            CancellationToken cancel = new CancellationToken()) {
            throw new NotImplementedException();
        }

        public override Task<IReadOnlyList<SignificantInput<StringRegion>>> GetSignificantInputsAsync(
            double? confidenceThreshold = null, CancellationToken cancel = new CancellationToken()) {
            throw new NotImplementedException();
        }

        public override Task<IReadOnlyList<StringRegion>> ComputeTopKOutputsAsync(
            StringRegion input, int k, RankingMode rankingMode = null,
            double? confidenceThreshold = null, CancellationToken cancel = new CancellationToken()) {
            throw new NotImplementedException();
        }

        protected override JsonSerializerSettings JsonSerializerSettingsInstance { get; } =
            new JsonSerializerSettings();

        protected override string LoggingTypeName => "Sample";
    }
}
