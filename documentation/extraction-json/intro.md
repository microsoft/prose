---
date: 2017-02-02
title: "Json Extraction"
---

**Extraction.Json** automatically extracts tabular data from Json files. It supports extracting [Newline Delimited Json](http://ndjson.org/) and truncated Json.

The [Usage]({{ site.baseurl }}/documentation/extraction-json/usage) page and the [Sample Project](https://github.com/Microsoft/prose/tree/master/Extraction.Json) illustrate the API usage.

**Extraction.Json** supports two main modes of extraction: 

1.  No Joining Inner Arrays: arrays are not joined and are kept as a single cell in the output table. Each Json outer object corresponds to one row in the output table.
2.  Joining Inner Arrays: arrays are joined with other fields. Each Json outer object corresponds to multiple rows in the output table.

We use the following Json to illustrate different extraction modes:

```json
[
  {
    "person": {
      "name": {
        "first": "Carrie",
        "last": "Dodson"
      },
      "address": "1 Microsoft Way",
      "phone number": []
    }
  },
  {
    "person": {
      "name": {
        "first": "Leonard",
        "last": "Robledo"
      },
      "phone number": [
        "123-4567-890",
        "456-7890-123",
        "789-0123-456"
      ]
    }
  }
]
```

## No Joining Inner Arrays

In this mode, **Extraction.Json** produces the following output, in which each outer object corresponds to one row:

| person.name.first | person.name.last | person.address  | person.phone number                            | 
|-------------------|------------------|-----------------|------------------------------------------------| 
| Carrie            | Dodson           | 1 Microsoft Way |                                                | 
| Leonard           | Robledo          |                 | ["123-4567-890","456-7890-123","789-0123-456"] | 


## Joining Inner Arrays

We can view each inner array as an external table and are joined with the main table using a surrogate key.
In this mode, there are two join semantics: inner join and outer join. These semantics are similar to those in database terms.

### Inner Join

Under inner join semantics, the outer object having an empty array does not appear in the output table (because inner joining with an empty table results in another empty table). 

**Extraction.Json** produces the following table for the above Json:

| person.name.first | person.name.last | person.address  | person.phone number | 
|-------------------|------------------|-----------------|---------------------| 
| Leonard           | Robledo          |                 | 123-4567-890        | 
| Leonard           | Robledo          |                 | 456-7890-123        | 
| Leonard           | Robledo          |                 | 789-0123-456        | 

Note that the values of `person.name.first` and `person.name.last` are duplicated (as a result of the join), and the row of "Carrie Dodson" does not exist in the output table (because its `person.phone number` is empty.)


### Outer Join

Under outer join semantics, the outer object having an empty array still appears in the output table. 
This is the default semantics.

**Extraction.Json** produces the following table for the above Json:

| person.name.first | person.name.last | person.address  | person.phone number | 
|-------------------|------------------|-----------------|---------------------| 
| Carrie            | Dodson           | 1 Microsoft Way |                     | 
| Leonard           | Robledo          |                 | 123-4567-890        | 
| Leonard           | Robledo          |                 | 456-7890-123        | 
| Leonard           | Robledo          |                 | 789-0123-456        | 
