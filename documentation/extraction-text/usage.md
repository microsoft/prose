---
date: 2015-09-02T20:00:16-07:00
title: Text Extraction - Usage
toc: true
---

The Extraction.Text learning APIs are accessed through the `Extraction.Text.Learner` class.
The two primary methods are `LearnRegion()` and `LearnSequence()` which take a set of examples
and learn a Extraction.Text program consistent with those examples.

The Extraction.Text program is defined in `Extraction.Text.Program` class.
A program is either a substring program or a sequence program.
The key method is `Run()` to execute the program on some input string(s) to obtain the extracted output.
Other important methods include serialization (`Serialize()`) and deserialization (`Load()`) of a program.

In order to use
Extraction.Text, you need assembly references to `Microsoft.ProgramSynthesis.Extraction.Text.dll`, `Microsoft.ProgramSynthesis.Extraction.Text.Learner.dll`
and `Microsoft.ProgramSynthesis.Extraction.Text.Semantics.dll`. Again, the sample project `Extraction.Text.Sample` illustrates our API usage.

StringRegion
===

A substring in Extraction.Text is called `StringRegion`.
A `StringRegion` is a triple `(S, Start, End)`, where `S` is the input string, `Start` and `End` are the starting/ending positions within `S`.

A position specifies the location between two characters in `S`, and is zero-based.
For example, the position at the beginning of `S` is 0, the one between the first and second character is 1, and so on.

We can create a `StringRegion` from a string using the following method:

```csharp
StringRegion StringRegion.Create(string s);
```

**Notes:** We should **NOT** use `Create()` method to create sub-regions within a `StringRegion`. That is, we should not use ``StringRegion.Create(region.S.substring(_))``.
It is because during learning, we build a matching cache for the input string.
Creating `StringRegion`s for substrings forces us to calculate new caches for these substrings, which is not necessary because the cache for entire parent string exists.

Instead, we should use the `Slice` method to create sub-regions from the input `StringRegion`.

```csharp
StringRegion Slice(uint start, uint end);
```

For example, we can create two sub-regions within a `StringRegion` as follows:

```csharp
StringRegion record = new StringRegion("Carrie Dodson 100");
StringRegion name = record.Slice(0u, 13u); // "Carrie Dodson""
StringRegion number = record.Slice(14u, 17u); // "100"
```

ExampleSpec
===

A `ExampleSpec` is a pair of a referencing `StringRegion` and its corresponding output `StringRegion`.


The referencing `StringRegion` can be one of the following kinds:

- **Parent:** The referencing `StringRegion` *contains* the example.
For instance,

```csharp
ExampleSpec nameRefRecExample = new ExampleSpec<StringRegion>(record /* Carrie Dodson 100 */, name /* Carrie Dodson */);
```

- **Preceding Sibling:** The referencing `StringRegion` is a sibling that appears *before* the example.
For instance,

```csharp
ExampleSpec numRefNameExample = new ExampleSpec<StringRegion>(name /* Carrie Dodson */, number /* 100 */);
```

- **Succeeding Sibling:** The referencing `StringRegion` is a sibling that appears *after* the example.
For instance,

```csharp
ExampleSpec nameRefNumExample = new ExampleSpec<StringRegion>(number /* 100 */, name /* Carrie Dodson */);

```

The referencing `StringRegion` represents some existing knowledge on the structure of the input `StringRegion`.
Extraction.Text learns a new region/sequence program using referencing `StringRegion` as clues.
If we do not have such clues (for instance, when we learn the first program), we can use the entire input `StringRegion` as the referencing `StringRegion`.

## Positive & Negative Example

An example is either positive or negative in Extraction.Text.

The examples must be consistent.
Positive examples must not overlap with each other.
Negative example must not overlap with positive examples.


Extraction.Text guarantees that the learnt program is consistent with the provided positive examples.
That is, the output of this program on the training `StringRegion` should *match* at least these positive examples.

The program should also be consistent with the provided negative examples.
Specifically, the output of this program should not *overlap* with any negative examples.

Extracting a StringRegion
===================

## Basic Usage

