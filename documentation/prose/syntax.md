---
title: Grammar syntax
---
{% include toc.liquid.md %}

This document describes the syntax of PROSE v1 DSL grammars.

> **Note:** this syntax is volatile and subject to breaking changes in the future. We will notify you about each breaking change in accordance with [SemVer](http://semver.org/).

> Hereafter in the general syntax descriptions, angle brackets `<>` denote a placeholder to be filled with a concrete value, and curly braces `{}` with an `opt` subscript denote an optional component (unless specified otherwise).

## Basic structure

The basic structure of a `*.grammar` file is as follows:

```
<Namespace usings>
<Semantics usings>
<Learner usings>

language <Name>;
<Feature declarations>

<Nonterminal symbols and rules>
<Terminal symbols>
```
{: .language-bnf}

It first specifies some metadata about the DSL, and then describes it as a grammar. A PROSE language is represented as a [context-free grammar](https://en.wikipedia.org/wiki/Context-free_grammar) – *i.e.,* as a set of *rules*, where each *symbol* on the left-hand side is bound to a set of possible expansions of this symbol on the right-hand side.


## Usings

### Namespace usings

These statements are identical to the corresponding C# forms. They import a namespace into the current scope.

```
using System.Text.RegularExpressions;
```
{: .language-bnf}

### Semantics usings

These statements specify *semantics holders* – static classes that contain implementations of the grammar's operators. There may be more than one semantics holder, as long as their members do not conflict with each other.

```
using semantics TestLanguage.Semantics.Holder;
```
{: .language-bnf}

### Learner usings

These statements specify *learning logic holders* – non-static classes that inherit `DomainLearningLogic` and contain domain-specific helper learning logic such as [witness functions]({{site.baseurl}}/documentation/prose/backpropagation/#witness-functions) and value generators. There may be more than one learning logic holder.

```
using learners TestLanguage.Learning.LogicHolder;
```
{: .language-bnf}

## Language name

May be any valid C# *full type identifier* – that is, a dot-separated string where each element is a valid C# identifier.

## Features

A [feature]({{site.baseurl}}/documentation/prose/tutorial/#features) is a computed property on an AST in the language. Each such property has a name, type, and associated *feature calculator* functions that compute the value of this property on each given AST. A feature may be declared as *complete*, which requires it to be defined on every possible AST kind in the language. By default, a feature is not complete.

**Syntax:**

```
{@complete} feature <Type> <Name> = <Implementation class 1>, …, <Implementation class N>;
```
{: .language-bnf}

Here `@complete` is an optional completeness annotation, and the comma-separated identifiers on the right specify one or more classes that inherit `Feature<T>` and provide implementations of the feature calculators. Notice that one feature may be implemented in multiple possible ways (*e.g.* the program's `RankingScore` may be computed differently as `LikelihoodScore` or `PerformanceScore`, depending on the requirements), thus it is possible to specify multiple implementation classes for the same feature.

> A feature class does *not* have to be specified in the `*.grammar` file to properly interact with the framework. As long as it inherits `Feature<T>` and holds the required feature calculator implementations, its instances may be used at runtime to compute the value of the corresponding feature on the ASTs. However, specifying it in the `*.grammar` file provides additional information to the `dslc` grammar compiler. The compiler can then verify your feature calculator definitions and provide more detailed error messages.

**Example:**

```
using TestLanguage.ScoreFunctions;
@complete feature double RankingScore = LikelihoodScore, PerformanceScore, ReadabilityScore;

// alternatively:
@complete feature double RankingScore = TestLanguage.ScoreFunctions.LikelihoodScore,
                                        TestLanguage.ScoreFunctions.PerformanceScore,
                                        TestLanguage.ScoreFunctions.ReadabilityScore;

feature HashSet<int> UsedConstants = TestLanguage.UsedConstantsCalculator;
```
{: .language-bnf}

## Terminal rules

Each *terminal symbol* of the grammar is associated with its own unique *terminal rule*. Terminal rules specify the leaf symbols that will be replaced with literal constants or variables in the AST. For example:

-   A terminal rule `int k;` specifies a symbol $k$ that represents a literal integer constant.
-   A terminal rule `@input string v;` specifies a *variable* symbol $v$ that contains program input data *at runtime.*

**Syntax:**

```
{@values[<Generator member>]} {@input} <Type> <Symbol name>;
```
{: .language-bnf}

### Annotations

##### `@input`

Denotes the input variable passed to the DSL programs. A DSL program may depend only on a single input variable, although of an arbitrary type.

##### `@values`

A user can specify the list of possible values that a literal symbol can be set to. This is done with a **@values[**$G$**]** annotation, where $G$ is a **value generator** – a reference to a user-defined static field, property, or method. The compiler will search for $G$ in the provided learning logic holders, and will report an error if it does not find a type-compatible member.

**Example:** given the following declaration of a terminal `s`:

```
using learners TestLanguage.Learners;
@values[StringGen] string s;
```
{: .language-bnf}

any of the following members in `TestLanguage.Learners` can serve as a generator for `s`:

```csharp
namespace TestLanguage
{
	public class Learners : DomainLearningLogic
	{
		// Field
		public static string[] StringGen = {"", "42", "foobar"};

		// Property
		public static string[] StringGen => new[] {"", "42", "foobar"};
        public static string[] StringGen {
            get { return new[] {"", "42", "foobar"}; }
        }

		// Method
		public static string[] StringGen() => new[] {"", "42", "foobar"};
        public static string[] StringGen() {
            return new[] {"", "42", "foobar"};
        }
	}
}
```

## Nonterminal rules

A *nonterminal rule* describes a possible [production](https://en.wikipedia.org/wiki/Production_(computer_science)) in a context-free grammar of the DSL. In contrast to conventional programming languages, the productions of PROSE grammars describe not the surface syntax of DSL programs, but their direct semantics as ASTs. In other words, where a conventional context-free grammar would specify something like

```
expression := atom '+' atom | atom '-' atom ;
```

the corresponding PROSE grammar specifies

```
expression := Plus(atom, atom) | Minus(atom, atom) ;
```
{: .language-dsl}

This snippet contains two nonterminal rules `expression := Plus(atom, atom)` and `expression := Minus(atom, atom)`. The functions `Plus` and `Minus` are *operators* in the grammar – domain-specific functions that may be included as steps of your  DSL programs. Thusly, PROSE DSLs do not have a syntax – they directly describe a grammar of possible domain-specific program actions.

### Structure

Every nonterminal rule has a *head* and a *body*. Its head is a typed *nonterminal symbol* on the left-hand side of the production. Its body is a sequence of free symbols on the right-hand side, which may be nonterminal or terminal (i.e. variables or constants). There exist multiple different kinds of nonterminal rules, which differ in their semantics as well as in the roles of the symbols in their bodies.

**Syntax:**

```
{@start} <Type> <Symbol name> := <Rule 1> | ... | <Rule N>;
```
{: .language-bnf}

#### Annotations

##### `@start`

An optional annotation that specifies the _start symbol_ of the grammar – that is, the root nonterminal symbol of all programs in this DSL.

### Operator rules

> Coming soon...

### Conversion rules

> Coming soon...

### Let bindings

> Coming soon...

### Standard concepts

> Coming soon...

#### Lambda functions

> Coming soon...

### External rules

> Coming soon...
