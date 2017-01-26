---
date: 2015-08-31T15:02:56-07:00
title: Usage
---

{% include outdated.html %}

{% include toc.liquid.md %}

# Terminology

A **domain-specific language (DSL)** is a context-free programming
language, created for a specific purpose, which can express tasks from a
certain domain. A DSL consists of **symbols** and **operators** upon
these symbols. A DSL is described as a context-free grammar – i.e., as a
set of rules, where each symbol on the left-hand side is bound to a set
of possible operators on the right-hand side that represent this symbol.
Every operator of a DSL is **pure** – it does not have side effects.

A **program** in a DSL transforms **input** data into **output** data.
A program is represented as an **abstract syntax tree (AST)** of the DSL
operators. Each node in this tree (similarly, each DSL operator) has
some invocation semantics. More formally, each AST node has a method
`Invoke`. It takes as input a **state** $\mathbf{\sigma}$ and
returns some output. A state is a mapping from DSL variables to their
values. Initially, the topmost AST node invoked on a state
`$\sigma = \left\{ v \mapsto i \right\}$`, where $v$ is the DSL variable
that represents program input, and $i$ is the value of the input data on
which the program is invoked. AST nodes can modify this state (add new
variable bindings in scope) and pass it down to children nodes in
recursive invocations of `Invoke`.

# Architecture

The Microsoft.ProgramSynthesis package unites the core pieces of the meta-synthesizer.
Here are the included assemblies:

-   **Microsoft.ProgramSynthesis** includes the core functionality for manipulating
	ASTs, validating DSLs, maintain a DSL and a set of its operators,
	and core infrastructure for the synthesis algorithms.
-   **dslc.exe** is a DSL compiler. This is the executable that
	takes a language definition, parses it, validates it, potentially
	emits some diagnostics (warnings and/or errors), and serializes the
	parsed language representation into a portable file.
-   **Microsoft.ProgramSynthesis.Learning** is a core library of synthesis algorithms,
	standard operators and strategies.
-   **Microsoft.ProgramSynthesis.Utils** is a collection of utility methods that are used
	in different parts of the project.

A typical workflow of a DSL designer consists of the following steps:

1.  Define a DSL in a separate file (usually has a \*.grammar
	extension).
2.  Define the semantics and all the supporting infrastructure for the
	DSL in a separate .NET assembly (see Section 4 below).
3.  Use one of the two options below to compile the DSL definition into
	a `Grammar` object:
	*  At compile-time, invoke the DSL compiler **dslc.exe**
		manually on a \*.grammar file with a DSL definition. Deploy the
		resulting serialized \*.grammar.xml file with your application.
		At run-time, deserialize it using `Grammar.Deserialize` method.
	*  Deploy the **dslc.exe** and the \*.grammar file with your
		application. At run-time, compile your DSL definition in memory
		using `DSLCompiler.Compile` method.

# Language definition