Extraction.Text may take *one or more* examples to learn a region program.
It accepts multiple examples that come from different regions in a file, or from different files.
Note that positive examples should *not* share the same referencing region, or we have duplicating/conflicting examples.

The below example illustrates a learning session with 2 positive examples.
Note that Extraction.Text can learn the intended program with just 1 example.

```csharp
var input1 = StringRegion.Create("Carrie Dodson 100");
var input2 = StringRegion.Create("Leonard Robledo 75");

var positiveExamples = new[] {
    new ExampleSpec<StringRegion>(input1, input1.Slice(7, 13)), // "Carrie Dodson 100" => "Dodson"
    new ExampleSpec<StringRegion>(input2, input2.Slice(8, 15)) // "Leonard Robledo 75" => "Robledo"
};
var negativeExamples = Enumerable.Empty<ExampleSpec<StringRegion>>();

Program topRankedProg = Learner.Instance.LearnRegion(positiveExamples, negativeExamples);

var testInput = StringRegion.Create("Margaret Cook 320"); // expect "Cook"
IEnumerable<StringRegion> run = topRankedProg.Run(testInput);
// Retrieve the first element because this is a region textProgram
var output = run.FirstOrDefault();
Console.WriteLine("\"{0}\" => \"{1}\"", testInput, output);
```

We use similar set up to learn a program where the number references the word,
and to learn a program where the word references the number.


## Learning With Negative Examples
We specify negative examples to rule out programs whose outputs overlap with at least one of our negative examples.

The below example illustrates the usage of negative examples.

```csharp
var input = StringRegion.Create("Carrie Dodson 100\nLeonard Robledo NA\nMargaret Cook 320");
StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

// Suppose we want to extract "100", "320".
var positiveExamples = new[] {
    new ExampleSpec<StringRegion>(records[0], records[0].Slice(14, 17)) // "Carrie Dodson 100" => "100"
};
var negativeExamples = new[] {
    new ExampleSpec<StringRegion>(records[1], records[1]) // no extraction in "Leonard Robledo NA"
};

// Extraction.Text will find a program whose output does not OVERLAP with any of the negative examples.
Program topRankedProg = Learner.Instance.LearnRegion(positiveExamples, negativeExamples);

foreach (var r in topRankedProg.Run(records))
{
    var output = r.Output != null ? r.Output.Value : "null";
    Console.WriteLine("\"{0}\" => \"{1}\"", r.Reference, output);
}
```

## Learning With Additional References

We may provide additional references (which may come from the same file or different files) to Extraction.Text.
Extraction.Text observes the behavior of the learnt programs on these additional references, and uses this observation to better rank programs.

**TIPS**: It is always a good idea to provide additional references.
Extraction.Text will be more likely to identify the desired program.
The only drawback is the learning time will increase (because Extraction.Text has to evaluate the learnt programs on these additional references),
but it is often marginal.

```csharp
var input = StringRegion.Create("Carrie Dodson 100\nLeonard Robledo 75\nMargaret Cook ***");
StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

// Suppose we want to extract "100", "75", and "***".
var positiveExamples = new[] {
    new ExampleSpec<StringRegion>(records[0], records[0].Slice(14, 17)) // "Carrie Dodson 100" => "100"
};
var negativeExamples = Enumerable.Empty<ExampleSpec<StringRegion>>();

// Additional references help Extraction.Text observe the behavior of the learnt programs on unseen data.
// In this example, if we do not use additional references, Extraction.Text may learn a program that extracts the first number.
// On the contrary, if other references are present, it knows that this program is not applicable on the third record "Margaret Cook ***",
// and promotes a more applicable program.
Program topRankedProg =
    Learner.Instance.LearnRegion(positiveExamples, negativeExamples, records.Skip(1));

foreach (var r in topRankedProg.Run(records))
{
    var output = r.Output != null ? r.Output.Value : "null";
    Console.WriteLine("\"{0}\" => \"{1}\"", r.Reference, output);
}
```

## Learning With Regular Expressions

Users also have an option to provide 3 regular expressions for the extracted region: the lookbehind regex, the matching regex, and the lookahead regex.
All 3 regular expressions are optional.

