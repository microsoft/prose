# Microsoft Program Synthesis using Examples SDK

The Program Synthesis using Examples (PROSE) SDK includes a set of technologies for the automatic generation of programs
from input-output examples. This repo includes samples, release notes, and some other miscellaneous projects related to
the Microsoft PROSE SDK.

The samples are split into two categories:

- Samples for using existing PROSE DSL APIs to accomplish tasks in
  [api-samples/api-samples.sln](api-samples/api-samples.sln). 
- Samples for creating program synthesis solutions using the PROSE SDK by authoring a DSL in the
  [dsl-samples](dsl-samples) directory:
  - [DSL authoring tutorial](dsl-samples/tutorial)
  - [ProseSample](dsl-samples/ProseSample/ProseSample.sln)
  - [DSL for merge conflict resolution](dsl-samples/MergeConflictsResolution/MergeConflictsResolution.sln)

You can find guides for some of these sample projects and other information about the PROSE project here:
[https://microsoft.github.io/prose/](https://microsoft.github.io/prose/)

## Contributing

The source of truth for all the information in this repository is actually an internal Microsoft repository, and any
changes made here are at risk of being overwritten by future public releases from the PROSE team.  If you detect issues
with any of the samples or other things in this repo, please [open an issue](https://github.com/microsoft/prose/issues),
and someone from the PROSE team will work with you to see that the problem is addressed.

---
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact
[opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
