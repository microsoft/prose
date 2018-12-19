---
date: 2015-09-02T20:00:06-07:00
title: "Text Transformation"
---

**`Transformation.Text`** is a system that performs string transformations using examples
allowing for many tasks involving strings to be performed automatically.
`Transformation.Text` is based on the same research as the
[FlashFill feature in Excel](https://research.microsoft.com/en-us/um/people/sumitg/flashfill.html),
but with extended capabilities for semantic transformations involving dates
and numbers as well as support for interactivity due to being part of PROSE.
The [Usage]({{ site.baseurl }}/documentation/transformation-text/usage) page and the
[`Transformation.Text` sample project](https://github.com/Microsoft/prose/tree/master/Transformation.Text) show examples of how to use the
Transformation.Text API.


Example Transformation
----------------------

Given an example like

|Input1 | Input2     | Example output |
|:------|:-----------|:---------------|
| Greta | Hermansson | Hermansson, G. |

Transformation.Text will generate a program to perform the same
transformation given any other first name, last name pair:

|Input1  | Input2   | Program output |
|:-------|:---------|:---------------|
| Kettil | Hansson  | Hansson, K.    |
| Etelka | Bala     | Bala, E.       |
| ...    | ...      | ...            |
