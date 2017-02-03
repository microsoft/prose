---
date: 2017-02-03
title: "Json Transformation"
---

**Transformation.Json** transforms the structure of Json using input/output examples. 

The [Usage]({{ site.baseurl }}/documentation/transformation-json/usage) page and the [Sample Project](https://github.com/Microsoft/prose/tree/master/Transformation.Json) illustrate the API usage.

## Example Transformation

Given an example to transform this input Json:

``` json
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
}
```

into this output Json:

``` json
[
  {
    ""John"" : ""To Be Processed""
  },
  {
    ""Paul"" : ""To Be Processed""
  }
]
```

**Transformation.Json** will generate a program to perform the same
transformation given any other similar and larger input Json. For example, the learned program transforms this input:

``` json
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
}
```

into this output:

``` json
[
  {
    ""John"" : ""To Be Processed""
  },
  {
    ""Paul"" : ""To Be Processed""
  },
  {
    ""Alice"" : ""Finished""
  }
]
```
