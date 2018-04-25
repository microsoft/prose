using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Diagnostics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ProgramSynthesis.Learning.Strategies;

namespace ProseTutorial {
    [TestClass]
    public class SubstringTest {

        private const string GrammarPath = @"../../../../ProseTutorial/synthesis/grammar/substring.grammar";

        [TestMethod]
        public void TestLearnSubstringPositiveAbsPos() {
            //parse grammar file 
            var grammar = CompileGrammar();
            //configure the prose engine 
            var prose = ConfigureSynthesis(grammar.Value);

            //create the example
            var input = State.CreateForExecution(grammar.Value.InputSymbol, "19-Feb-1960");
            var examples = new Dictionary<State, object> { { input, "Feb" } };
            var spec = new ExampleSpec(examples);

            //learn the set of programs that satisfy the spec 
            var learnedSet = prose.LearnGrammar(spec);

            //run the first synthesized program in the same input and check if 
            //the output is correct
            var programs = learnedSet.RealizedPrograms;
            var output = programs.First().Invoke(input) as string;
            Assert.AreEqual("Feb", output);
        }

        [TestMethod]
        public void TestLearnSubstringPositiveAbsPosSecOcurrence() {
            var grammar = CompileGrammar();
            var prose = ConfigureSynthesis(grammar.Value);

            var firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "16-Feb-2016");
            var secondInput = State.CreateForExecution(grammar.Value.InputSymbol, "14-Jan-2012");
            var examples = new Dictionary<State, object> { { firstInput, "16" }, { secondInput, "12" } };
            var spec = new ExampleSpec(examples);

            var learnedSet = prose.LearnGrammar(spec);

            var programs = learnedSet.RealizedPrograms;
            var firstProgram = programs.First();
            var output = firstProgram.Invoke(firstInput) as string;
            Assert.AreEqual("16", output);
            output = firstProgram.Invoke(secondInput) as string;
            Assert.AreEqual("12", output);
        }

        [TestMethod]
        public void TestLearnSubstringPositiveAbsPosSecOcurrenceOneExp() {
            var grammar = CompileGrammar();
            var prose = ConfigureSynthesis(grammar.Value);

            var firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "16-Feb-2016");
            var examples = new Dictionary<State, object> { { firstInput, "16" } };
            var spec = new ExampleSpec(examples);

            var learnedSet = prose.LearnGrammar(spec);

            var programs = learnedSet.RealizedPrograms;
            var output = programs.First().Invoke(firstInput) as string;
            Assert.AreEqual("16", output);

            //checks whether the total number of synthesized programs was exactly 2 for this ambiguous example. 
            Assert.AreEqual(8, programs.Count());
        }


        [TestMethod]
        public void TestLearnSubstringNegativeAbsPos() {
            var grammar = CompileGrammar();
            var prose = ConfigureSynthesis(grammar.Value);

            var firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "(Gustavo Soares)");
            var secondInput = State.CreateForExecution(grammar.Value.InputSymbol, "(Titus Barik)");
            var examples = new Dictionary<State, object> { { firstInput, "Gustavo Soares" }, { secondInput, "Titus Barik" } };
            var spec = new ExampleSpec(examples);

            var learnedSet = prose.LearnGrammar(spec);

            var programs = learnedSet.RealizedPrograms;
            var output = programs.First().Invoke(firstInput) as string;
            Assert.AreEqual("Gustavo Soares", output);
            output = programs.First().Invoke(secondInput) as string;
            Assert.AreEqual("Titus Barik", output);
        }

        [TestMethod]
        public void TestLearnSubstringNegativeAbsPosRanking() {
            var grammar = CompileGrammar();
            var prose = ConfigureSynthesis(grammar.Value);

            var firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "(Gustavo Soares)");
            var examples = new Dictionary<State, object> { { firstInput, "Gustavo Soares" } };
            var spec = new ExampleSpec(examples);

            var scoreFeature = new RankingScore(grammar.Value);
            var topPrograms = prose.LearnGrammarTopK(spec, scoreFeature, 1, null);
            var topProgram = topPrograms.RealizedPrograms.First();

            var output = topProgram.Invoke(firstInput) as string;
            Assert.AreEqual("Gustavo Soares", output);
            var secondInput = State.CreateForExecution(grammar.Value.InputSymbol, "(Titus Barik)");
            output = topProgram.Invoke(secondInput) as string;
            Assert.AreEqual("Titus Barik", output);
        }

        [TestMethod]
        public void TestLearnSubstringTwoExamples()
        {
            var grammar = CompileGrammar();
            var prose = ConfigureSynthesis(grammar.Value);

            var firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "Gustavo Soares");
            var secondInput = State.CreateForExecution(grammar.Value.InputSymbol, "Sumit Gulwani");
            var examples = new Dictionary<State, object> { { firstInput, "Soares" }, { secondInput, "Gulwani" } };
            var spec = new ExampleSpec(examples);

            var learnedSet = prose.LearnGrammar(spec);
            var programs = learnedSet.RealizedPrograms;
            var output = programs.First().Invoke(firstInput) as string;
            Assert.AreEqual("Soares", output);
            var output2 = programs.First().Invoke(secondInput) as string;
            Assert.AreEqual("Gulwani", output2);
        }

        [TestMethod]
        public void TestLearnSubstringOneExample()
        {
            var grammar = CompileGrammar();
            var prose = ConfigureSynthesis(grammar.Value);

            var input = State.CreateForExecution(grammar.Value.InputSymbol, "Gustavo Soares");
            var examples = new Dictionary<State, object> { { input, "Soares" } };

            var spec = new ExampleSpec(examples);

            var scoreFeature = new RankingScore(grammar.Value);
            var topPrograms = prose.LearnGrammarTopK(spec, scoreFeature, 1, null);
            var topProgram = topPrograms.RealizedPrograms.First();
            var output = topProgram.Invoke(input) as string;
            Assert.AreEqual("Soares", output);

            var input2 = State.CreateForExecution(grammar.Value.InputSymbol, "Sumit Gulwani");
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
            return DSLCompiler.
                Compile(new CompilerOptions() {
                    InputGrammarText = File.ReadAllText(GrammarPath),
                    References = CompilerReference.FromAssemblyFiles(typeof(Semantics).GetTypeInfo().Assembly)
                });
        }
    }
}