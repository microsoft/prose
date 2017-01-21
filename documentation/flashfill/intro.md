---
date: 2015-09-02T20:00:06-07:00
title: "Introduction"
---

FlashFill (or, officially, `Transformation.Text`) is a system that performs string transformations using examples
allowing for many tasks involving strings to be performed automatically.
The [Usage]({{ site.baseurl }}/documentation/flashfill/usage) page and the
`Transformation.Text.Sample` project show examples of how to use the FlashFill API.


Example Transformation
----------------------

Given an example like

|Input1 | Input2     | Example output |
|:------|:-----------|:---------------|
| Greta | Hermansson | Hermansson, G. |

FlashFill will generate a program to perform the same transformation given any
other first name, last name pair:

|Input1  | Input2   | Program output |
|:-------|:---------|:---------------|
| Kettil | Hansson  | Hansson, K.    |
| Etelka | Bala     | Bala, E.       |
| ...    | ...      | ...            |
