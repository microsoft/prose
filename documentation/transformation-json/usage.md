---
date: 2017-02-02
title: Json Transformation - Usage
---
{% include toc.liquid.md %}

The main entry point is `Session` class's `Learn()` method, which returns a `Program` object.
The `Program`'s key method is `Run()` that executes the program on an input Json to obtain the transformed output Json. 

Other important methods are `Serialize()` and `Deserialize()` to serialize and deserialize `Program` object.

The [Sample Project](https://github.com/Microsoft/prose/tree/master/Transformation.Json) illustrates our API usage.

## Basic Usage

The following snippet illustrates a learning session to generate a Json transformation program from the example:

```csharp
// The training input file, which is a small prefix of the input file.
JToken trainInput = JToken.Parse(@"
{
  ""datatype"": ""local"",
  ""data"": [
    {
      ""Name"": ""John"",
      ""status"": ""To Be Processed"",
      ""LastUpdatedDate"": ""2013-05-31 08:40:55.0""
    },
    {
      ""Name"": ""Paul"",
      ""status"": ""To Be Processed"",
      ""LastUpdatedDate"": ""2013-06-02 16:03:00.0""
    }
  ]
}");

// The training output file, which is the desired output for the training input.
JToken trainOutput = JToken.Parse(@"
[
    {
      ""John"" : ""To Be Processed""
    },
    {a
      ""Paul"" : ""To Be Processed""
    }
]");

var session = new Session();
session.AddConstraints(new Example<JToken,JToken>(trainInput, trainOutput));
Program program = session.Learn();
```

## Serializing/Deserializing a Program

The `Transformation.Json.Program.Serialize()` method serializes the learned program to a string.
The `Transformation.Json.Loader.Instance.Load()` method deserializes the program text to a program.


```csharp
// program was learned previously
string progText = program.Serialize();
Program loadProg = Loader.Instance.Load(progText);
```

## Executing a Program

Use the `Run()` method to obtain the transformed Json output:

```csharp
JToken input = JToken.Parse(@"
{
  ""datatype"": ""local"",
  ""data"": [
    {
      ""Name"": ""John"",
      ""status"": ""To Be Processed"",
      ""LastUpdatedDate"": ""2013-05-31 08:40:55.0""
    },
    {
      ""Name"": ""Paul"",
      ""status"": ""To Be Processed"",
      ""LastUpdatedDate"": ""2013-06-02 16:03:00.0""
    },
    {
      ""Name"": ""Alice"",
      ""status"": ""Finished"",
      ""LastUpdatedDate"": ""2013-07-02 12:04:00.0""
    }
  ]
}");

JToken output = program.Run(input);
```

Output is:

``` json
[
  {
    "John" : "To Be Processed"
  },
  {
    "Paul" : "To Be Processed"
  },
  {
    "Alice" : "Finished"
  }
]
```
