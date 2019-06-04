---
date: 2018-05-23
title: "Matching Text - Usage"
---

The `Matching.Text` API is accessed through the `Matching.Text.Session` class. The input strings are added using
`Session.Constraints.Add()`. Once the inputs are added, calling `Session.LearnPatterns()` returns a list of
`PatternInfo` objects that describe each pattern.

Each `PatternInfo` object either has:
1. The `IsNull` field set to true that indicates that the pattern matches only `null` strings, or
2. The `IsNull` field set to false, and the strings that match the pattern are those that match the regular expression
   in the `Regex` field and do not match the regular expressions in the `RegexesToExclude` field.

The other fields indicate the frequency of the pattern (`MatchingFraction`), a description in a PROSE specific format
(`Description`), and a few examples of the input strings matched by the pattern (`Examples`).

## Basic usage

```csharp
using Microsoft.ProgramSynthesis.Matching.Text;

Session session = new Session();
IEnumerable<string> inputs = new[] {
    "21-Feb-73",
    "2 January 1920a ",
    "4 July 1767 ",
    "1892",
    "11 August 1897 ",
    "11 November 1889 ",
    "9-Jul-01",
    "17-Sep-08",
    "10-May-35",
    "7-Jun-52",
    "24 July 1802 ",
    "25 April 1873 ",
    "24 August 1850 ",
    "Unknown ",
    "1058",
    "8 August 1876 ",
    "26 July 1165 ",
    "28 December 1843 ",
    "22-Jul-46",
    "17 January 1871 ",
    "17-Apr-38",
    "28 February 1812 ",
    "1903",
    "1915",
    "1854",
    "9 May 1828 ",
    "28-Jul-32",
    "25-Feb-16",
    "19-Feb-40",
    "10-Oct-50",
    "5 November 1880 ",
    "1928",
    "13-Feb-03",
    "8-Oct-43",
    "1445",
    "8 July 1859 ",
    "25-Apr-27",
    "25 November 1562 ",
    "2-Apr-10",
};
session.Inputs.Add(inputs);
IReadOnlyList<PatternInfo> patterns = session.LearnPatterns();

// Five patterns are returned corresponding to the formats "dd-MMM-yy", "dd MMMM yyyy ", "yyyy", "Unknown", and "2 January 1920a ".
```

