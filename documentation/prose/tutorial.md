---
date: 2015-09-02T20:14:43-07:00
title: Getting Started
toc: true
layout: default
---

# Synthesis Framework

The core component of the PROSE SDK is its program synthesis framework for custom domain-specific languages (DSLs).
It allows you to define a DSL that describes a typical space of tasks in your application domain, and automatically provides parsing, execution, and synthesis technologies for this DSL.
[FlashFill]({{ site.baseurl }}/documentation/flashfill/intro) and [text]({{ site.baseurl }}/documentation/extraction-text/intro)/[Web]({{ site.baseurl }}/documentation/extraction-web/intro) extraction DSLs are programming-by-example technologies that have been developed on top of the PROSE core synthesis framework.

A DSL consists of several components:

1. **Syntax** -- a *context-free grammar* describing a space of possible programs in a DSL.
2. **Semantics** -- an executable function for each user-defined DSL operator.
3. [optional] **Features** -- computed attributes on individual programs in the DSL (for instance, a syntactic score of a program for ranking purposes).
4. [optional] **Witness functions** -- small "inverse semantics" functions that enable [$D^4$ synthesis strategy]({{ site.baseurl }}/documentation/prose/d4), the main synthesis technology provided in the PROSE framework.

Below, we illustrate the usage of all 4 components on a small example DSL -- a portion of FlashFill.

# Tutorial

## Syntax
Our example DSL describes a space of programs that extract a substring from a given string. They can do it in two possible ways -- either extract a substring based on absolute position indices, or based on matches of regular expressions.

Here's the first version of the grammar of our DSL:

> **Note:** the syntax below will be improved and simplified in the upcoming v0.2 preview.

```
reference 'SubstringExtraction.dll';
using semantics SubstringExtraction.Semantics;
language SubstringExtraction;

@input string inp;

// Extract a substring from 'inp' between positions 'posPair'
@start string out := Substring(inp, posPair);
Tuple<int?, int?> posPair := PositionPair(pos, pos)
                             = Pair(pos, pos);
int? pos := // A position at index 'k' (from the left if k >= 0, or from the right if k < 0)
            AbsolutePosition(inp, k)
            // A position where two regexes 'positionBoundaries' match to left and to the right,
            // respectively, and it is the 'k'-th such position
          | RegexPosition(inp, positionBoundaries, k);
Tuple<Regex, Regex> positionBoundaries := RegexPair(r, r)
                                          = Pair(r, r);

Regex r;
int k;
```

Here are some possible extraction programs contained in the `SubstringExtraction` DSL:

 - First 5 characters: `Substring(inp, PosPair(AbsolutePosition(inp, 0), AbsolutePosition(inp, 5)))`
 - Last character: `Substring(inp, PosPair(AbsolutePosition(inp, -2), AbsolutePosition(inp, -1)))`
 - A substring from the start until (but not including) the last number[^regex]:
`Substring(inp, PosPair(AbsolutePosition(inp, -2), RegexPosition(inp, RegexPair(//, /\d+/), -1))`
 - A substring between the first pair of parentheses: `Substring(inp, PosPair(RegexPosition(inp, RegexPair(/\(/, //), 0), RegexPosition(inp, RegexPair(//, /\)/), 0)))`.
   Note that this program will not extract the desired content if `inp` contains unbalanced parentheses (for instance, `inp == "A) Bread ($2.00)"`). This problem can be addresses by a language extension.

In general, a DSL consists of **symbols** and **operators** upon
these symbols. In a context-free grammar, a DSL is represented as a
set of *rules*, where each symbol on the left-hand side is bound to a set
of possible operators on the right-hand side that represent this symbol.
Every operator of a DSL must be **pure** – it should not have observable side effects. In other words, PROSE DSLs are functional -- they operate upon immutable states.

A **program** in a DSL transforms **input** data into **output** data.
One terminal symbol in a DSL should be marked as `@input` -- this is the input variable to all programs in this DSL.
One nonterminal symbol in a DSL should be marked as `@start` -- this is the root symbol for all programs in the DSL.