The main class `Grammar` represents a context-free grammar as a
set of DSL rules and a list of references to operators’ semantics and/or
custom operator learners, implemented in C\# in a separate assembly. The
method `DSLCompiler.LoadGrammar` loads a grammar definition from a string.
Multiple examples of its usage can be found in our [sample repository](https://github.com/microsoft/prose).

Here’s a typical language definition:

```
reference 'TestSemantics.dll';
using TestSemantics;
using semantics TestSemantics.FlashFill;
using learners TestSemantics.FlashFill.Learners;

language FlashFill;

@start string f := ConstStr(s) | let x: string = v in SubStr(x, P, P);
int P := CPos(x, k) | Pos(x, r, r, k);
int k;
@values[StringGen] string s;
Regex r;
@input string v;
```
{: .language-dsl}

NOTE: The operators `ConstStr`, `SubStr`, `CPos` and `Pos` are defined in
[Black-box operators](#black-box-operators) below.

First, we reference external assemblies with our implementation of
operators’ semantics and/or custom operator learners. Second, we
reference any namespaces for external typename lookup. Finally, we
specify the static classes for semantics and learners. There may be
several **reference**, **using**, **using semantics**, and **using learners** instructions. In this example,
`TestSemantics.FlashFill` is a static class defined in the
assembly “TestSemantics.dll”, as is
`TestSemantics.FlashFill.Learners`.

Next, we specify the language name and its rules in a form of a
context-free grammar (BNF). Each **nonterminal** rule has a symbol on
the left-hand side and a list of operators on the right-hand side,
separated by “|”. Each **terminal** rule has only the left-hand side.
Each symbol has its type, specified on the LHS. The supported types are:
any of the standard C\# types, including classes from
`System.Collection.Generic`, `System.Text.RegularExpressions`, etc.,
or your own custom types, defined in the same semantics assembly. In the
latter case, the framework searches for the type name in the namespaces
referenced in **using** instructions.

Exactly one nonterminal rule should be annotated as “**@start**” – this
is a start symbol of the grammar (i.e., the topmost node of every AST).
Exactly one terminal rule should be annotated as “**@input**” – this is
the input data to the program.

## Terminals

Terminal rules specify the leaf symbols that will be replaced with
literal constants or variables in the AST. For example:

-   A terminal rule `int k;` specifies a symbol $k$ that represents a
	literal integer constant.
-   A terminal rule `@input string v;` specifies a *variable* symbol $v$
	that will be replaced with program input data *at runtime.*

A user can specify the list of possible values that a literal symbol can
be set to. This is done with a **@values[**$G$**]** annotation,
where $G$ is a **value generator** – a reference to a user-defined
static field, property, or method. $G$ should evaluate to
`IEnumerable` (thus, it can be any standard .NET collection, such
as an array or a list, or a user-defined collection type). The framework
searches for the definition of $G$ in the *learner classes,* specified
by a **using learners** instruction. For our example grammar, any of the
following definition can serve as a value generator for $s$:

```csharp
public static class FlashFill
{
	public static class Learners
	{
		public static string[] StringGen = {"", "123", "Gates, B.", "ABRACADABRA"};
		public static string[] StringGen
		{
			get { return new[] {"", "123", "Gates, B.", "ABRACADABRA"}; }
		}
		public static string[] StringGen()
		{
			return new[] { "", "123", "Gates, B.", "ABRACADABRA" };
		}
	}
}
```
If no value generator is provided, the framework can either pick a
standard generator for some common types (such as `byte`), or
assume that the literal can be set to any value. This can impact the
performance of some synthesis strategies or even make them inapplicable.

## Black-box operators

A **black-box** operator is an operator that does not refer to any
standard concepts, and its invocation semantics do not modify the
state $\sigma$ that has been passed to it. In the example above
`ConstStr`, `SubStr`, `CPos`, `Pos` are
black-box operators. Note that `SubStr` is a black-box operator,
even though the `Let` concept surrounding it isn’t.
`SubStr`’s semantics don’t modify $\sigma$, they just pass it
on to parameter symbols. `Let`’s semantics change the state
$\sigma$ by extending it with a new variable binding $x \mapsto v$.

The semantics of every black-box operator should be defined in the
semantics assembly with the same name as a public static method:

```csharp
public static class Semantics
{
	public static int CPos(string s, int k)
	{
		return k < 0 ? s.Length + 1 + k : k;
	}

	public static int? Pos(string s, Regex leftRegex, Regex rightRegex,
						   int occurrence)
	{
		var boundaryRegex = new Regex(string.Format("(?<={0}){1}", leftRegex, rightRegex));
		var matches = boundaryRegex.Matches(s);
		int index = occurrence > 0 ? occurrence - 1 : matches.Count + occurrence;
		if (index < 0 || index >= matches.Count)
		{
			return null;
		}
		return matches[index].Index;
	}

	public static string ConstStr(string s)
	{
		return s;
	}

	public static string SubStr(string s, int left, int right)
	{
		if (left < 0 || right > s.Length || right < left)
		{
			return null;
		}
		return s.Substring(left, right - left);
	}

	public static string Concat(string s, string t)
	{
		return s + t;
	}
}
```

It is important to note that the learning process requires that semantic
functions are *total*.  If the function is inapplicable for the current
choice of arguments or if for any other reason it would throw an exception,
it must return **null** instead[^1].
In such a case, if the return type of a function is a .NET value type, it should be made nullable.

> This choice was made to enable efficient program synthesis. During learning,
> PROSE may repeatedly invoke partial programs on various inputs for
> verification purposes. If an input is invalid and a program handles it by
> throwing an exception, it slows down the learning by two orders of
> magnitude.

Nulls are automatically propagated upward like exceptions: if an argument to
a semantic function is **null**, its result is automatically presumed to be
**null** as well.
You can override this behavior by annotating your semantics function with a
`[LazySemantics]` attribute.
In that case, your function will receive any **null** arguments as usual, and
must handle it on its own.

Every grammar operator should be pure, and its *formal* signature should
be $\sigma \rightarrow T$ (where $T$ is the type of the corresponding
LHS symbol). The *actual* signature of the semantics function that
implements a black-box operator is
`$\left( T_{1},T_{2},\ldots,T_{k} \right) \rightarrow T$`, where $T_{j}$
is the type of $j^{\mathrm{\text{th}}}$ parameter symbol, and $T$ is the
return type of the LHS symbol. Consequently, if one needs to invoke
their operators on anything other than the program input data, they have
to introduce additional variables using `Let` construct and/or
lambda functions.

## Let construct

`Let` construct is a standard concept with the following syntax:

<div> $$
\text{let } x_{1}\colon \tau_1 = v_{1},\,\ldots,\,x_{k}\colon \tau_k = v_{k}\text{ in } RHS
$$ </div>

where $RHS$ is any standard rule RHS (a black-box operator, a grammar symbol, etc.).
The RHS and any of the symbols it (indirectly) references can make use of the
variables $x_1,\ldots,x_k$ Grammar symbols $v_1,\ldots,v_k$ are parameters of the
rule; at runtime, each variable $x_j$ is bound to some value of the corresponding
symbol $v_j$.

**Example:**

The running grammar definition FlashFill contains the following rule:
```
@start string f := let x: string = v in SubStr(x, P, P)
```
{: .language-dsl}

The formal signature of this rule has three free parameters:
$v,\ P,\ P.$ At runtime, the `Let` construct, when given a state
$\sigma$, executes the following operations:

* $\vartheta := \llbracket v \rrbracket \sigma$ <br />
 (Execute the parameter symbol $v$ on the state and save the value as $\vartheta$)
* `$\sigma^{'} := \sigma \cup \left\{ x \mapsto \vartheta \right\}$` <br />
  (Add a new variable $x$ to the state, bind it to the value $\vartheta$)
* $\mathrm{\text{return }} \llbracket SubStr(x,P,P) \rrbracket \sigma'$ <br />
  (Invoke the RHS – the black-box operator `SubStr` – on the new state)

The RHS indirectly references the variable $x$ further in the grammar
through the symbol $P$. Namely, the grammar further contains the
following rule:
```
int P := Pos(x, r, r, k)
```
{: .language-dsl}

`Pos` is a simple black-box operator, just like `SubStr`.
When given a state $\sigma'$, it passes it on to its parameter symbols
$x,r,r,k$. The latter three symbols are terminals; they will be
represented as literal constants of the corresponding types in any final
program (AST). The first symbol is a variable $x$; when executed on a
state $\sigma'$, it will just extract the binding $x \mapsto \vartheta$
from it, and return $\vartheta$.

## Standard concepts

There exists a range of concepts that are common for many DSLs and
implement standard functionality. In particular, many list/set
processing concepts (`Map`, `Filter`, `TakeWhile`, etc.) encode various forms of loops that arise in many
DSLs for different purposes. The synthesizer treats many such concepts
as first-class citizens and is aware of many efficient strategies for
synthesizing such concepts from example specifications. Consequently, we
encourage framework users to use **standard concepts** for *any* loop
form that arises in their DSLs, instead of encoding it as a black-box
operator. In our experience, most loop forms commonly used in DSLs can
be expressed as combinations of standard concepts.

The current list of supported concepts can be found in the
`Microsoft.ProgramSynthesis.Rules.Concepts` namespace.

Most standard loop concepts express some form of list/set processing
with lambda functions. We explain their usage here on a simple
`Filter` example.

A $Filter(p,\ s)$ concept is an operator that takes as input a
predicate symbol $p$ and a set symbol $s$. It evaluates $s$ on a state $\sigma$ to
some set $S$ of objects. It also evaluates $p$ on a state $\sigma$ to
some lambda function $L = \lambda x:e$. Finally, it filters the set $S$
using $L$ as a predicate, and returns the result.
Essentially, $Filter$ is equivalent to [Select in LINQ](https://msdn.microsoft.com/en-us/library/vstudio/bb548891(v=vs.110\).aspx).

Consider the following grammar for various filters of an array of
strings:

```
reference 'TestSemantics.dll';
using TestSemantics;
using semantics TestSemantics.Flare;
using learners TestSemantics.Flare.Learners;

language Flare;

@input string[] v;
Regex r;
StringFilter f := Match(r) | FTrue();
@start string[] P := Selected(f, v) = Filter(f, v);
```
{: .language-dsl}

The input string array $v$ is filtered with a filter $f$. A filter $f$
can filter elements either according to a regular expression $r$, or
trivially (by returning `true`).

The main rule of the grammar is `string[] P := Selected(f, v)`. Here
`Selected(f, v)` is a *formal* operator signature: this is how it would
look like if it was implemented as a black-box operator. However, the
*actual* implementation of `Selected(f, v)` refers to the standard
`Filter` concept instead of a black-box semantics implementation.
The *arguments* of the `Filter` concept are in this case the
*parameters* of a `Selected` operator – the symbols $f$ and $v$.
At runtime, the framework interprets a `Selected` AST node by
executing the standard `Filter` semantics with the corresponding
arguments.

For a valid execution, the runtime types of the symbols $f$ and $v$
should satisfy the following contract:

1.  The type of $v$ should implement `IEnumerable`. It can be a
	standard .NET collection (array,
	`List<T>`, etc.), or a user-defined
	custom type that implements `IEnumerable`.

2.  The type of $f$ should have *functional semantics*. In other words,
	it should behave like a lambda function, because at runtime it will
	be “invoked” with array elements as arguments. Moreover, for the
	`Filter` concept specifically, the return type of this
	“function” should be assignable to `bool`.

### Lambda functions

One can capture functional semantics in an explicit
lambda function in the grammar:

```
reference 'TestSemantics.dll';
using semantics TestSemantics.Flare;
using learners TestSemantics.Flare.Learners;

language Flare;

@input string[] v;
Regex r;
bool f := Match(x, r) | True();
@start string[] P := Selected(f, v) = Filter(\x: string => f, v);
```
{: .language-dsl}

The first argument of the `Filter` concept is a
lambda function $\lambda x:f$. In our syntax, it is represented as
`\x: string => f`[^3]. Here $x$ is a freshly bound variable (lambda
function parameter), and $f$ is a grammar symbol that represents
function body. The runtime type of $x$ should be specified explicitly
after a colon.

Just as with `Let` constructs, the lambda function body symbol
and all its indirect descendants can now reference the variable symbol
$x$. At runtime, it will be successively bound to every element of the
input string array. Since this binding introduces a new variable in the
state $\sigma$, a lambda definition cannot be expressed as a black-box
rule. Instead, it is a special rule kind with first-class treatment in
the framework (again, just as `Let`).

The corresponding semantics implementation is now much simpler:

```csharp
public static class Flare
{
	public static bool Match(string x, Regex r)
	{
		return r.IsMatch(x);
	}

	public static bool True()
	{
		return true;
	}
}
```
Note that a lambda function body is a free symbol on the RHS of the “=”
sign. In other words, the set of free symbols on the RHS includes the
direct parameters of the concept (in the example above, $v$) and the
lambda function bodies (in the example above, $f$). To make the concept
rule well-defined, this set should be exactly the same as the set of
free symbols on the LHS of the “=” sign (i.e. the set of formal
parameters of the rule). However, they should only be equivalent as
*sets*, the order of parameters does not matter. In case of multiple
usages of the same symbol among parameters, the correspondence between
the LHS and the RHS is resolved in a left-to-right order.

**Example:**

Consider the following concept rule:

```
int S := F(v, P, P) = G(\x: string => P, v, P);
```
{: .language-dsl}

Here $G$ is some standard concept, and $S,\ v,P$ are grammar symbols.
The correspondence between formal parameters on the LHS and free symbols
on the RHS is resolved as follows:

-   The first parameter $v$ on the LHS corresponds to the second
	argument $v$ on the RHS.
-   The second parameter $P$ on the LHS corresponds to the body of the
	first argument $\lambda x:P$ on the RHS.
-   The third parameter $P$ on the LHS corresponds to the third argument
	$P$ on the RHS.

Two usages of the same symbol $P$ among parameters were resolved in a
left-to-right order.

# Language usage

Definition and usage of custom DSLs starts with the following steps:

1.  Define a string representation of your DSL grammar in our syntax.
2.  Implement your black-box operator semantics, custom types, and value
	generators in a separate assembly. Make sure that it is accessible
	at a path specified in the grammar string[^4].
3.  Load the grammar into a `Grammar` object:
	* Either programmatically using `DSLCompiler.LoadGrammar`
		or `DSLCompiler.LoadGrammarFromFile`.
	* Or in two steps, with manual DSL compilation using
		**dslc.exe** and loading the serialized `Grammar`
		object using `Grammar.Deserialize`.
4.  Use this `Grammar` object to parse specific AST strings into
	ASTs and invoke these ASTs on states.

## AST parsing and printing

The example below shows the last steps, assuming loading the grammar
programmatically (option “a”):

```csharp
 const string TestFlareGrammar = @"
		 reference 'TestSemantics.dll';
		 using TestSemantics;
		 using semantics TestSemantics.Flare;
		 language Flare;
		 @input string[] v;
		 Regex r;
		 StringFilter f := Match(r) | FTrue();
		 @start string[] P := Selected(f, v) = Filter(f, v);";
var grammar = DSLCompiler.LoadGrammar(TestFlareGrammar).Value;
var ast = grammar.ParseAST("Selected(Match(new Regex(\"[a-z]+\")), v)",
	ASTSerializationFormat.HumanReadable);
var state = new State(grammar.InputSymbol, new[] {"1", "ab", ""});
Assert.That(ast.Invoke(state), Is.EqualTo(new[] {"ab"}).AsCollection);
```

The second (optional) parameter of the `ParseAST` method
specifies the serialized format of the program. As of now, two formats
are supported: XML and human-readable AST syntax (shown above), with XML
being the default. Parsing human-readable AST syntax requires ANTLR, so
it is not supported if no third-party dependencies are allowed at
runtime. If you want to build **Microsoft.ProgramSynthesis** *without* support for
human-readable AST (and, consequently, without ANTLR dependency), use
the build configuration “ReleaseNoHumanReadableAsts” for this project
instead of “Release”.

To serialize the AST $p$ into a string representation, call
`p.PrintAST`. This method also accepts an optional **format**
parameter, with XML being the default.

Note that explicit lambda functions are part of the implementation of a
concept rule. The AST that is being parsed should reflect its formal
interface, not the implementation. In other words, your final ASTs
should only use the terms from the left-hand side of an “=” sign, and
nothing from the right-hand side. Here’s an example of AST parsing for a
grammar with explicit lambda functions:

```csharp
const string TestFlareGrammar = @"
   reference 'TestSemantics.dll';
   using semantics TestSemantics.Flare;
   using learners TestSemantics.Flare.Learners;
   language Flare;
   @input string[] v;
   Regex r;
   bool f := Match(x, r) | True();
   @start string[] P := Selected(v, f) = Filter(\x: string => f, v);
";
var grammar = DSLCompiler.LoadGrammar(TestFlareGrammar).Value;
var ast = grammar.ParseAST("Selected(v, Match(x, new Regex(\"[a-z]+\")))")",
	ASTSerializationFormat.HumanReadable);
var state = new State(grammar.InputSymbol, new[] {"1", "ab", ""});
Assert.That(ast.Invoke(state), Is.EqualTo(new[] {"ab"}).AsCollection);
```

You can also parse ASTs with arbitrary symbols as roots, not only the
start symbol of the grammar. To do so, call `p.ParseAST` where
$p$ is a root `Symbol` variable. The call `grammar.ParseAST(s)`
is equivalent to `grammar.StartSymbol.ParseAST(s)`.

## DSL Compiler

The DSL compiler **dslc.exe** is a command-line tool that takes a
grammar definition file, and compiles it into a serialized format,
potentially emitting any warnings/errors on the way. The command-line
usage is shown below:

```console
Microsoft Program Synthesis using Examples DSL Compiler 0.5.0.180-9c4db2d
Created by Microsoft Program Synthesis using Examples team (prose-contact@microsoft.com), 2014-2015.
Usage: dslc.exe [options] INPUT_GRAMMAR

  -p, --path			   Additional directories to locate assembly
						   references, separated by semicolons.

  -o, --output			 (Default: "<DSLName>.grammar.xml") Output file for
						   the serialized grammar object.

  -v, --verbosity		  (Default: Normal) Define verbosity level of
						   messages printed by the compiler. Possible values:
						   Silent=0, Errors=1, Warnings=2,
						   Normal (Errors | Warnings), Debug=4,
						   Verbose (Normal | Debug).

  -w, --warn-categories	(Default: All) Define categories of warnings/errors
						   that should be validated, separated by semicolons.
						   Possible values: None, Core, Syntax, Semantics,
						   Learning, Features, All.

  --indent				 Indent the XML in the serialized grammar file.

  --help				   Display this help screen.

```

## Partial programs

In many applications there is need to manipulate **partial programs** –
ASTs where some tree nodes are replaced with **holes**. A hole is a
special type of an AST node: it’s an unfilled placeholder for some
instantiation of a corresponding grammar symbol. In the AST node class
hierarchy it is represented as `Microsoft.ProgramSynthesis.AST.Hole`.

To get a list of descendant holes in a program $p$, call
`p.Holes`. Note that in current architecture AST nodes do not
maintain any references to their parents. Consequently, to enable
practical usages, the `Holes` property returns not the
`Hole` nodes themselves, but their *positions* instead. A
position is represented as a tuple $(P,\ k,\ H)$, where $P$ is a parent
AST node of a hole, $k$ is the hole’s index in the list of $P$’s
children, and $H$ is the hole itself.

A string representation of a hole of symbol $S$ is **“?S”.** The
framework supports parsing AST strings that contain holes.

# Synthesis

## Specifications

Program synthesis in the PROSE SDK is defined as a process of
generating a **set of programs** $\tilde{P}$ that start with a
**root symbol** $P$, given a **specification**
$\varphi$**.** A specification is a way of defining the desired
behavior of every program in $\tilde{P}$. Different synthesis
applications have different *kinds* of specifications to specify the
desired program behavior. Some of the examples from prior work include:

-   In FlashFill, $\varphi$ is an *example specification*: for every given
	input state $\sigma$, the desired program output should be equal to
	the given string $\varphi\lbrack\sigma\rbrack$.

-   In Extraction.Text, $\varphi$ is a *subset specification*. It assumes that
	the runtime type of a root symbol $P$ is some sequence
	`IEnumerable<T>`. The specification $\varphi$ expresses
	the following property: for every given input state $\varphi$, the
	desired program output should be a *superset* of the given subset
	$\varphi\lbrack\sigma\rbrack$. In terms of Extraction.Text,
	$\varphi\lbrack\sigma\rbrack$ are the substrings highlighted by the
	user, and the program output includes them and the rest of the
	substrings that should be highlighted.

The supported specification kinds are located in the namespace
`Microsoft.ProgramSynthesis.Specifications`. All of them inherit
the base abstract class `Specification`. This class has a single
abstract method `Valid`$\colon$ `(State, object)` $\to$ `bool`,
which returns `true` if and only if the program output on the
given input state satisfies the constraint that the specification
expresses.

`Spec` is the main abstract base class for all
*inductive specifications*, i.e. those that constraint the program
behavior on a set of provided input states $\varphi$.`ProvidedInputs`.
Some of the main inductive specification kinds are described below:

-   `InductiveConstraint` specifies an arbitrary constraint
	`Constraint`$\colon$ `(State, object)` $\to$ `bool` on the
	set of provided input states.
-   `SubsequenceSpec` specifies a
	subsequence of the desired output sequence for each provided input
	state in the dictionary
	`Examples`$\colon$ `State` $\mapsto$ `IEnumerable<object>`.
	-   `PrefixSpec` is a subclass of
		`SubsequenceSpec` where the desired
		subsequence of a program output is also required to be its
		prefix.
-   `FunctionalOutputSpec` describes the behavior of the
	program whose output type is a lambda function or a functional
	symbol. For each input state, it specifies a set of input/output
	pairs. These pairs are the behavior examples for the function *that
	is the output of a desired program on a given state*.
-   `DisjunctiveExamplesSpec` specifies a set of
	possible desired outputs for each provided input state. On a given
	state $\sigma$, the program output is allowed to equal *any of* the
	given possible outputs
	$\varphi$.`DisjunctiveExamples`$\lbrack\sigma\rbrack$.
	-   `ExampleSpec` is a subclass of
		`DisjunctiveExamplesSpec` where the size of
		$\varphi$.`DisjunctiveExamples`$\lbrack\sigma\rbrack$ is constrained to 1.
		In other words, this is the simplest specification
		that specifies the single desired program output for each
		provided input state.

## Strategies

The main point of program synthesis in the framework is the
`SynthesisEngine` class. Its function is to execute different
**synthesis strategies** for synthesizing parts of the resulting
program. The synthesis process starts with an
`engine.LearnGrammar(`$\varphi$`)` call to synthesize a program that starts
with a start symbol of the grammar, or, more generally,
`engine.LearnSymbol(`$P$, $\varphi$`)` call to synthesize a program that starts
with a given grammar symbol $P$.

Given a **learning task** $\left\langle P,\varphi \right\rangle$
to synthesize a program that starts with a symbol $P$ and satisfies the
specification $\varphi$, the engine can assign this task to any of the
available **synthesis strategies**. A synthesis strategy represents a
specific *algorithm* that can synthesize a program set $\tilde{P}$ for a
particular kind of a learning task $\left\langle P,\varphi\right\rangle$.
In other words, a synthesis strategy is parameterized by its supported
*specification type*, takes a learning task
$\left\langle P,\varphi\right\rangle$ where $\varphi$ should be an instance
of this specification type, and learns a set of programs  $\tilde{P}$
that are consistent with $\varphi$.

A synthesis strategy is represented as a class inheriting
`Microsoft.ProgramSynthesis.Learning.SynthesisStrategy<TSpec, TConfig>`,
which specifies the following contract:

```csharp
public abstract class SynthesisStrategy<TSpec, TConfig> : ISynthesisStrategy
		where TSpec : Spec where TConfig : StrategyConfig, new()
{
	void Initialize(SynthesisEngine engine);
	abstract Optional<ProgramSet> LearnSymbol(SynthesisEngine engine, LearningTask<TSpec> task, CancellationToken cancel);
	bool CanCall(Spec spec);
}
```
Here `TSpec` is a supported specification type, `TConfig` is a type of
a configuration that will be passed to the strategy constructor, `LearnSymbol`
is the main learning method, and `CanCall` is the function that determines
whether this synthesis strategy supports learning for a given
specification `spec` (in the default implementation, the result is
`true` if and only if `spec` is an instance of `TSpec`).

## Version space algebra

The return type of `Learn` in `SynthesisStrategy` is
`Microsoft.ProgramSynthesis.VersionSpace.ProgramSet`. This is an
abstract base class for our representation for the sets of programs. A
**version space**, defined by Mitchell  and Lau , is a succinct
representation of a program set (*hypothesis space* in machine learning
community), consistent with a specification. A version space can be
defined explicitly (as a set of programs), or composed from smaller
version spaces using standard set operators. The latter property is a
key to succinctness of version spaces: representing exponential sets of
programs using composition operators requires only polynomial space.
Such a structure defines an *algebra* over primitive version spaces,
hence the name.

An abstract `ProgramSet` class defines the following contract:

```csharp
public abstract class ProgramSet
{
	protected ProgramSet(Symbol symbol)
	{
		Symbol = symbol;
	}

	public Symbol Symbol { get; private set; }

	public abstract IEnumerable<ProgramNode> RealizedPrograms { get; }

	public abstract ulong Size { get; }

	public virtual bool IsEmpty
	{
		get { return RealizedPrograms.IsEmpty(); }
	}

	public abstract ProgramSet Intersect(ProgramSet other);

	public Dictionary<object, ProgramSet> ClusterOnInput(State input);

	public Dictionary<object[], ProgramSet> ClusterOnInputs(IEnumerable<State> input);

	public abstract XElement ToXML();
}
```
The property `RealizedPrograms` calculates the set of programs
stored in the version space.

### Direct version space
Direct version space is a primitive version space
that represents a set of programs explicitly, by storing a reference to
`IEnumerable<ProgramSet>`. Since
`IEnumerable<T>` in .NET is lazy,
storing it in a version space does not by itself enumerate it into an
explicit set. This allows you to implement a synthesis strategy as an
*iterator* in C\# (with **yield return** statements), which calculates
the required number of consistent programs on the fly, as needed.
Moreover, such an iterator can even be theoretically infinite, as long
as the end-user requests only a finite number of consistent programs.

### Union version space
A union of $k$ version spaces is a version space
that contains those and only those programs that belong to at least one
of the given $k$ spaces. Such a version space naturally arises when we
want to learn a set of programs that start with a certain grammar symbol
by learning each of its possible RHS rules automatically. For example:

<div> $$
	P\ : = F\left( A,\ B \right) \mid G\left( C, D \right)
$$ </div>

Here $P,A,B,C,D$ are grammar symbols, and $F,G$ are black-box operators.
Given a learning task $\left\langle P,\varphi \right\rangle$, one way to
resolve it is to independently learn a set of programs $\tilde{F}$ of
form $F(?A,\ \ ?B)$, and a set of programs $\tilde{G}$ of form
$G(?C,\ \ ?D)$. If all the programs in $\tilde{F}$ and $\tilde{G}$ are
consistent with $\varphi$, then $\tilde{F} \cup \tilde{G}$ is a valid
answer to the outer learning task $\left\langle P,\varphi \right\rangle$.

### Join version space
A join version space is defined for a single
operator `$P := F\left( E_{1},\ldots,E_{k} \right)$`. It represents a set of
programs $\tilde{P}$ formed by a Cartesian product of parameter version
spaces `$\tilde{E_{1}},\ldots,\tilde{E_{k}\ }$`. In general, not every
combination of the parameter programs from this Cartesian product is a
valid sequence of parameters for the operator $F$, thus join version
space depends on the operator logic to filter out the invalid
combinations.

The table below outlines the APIs for building version spaces in the
Microsoft Program Synthesis using Examples framework:

 Version space			 | Given									| Builder code
 --------------------------|------------------------------------------|------------------
 Empty					 | `Symbol s`							   | `ProgramSet.Empty(s)`
 Explicit list of programs | `Symbol s` <br/> `ProgramNode p1, …, pk` | `ProgramSet.List(s, p1, …, pk)`
 Lazy stream of programs| `Symbol s` <br /> `IEnumerable<ProgramNode> stream` | `new DirectProgramSet(s, stream)`
 Union of version spaces for the rules | `Symbol s` <br/> `ProgramSet v1, …, vk` | `new UnionProgramSet(s, v1, …, vk)`
 Join of version spaces for the parameters |  `Symbol s` <br/> `GrammarRule r // r.Head == s` <br/> `ProgramSet v1, …, vk` | `new JoinProgramSet(r, v1, …, vk)`


[^1]: In other words, **null** is used as a special value $\bot$ that is typically found in a formal definition of a language.

[^3]: Following Haskell syntax, we start our lambda functions with the “\\” character, which is supposed to approximately represent the letter $\lambda$.

[^4]: The common recipe that we use in our development is to reference the semantics DLL from the main project in a Visual Studio solution. This way, the semantics DLL is automatically copied to the target subdirectory on each build next to the main executable, and you can refer to it in the grammar string by simply using its filename. Alternatively, you can specify additional library paths as extra parameters to the `LoadGrammar` method.
