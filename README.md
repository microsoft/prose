# Microsoft Program Synthesis using Examples SDK

The Program Synthesis using Examples (PROSE) SDK includes a set of technologies for the automatic generation of programs
from input-output examples. This repo includes samples, release notes, and some other miscellaneous projects related to
the Microsoft PROSE SDK.

The samples are split into two categories:

- Samples for using existing PROSE DSL APIs to accomplish tasks in [api-samples/api-samples.sln](WranglingSamples.sln). 
- Samples for creating program synthesis solutions using the PROSE SDK by authoring a DSL in the
  [dsl-samples](dsl-samples) directory:
  - [DSL authoring tutorial](dsl-samples/tutorial)
  - [ProseSample](dsl-samples/ProseSample/ProseSample.sln)
  - [DSL for merge conflict resolution](dsl-samples/MergeConflictsResolution/MergeConflictsResolution.sln)

You can find guides for some of these sample projects and other information about the PROSE project here:
[https://microsoft.github.io/prose/](https://microsoft.github.io/prose/)

Optionally, you can get started quickly using [Docker](https://www.docker.com/get-started):

```sh
git clone https://github.com/microsoft/prose.git
cd prose
docker build -t prose-samples .
docker run -it --rm -v "$(pwd):/opt/prose-samples" -w "/opt/prose-samples" prose-samples bash
# Inside the Docker container
cd dsl-samples/ProseSample/ProseSample  # ... or the directory for any other sample project
dotnet run  # run the sample in the current directory
```

---
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact
[opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

Test Addition of Content