```csharp
var input = StringRegion.Create("Carrie Dodson 100\nLeonard Robledo NA\nMargaret Cook 320");
StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

// Suppose we want to extract the number out of a record
var positiveExamples = new[] {
    new ExampleSpec<StringRegion>(records[0], records[0].Slice(14, 17)), // "Carrie Dodson 100" => "100"
};
var negativeExamples = Enumerable.Empty<ExampleSpec<StringRegion>>();

Regex lookBehindRegex = new Regex("\\s");
Regex lookAheadRegex = null;
Regex matchingRegex = new Regex("\\d+");

IEnumerable<Program> topRankedPrograms =
    Learner.Instance.LearnTopKRegion(positiveExamples, negativeExamples, null, 1, lookBehindRegex, matchingRegex, lookAheadRegex);

Program topRankedProg = topRankedPrograms.FirstOrDefault();

foreach (var r in topRankedProg.Run(records))
{
    var output = r.Output != null ? r.Output.Value : "null";
    Console.WriteLine("\"{0}\" => \"{1}\"", r.Reference, output);
}
```


Extracting a Sequence of StringRegions
===

Learning a program to extract a sequence of `StringRegion` is similar to learning a program to extract a `StringRegion`.

> **IMPORTANT:**
>
> - Give at least 2 examples because generalizing a sequence from a single element is hard.
>
> - Give positive examples continuously (i.e., we cannot skip any positive examples).
> Extraction.Text assumes that all the text prior to the last example that are not selected are negative examples.
> For instance, in extracting {"A", "C"} from "A B C", Extraction.Text assumes that " B " is a negative example.

## Extracting from a Parent Region

```csharp
var input = StringRegion.Create("United States\nCarrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320\n" +
                                "Canada\nConcetta Beck 350\nNicholas Sayers 90\nFrancis Terrill 2430\n" +
                                "Great Britain\nNettie Pope 50\nMack Beeson 1070");
// Suppose we want to extract all last names from the input string.
var positiveExamples = new[] {
    new ExampleSpec<StringRegion>(input, input.Slice(14, 20)), // input => "Carrie"
    new ExampleSpec<StringRegion>(input, input.Slice(32, 39)) // input => "Leonard"
};
var negativeExamples = Enumerable.Empty<ExampleSpec<StringRegion>>();

Program topRankedProg = Learner.Instance.LearnSequence(positiveExamples, negativeExamples);

foreach (var r in topRankedProg.Run(input))
{
    var output = r != null ? r.Value : "null";
    Console.WriteLine(output);
}
```

## Extracting from a Sequence of Referencing Regions
</h2>

The examples (if any) in each referencing region have to be continous.
However, we do not need to provide all examples in previous regions before providing examples in the current region.

For instance, this set of examples is wrong { "Dodson", "Cook"} (missing "Robledo"), but
this set is valid { "Dodson", "Robledo", "Beck"}. Note that we do not need to include "Cook" to
include "Beck", because "Beck" belongs to a different referencing region.

```csharp
var input = StringRegion.Create("United States\nCarrie Dodson 100\nLeonard Robledo 75\nMargaret Cook 320\n" +
                                "Canada\nConcetta Beck 350\nNicholas Sayers 90\nFrancis Terrill 2430\n" +
                                "Great Britain\nNettie Pope 50\nMack Beeson 1070");
StringRegion[] countries = { input.Slice(0, 13), input.Slice(69, 75), input.Slice(134, 147) };

// Suppose we want to extract all last names from the input string.
var positiveExamples = new[] {
    new ExampleSpec<StringRegion>(countries[0], input.Slice(14, 20)), // "United States" => "Carrie"
    new ExampleSpec<StringRegion>(countries[0], input.Slice(32, 39)), // "United States" => "Leonard"
};
var negativeExamples = Enumerable.Empty<ExampleSpec<StringRegion>>();

Program topRankedProg = Learner.Instance.LearnSequence(positiveExamples, negativeExamples);

foreach (var r in topRankedProg.Run(countries))
{
    var output = r.Output != null ? r.Output.Value : "null";
    Console.WriteLine("\"{0}\" => \"{1}\"", r.Reference, output);
}
```

The other APIs for learning a sequence program are similar to their region learning counterparts.



Learning multiple programs
==========================

