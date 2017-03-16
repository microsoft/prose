---
title: Release Notes
---
{% include toc.liquid.md %}

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