A program is represented as an **abstract syntax tree (AST)** of the DSL
operators. Each node in this tree (similarly, each DSL operator) has
some invocation semantics. More formally, each AST node has a method
`Invoke`.
It takes as input a **state** $\mathbf{\sigma}$ and
returns some output. A state is a mapping from DSL variables to their
values. Initially, the topmost AST node invoked on a state with a single variable binding for the DSL's *input variable*.

Here's how you can parse and execute a program in our `SubstringExtraction` DSL:

``` csharp
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;

// Parse the DSL grammar above, saved in a .grammar file
var grammar = DSLCompiler.LoadGrammarFromFile("SubstringExtraction.grammar").Value;
// Parse a program in this grammar. PROSE supports two serialization formats:
// "human-readable" expression format, used in this tutorial, and machine-readable XML.
var ast = grammar.ParseAST("Substring(inp, PosPair(AbsolutePosition(inp, 0), AbsolutePosition(inp, 5)))",
                           ASTSerializationFormat.HumanReadable);
// Create an input state to the program. It contains one binding: a variable 'inp' (DSL input)
// is bound to the string "PROSE Rocks".
var input = State.Create(grammar.InputSymbol, "PROSE Rocks");
// Execute the program on the input state.
var output = (string) ast.Invoke(state);
Assert(output == "PROSE");
```

However, at this moment grammar parsing will fail since we haven't defined any execution semantics for the operators in our DSL, only its syntax.
Let's fix that.

## Semantics
An *executable semantics* for an operator $F$ in PROSE is a .NET function that matches the signature of $F$.
You need to provide it for every operator in your DSL that is not imported from the standard library of PROSE or another language.
In our example, such operators are `Substring`, `AbsolutePosition`, and `RegexPosition`.

All semantics functions should be defined as static methods. Static classes where PROSE searches for such functions (called *semantics holders*) are indicated in the grammar with a `using semantics` declaration.
A DSL may contain multiple `using semantics` declarations, but each operator should correspond to exactly one semantics function with the same name and signature in one of semantics holders.

``` csharp
static class Semantics
{
	static string Substring(string inp, Tuple<int?, int?> posPair)
	{
		if (posPair.Item1 == null || posPair.Item2 == null)
			return null;
		int start = posPair.Item1.Value;
		int end = posPair.Item2.Value;
		if (start < 0 || start >= inp.Length ||
		    end < 0 || end >= inp.Length || end < start)
		    return null;
		return inp.Substring(start, end - start);
	}

	static int? AbsolutePosition(string inp, int k)
	{
		if (k > inp.Length || k < -inp.Length - 1)
			return null;
		return k >= 0 ? k : (inp.Length + k + 1);
	}

	static int? RegexPosition(string inp, Tuple<Regex, Regex> regexPair, int occurrence)
	{
		if (regexPair.Item1 == null || regexPair.Item2 == null)
			return null;
		Regex left = regexPair.Item1;
		Regex right = regexPair.Item2;
		var rightMatches = right.Matches(inp).ToDictionary(m => m.Index);
		var matchPositions = new List<int>();
		foreach (Match m in left.Matches(inp))
		{
		    if (rightMatches.ContainsKey(m.Index + m.Length))
			    matchPositions.Add(m.Index + m.Length);
        }
	    if (occurrence >= matchPositions.Count ||
	        occurrence < -matchPositions.Count)
	        return null;
	    return occurrence >= 0
		    ? matchPositions[occurrence]
		    : matchPositions[matchPositions.Count + occurrence];
	}
}
```

These examples illustrate several important points that you should keep in mind when designing a DSL:

 - DSL operators must be *total* (return a value for each possible combination of inputs) and *pure* (deterministic without observable side effects). A invalid input or any other exceptional situation should be handled not by throwing an exception, but by returning `null` instead. In PROSE, `null` is a valid value with a meaning "computation failed".
 - Semantics functions should have the same name and signature as their corresponding DSL operators. They don't have access to the current input state -- if you need to access a DSL variable in scope, include it explicitly as a parameter. In our example, `inp` is a parameter for both `AbsolutePosition` and `RegexPosition`.

