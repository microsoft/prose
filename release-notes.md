---
title: Release Notes
---
{% include toc.liquid.md %}

# Release 2.1.0 -- 2017/04/17

## New Features

- Split.Text now supports fixed width files.
- Transformation.Json now includes Transformation.Text support.  This means it can synthesize programs which not only
  change the structure of a json document but also transform the values.

## Bug fixes / Enhancements

- A number of bug fixes/enhancements were made to Extraction.Json--especially for the scenario of handling
  new-line delimited json (ndjson) documents or the similar case of having an input column containing a json
  document in each row which you want to extract into multiple output columns.  These changes include:
    - Supporting empty rows in ndjson documents.
    - Handling json files with byte order marks (BOMs) at the beginning.
    - Allowing different sets of properties and/or the properties appearing in different order when extracting
      from a series of json documents.
    - Supporting a `NamePrefix` constraint so that output column names can all be given a common prefix.
- Tranformation.Text now accepts strongly typed number and date inputs in addition to strings improving both
  performance and correctness for those cases.
- Miscellaneous Transformation.Text date handling improvements (both performance and correctness)
- Extraction.Json now supports JSON arrays that have only values -- eg. `[ "1", "2" ]`
- Compound.Split API simplification: The InputStream constraint is no longer used to specify that the input comes
  from a stream.  Instead, if a stream is passed to the session, it assumes that behavior automatically.
- Split.File now supports new lines in quotes.

# Release 2.0.1 -- 2017/03/17

Minor bug fix release.

# Release 2.0.0 -- 2017/03/16

We have bumped the major number this time around because this release includes a breaking change (and we are doing our
best to conform to semantic versioning).  The change is to the way `dslc` compiles grammar files.  Previously it produced
a .Net assembly.  Now it outputs c# source code which can be compiled into your main DSL assembly.  This not only
reduces the number of assemblies required to deploy your solution but also allows us to generate helper methods for 
advanced scenarios involving manually creating program trees for a DSL.

## New Features

- Grammar files may now have a `using grammar <grammarName> = <class>.<property>` statement to indicate that the grammar
  depends on another grammar.
- Extraction.Json now supports learning programs from multiple input json documents.  This makes it possible to have a 
  scenario like Split.Text where a column of data contains a different json document on each row, and you want to flatten
  all of those json documents into a set of new columns extracted from the json.
- Telemetry support.  Users of our DSLs may now pass an implementation of our `ILogger` interface to the `Session` object
  for the DSL.  If this is done, PROSE will call the `ILogger` with telemetry information such as the time required to
  learn programs, etc.

## Bug fixes / Enhancements

- Significant inputs are now computed using the Z3 theorem prover from MSR (included in our nuget package as a set of 
  native libraries for Windows, Mac and Linux) which significantly decreases the time required to determine them.
- Transformation.Text significant inputs now include ambiguous dates.
- Extraction.Json now supports extra characters at the end of lines when extracting from new-line delimited json docs.
- Split.Text now supports contexttual field-based delimiters which improve learning performance and address multiple bugs.
- Translation capabilities have now been merged directly into their main DSL assemblies as part of our long-term effort
  to reduce the number of assemblies per DSL.
- Matching.Text now uses an improved sampling algorithm for clustering.
- Several other ranking and stability improvements.


# Release 1.1.0 -- 2017/02/15

With this release we are starting a regular monthly public release rhythm.  The biggest change in this month's release is 
major improvements to our samples and documentation.  At the same time we have added several small new features and bug
fixes.

## New Features

- Made more spec methods `protected internal` to enable third-party implementation of [`Spec`](https://prose-docs.azurewebsites.net/html/T_Microsoft_ProgramSynthesis_Specifications_Spec.htm) subclasses.
- Added new documentation and samples to facilitate third-party DSL development. Fixes [\#10](https://github.com/Microsoft/prose/issues/10).
- More flexibility in time ranges in `Transformation.Text`.
- Allowed omitting leading zeros at the start of numeric date formats in `Transformation.Text`.

## Bug Fixes

- Improved `RunMerge`'s handling of empty lines in `Split.File`.
- Fixed learning with missing lookahead/lookbehind regexes in `Extraction.Text` ([\#7](https://github.com/Microsoft/prose/issues/7)).
- Improved `Extraction.Json`'s handling of documents where the top-level is an array or a single object containing an array.
- Several bug fixes that improve stability, performance and reduce the number of examples required to learn the intended program across the SDK.


# Release 1.0.3 -- 2017/01/22

First public release. Added support for .NET Core as well as several new DSLs.

# Release 0.1.1-preview -- 2015/10/29

First public preview.
