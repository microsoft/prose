---
title: Release Notes
---
{% include toc.liquid.md %}

# Release 1.1.0 - 2017/2/15

With this release we are starting a regular monthly public release rhythm.  The biggest change in this month's release is 
major improvements to our samples and documentation.  At the same time we have added several small new features and bug
fixes.

## New Features

- Made more spec methods protected internal and added documentation to facilitate third party DSL development.
- More flexibility in time ranges in `Transformation.Text`.
- Allow omitting leading zeros at the start of numeric date formats in `Transformation.Text`.

## Bug Fixes

- Improve `RunMerge`'s handling of empty lines in `Split.File`.
- Improvement to `Extraction.Json`'s handling of documents where the top-level is an array or a single object containing an array.
- Several bug fixes that improve stability, performance and reduce the number of examples required to learn the intended program
  across the SDK.


# Release 1.0.3 - 2017/01/22

First public release.  Added support for CoreClr as well as several new DSLs.

# Release 0.1.1-preview - 2015/10/29

First public preview.