> **Note:** The `dslc` grammar compiler uses reflection to find definitions of external components of a grammar, such as semantics functions. It searches over the assemblies specified with `reference` statements in the grammar. Those assemblies must be built and present at given locations when you execute `dslc` (in a command-line or API form). If you build your semantics functions and your grammar definition in the same solution, make sure to separate them into different projects and make the grammar project depend on the semantics project, so that the latter one is built first.

Syntax and semantics above constitute a minimal DSL definition.
They are sufficient for our little parsing/execution sample to work.
Let's proceed now to synthesizing programs in this DSL.

## Synthesis
PROSE comes with a default synthesis strategy which we call [$D^4$]({{ site.baseurl }}/documentation/prose/d4). It also enables researches in the field of synthesis to develop their own strategies on top of its common API.
However, in this tutorial we explain how to leverage $D^4$ for synthesis of programs in our `SubstringExtraction` DSL.

Program synthesis starts with a specification: what do we want from a desired program?
In PROSE, specifications are *inductive*: they specify an output of a desired program on some input state, or, more generally, some property of this output.
In this tutorial, we start with the simplest form of such a spec -- `ExampleSpec`, an input-output example.
Given a spec, we invoke a learning session on it, generating a set of programs in the DSL that are consistent with the input-output examples in the spec.

``` csharp
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Learning;

var input = State.Create(grammar.InputSymbol, "PROSE Rocks");
string desiredOutput = "PROSE";
var spec = new ExampleSpec(input, desiredOutput);
var engine = new SynthesisEngine(grammar);
ProgramSet learned = engine.LearnGrammar(spec);
Assert(learned?.Size > 0);
```

At this moment the learning result will be empty.
The reason for this is that PROSE does not have information about your DSL to perform any kind of reasoning over it.
For instance, terminal symbols `k` and `r`  should be replaced with literal `int` or `Regex` constants, respectively, in each generated program.
However, they are seemingly unbounded: any integer or regular expression on Earth could possibly be present in a desired program, thus our search space is effectively infinite.

What about the specification, then?
An input-output example that we provided drastically restricts the search space size.
For instance, the input string `"PROSE Rocks"` is 11 characters long, hence any absolute position extraction logic `AbsolutePosition(inp, k)` with $k > 12$ or $k < -11$ cannot be consistent with the spec.
What we just did was backwards reasoning over the DSL structure: we deduced a constraint on `k` in a desired program from a constraint on the entire program.
To do that, we essentially *inverted the semantics of `AbsolutePosition`*, deducing its inputs (or their properties) given the output.
In PROSE, such a procedure is called a *witness function*, and it is a surprisingly simple way to specify immensely powerful hints for the learning process.

### Witness Functions
A witness function is defined for a *parameter* of a DSL operator.
In its simplest form a witness function deduces a specification on that parameter given a specification on the entire operator.
A witness function does not by itself constitute a learning algorithm (or even a substantial portion of it), it is simply a domain-specific property of some operator in your language -- its inverse semantics.

For instance, the first witness function we'll write in this tutorial is defined for the parameter `posPair` of the rule `Substring(inp, posPair)` of our `SubstringExtraction` DSL.
It takes as input an `ExampleSpec` $\phi$ on an output of `Substring(inp, posPair)`, and deduces a spec $\phi'$ on an output of `posPair` subexpression that is necessary (or even better, necessary and sufficient) for the entire expression to satisfy $\phi$.

Consider a program `Substring(inp, posPair)` that outputs `"PROSE"` on a given input state $\\{$ `inp` $:=$ `"PROSE Rocks"` $\\}$. What could be a possible spec on `posPair`? Clearly, we know it precisely for the given example: `posPair`, whatever this program is, must have evaluated to `(0, 5)` because this is the only occurrence of the string `"Rocks"` in the given input `"PROSE Rocks"`.

