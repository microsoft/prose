---
title: Standard concepts
---

***Standard concepts*** are built-in PROSE operators.
They have predefined semantics and, most of the time, witness functions for [backpropagation]({{site.baseurl}}/documenation/prose/backpropagation).
Thus, you can use standard concepts arbitrarily in your own DSLs without reimplementing them or designing your own synthesis procedures for them.

# Concept rules

The grammar syntax for a simple *concept rule* looks as follows:

```
Type P := CustomOperatorName(E1, ..., Ek) = ConceptName(E1, ..., Ek);
```
{: .language-dsl }

Here `ConceptName` is the name of the concept (*e.g.* `Pair`), and `CustomOperatorName` is a DSL-specific name for *this particular usage* of the concept (*e.g.* `RegexPair`).
The parameter symbols `E1`, $...$, `Ek` are passed directly to the standard concept.

> **Example:** in the `SubstringExtraction` language in our [tutorial]({{site.baseurl}}/documentation/prose/tutorial), the standard concept `Pair` is referenced as follows:
>
> ```
> Tuple<Regex, Regex> positionBoundaries := RegexPair(r, r) = Pair(r, r);
> Regex r;
> ```
> {: .language-dsl }
>
> The two `r` parameters denote the regexes to the left and to the right of a given position boundary. They are united into a tuple with a standard concept `Pair`.

## Lambda functions

More complex concept rules may include a *lambda function* on their right-hand side.
For instance, a list-processing operator `Filter` takes as input a predicate of type `Func<T, bool>` and a sequence of type `IEnumerable<T>`, and returns the filtered subsequence of elements that satisfy the predicate.
Here is a complete example of referencing `Filter` in a DSL:

```
#reference 'file:TestLanguage.dll';
using semantics TestLanguage.Filtering;
using learners TestLanguage.Filtering.Learners;

language Filtering;

@input string[] v;
Regex r;
bool f := Match(x, r) | True();
@start string[] P := Selected(f, v) = Filter(\x: string => f, v);
```
{: .language-dsl }

Here the *custom operator* `Selected(f, v)` is implemented as a *concept rule* `Filter`.
The first parameter of `Filter` is a lambda `\x: string => f`, and the second one is `v`.
Notice that *you can either use a formal parameter directly in a concept, or pass it down into a lambda body*.

# List of concepts

Concept                                           | Semantics                      | Specs handled by PROSE    | Witness functions needed?
-----------|----------------------------------|--------------------------------|---------------------------|--------------------------
**Pair**(`x: T`, `y: T`): `Tuple<T, T>`    | Combine `x` and `y` in a tuple | `DisjunctiveExamplesSpec` | &mdash;
**Map**(`f: Func<T, U>`, `seq: IEnumerable<T>`): `IEnumerable<U>` | Apply `f` to each element of `seq`, and return a sequence of results | `PrefixSpec` | `seq`
**Filter**(`f: Func<T, bool>`, `seq: IEnumerable<T>`): `IEnumerable<T>` | Return only those elements of `seq` that satisfy the predicate `f` | `PrefixSpec`, `SubsequenceSpec`, `ExampleSpec` | &mdash;
**FilterNot**(`f: Func<T, bool>`, `seq: IEnumerable<T>`): `IEnumerable<T>` | Return only those elements of `seq` that **do not** satisfy `f` | `PrefixSpec`, `SubsequenceSpec`, | &mdash;
**Kth**(`seq: IEnumerable<T>`, `k: int`): `T` | Return an element of `seq` at the specified index, from the left if $k \ge 0$ or from the right if $k < 0$ | `DisjunctiveExamplesSpec` | &mdash;
**TakeWhile**(`f: Func<T, bool>`, `seq: IEnumerable<T>`): `IEnumerable<T>` | Return the longest prefix of `seq` where all the elements satisfy  `f` | `SubsequenceSpec` | &mdash;
**FilterInt**(`initIter: Tuple<int, int>`, `seq: IEnumerable<T>`): `IEnumerable<T>` | Return a subsequence of `seq` defined by the arithmetic progression starting at the index `initIter.Item0` (0-based) with the step `initIter.Item1` | `PrefixSpec`, `SubsequenceSpec` | &mdash;
**First**(`f: Func<T, bool>`, `seq: IEnumerable<T>`): `T` | Return the first element of `seq` that satisfies  `f` | `ExampleSpec` | &mdash;








