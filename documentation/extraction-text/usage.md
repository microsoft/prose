---
date: 2015-09-02T20:00:16-07:00
title: Text Extraction - Usage
---

{% include toc.liquid.md %}

The main entry points for learning substring extraction and sequence extraction are the `Learn()` methods in `RegionSession` class and `SequenceSession` class, respectively.

These methods generate `RegionProgram` and `SequenceProgram`, whose `Run()` methods produce the output substring and output sequence given an input string.

These program classes have the `Serialize()` methods to serialize the program. The `RegionLoader.Deserialize()` and `SequenceLoader.Deserialize()` methods deserialize a program text into `RegionProgram` and `SequenceProgram`, respectively.

The [Sample Project](https://github.com/Microsoft/prose/tree/master/Extraction.Text) illustrates our API usage.

## StringRegion

A substring in Extraction.Text is called `StringRegion`.
A `StringRegion` is a triple `(S, Start, End)`, where `S` is the input string, `Start` and `End` are the starting/ending positions within `S`.

A position specifies the location between two characters in `S`, and is zero-based.
For example, the position at the beginning of `S` is 0, the one between the first and second character is 1, and so on.

We can create a `StringRegion` from a string using the following method, in which `tokens` is a dictionary of `<name,  building-block regular expression>` to build the matches:

```csharp
StringRegion StringRegion.Create(string s, IReadOnlyDictionary<string, Token> tokens);
```

Alternatively, each `Session` also has a method to create a `StringRegion` using the default `tokens` dictionary:

```csharp
StringRegion RegionSession.CreateStringRegion(string s);

StringRegion SequenceSession.CreateStringRegion(string s);
```

> **Note:** We should *not* use `Create()` method to create sub-regions within a `StringRegion`. That is, we should not use ``StringRegion.Create(region.S.substring(_))``.
> 
> It is because during learning, we build a matching cache for the input string. Creating `StringRegion`s for substrings re-calculate new caches for these substrings, which is unnecessary because the cache for entire parent string exists.

Instead, we should use the `Slice` method to create sub-regions from the input `StringRegion`.

```csharp
StringRegion Slice(uint start, uint end);
```

For example, we can create two sub-regions within a `StringRegion` as follows:

```csharp
StringRegion record = RegionSession.CreateStringRegion("Carrie Dodson 100");
StringRegion name = record.Slice(0u, 13u); // "Carrie Dodson""
StringRegion number = record.Slice(14u, 17u); // "100"
```

## Extracting a StringRegion

### Positive Example

An `RegionExample` is a pair of an input`StringRegion` and its corresponding output `StringRegion`.

The input can be one of the following kinds:

- **Parent:** The input *contains* the example.

```csharp
var example = new RegionExample(record /*Carrie Dodson 100*/ , name /*Carrie Dodson*/);
```

- **Preceding Sibling:** The input is a sibling that appears *before* the example.

```csharp
var example = new RegionExample(name /*Carrie Dodson*/, number /*100*/);
```

- **Succeeding Sibling:** The input is a sibling that appears *after* the example.

```csharp
var example = new RegionExample(number /*100*/, name /*Carrie Dodson*/);
```

Most applications use only the *referencing-parent* example.

### Negative Example

A `RegionNegativeExample` defines a subregion in which the output of the learned program should not *overlap* with.

For instance, the following negative example dictates that all learned programs should not produce output that overlaps with "Carrie Dodson" given the input "Carrie Dodson 100".

``` csharp
var example = new RegionNegativeExample(record /*Carrie Dodson 100*/ , name /*Carrie Dodson*/);
```

### Basic Usage

**Extraction.Text** may take *one or more* examples to learn a region program. It accepts multiple examples that come from different regions in a file, or from different files.

**Important:** Each region should have at most one positive example, or we have duplicating/conflicting examples.

The below example illustrates a learning session with 2 positive examples.
Note that Extraction.Text can learn the intended program with just 1 example.

```csharp
var session = new RegionSession();

var input1 = RegionSession.CreateStringRegion("Carrie Dodson 100");
var input2 = RegionSession.CreateStringRegion("Leonard Robledo 75");

session.Constraints.Add(
    new RegionExample<StringRegion>(input1, input1.Slice(7, 13)), // "Carrie Dodson 100" => "Dodson"
    new RegionExample<StringRegion>(input2, input2.Slice(8, 15)) // "Leonard Robledo 75" => "Robledo"
);

Program topRankedProg = session.Learn();
```

### Running RegionProgram

Executing the `Run(StringRegion)` method on the input string returns the extraction output.

``` csharp
var testInput = RegionSession.CreateStringRegion("Margaret Cook 320");
StringRegion output = topRankedProg.Run(testInput); // expect "Cook"
Console.WriteLine("\"{0}\" => \"{1}\"", testInput, output);
```

> **IMPORTANT:** If you have multiple references and the referencing `StringRegion`s are (preceding or succeeding) siblings, do *NOT* use this method.
> Use `Run(IEnumerable<StringRegion> references)` instead.
>
>It is because `references.SelectMany(r => new ExtractionExample<StringRegion>(r, program.Run(r))` is *NOT* equivalent to `program.Run(references)`.
>
>In these sibling referencing scenarios, **Extraction.Text** seeks for the output in the `StringRegion` formed by two contiguous siblings. If only one referencing sibling is provided, Extraction.Text will search for the output until it reaches end of file (in case of preceding siblings) or begin of file (in case of succeeding siblings).
>
>For example, consider the task of extracting `{"11", "33"}` from `"AA-11\nBB-bb\nCC-33"`.
Suppose the referencing (preceding) siblings are `{"AA", "BB", "CC"}`.
>
>If we use only `"BB"` as the reference, **Extraction.Text** will search for the first number in `"BB-bb\nCC-33"`, which is `33`. This is clearly not intended.
>On the other hand, if we pass the list `{"AA", "BB", "CC"}` as references, **Extraction.Text** will not find a number for `"BB"` (the search range is `"BB-bb\n"`).
This is the intended behavior of this program.

Parent referencing scenario is not affected because Extraction.Text searches for the match *within* the parent.

### Learning With Negative Examples
We specify negative examples to rule out programs whose outputs overlap with at least one of the negative examples.

The below example illustrates the usage of negative examples.

```csharp
var session = new RegionSession();
StringRegion input = RegionSession.CreateStringRegion("Carrie Dodson 100\nLeonard Robledo NA\nMargaret Cook 320");
StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

// Suppose we want to extract "100", "320".
session.Constraints.Add(
    new RegionExample(records[0], records[0].Slice(14, 17)), // "Carrie Dodson 100" => "100"
    new RegionNegativeExample(records[1], records[1]) // no extraction in "Leonard Robledo NA"
);

// Extraction.Text will find a program whose output does not OVERLAP with any of the negative examples.
RegionProgram topRankedProg = session.Learn();

foreach (StringRegion record in records)
{
    string output = topRankedProg.Run(record)?.Value ?? "null";
    Console.WriteLine("\"{0}\" => \"{1}\"", record, output);
}
```

### Learning With Additional References

We may provide additional references (which may come from the same file or different files) to the learning session.
**Extraction.Text** observes the behavior of the learnt programs on these additional references, and uses this observation to better rank programs.

> **TIPS**: It is always a good idea to provide additional references because it provides more information to to identify the desired program. Learning time may increase, but it is often marginal.

```csharp
var session = new RegionSession();
StringRegion input = RegionSession.CreateStringRegion("Carrie Dodson 100\nLeonard Robledo 75\nMargaret Cook ***");
StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

// Suppose we want to extract "100", "75", and "***".
session.Constraints.Add(new RegionExample(records[0], records[0].Slice(14, 17))); // "Carrie Dodson 100" => "100"

// Additional references help Extraction.Text observe the behavior of the learnt programs on unseen data.
// In this example, if we do not use additional references, Extraction.Text may learn a program that extracts the first number.
// On the contrary, if other references are present, it knows that this program is not applicable on the third record "Margaret Cook ***",
// and promotes a more applicable program.
session.Inputs.Add(records.Skip(1));

RegionProgram topRankedProg = session.Learn();

foreach (StringRegion record in records)
{
    string output = topRankedProg.Run(record)?.Value ?? "null";
    Console.WriteLine("\"{0}\" => \"{1}\"", record, output);
}
```

### Learning With Regular Expressions

Users also have an option to provide 3 regular expressions for the extracted region: the lookbehind regex, the matching regex, and the lookahead regex.
All 3 regular expressions are optional.

```csharp
StringRegion input = RegionSession.CreateStringRegion("Carrie Dodson 100\nLeonard Robledo NA\nMargaret Cook 320");
StringRegion[] records = { input.Slice(0, 17), input.Slice(18, 36), input.Slice(37, 54) };

// Suppose we want to extract the number out of a record
var examples = new[] {
    new RegionExample(records[0], records[0].Slice(14, 17)), // "Carrie Dodson 100" => "100"
};

Regex lookBehindRegex = new Regex("\\s");
Regex lookAheadRegex = null;
Regex matchingRegex = new Regex("\\d+");

IEnumerable<RegionProgram> topRankedPrograms = RegionLearner.Instance.LearnTopK(examples, null, 1, lookBehindRegex, matchingRegex, lookAheadRegex);

RegionProgram topRankedProg = topRankedPrograms.FirstOrDefault();

foreach (StringRegion record in records)
{
    string output = topRankedProg.Run(record)?.Value ?? "null";
    Console.WriteLine("\"{0}\" => \"{1}\"", record, output);
}
```


## Extracting a Sequence of StringRegions

Learning a program to extract a sequence of `StringRegion` is similar to learning a program to extract a `StringRegion`.

> **IMPORTANT:**
>
> - Give at least 2 examples because generalizing a sequence from a single element is hard.
>
> - Give positive examples continuously (i.e., we cannot skip any positive examples).
> Extraction.Text assumes that all the text prior to the last example that are not selected are negative examples.
> For instance, in extracting {"A", "C"} from "A B C", Extraction.Text assumes that " B " is a negative example.

### Positive Example

An `SequenceExample` is a pair of an input `StringRegion` and its corresponding *subsequence* of the indented output sequence.

```csharp
var input = SequenceSession.CreateStringRegion(
    "United States\n Carrie Dodson 100\n Leonard Robledo 75\n Margaret Cook 320\n" +
    "Canada\n Concetta Beck 350\n Nicholas Sayers 90\n Francis Terrill 2430\n" +
    "United Kingdom\n Nettie Pope 50\n Mack Beeson 1070");
// Suppose we want to extract all last names from the input string.
var sequenceExample = new SequenceExample(
                        input,  // input
                        new[] { // subsequence
                            input.Slice(15, 21), // input => "Carrie"
                            input.Slice(34, 41), // input => "Leonard"
                      });
```

Similar to `Example` in region learning, a `SequenceExample` can also be one of the three referencing kinds, in which *referencing-parent* is the most popular (it is also demonstrated above).

### Negative Example

A `SequenceNegativeExample` defines a subregion in which the output of the learned program should not *overlap* with.

For instance, the following negative example dictates that the output of all learned programs on `input` should not overlap with `"United States"`.

``` csharp
var example = new SequenceNegativeExample(input , input.Slice(0, 13) /*United States*/);
```

### Basic Usage

The examples (if any) in each referencing region have to be continuous.
However, we do not need to provide all examples in previous regions before providing examples in the current region.

For instance, this set of examples is wrong `{ "Dodson", "Cook" }` (missing `"Robledo"`), but
this set is valid `{ "Dodson", "Robledo", "Beck" }`. Note that we do not need to include `"Cook"` to include `"Beck"`, because `"Beck"` belongs to a different referencing region.

```csharp
var session = new SequenceSession();
// It is advised to learn a sequence with at least 2 examples because generalizing a sequence from a single element is hard.
// Also, we need to give positive examples continuously (i.e., we cannot skip any example).
var input = SequenceSession.CreateStringRegion(
    "United States\n Carrie Dodson 100\n Leonard Robledo 75\n Margaret Cook 320\n" +
    "Canada\n Concetta Beck 350\n Nicholas Sayers 90\n Francis Terrill 2430\n" +
    "United Kingdom\n Nettie Pope 50\n Mack Beeson 1070");
    
// Suppose we want to extract all last names from the input string.
session.Constraints.Add(
    new SequenceExample(input, new[] {
                            input.Slice(15, 21), // input => "Carrie"
                            input.Slice(34, 41), // input => "Leonard"
                        })
);

SequenceProgram topRankedProg = session.Learn();

foreach (StringRegion r in topRankedProg.Run(input))
{
    string output = r != null ? r.Value : "null";
    Console.WriteLine(output);
}
```

The other APIs for learning a sequence program are similar to their region learning counterparts.



## Learning multiple programs

There are usually a large number of programs consistent with any given set of examples. **Extraction.Text** uses a ranking scheme to return the most likely program for the examples, but in some cases this may not be the desired program.

### Learn Top k Programs

`LearnTopK` returns the top `k` ranked programs. If there are fewer than `k` programs, this method returns all programs. If there are tied programs at `k`, it includes all tied programs.

```csharp
var session = new RegionSession();
StringRegion input = RegionSession.CreateStringRegion("Carrie Dodson 100");

session.Constraints.Add(new RegionExample(input, input.Slice(14, 17))); // "Carrie Dodson 100" => "Dodson"

IEnumerable<RegionProgram> topKPrograms = session.LearnTopK(3);

var i = 0;
StringRegion[] otherInputs = {
    input, RegionSession.CreateStringRegion("Leonard Robledo NA"),
    RegionSession.CreateStringRegion("Margaret Cook 320")
};
foreach (RegionProgram prog in topKPrograms)
{
    Console.WriteLine("Program {0}:", ++i);
    foreach (StringRegion str in otherInputs)
    {
        var r = prog.Run(str);
        Console.WriteLine(r != null ? r.Value : "null");
    }
}
```

### Learn All Programs

`LearnAll` returns a set of all programs consistent with the examples.

```csharp
var session = new RegionSession();
StringRegion input = RegionSession.CreateStringRegion("Carrie Dodson 100");

session.Constraints.Add(new RegionExample(input, input.Slice(14, 17))); // "Carrie Dodson 100" => "Dodson"

ProgramSet allPrograms = session.LearnAll().ProgramSet;
IEnumerable<ProgramNode> topKPrograms = allPrograms.TopK(RegionLearner.Instance.ScoreFeature, 3);

var i = 0;
StringRegion[] otherInputs = {
    input, RegionSession.CreateStringRegion("Leonard Robledo NA"),
    RegionSession.CreateStringRegion("Margaret Cook 320")
};
foreach (ProgramNode prog in topKPrograms)
{
    Console.WriteLine("Program {0}:", ++i);
    var program = new RegionProgram(programNode, ReferenceKind.Parent);
    foreach (StringRegion str in otherInputs)
    {
        StringRegion r = program.Run(str);
        Console.WriteLine(r == null ? "null" : r.Value);
    }
}
```


## Serializing/Deserializing a Program

The  `Serialize()` methods of `RegionProgram` and `SequenceProgram` serialize the learned `RegionProgram` and `SequenceProgram` to a string.
The `Load()` methods of `RegionLoader` and `SequenceLoader` deserialize the program text to a `RegionProgram` and `SequenceProgram`.


```csharp
// program was learnt previously
string progText = program.Serialize();
RegionProgram loadProg = RegionLoader.Instance.Load(progText);
```