In a more complex example, however, there is no single answer.
For instance, suppose `inp == "(206) 279-6261"`, and the corresponding desired output in a spec is `"2"`. In this case, the substring `"2"` could have been extracted from 3 different places in the input string.
Therefore, instead of *witnessing* a single output value for `posPair` on a given input, in this case we witness a *disjunction* of three possible output values.
A disjunction of possible outputs has its own representative spec type in PROSE -- `DisjunctiveExamplesSpec`.

The two cases above lead us to a general definition of a witness function for `posPair`: find all occurrences of the desired output string in the input, and return a disjunction of them. In PROSE, you express it in the following way:

``` csharp
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Learning;

static class WitnessFunctions
{
	[WitnessFunction("Substring", parameterIndex: 1)]
	static DisjunctiveExamplesSpec WitnessPositionPair(GrammarRule rule, int parameter, ExampleSpec spec)
	{
		var result = new Dictionary<State, IEnumerable<object>>();
		foreach (var example in spec.Examples)
		{
			State inputState = example.Key;
			// the first parameter of Substring is the variable symbol 'inp'
			// we extract its current bound value from the given input state
			var inp = (string) inputState[rule.Body[0]];
			var substring = (string) example.Value;
			var occurrences = new List<Tuple<int?, int?>>();
			// Iterate over all occurrences of 'substring' in 'inp', and add their position boundaries
			// to the list of possible outputs for posPair.
			for (int i = inp.IndexOf(substring);
			     i >= 0;
			     i = inp.IndexOf(substring, i + 1))
		    {
			    occurrences.Add(Tuple.Create((int?) i, (int?) i + substring.Length));
		    }
		    if (occurrences.Count == 0) return null;
		    result[inputState] = occurrences;
		}
	    return new DisjunctiveExamplesSpec(result);
	}
}
```

We put this witness function in a different static class called, for instance, `SubstringExtraction.WitnessFunctions`.
Such a class is called a *learning holder*, and it contains all hints and annotations that a DSL designer wants to provide to help the PROSE synthesis engine.
A learning holder is specified in the grammar file similarly to a semantics holder, with its own statement:

```
using learners SubstringExtraction.WitnessFunctions;
```
Just like with semantics holders, a grammar may contain multiple learning holders.

Some important points on writing witness functions:

 - A witness function is defined for one parameter of an operator, not the entire operator.
 - A witness function takes as input a spec on the output of *the entire operator expression*, and outputs a spec on the output of *one parameter program in that expression*.
 - Ideally, the spec produced by a witness function is necessary and sufficient to satisfy the given outer spec. You can also write an *imprecise* witness function, whose produced spec is only necessary for the outer spec to hold (in other words, it is an *overapproximation*). Such a witness function says "I cannot constrain this parameter precisely, but I can narrow down the space of possibilities. All valid parameter programs should satisfy my produced spec, but there may be some invalid ones that also satisfy it." To mark a witness function as imprecise, add a property `Verify = true` to its `[WitnessFunction]` attribute.
 - You don't need to define witness functions for parameters that are grammar variables in a state (such as `inp`). More generally, you don't need to define witness functions for a parameter $p$ if all DSL programs that may derive from $p$ do not include any literals.
 - You don't need to define witness functions for operators from the standard library (with some exceptions).
 - Returning `null` from a witness function means "The given spec is inconsistent, no program can possibly satisfy it."

#### Absolute positions
Covering more DSL operators with witness functions is straightforward.
The next one witnesses `k` in the `AbsolutePosition` operator.
Given an example of position $\ell$ that `AbsolutePosition(inp, k)` produced, `k` must have been one of two options: the offset of $\ell$ from the left or from the right in `inp`.
We can apply similar logic if we are given not one position $\ell$ but a disjunction of them: the witness function just enumerates over each option, collecting all possible $k$s.

