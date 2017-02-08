---
title: Grammar syntax
---
{% include toc.liquid.md %}

This document describes the syntax of PROSE v1 DSL grammars.

> **Note:** this syntax is volatile and subject to breaking changes in the future. We will notify you about each breaking change in accordance with [SemVer](http://semver.org/).

> Hereafter in the general syntax descriptions, angle brackets `<>` denote a placeholder to be filled with a concrete value, and curly braces `{}` with an `opt` subscript denote an optional component (unless specified otherwise).

# Basic structure

The basic structure of a `*.grammar` file is as follows:

```
<References>
<Namespace usings>
<Semantics usings>
<Learner usings>

language <Name>;
<Feature declarations>

<Nonterminal symbols and rules>
<Terminal symbols>
```
{: .language-bnf}

# References

A PROSE grammar may reference any .NET assembly. During compilation, it will be resolved via standard assembly resolution rules of the current .NET runtime (desktop, Mono, or Core).
In particular, note that the referenced assembly may be a *compiled language assembly*.

There are two syntax forms for specifying a reference.

## File reference

```
#reference 'file:<Filename>';
```
{: .language-bnf}

Reference an assembly via its absolute or relative path. The compiler will look for it in the provided list of *library paths* (`dslc -p`). This list always includes the directory of the currently executing assembly and the current working directory.
**Example:**

```
#reference 'file:TestLanguage.Semantics.dll';
```
{: .language-bnf}

## Qualified assembly reference

```
#reference '<AssemblyQualifiedName>';
```
{: .language-bnf}

Reference an assembly via its assembly qualified name. The compiler will load it using [`Assembly.Load`](https://msdn.microsoft.com/en-us/library/ky3942xh.aspx) and its standard probing mechanism of the current .NET runtime.
**Example:**

```
#reference 'System.Collections.Immutable, Version=1.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a';
```
{: .language-bnf}

# Usings

## Namespace usings

These statements are identical to the corresponding C# forms. They import a namespace into the current scope.

```
using System.Text.RegularExpressions;
```
{: .language-bnf}

## Semantics usings

These statements specify *semantics holders* – static classes that contain implementations of the grammar's operators. There may be more than one semantics holder, as long as their members do not conflict with each other.

```
using semantics TestLanguage.Semantics.Holder;
```
{: .language-bnf}

## Learner usings

These statements specify *learning logic holders* – non-static classes that inherit `DomainLearningLogic` and contain domain-specific helper learning logic such as [witness functions]({{site.baseurl}}/documentation/prose/backpropagation/#witness-functions) and value generators. There may be more than one learning logic holder.

```
using learners TestLanguage.Learning.LogicHolder;
```
{: .language-bnf}

# Language Name

May be any valid C# *full type identifier* – that is, a dot-separated string where each element is a valid C# identifier.

# Features

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

# Symbols and rules

A PROSE language is described as a [context-free grammar](https://en.wikipedia.org/wiki/Context-free_grammar) – *i.e.,* as a set of *rules*, where each *symbol* on the left-hand side is bound to a set of possible *operators* on the right-hand side that represent possible expansions of this symbol.

## Terminal rules

Each *terminal symbol* of the grammar is associated with its own unique *terminal rule*. Terminal rules specify the leaf symbols that will be replaced with literal constants or variables in the AST. For example:

-   A terminal rule `int k;` specifies a symbol $k$ that represents a literal integer constant.
-   A terminal rule `@input string v;` specifies a *variable* symbol $v$ that contains program input data *at runtime.*

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

		// Method
		public static string[] StringGen() => new[] {"", "42", "foobar"};
	}
}
```

**Syntax:**

```
{@values[<Generator member>]} {@input} <Type> <Symbol name>;
```
{: .language-bnf}

## Nonterminal rules

> Coming soon.
