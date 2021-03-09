using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Diagnostics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.VersionSpace;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ProseTutorial
{
    [TestClass]
    public class SubstringTest
    {
        private const string GrammarPath = @"../../../../ProseTutorial/synthesis/grammar/substring.grammar";

        [TestMethod]
        public void TestLearnSubstringPositiveAbsPos()
        {
            //parse grammar file 
            Result<Grammar> grammar = CompileGrammar();
            //configure the prose engine 
            SynthesisEngine prose = ConfigureSynthesis(grammar.Value);

            //create the example
            State input = State.CreateForExecution(grammar.Value.InputSymbol, "19-Feb-1960");
            var examples = new Dictionary<State, object> {{input, "Feb"}};
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
        public void TestLearnSubstringPositiveAbsPosSecOcurrence()
        {
            Result<Grammar> grammar = CompileGrammar();
            SynthesisEngine prose = ConfigureSynthesis(grammar.Value);

            State firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "16-Feb-2016");
            State secondInput = State.CreateForExecution(grammar.Value.InputSymbol, "14-Jan-2012");
            var examples = new Dictionary<State, object> {{firstInput, "16"}, {secondInput, "12"}};
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
        public void TestLearnSubstringPositiveAbsPosSecOcurrenceOneExp()
        {
            Result<Grammar> grammar = CompileGrammar();
            SynthesisEngine prose = ConfigureSynthesis(grammar.Value);

            State firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "16-Feb-2016");
            var examples = new Dictionary<State, object> {{firstInput, "16"}};
            var spec = new ExampleSpec(examples);

            ProgramSet learnedSet = prose.LearnGrammar(spec);
            Assert.IsTrue(learnedSet.RealizedPrograms.Count() > 1);
            foreach (ProgramNode program in learnedSet.RealizedPrograms)
            {
                var output = program.Invoke(firstInput) as string;
                Assert.AreEqual("16", output);
            }
        }


        [TestMethod]
        public void TestLearnSubstringNegativeAbsPos()
        {
            Result<Grammar> grammar = CompileGrammar();
            SynthesisEngine prose = ConfigureSynthesis(grammar.Value);

            State firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "(Toby Miller)");
            State secondInput = State.CreateForExecution(grammar.Value.InputSymbol, "(Courtney Lynch)");
            var examples =
                new Dictionary<State, object> {{firstInput, "Toby Miller"}, {secondInput, "Courtney Lynch"}};
            var spec = new ExampleSpec(examples);

            ProgramSet learnedSet = prose.LearnGrammar(spec);

            IEnumerable<ProgramNode> programs = learnedSet.RealizedPrograms;
            var output = programs.First().Invoke(firstInput) as string;
            Assert.AreEqual("Toby Miller", output);
            output = programs.First().Invoke(secondInput) as string;
            Assert.AreEqual("Courtney Lynch", output);
            State differentInput = State.CreateForExecution(grammar.Value.InputSymbol, "(Alan Jasinska)");
            output = programs.First().Invoke(differentInput) as string;
            Assert.AreEqual("Alan Jasinska", output);
        }

        [TestMethod]
        public void TestLearnSubstringNegativeAbsPosRanking()
        {
            Result<Grammar> grammar = CompileGrammar();
            SynthesisEngine prose = ConfigureSynthesis(grammar.Value);

            State firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "(Toby Miller)");
            var examples = new Dictionary<State, object> {{firstInput, "Toby Miller"}};
            var spec = new ExampleSpec(examples);

            var scoreFeature = new RankingScore(grammar.Value);
            ProgramSet topPrograms = prose.LearnGrammarTopK(spec, scoreFeature, 1, null);
            ProgramNode topProgram = topPrograms.RealizedPrograms.First();

            var output = topProgram.Invoke(firstInput) as string;
            Assert.AreEqual("Toby Miller", output);
            State secondInput = State.CreateForExecution(grammar.Value.InputSymbol, "(Courtney Lynch)");
            output = topProgram.Invoke(secondInput) as string;
            Assert.AreEqual("Courtney Lynch", output);
        }

        [TestMethod]
        public void TestLearnSubstringTwoExamples()
        {
            Result<Grammar> grammar = CompileGrammar();
            SynthesisEngine prose = ConfigureSynthesis(grammar.Value);

            State firstInput = State.CreateForExecution(grammar.Value.InputSymbol, "Toby Miller");
            State secondInput = State.CreateForExecution(grammar.Value.InputSymbol, "Courtney Lynch");
            var examples = new Dictionary<State, object> {{firstInput, "Miller"}, {secondInput, "Lynch"}};
            var spec = new ExampleSpec(examples);

            ProgramSet learnedSet = prose.LearnGrammar(spec);
            IEnumerable<ProgramNode> programs = learnedSet.RealizedPrograms;
            var output = programs.First().Invoke(firstInput) as string;
            Assert.AreEqual("Miller", output);
            var output2 = programs.First().Invoke(secondInput) as string;
            Assert.AreEqual("Lynch", output2);
            State differentInput = State.CreateForExecution(grammar.Value.InputSymbol, "Yun Jasinska");
            output = programs.First().Invoke(differentInput) as string;
            Assert.AreEqual("Jasinska", output);
        }

        [TestMethod]
        public void TestLearnSubstringOneExample()
        {
            Result<Grammar> grammar = CompileGrammar();
            SynthesisEngine prose = ConfigureSynthesis(grammar.Value);

            State input = State.CreateForExecution(grammar.Value.InputSymbol, "Toby Miller");
            var examples = new Dictionary<State, object> {{input, "Miller"}};

            var spec = new ExampleSpec(examples);

            var scoreFeature = new RankingScore(grammar.Value);
            ProgramSet topPrograms = prose.LearnGrammarTopK(spec, scoreFeature, 1, null);
            ProgramNode topProgram = topPrograms.RealizedPrograms.First();
            var output = topProgram.Invoke(input) as string;
            Assert.AreEqual("Miller", output);

            State input2 = State.CreateForExecution(grammar.Value.InputSymbol, "Courtney Lynch");
            var output2 = topProgram.Invoke(input2) as string;
            Assert.AreEqual("Lynch", output2);
        }

        public static SynthesisEngine ConfigureSynthesis(Grammar grammar)
        {
            var witnessFunctions = new WitnessFunctions(grammar);
            var deductiveSynthesis = new DeductiveSynthesis(witnessFunctions);
            var synthesisExtrategies = new ISynthesisStrategy[] {deductiveSynthesis};
            var synthesisConfig = new SynthesisEngine.Config {Strategies = synthesisExtrategies};
            var prose = new SynthesisEngine(grammar, synthesisConfig);
            return prose;
        }

        private static Result<Grammar> CompileGrammar()
        {
            return DSLCompiler.Compile(new CompilerOptions
            {
                InputGrammarText = File.ReadAllText(GrammarPath),
                References = CompilerReference.FromAssemblyFiles(typeof(Semantics).GetTypeInfo().Assembly)
            });
        }
    }
}