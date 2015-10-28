---
date: 2015-09-02T20:00:16-07:00
title: Usage
toc: true
---


The FlashFill API is accessed through the `FlashFill.FlashFillProgram` class.
The primary method is `Learn()` which takes a set of examples
and learns a FlashFill program consistent with those examples. In order to use
FlashFill, you need assembly references to `FlashFill.dll` and
`FlashFill.Semantics.dll`.


Basic usage
===========

```csharp
IEnumerable<FlashFillExample> examples = new[]
{
	new FlashFillExample("Greta Hermansson", "Hermansson, G.")
};
var program = FlashFillProgram.Learn(examples);
var output = program.Run("Kettil Hansson"); // output is "Hansson, K."
```

The examples are given as an `IEnumerable<FlashFillExample>` with the input and
the correct output. They may also be provided as an
`IDictionary<string, string>` or `IDictionary<IEnumerable<string, string>>`.
Note that the number of strings must be the same for all inputs.


#### One example with multiple strings

```csharp
var examples = new[]
{
	new FlashFillExample(new FlashFillInput("Greta", "Hermansson"), "Hermansson, G.")
};
FlashFillProgram program = FlashFillProgram.Learn(examples);
string output = program.Run("Kettil", "Hansson"); // output is "Hansson, K.
```

#### Multiple examples

FlashFill can be given multiple examples in order to generate a program that
will generalize over differently formatted inputs. In this example, we give
FlashFill a phone number to normalized in two different formats and it is able
to take a phone number in a third similar format and normalize it as well.

```csharp
var examples = new[]
{
	new FlashFillExample("212-555-0183", "212-555-0183"),
	new FlashFillExample("(212) 555 0183", "212-555-0183")
};
FlashFillProgram program = FlashFillProgram.Learn(examples);
string output = program.Run("425 311 1234"); // output is "425-311-1234"
```

If your input data is in multiple formats, you will likely have to provide
more than one example. A common workflow is to have the user give a small
number of examples and then inspect the output and have the option of
providing additional examples if they discover an undesired result.
Note that there is no special API for amending a program given new examples,
just call `Learn()` again with all of the examples.


Inputs without known outputs
============================

Most likely, when learning a program, you will have some idea of other inputs
you intend to run the program on in the future. FlashFill can take those inputs
and use them to help decide which program to return.

```csharp
var examples = new Dictionary<string, string>
{
	{ "02/04/1953", "1953-04-02" }
};
var additionalInputs = new[]
{
	"04/02/1962",
	"27/08/1998"
};
FlashFillProgram program = FlashFillProgram.Learn(examples, additionalInputs);
string output = program.Run("31/01/1983"); // output is "1983-01-31"
```


Learning multiple programs
==========================

There are usually a large number of programs consistent with any given set of
examples. FlashFill has a ranking scheme which it uses to return the most
likely program for the examples it has seen, but in some cases this may not
be the desired program.

`LearnTopK` has a parameter `k` which specifies how many programs
it should try to learn; it returns the top `k` ranked programs (or programs with
the top `k` ranks if there are ties).

```csharp
var examples = new[]
{
	new FlashFillExample("Greta Hermansson", "Hermansson, G.")
};
// Learn top 10 programs instead of just the single top program.
IEnumerable<FlashFillProgram> programs = FlashFillProgram.LearnTopK(examples, k: 10);
foreach (FlashFillProgram program in programs)
{
	Console.WriteLine(program.Run("Kettil hansson")); // note "hansson" is lowercase
}
```

The first several programs output "doe, J.", but after that one outputs
"Doe, J.". This could be used to ask the user which they meant or to do
automated reranking of the top results based on some logic other than
FlashFill's internal ranking system.


Serializing programs
====================

Sometimes you will want to learn a program in one session and run it on other
data in a future session or transfer learned programs between computers.
In order to do so, FlashFill supports serializing programs:

```csharp
IEnumerable<FlashFillExample> examples = new[]
{
	new FlashFillExample("Kettil Hansson", "Hansson, K.")
};
FlashFillProgram program = FlashFillProgram.Learn(examples);
// FlashFillPrograms can be serialized using .ToString().
string serializedProgram = program.ToString();
// Serialized programs can be loaded in another program using the FlashFill API using .Load():
FlashFillProgram parsedProgram = FlashFillProgram.Load(serializedProgram);
// The program can then be run on new inputs:
Console.WriteLine(parsedProgram.Run("Etelka Bala")); // outputs "Bala, E."
```

API
===

See [Documentation](/documentation/api) for the full API documentation.

`FlashFillInput` and `FlashFillExample` types
---------------------------------------------

`FlashFillInput` wraps a single input of one or more strings and
`FlashFillExample` wraps a `FlashFillInput` and a corresponding output `string`.
The FlashFill API methods all have helpers that take `string` based types
in addition to those types.

```csharp
var input1 = new FlashFillInput("one string");
var input2 = new FlashFillInput("two", "strings");
var input3 = new FlashFillInput(new List<string>() { "a", "b" });
string[] strs = input3.InputStrings;

var example1 = new FlashFillExample(input1, "out");
var example2 = new FlashFillExample("in", "out");
var example3 = new FlashFillExample(new List<string>() { "a", "b" }, "out");
FlashFillInput ex3Input = example3.Input;
string ex3Output = example3.Output;
```


Learning FlashFill programs
---------------------------

`FlashFillProgram` has three different methods for learning:

* `Learn()` returns the single top-ranked program as a `FlashFillProgram`.
* `LearnTopK()` takes an integer `k` and returns the top-`k` ranked
	programs as an `IEnumerable<FlashFillProgram>`.
* `LearnAll()` learns all programs consistent with the examples, giving
	the result compactly as a `ProgramSet`.

All three have variants that take the examples as an
`IEnumerable<FlashFillExample>`, `IDictionary<string, string>`,
or `IDictionary<IEnumerable<string>, string>`.

To run a `FlashFillProgram`, use its `Run()` method:

```csharp
public string Run(FlashFillInput input)

public string Run(IEnumerable<string> input)

public string Run(params string[] input)
```