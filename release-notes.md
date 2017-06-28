---
title: Release Notes
---
{% include toc.liquid.md %}

# Release 3.0.0 -- 2017/06/28

*This release fixes the issues using the nuget packages in VS2015 and VS2017 (when using old-style packages.config).*

## Breaking Changes

- De-serializing programs in human readable format is removed.  Human readable was never a reliable serialization format
  (although it is nice for debugging purposes) because it does not contain enough information to guarantee backward compatibility.
  We have removed de-serialization support to encourage the use of the XML serialization format.  This also means that the ANTLR
  runtime dependency has been removed from our core nuget package.
- The paraphrasing library is removed.  Transformation.Text still contains its separate support for the explanations feature.
  In effect this is an implementation detail, but consumers may need to remove any DLL references they had to
  Microsoft.ProgramSynthesis.Paraphrasing.
- Moved Microsoft.ProgramSynthesis.Compiler.dll from the main nuget package to the compiler package.  Previously the compiler
  package just contained the dslc.exe tool for compiling a grammar into source code so that it can be built into a DSL's DLL,
  now it also contains the DLL which is used for runtime parsing of grammar files.  This means that the core package which is all
  you need if you are consuming prebuilt DSLs no longer has a dependency on ANTLR4.Runtime (but the compiler package does have
  that dependency).
- The netstandard1.6 libraries are now built for CoreClr 1.1.
- The SDK now depends on System.Collections.Immutable version 1.3.0 (up from 1.2.0) and NewtonSoft.Json version 8.0.3 (up from 8.0.2).
- All uses of Tuple have been replaced with ValueTuple.
- Extraction.Json:
    - No longer automatically treats arrays as objects.  Instead clients are expected to use constraints to control how arrays
      are flattened.  Default is for arrays to turn into rows, but there is a new constraint which allows flattening them into
      columns.

## Bug fixes / Enhancements

- Detection.DataType is a new DLL added to the package which when given a series of strings can detect the datatypes those 
  string values represent (ie. number, date, etc.).
- Extraction.Json:
    - Fixed case where a single-line JSON was misidentified as NDJSON.
- Transformation.Text:
    - Rounding times.
    - Rounding of scaled numbers.
    - DayOfYear (1-366) format for dates.
    - Performance improvements.
    - Stability fixes.
- Split.Text
    - Field detection, expressiveness and error handling improvements.
    - Performance improvements (especially for large string sizes).
- Grammar files now support an annotation to indicate that a rule is deprecated (but kept in the grammar to allow deserialization
  of programs learned on older versions).

# Release 2.3.0 -- 2017/05/15

*Note: We have had some reports of folks having trouble using the public nuget packages in VS2015 since the 2.0.0 release.
We are aware of the issues and hope to have a fix soon.  In the meantime, the available packages should work in VS2017.*

## Bug fixes / Enhancements

- This release includes significant performance improvements including:
    - A long-standing memory leak was fixed in the core framework that affected learning performance.
    - Runtime performance improvements for generated python programs (on order of 2x faster).
    - Transformation.Text learning performance is improved.      
    - Transformation.Text significant inputs performance is improved.
    - Transformation.Text date/time parsing (both during learning and dat runtime) performance is improved.
- Fixed bugs in Split.Text related to inconsistent number of occurrences of delimiters (e.g. space delimiter in phrases of
  different lengths).
- When a delimiter is specified, Split.Text now ensures that the dominant format selected contains at least one delimiter
  occurrence.
- Extraction.Json programs now contain metadata information indicating if they extract an object, an array or an object
  which has only a single array property.
- Fix for a bug when generating programs for MergeColumns constraints with no separators/constants specified if there was
  a constant string at the end.

# Release 2.2.0 -- 2017/04/24

## Bug fixes / Enhancements

- Transformation.Text now supports scientific notation and more flexible ways of indicating that a number is negative
  when parsing.  (It does not format numbers in these ways--only parses them.)
- Transformation.Text's significant inputs support has been improved in cases where no program can be learned from the
  current set of examples.
- Some API usability improvements have been made to Matching.Text.
- Miscellaneous performance and stability improvements.

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
