---
date: 2017-02-02
title: Json Extraction - Usage
---
{% include toc.liquid.md %}

The main entry point is `Session` class's `Learn()` method, which returns a `Program` object.
The `Program`'s key method is `Run()` that executes the program on an input Json to obtain the extracted output. 
Each program also has a `Schema` property that defines the structure of the extracted data.

Other important methods are `Serialize()` and `Deserialize()` to serialize and deserialize `Program` object.

To use
Extraction.Json, one needs to reference `Microsoft.ProgramSynthesis.Extraction.Json.dll`, `Microsoft.ProgramSynthesis.Extraction.Json.Learner.dll`
and `Microsoft.ProgramSynthesis.Extraction.Json.Semantics.dll`.

The [Sample Project](https://github.com/Microsoft/prose/tree/master/Extraction.Json) illustrates our API usage.

## Basic Usage

By default, **Extraction.Json** learns a *join* program in which inner arrays are joined with other fields. 
As a result, an outer object in the input Json can be flattened into several rows in the output table.

The below snippet illustrates a learning session to generate such program from the input `jsonText`:

```csharp
string jsonText = ...

var session = new Session();
session.Constraints.Add(new FlattenDocument(jsonText));
Program program = session.Learn();
```

Clients may add `NoJoinInnerArrays` constraint to the session to learn `non-join` programs, as illustrated in the following snippet:

```csharp
var noJoinSession = new Session();
noJoinSession.Constraints.Add(new FlattenDocument(jsonText), new NoJoinInnerArrays());
Program noJoinProgram = noJoinSession.Learn();
```

The [Introduction page]({{ site.baseurl }}/documentation/extraction-json/intro) has more discussion on this topic.


## Serializing/Deserializing a Program

The `Extraction.Json.Program.Serialize()` method serializes the learned program to a string.
The `Extraction.Json.Loader.Instance.Load()` method deserializes the program text to a program.


```csharp
// program was learned previously
string progText = program.Serialize();
Program loadProg = Loader.Instance.Load(progText);
```

## Executing a Program

Given an input Json, a program can generate a hierarchical tree or a flattened table.
If the program is a join program, the table is flattened either using *outer join* (default) or *inner join* semantics.

### Generating a Tree

Use this method to obtain a hierarchical tree of the input document.

```csharp
// program was learned previously
ITreeOutput<JsonRegion> tree = program.Run(jsonText);
```

### Generating a Table

Supply the desired join semantics to the `RunTable()` method as follows:

```csharp
// program was learned previously
IEnumerable<TableRow<JsonRegion>> outerJoinTable = program.RunTable(jsonText, TreeToTableSemantics.OuterJoin);

IEnumerable<TableRow<JsonRegion>> innerJoinTable = program.RunTable(jsonText, TreeToTableSemantics.InnerJoin);

```


