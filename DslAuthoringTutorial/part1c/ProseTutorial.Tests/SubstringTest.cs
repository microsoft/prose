using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Diagnostics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.VersionSpace;

namespace ProseTutorial {
    [TestClass]
    public class SubstringTest {
        private const string GrammarPath = @"../../../../ProseTutorial/synthesis/grammar/substring.grammar";

        [TestMethod]
        public void TestLearnSubstringPositiveAbsPos() {
            //parse grammar file 
            Result<Grammar> grammar = CompileGrammar();
            //configure the prose engine 
            SynthesisEngine prose = ConfigureSynthesis(grammar.Value);

            //create the example
            State input = State.CreateForExecution(grammar.Value.InputSymbol, "19-Feb-1960");
            var examples = new Dictionary<State, object> { { input, "Feb" } };
            var spec = new ExampleSpec(examples);

            //learn the set of programs that satisfy the spec 
            ProgramSet learnedSet = prose.LearnGrammar(spec);

            //run the first synthesized program in the same input and check if 
            //the output is correct
            IEnumerable<ProgramNode> programs = learnedSet.RealizedPrograms;
            var output = programs.First().Invoke(input) as string;
            Assert.AreEqual("Feb", output);

            State differentInput = State.CreateForExecution(grammar.Value.InputSymbol, "15-Jan-2000");
            output = programs.First().Invoke(differentInput) as string;
            Assert.AreEqual("Jan", output);
        }

        [TestMethod]
        public void TestLearnSubstringPositiveAbsPosSecOcurrence() {
            Result<Grammar> grammar = CompileGrammar();
            SynthesisEngine prose = ConfigureSynthesis(grammar.Value);

            State firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "16-Feb-2016");
            State secondInput = State.CreateForExecution(grammar.Value.InputSymbol, "14-Jan-2012");
            var examples = new Dictionary<State, object> { { firstInput, "16" }, { secondInput, "12" } };
            var spec = new ExampleSpec(examples);

            ProgramSet learnedSet = prose.LearnGrammar(spec);

            IEnumerable<ProgramNode> programs = learnedSet.RealizedPrograms;
            ProgramNode firstProgram = programs.First();
            var output = firstProgram.Invoke(firstInput) as string;
            Assert.AreEqual("16", output);
            output = firstProgram.Invoke(secondInput) as string;
            Assert.AreEqual("12", output);
            State differentInput = State.CreateForExecution(grammar.Value.InputSymbol, "15-Apr-1500");
            output = programs.First().Invoke(differentInput) as string;
            Assert.AreEqual("00", output);
        }

        [TestMethod]
        public void TestLearnSubstringPositiveAbsPosSecOcurrenceOneExp() {
            Result<Grammar> grammar = CompileGrammar();
            SynthesisEngine prose = ConfigureSynthesis(grammar.Value);

            State firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "16-Feb-2016");
            var examples = new Dictionary<State, object> { { firstInput, "16" } };
            var spec = new ExampleSpec(examples);

            ProgramSet learnedSet = prose.LearnGrammar(spec);

            IEnumerable<ProgramNode> programs = learnedSet.RealizedPrograms;
            var output = programs.First().Invoke(firstInput) as string;
            Assert.AreEqual("16", output);