There are usually a large number of programs consistent with any given set of
examples. Extraction.Text has a ranking scheme which it uses to return the most
likely program for the examples it has seen, but in some cases this may not
be the desired program.

## Learn Top k Programs

`LearnTopKRegion` and `LearnTopKSequence` have a parameter `k` which specifies how many programs
they should try to learn; they return the top `k` ranked programs (or programs with
the top `k` ranks if there are ties).

```csharp
var input = StringRegion.Create("Carrie Dodson 100");

var positiveExamples = new[] {
    new ExampleSpec<StringRegion>(input, input.Slice(14, 17)) // "Carrie Dodson 100" => "Dodson"
};
var negativeExamples = Enumerable.Empty<ExampleSpec<StringRegion>>();

IEnumerable<Program> topKPrograms = Learner.Instance.LearnTopKRegion(positiveExamples, negativeExamples, 3);
```

# Learn All Programs

`LearnAllRegion` and `LearnAllSequence` return a set of all programs consistent with the examples.

```csharp
var input = StringRegion.Create("Carrie Dodson 100");

var positiveExamples = new[] {
    new ExampleSpec<StringRegion>(input, input.Slice(14, 17)) // "Carrie Dodson 100" => "Dodson"
};
var negativeExamples = Enumerable.Empty<ExampleSpec<StringRegion>>();

ProgramSet allPrograms = Learner.Instance.LearnAllRegion(positiveExamples, negativeExamples);
```


Serializing/Deserializing a Program
===

The `Extraction.Text.Program.Serialize()` method serializes the learnt program to a string.
The `Extraction.Text.Program.Load()` method deserializes the program text to a program.



```csharp
// program was learnt previously
string progText = program.Serialize();
Program loadProg = Program.Load(progText);
```

Executing a Program
===

The following APIs execute the learnt program on the referencing `StringRegion`(s) to obtain the output result.
Note that the result may contain `null` (on some referencing `StringRegion`s) because the learnt program may not be general enough
to cover these `StringRegion`s.
To avoid this problem, provide additional references during learning so Extraction.Text has more information
on the structure of the file.

```csharp
public IEnumerable<StringRegion> Run(StringRegion reference);
public IEnumerable<ExampleSpec<StringRegion>> Run(IEnumerable<StringRegion> references);
public IEnumerable<StringRegion> OutputRun(IEnumerable<StringRegion> references);
```

## Run(StringRegion)

The output of the sequence program is the entire returned list.
The output of the region program is the first element in the returned list (since it only extracts a `StringRegion` from a reference).


> **IMPORTANT:** If you have multiple references and the referencing `StringRegion`s are (preceding or succeeding) siblings, do *NOT* use this method.
> Instead, use `Run(IEnumerable<StringRegion> references)` or `OutputRun(IEnumerable<StringRegion> references)`.

It is because `references.SelectMany(r => new ExampleSpec<StringRegion>(r, program.Run(r))` is *NOT* equivalent to `program.Run(references)`.

In these sibling referencing scenarios, Extraction.Text seeks for the output in the `StringRegion` formed by two contiguous siblings.
If only one referencing sibling is provided, Extraction.Text will search for the output until it reaches end of file (in case of preceding siblings)
or begin of file (in case of succeeding siblings).

For example, consider the task of extracting {"11", "33"} from "AA-11\nBB-bb\nCC-33".
Suppose the referencing (preceding) siblings are {"AA", "BB", "CC"}.

If we use only "BB" as the reference, Extraction.Text will search for the first number in "BB-bb\nCC-33", which is 33. This is clearly not intended.
On the other hand, if we pass the list {"AA", "BB", "CC"} as references, Extraction.Text will not find a number for "BB" (the search range is "BB-bb\n").
This is the intended behavior of this program.

Parent referencing scenario is not affected because Extraction.Text searches for the match *within* the parent.

## Run(IEnumerable\<StringRegion\>)

This method returns a sequence of (reference, output) pairs.
Use this method if the application needs to maintain the relationship between the referencing `StringRegion`s and the output `StringRegion`s.

## OutputRun(IEnumerable\<StringRegion\>)

Use this method if the application only needs the output `StringRegion`s.
