---
title: Interactivity
---

{% include toc.liquid.md %}

Producing a program using a program synthesis system involves a series of interactions between the user and the system.
These interactions take the general form of the user providing information about the task followed by reviewing the
synthesized program to determine what, if any, additional information they need to provide to accomplish their intended
goal. More concretely, a user might provide an example for one input and manually inspect the output of the synthesized
program on other inputs, looking for an input with an incorrect output to give a second example on. By seeking to
capture that entire process instead of just the step where a program is learned from examples, PROSE&apos;s `Session`
API is a useful model for real scenarios.

PROSE&apos;s `Session` provides a stateful API for program synthesis to support interactive workflows. A `Session`
represents a user&apos;s efforts to synthesize a program for a single task. To begin a task, a new `Session` object is
constructed and maintained until the user is satisfied with the final synthesized program (and possibly serialized for
future refinements to that program). In addition to keeping track of the inputs and constraints to be fed to the
synthesizer, the `Session` keeps track of programs which have been learned and provides APIs for helping the user select
new inputs and outputs.

Each DSL exposes a `Session` subclass as the entrypoint to its learning API (e.g. `Transformation.Text.Session`). To
implement a `Session` for your own DSL, extend `Wrangling.NonInteractiveSession` (or the base class `Wrangling.Session`
if you want more control).

```csharp
using Microsoft.ProgramSynthesis.Transformation.Text;
// construct a session
var session = new Session();
```


## Inputs

The collection of all known inputs the program is expected to be run on should be provided using `.Inputs.Add()`. If
that set is large, then providing all of them may not be worthwhile (as the algorithms will only have time to consider a
subset anyway). If selecting a subset of inputs to provide, they should be representative of the inputs the program will
be run on. The inputs provided can accessed using `.Inputs` and removed using `.RemoveInputs()` or `.RemoveAllInputs()`.

The inputs are used when learning and ranking programs (unless `.UseInputsInLearn` is set to `false`), as well as for
[suggesting inputs that more information is likely needed for](#significant-inputs).

```csharp
// provide some inputs
session.Inputs.Add(new InputRow("Greta", "Hermansson"),
                  new InputRow("Kettil", "Hansson"),
                  new InputRow("Etelka", "bala"));
```


## Constraints

Constraints are any information that describe the program to be synthesized. The most common kind of constraint is an
example, but DSLs may support many kinds of constraints including negative examples, types for the output, programs the
synthesized program should similar to, or any constraint the author of the synthesizer wishes to define.

The base type for constraints is `Constraint<TInput, TOutput>` where `TInput` and `TOutput` are the input and output
types of programs in the DSL being synthesized. For example, for [`Transformation.Text`]({{ site.baseurl
}}/documentation/transformation-text/intro/), the type of constraints is `Constraint<IRow, object>`.

In order to provide constraints to a `Session`, use `.Constraints.Add()`. The constraints provided can accessed using
`.Constraints` and removed using `.RemoveConstraints()` or `.RemoveAllConstraints()`.

```csharp
// give an example
session.Constraints.Add(new Example(new InputRow("Greta", "Hermansson"), "Hermansson, G."))
```


## Synthesizing programs

Once a `Session` has some inputs and constraints, a program can be synthesized.
Programs are generated using the various `.Learn*()` methods, which use the
information in `.Inputs` and `.Constraints` to learn a program. They, like
all `Session` methods that do any significant amount of computation,
have `Async` variants which do the computation on a separate thread to make
it easier to attach a `Session` to a GUI.

* `Learn()`/[`LearnAsync()` returns the single top-ranked program as a `Program`.
* `LearnTopK()`/[`LearnTopKAsync()` takes an integer `k` and returns the top-`k` ranked programs as an
  `IReadOnlyList<Program>`.
* `LearnAll()`/[`LearnAllAsync()` learns all programs consistent with the examples, giving the result compactly as a
  `ProgramSet` (wrapped in an `IProgramSetWrapper`).

```csharp
var program = session.Learn();
```


## Explanations

In order to decide if the synthesized program is satisfactory, the user has to comprehend what has been learned. As we
assume that, in general, the user is not a programmer, simply showing the code to the user is a poor way to explain the
what the program is doing. Even experienced programmers can have difficulty reading programs, especially ones in DSLs
designed to be easy for a computer to synthesize programs in as opposed to being easy for a human to read.


### Running the program

The most straightforward way to explain the program is to run it.
To run a `Program`, use its `Run()` method:

```csharp
foreach(var input in session.Inputs)
{
    Console.Out.WriteLine(program.Run(input));
}
```

|Input1  | Input2     | Program output |
|:-------|:-----------|:---------------|
| Greta  | Hermansson | Hermansson, G. |
| Kettil | Hansson    | Hansson, K.    |
| Etelka | bala       | bala, E.       |


### DSL-specific

Other explanations might be DSL-specific. For instance, `Transformation.Text` offers a feature called "output
provenance" which pairs up substrings of the output with the substrings in the input they were selected from:

```csharp
foreach(var input in session.Inputs)
{
    Console.Out.WriteLine(program.ComputeOutputProvenance(input));
}
```

Shown with italic and bold substrings corresponding to each other between
the input and the output:

|Input1      | Input2       | Program output       |
|:-----------|:-------------|:---------------------|
| **G**reta  | *Hermansson* | *Hermansson*, **G**. |
| **K**ettil | *Hansson*    | *Hansson*, **K**.    |
| **E**telka | *bala*       | *bala*, **E**.       |

An interactive variant of this could allow the user to select where in the
input a specific part of the output should come from, although the current
implementation of `Transformation.Text` does not support such a constraint.


## Significant Inputs

While explanations help the user understand how the program works on inputs they are looking at, if the input set is
large, it is likely some problems occur only on inputs the user is not looking at. .GetSignificantInputClustersAsync()
can suggest inputs that the user should take a look at. The default algorithm works for any DSL that supports learning
multiple programs: it looks for inputs where the top program disagrees with other highly ranked programs. The return
value is a set of clusters instead of single inputs because sets of inputs the algorithm cannot distinguish are returned
together, so, for example, the UI could give preference to inputs that are currently on screen.

When presenting a significant input to the user, possible alternative outputs can be suggested using
.ComputeTopKOutputsAsync():

```csharp
foreach(SignificantInput<IRow> sigInput in await session.GetSignificantInputsAsync())
{
    Console.Out.WriteLine("Input[Confidence=" + sigInput.Confidence + "]: " + sigInput.Input);
    foreach(object output in await session.ComputeTopKOutputsAsync(sigInput.Input, 5))
    {
        Console.Out.WriteLine("Possible output: " + output);
    }
}
```

If the significant inputs algorithm returns nothing at all, that indicates an assertion that the user has given
sufficiently specific constraints to define the program to synthesize (modulo the inputs provided), which should give
the user confidence that the synthesized program is correct.


## Conclusion

PROSE&apos;s `Session` API provides a mechanism for supporting an interactive synthesis task. After loading in the data
to work with, the user can switch between providing constraints, generating programs interacting with their
explanations, and requesting pointers to significant inputs. This rich vocabularly allows a user to interact with the
program synthesis in a way such that they can have confidence that the program they generate will generalize as desired.
