---
date: 2017-02-03T20:00:16-07:00
title: Text Splitting - Usage
---
{% include toc.liquid.md %}

The Split.Text APIs are accessed through the `SplitSession` class. The user can create a new `SplitSession` object, add input data and various constraints to the session, and then call the `Learn()` method to obtain a `SplitProgram`. This is the program that is learnt from the given input data and constraints. The `SplitProgram`'s key method is the `Run()` method which executes the program to perform a split on any given text input. 

To use Split.Text, one needs to reference `Microsoft.ProgramSynthesis.Split.Text.dll`, `Microsoft.ProgramSynthesis.Split.Text.Semantics.dll`
and `Microsoft.ProgramSynthesis.Split.Text.Learning.dll`, `Microsoft.ProgramSynthesis.Extraction.Text.Semantics.dll` and `Microsoft.ProgramSynthesis.Extraction.Text.Learning.dll`.

The complete code for the scenarios described in this walk-through is available in the [Sample Project](https://github.com/Microsoft/prose/tree/master/Split.Text) which illustrates our API usage. 

## Initializing the session

The user can create a new Split session and add the input data as follows:

```csharp
// create a new ProseSplit session
var splitSession = new SplitSession();

// add the input rows to the session
// each input is a StringRegion object containing the text to be split
var inputs = new List<StringRegion> {
       SplitSession.CreateStringRegion("PE5 Leonard Robledo (Australia)"),
       SplitSession.CreateStringRegion("U109 Adam Jay Lucas (New Zealand)"),
       SplitSession.CreateStringRegion("R342 Carrie Dodson (United States)")
};
splitSession.Inputs.Add(inputs);
```

Each row of text in the input data is added as a `StringRegion` object created from the text content in that row. If we want we can also add some constraints to the session to specify basic properties of the desired splitting, such as whether we want to include the delimiters in the resulting split or not. If we do not want delimiters in the output, we can specify with a constraint as follows:

```csharp
splitSession.Constraints.Add(new IncludeDelimitersInOutput(false));
```

We can clear any constraints provided in the session at any time by calling the `splitSession.RemoveAllConstraints()` method. 

## Learning a new split program

**Split.Text** can learn a program using only the provided input data in a purely predictive fashion, without any examples or other output constraints. This can be done by simply calling the `Learn()` function after adding the inputs.

```csharp
// call the learn function to learn a splitting program from the given input examples
SplitProgram program = splitSession.Learn();

// check if the program is null (no program could be learnt from the given inputs)
if (program == null)
{
    Console.WriteLine("No program learned.");
    return;
}
``` 


## Serializing/Deserializing a program

The `SplitProgram.Serialize()` method serializes the learned program to a string. The `SplitProgramLoader.Instance.Load()` method deserializes the program text to a program.


```csharp
// serialize the learnt program and then deserialize
string progText = program.Serialize();
program = SplitProgramLoader.Instance.Load(progText);
```

## Executing the learnt program

The learnt split program can be executed on any input `StringRegion` to produce an array of `SplitCell`s. For example, we can execute the learnt program on each of the inputs as follows:

```csharp
SplitCell[][] splitResult = 
inputs.Select(input => program.Run(input)).ToArray();
```
Each `SplitCell` object represents information about a single split cell. It's `CellValue` field is the sub-region of the input that this split cell represents, and the `IsDelimiter` flag indicates whether this split cell is a field or delimiter value. The learnt program can be executed indepedently of the `Session` object on any new input text, and not just the inputs that have been entered into the session.

Executing the predictively learnt program on the three inputs given above, and having specified delimiters to not be included in the output, we get the following splitting:

|       PE5     |       Leonard Robledo |       Australia       |
|       U109    |       Adam Jay Lucas  |       New Zealand   |
|       R342    |       Carrie Dodson   |       United States   |



## Providing examples constraints

If the user desires a different split, then they can provide *examples constraints* to specify what kind of split they would like. For instance, if the user wants to separate the first name into a different split cell, then they can provide examples on some of the input rows as follows:

```csharp
splitSession.Constraints.Add(new NthExampleConstraint(inputs[0].Value, 0, "PE5"));
splitSession.Constraints.Add(new NthExampleConstraint(inputs[0].Value, 1, "Leonard"));
splitSession.Constraints.Add(new NthExampleConstraint(inputs[0].Value, 2, "Robledo"));
splitSession.Constraints.Add(new NthExampleConstraint(inputs[0].Value, 3, "Australia"));
splitSession.Constraints.Add(new NthExampleConstraint(inputs[1].Value, 0, "U109"));
splitSession.Constraints.Add(new NthExampleConstraint(inputs[1].Value, 1, "Adam"));
splitSession.Constraints.Add(new NthExampleConstraint(inputs[1].Value, 2, "Jay Lucas"));
splitSession.Constraints.Add(new NthExampleConstraint(inputs[1].Value, 3, "New Zealand"));
```

Each `NthExampleConstraint` takes three parameters: the input text on which the program will execute (the entire string), the index of the output split cell for which this example is being given, and the text value desired in that split cell. The examples constraints given above describe each of the four split cells that are desired for the first two inputs that have been given in this session. After calling `Learn()` with these constraints, we obtain a program that produces the following output splitting on the three inputs given in this session:

|      PE5     |       Leonard |       Robledo |       Australia       |
|      U109    |       Adam    |       Jay Lucas       |       New Zealand   |
|       R342    |       Carrie  |       Dodson  |       United States   |