``` csharp
[WitnessFunction("AbsolutePosition", 1)
static DisjunctiveExamplesSpec WitnessK(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
{
	var result = new Dictionary<State, IEnumerable<object>>();
	foreach (var example in spec.DisjunctiveExamples)
	{
		State inputState = example.Key;
		var ks = new HashSet<int?>();
		var inp = (string) inputState[rule.Body[0]];
		foreach (int? pos in example.Value)
		{
			ks.Add(pos);
			ks.Add(pos - inp.Length - 1);
		}
		if (ks.Count == 0) return null;
		result[inputState] = ks.Cast<object>();
	}
	return new DisjunctiveExamplesSpec(result);
}
```

#### Regex-based positions
Operator `RegexPosition` needs 2 witness functions: one for its `rr` parameter and one for its `k` parameter.
For the first one, we need to learn a list of regular expressions that match to the left and to the right of given position.
There are many techniques for doing that; in this tutorial, we will assume that we have a predefined list of "common" regexes like `/[0-9]+/`, and enumerate them exhaustively at a given position.

> **Note:** to make the semantics of `RegexPosition(inp, rr, k)` and its witness functions consistent, we need to agree on what does it mean for a regex to "match" at a given position.
> If we take a standard definition of "matching", and simply test each regex at each position, we will later run into problems when determining the corresponding `k` for each regex.
>
> Consider a string `inp = "abc def"`. We would like a program `RegexPosition(inp, RegexPair(//, /[a-z]+/), 1)` to match before a second word in `inp` -- in this case, at position #4.
> However, for that we need to assume *non-overlapping* semantics of regex matches, since the regex `/[a-z]+/` also matches at positions #0, #1, and #2.
> In fact, there are 6 matches of this regex in `inp`, but only two "words", by a "common sense" definition.
> Therefore, instead of testing a regex at each position, we need to first run it against the entire string, record a list of non-overlapping matches, and only then test a position for a match in that list.
>
> For computational efficiency, ideally we should *cache* the run of each predefined regex against each input string in the examples before the learning session starts.
> That way, we avoid recomputing it in each call of `RegexPosition` semantics and its witness functions.
> We avoid such caching in this tutorial for simplicity of presentation.

Here's a witness function for `rr`:

``` csharp
Regex[] UsefulRegexes = {
	new Regex(@"\w+"),  // Word
	new Regex(@"\d+"),  // Number
	// ...
};

// For efficiency, this function should be invoked only once for each input string before the learning session starts
static void BuildStringMatches(string inp, out List<Tuple<Match, Regex>>[] leftMatches,
                               out List<Tuple<Match, Regex>>[] rightMatches)
{
	leftMatches = new List<Tuple<Match, Regex>>[inp.Length + 1];
	rightMatches = new List<Tuple<Match, Regex>>[inp.Length + 1];
	for (int p = 0; p <= inp.Length; ++p)
	{
		leftMatches[p] = new List<Tuple<Match, Regex>>();
		rightMatches[p] = new List<Tuple<Match, Regex>>();
	}
	foreach (Regex r in UsefulRegexes)
	{
		foreach (Match m in r.Matches(inp))
		{
			leftMatches[m.Index + m.Length].Add(Tuple.Create(m, r));
			rightMatches[m.Index].Add(Tuple.Create(m, r));
		}
	}
}

[WitnessFunction("RegexPosition", 1)]
static DisjunctiveExamplesSpec WitnessRegexPair(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec)
{
	var result = new Dictionary<State, IEnumerable<object>>();
	foreach (var example in spec.DisjunctiveExamples)
	{
		State inputState = example.Key;
		var inp = (string) inputState[rule.Body[0]];
		List<Tuple<Match, Regex>>[] leftMatches, rightMatches;
		BuildStringMatches(inp, out leftMatches, out rightMatches);
		var regexes = new List<Tuple<Regex, Regex>>();
		foreach (int? pos in example.Value)
		{
			regexes.AddRange(from l in leftMatches[pos.Value]
							 from r in rightMatches[pos.Value]
							 select Tuple.Create(l.Item2, r.Item2));
        }
		if (regexes.Count == 0) return null;
		result[inputState] = regexes;
	}
	return new DisjunctiveExamplesSpec(result);
}
```

