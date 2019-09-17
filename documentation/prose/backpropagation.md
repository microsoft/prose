---
title: Backpropagation
---
{% include toc.liquid.md %}

Deductive strategy (i.e., backpropagation) is the main synthesis algorithm used by the PROSE SDK.
It relies on external annotations, provided by the DSL designer for the language operators -- [witness functions](#witness-functions).
Our [tutorial]({{ site.baseurl }}/documentation/prose/tutorial) and samples show many use cases for specific witness functions.

## Witness Functions
A witness function is a domain-specific deductive procedure for a parameter $k$ of an operator $F$, that, given an outer spec $\phi$ on $F$, answer a question: "What should a program used as this parameter satisfy in order for the entire $F$ expression to satisfy $\phi$?"
In other words, witness functions *backpropagate* specifications from expressions to their subexpressions.

There are two kinds of witness functions: non-conditional and conditional.

### Non-conditional
Non-conditional witness functions have the following signature:

``` csharp
[WitnessFunction("OperatorName", paramIndex)]
Spec Witness(GrammarRule rule, Spec outerSpec);
```

Since PROSE uses .NET reflection to extract information about witness functions, you should make the actual types in your signature as precise as possible.
In particular, the `outerSpec` parameter specifies what kind of spec for $F$ your witness function can handle.
Typically a different witness function is written for each spec kind: it takes a different requirement to satisfy a `PrefixSpec` that an `ExampleSpec`.

A witness function may produce an *overapproximation* to the required spec instead of a necessary and sufficient spec for the parameter #$k$.
PROSE can still use such a witness function, but it should be marked with `Verify = true` in its `[WitnessFunction]` metadata attribute.

If `outerSpec` is inconsistent (no program can possibly satisfy it), witness function should return `null`.
`null` in PROSE is a placeholder for an "always false" spec.
(An "always true" spec is called `TopSpec`).

### Conditional

Conditional witness functions depend not only on an outer spec on their operator, but also possibly on some other parameters of that operator.
They have the following signature:

``` csharp
[WitnessFunction("OperatorName", paramIndex, DependsOnParameters = new[] { prereqParam1, prereqParam2, ... }]
Spec Witness(GrammarRule rule, Spec outerSpec, Spec prereqSpec1, Spec prereqSpec2, ...);
```

As with non-conditional witness functions, prerequisite specs in the signature should be as precise as possible.
Typically they will be `ExampleSpec`s: deductive reasoning is easiest when you know precisely some fixed value of a prerequisite on the same input state.

You can use `DependsOnSymbols = new[] { prereqName1, prereqName2, ... }` in the attribute, referring to parameter names instead of their indices (if they are unambiguous).

### ID Annotations
If a target grammar rule does not have a name (for instance, it is a `let` rule or a conversion rule `A := B`), you can use an `@id` annotation in the grammar file to give it one, and then use this name as a reference in `[WitnessFunction]` attributes.

```
string expr := @id['LetSubstring'] let x = ChooseInput(inputs, k) in SubStr(x, posPair);
```
{: .language-dsl}

``` csharp
[WitnessFunction("LetSubstring", 0)]
Spec Witness(LetRule rule, Spec outerSpec);
```

In case of a `let` rule, it has two parameters: its "binding" expression (the part on the right-hand side of an equal sign) and its "body" expression (the part after **`in`**).
PROSE provides an automatic witness function for the body parameter, so you only to write one for the binding parameter (whose index in the containing `let` rule is $0$).

## Rule Learners
*Rule learners* are designed for use cases when you cannot express you deductive logic in terms of witness functions on individual parameters.
They are mini-strategies: search algorithms for one grammar rule.

> **Note:** Usage of rule learners is generally discouraged: if you can describe deductive reasoning as a witness function, PROSE framework can do a more aggressive optimization of its search process.

A rule learner has the following signature:

``` csharp
[RuleLearner("OperatorName")]
Optional<ProgramSet> Learn(SynthesisEngine engine, GrammarRule rule, LearningTask<Spec> task, CancellationToken token);
```

You can make recursive calls to `engine.LearnSymbol` in your rule learner to solve deductive subproblems.
The final result should be constructed as a `Optional<ProgramSet>` out of such subproblem results.
The learning task may have a more precise `Spec` type, for example `DisjunctiveExamplesSpec` or `ExampleSpec`.

> **Note:** it is a good .NET practice to check on the given `CancellationToken` regularly and throw a `TaskCancelledException` when you detect a cancellation request.
