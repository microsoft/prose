using System.Reflection;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Diagnostics;

namespace MergeConflictsResolution
{
    /// <summary>
    ///     Defines the language grammar.
    /// </summary>
    public class LanguageGrammar
    {
        private const string GrammarContent = @"
using Microsoft.ProgramSynthesis.Wrangling.Tree;
using MergeConflictsResolution;

using semantics MergeConflictsResolution.Semantics;
using learners MergeConflictsResolution.WitnessFunctions;

language MergeConflictsResolution;

@complete feature double Score = RankingScore;

@input MergeConflict x;
string path;
string[] paths;
int k;

int[] enabledPredicate;

@start IReadOnlyList<Node> proc := rule;

IReadOnlyList<Node> rule := @id['dupLet'] let dup: IReadOnlyList<IReadOnlyList<Node>> = find in Apply(predicate, action);

bool predicate := Check(dup, enabledPredicate);

IReadOnlyList<Node> action  := Concat(action, action)
							|  Remove(t, t)
							|  t;

IReadOnlyList<Node> t       := SelectUpstreamIdx(x, k)
                            |  SelectUpstreamPath(x, path)
                            |  SelectDownstreamIdx(x, k)
                            |  SelectDownstreamPath(x, path)
                            |  SelectDownstream(x)
                            |  SelectUpstream(x)
							|  SelectDup(dup, k) = Kth(dup, k);

IReadOnlyList<IReadOnlyList<Node>> find := FindMatch(x, paths);";

        private LanguageGrammar()
        {
            var options = new CompilerOptions
            {
                InputGrammarText = GrammarContent,
                References = CompilerReference.FromAssemblyFiles(
                    typeof(Semantics).GetTypeInfo().Assembly,
                    typeof(Microsoft.ProgramSynthesis.Wrangling.Tree.Node).GetTypeInfo().Assembly)
            };

            Result<Grammar> compile = DSLCompiler.Compile(options);
            Grammar = compile.Value;
        }

        public static LanguageGrammar Instance { get; } = new LanguageGrammar();

        public Grammar Grammar { get; }
    }
}