            //checks whether the total number of synthesized programs was exactly 2 for this ambiguous example. 
            Assert.AreEqual(8, programs.Count());
        }


        [TestMethod]
        public void TestLearnSubstringNegativeAbsPos() {
            Result<Grammar> grammar = CompileGrammar();
            SynthesisEngine prose = ConfigureSynthesis(grammar.Value);

            State firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "(Gustavo Soares)");
            State secondInput = State.CreateForExecution(grammar.Value.InputSymbol, "(Titus Barik)");
            var examples =
                new Dictionary<State, object> { { firstInput, "Gustavo Soares" }, { secondInput, "Titus Barik" } };
            var spec = new ExampleSpec(examples);

            ProgramSet learnedSet = prose.LearnGrammar(spec);

            IEnumerable<ProgramNode> programs = learnedSet.RealizedPrograms;
            var output = programs.First().Invoke(firstInput) as string;
            Assert.AreEqual("Gustavo Soares", output);
            output = programs.First().Invoke(secondInput) as string;
            Assert.AreEqual("Titus Barik", output);
            State differentInput = State.CreateForExecution(grammar.Value.InputSymbol, "(Alan Leung)");
            output = programs.First().Invoke(differentInput) as string;
            Assert.AreEqual("Alan Leung", output);
        }

        [TestMethod]
        public void TestLearnSubstringNegativeAbsPosRanking() {
            Result<Grammar> grammar = CompileGrammar();
            SynthesisEngine prose = ConfigureSynthesis(grammar.Value);

            State firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "(Gustavo Soares)");
            var examples = new Dictionary<State, object> { { firstInput, "Gustavo Soares" } };
            var spec = new ExampleSpec(examples);

            RankingScore scoreFeature = new RankingScore(grammar.Value);
            ProgramSet topPrograms = prose.LearnGrammarTopK(spec, scoreFeature, 1, null);
            ProgramNode topProgram = topPrograms.RealizedPrograms.First();

            var output = topProgram.Invoke(firstInput) as string;
            Assert.AreEqual("Gustavo Soares", output);
            State secondInput = State.CreateForExecution(grammar.Value.InputSymbol, "(Titus Barik)");
            output = topProgram.Invoke(secondInput) as string;
            Assert.AreEqual("Titus Barik", output);
        }

        [TestMethod]
        public void TestLearnSubstringTwoExamples() {
            Result<Grammar> grammar = CompileGrammar();
            SynthesisEngine prose = ConfigureSynthesis(grammar.Value);

            State firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "Gustavo Soares");
            State secondInput = State.CreateForExecution(grammar.Value.InputSymbol, "Sumit Gulwani");
            var examples = new Dictionary<State, object> { { firstInput, "Soares" }, { secondInput, "Gulwani" } };
            var spec = new ExampleSpec(examples);

            ProgramSet learnedSet = prose.LearnGrammar(spec);
            IEnumerable<ProgramNode> programs = learnedSet.RealizedPrograms;
            var output = programs.First().Invoke(firstInput) as string;
            Assert.AreEqual("Soares", output);
            var output2 = programs.First().Invoke(secondInput) as string;
            Assert.AreEqual("Gulwani", output2);
            State differentInput = State.CreateForExecution(grammar.Value.InputSymbol, "Alan Leung");
            output = programs.First().Invoke(differentInput) as string;
            Assert.AreEqual("Leung", output);
        }

        [TestMethod]
        public void TestLearnSubstringOneExample() {
            Result<Grammar> grammar = CompileGrammar();
            SynthesisEngine prose = ConfigureSynthesis(grammar.Value);

            State input = State.CreateForExecution(grammar.Value.InputSymbol, "Gustavo Soares");
            var examples = new Dictionary<State, object> { { input, "Soares" } };

            var spec = new ExampleSpec(examples);

            var scoreFeature = new RankingScore(grammar.Value);
            ProgramSet topPrograms = prose.LearnGrammarTopK(spec, scoreFeature, 1, null);
            ProgramNode topProgram = topPrograms.RealizedPrograms.First();
            var output = topProgram.Invoke(input) as string;
            Assert.AreEqual("Soares", output);

            State input2 = State.CreateForExecution(grammar.Value.InputSymbol, "Sumit Gulwani");
            var output2 = topProgram.Invoke(input2) as string;
            Assert.AreEqual("Gulwani", output2);
        }

        public static SynthesisEngine ConfigureSynthesis(Grammar grammar) {
            var witnessFunctions = new WitnessFunctions(grammar);
            var deductiveSynthesis = new DeductiveSynthesis(witnessFunctions);
            var synthesisExtrategies = new ISynthesisStrategy[] { deductiveSynthesis };
            var synthesisConfig = new SynthesisEngine.Config { Strategies = synthesisExtrategies };
            var prose = new SynthesisEngine(grammar, synthesisConfig);
            return prose;
        }

        private static Result<Grammar> CompileGrammar() {
            return DSLCompiler.Compile(new CompilerOptions() {
                InputGrammarText = File.ReadAllText(GrammarPath),
                References = CompilerReference.FromAssemblyFiles(typeof(Semantics).GetTypeInfo().Assembly)
            });
        }
    }
}