#### Conditional witness functions
The last witness function in this tutorial witnesses a match index `k` for each regex pair in `RegexPosition`.
To write such a witness function, an outer spec on `RegexPosition` alone is insufficient: we can only write it for each individual regex pair, but not for all possible regex pairs at once.
Reducing reasoning over all possible values to reasoning assuming a single fixed value is called [skolemization](https://en.wikipedia.org/wiki/Skolem_normal_form).
After inversion of semantics, skolemization is the second powerful idea that makes $D^4$ able to perform program synthesis efficiently.

Our witness function for `k` is *conditional* on `rr`: in addition to an outer spec, it takes an additional input -- a spec on its *prerequisite parameter* `rr`.
In general, it can be any spec that provides your witness function any useful information.
Typically, an `ExampleSpec` (i.e., a concrete value of prerequisite -- in this case `rr`) is the most useful and common prerequisite spec.
We use `ExampleSpec` here to deduce possible indices `k` for each regex pair in a manner similar to deducing absolute positions above.

``` csharp
[WitnessFunction("RegexPosition", 2, DependsOnParameters = new[] { 1 }]
static DisjunctiveExamplesSpec WitnessKForRegexPair(GrammarRule rule, int parameter, DisjunctiveExamplesSpec spec,
                                                    ExampleSpec rrSpec)
{
	var result = new Dictionary<State, IEnumerable<object>>();
	foreach (var example in spec.DisjunctiveExamples)
	{
		State inputState = example.Key;
		var inp = (string) inputState[rule.Body[0]];
		var regexPair = (Tuple<Regex, Regex>) rrSpec.Examples[inputState];
		Regex left = regexPair.Item1;
		Regex right = regexPair.Item2;
		var rightMatches = right.Matches(inp).ToDictionary(m => m.Index);
		var matchPositions = new List<int>();
		foreach (Match m in left.Matches(inp))
		{
		    if (rightMatches.ContainsKey(m.Index + m.Length))
			    matchPositions.Add(m.Index + m.Length);
        }
	    var ks = new HashSet<int?>();
	    foreach (int? pos in example.Value)
	    {
		    int occurrence = matchPositions.BinarySearch(pos.Value);
		    if (occurrence < 0) continue;
		    ks.Add(occurrence);
		    ks.Add(occurrence - matchPositions.Count);
	    }
	    if (ks.Count == 0) return null;
	    result[inputState] = ks.Cast<object>();
	}
    return new DisjunctiveExamplesSpec(result);
}
```

After adding these 4 witness functions to `SubstringExtraction.WitnessFunctions`, our `SynthesisEngine.LearnGrammar` call succeeds, and we get back a set of several dozens possible consistent programs!

## Ranking
Example are inherently an ambiguous form of specification.
A user-provided spec of several input-output examples usually produces a huge set of DSL programs that are consistent with it (often billions of them!).
To build a useful application, a synthesis-based technology has to somehow pick one "most likely" program from such a set.
Many disambiguation techniques exist; in this tutorial, we show the most common one -- ranking.

Ranking assigns each program a *score* -- an approximation to its "prior probability" of being a desired program.
For instance, string extraction based on absolute indices is less common than extraction based on regular expressions, therefore the former should be assigned a smaller score than the latter.

In PROSE, scores are represented using *computed features*.
A feature is a named attribute on a program AST, computed using provided *feature calculator* functions.
A feature can be *complete*, which means that it must be defined with some value for each possible DSL program, or *incomplete* if it only exists on some DSL programs.

A feature is defined in a DSL as follows:

```
@complete double feature Score = SubstringExtraction.ScoreCalculator;
```
Here `Score` is its name, `double` is its type, and `ScoreCalculator` is a static class that holds calculator functions.
Given a program AST `p`, you can access the value of `Score` on this AST as `p["Score"]` (converted to `double`).

> **Note:** by default, variable ASTs such as `inp` are automatically assigned a feature value of `default(T)` -- in case of `Score`, it's `0.0`.
> To override this behavior, put a `@vardefault[VarCalc]` annotation on the feature definition, where `VarCalc` is a member of the same static class with calculators.
> This member may be a (constant) field, a .NET property, or a parameterless method.

### Feature calculators
A feature calculator is defined for a grammar rule.
There are three ways to define a calculator: based on *recursive feature values*, based on *program syntax*, or based on *literal values*.

#### Calculation from recursive values
The most common feature definitions are inductive, recursively defined over the grammar.
For instance, a score for `RegexPosition(inp, rr, k)` would be defined as a formula over a score for `rr` and a score for `k`.
Such feature calculators take as input recursively computed values of the same feature on parameters of a current program AST:

``` csharp
[FeatureCalculator("RegexPosition", Method = CalculationMethod.FromRecursiveFeatureValues)]
static double ScoreRegexPosition(double inScore, double rrScore, double kScore) => rrScore * kScore;
```

#### Calculation from syntax nodes
When recursively computed feature values are insufficient, you can take into account the entire syntax of a program AST.
Such a calculator takes as input `ProgramNode` instances representing ASTs of parameter programs.
You can specify specific subclasses of `ProgramNode` instead as parameters, if you know that your grammar structure only allows some specific AST kinds at this place.

``` csharp
[FeatureCalculator("AbsolutePosition", Method = CalculationMethod.FromChildrenNodes]
static double ScoreAbsolutePosition(VariableNode inp, LiteralNode k)
{
	double score = (double) inp["Score"] + (double) k["Score"];
	int kValue = (int) k.Value;
	if (Math.Abs(k) <= 1)
		score *= 10;
	return score;
}
```

#### Calculation from literals
An inductively defined computed feature needs a basic case -- its value on literal program ASTs.
Feature calculators on terminal rules can take as input simply the value of a literal in a `LiteralNode` currently being scored.

Since terminal rules do not have names, you cannot associate a calculator with a rule simply by putting its name in a first parameter of the `[FeatureCalculator]` attribute.
Instead, you can annotate the rule itself with a `@feature` annotation, which specifies a calculator for each relevant feature.

```
@feature[Score=KScore] int k;
```

``` csharp
[FeatureCalculator(Method = CalculationMethod.FromLiteral)]
static double KScore(int k) => 1.0 / (1 + Math.Abs(k));
```

### Learning top programs

Our `SubstringExtraction` grammar needs a `Score` calculator for each rule (including standard library rules like `PositionPair`).
After you define all of them, you can now extract $k$ topmost-ranked programs out of the set of candidates returned by learning:

``` csharp
ProgramSet learned = engine.LearnGrammar(spec);
IEnumerable<ProgramNode> best = learned.TopK("Score", k: 1);
```
The method `TopK` assumes that your feature has a numerical type (convertible to `double`).
It returns an descendingly ordered sequence of programs (greater score values are better).
If several programs have the same score, they are all returned in the sequence, thus it may hold more programs than the requested value of `k`.

Instead of ordering a learned set of programs, you can instead learn only $k$ topmost-ranked programs in the first place, if you are not interested in the entire set of candidates.
Such a request significantly improves learning performance, since PROSE can perform aggressive filtering in the middle of the learning process.

``` csharp
IEnumerable<ProgramNode> bestLearned = engine.LearnGrammarTopK(spec, "Score", k: 1);
```

> **Important:** in order for both `TopK` methods to work soundly, your feature must be *monotonic* over the grammar structure. In other words, greater-scored subexpressions should produce greater-scored expressions.

We can now take the best program and apply it on new user-provided data.
Assuming scoring functions similar to FlashFill, this program will be "Extract the first word":

``` csharp
ProgramNode p = bestLearned.First();
Console.WriteLine(p);
/* Substring(inp, PositionPair(RegexPosition(inp, RegexPair(//, /\w+/), 0), RegexPosition(inp, RegexPair(/\w+/, //), 0)) */
State input = State.Create(grammar.InputSymbol, "Program Synthesis");
Console.WriteLine(p.Invoke(input));
/* Program */
```


[^regex]: PROSE uses JavaScript/Perl syntax for regular expression literals.
