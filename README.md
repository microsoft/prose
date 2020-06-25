# Microsoft Program Synthesis using Examples SDK

The Program Synthesis using Examples (PROSE) SDK includes a set of technologies for the automatic generation of
programs from input-output examples. This repo includes samples and sample data for the Microsoft PROSE SDK.

The samples are split into three categories:

* Data wrangling samples in [WranglingSamples.sln](WranglingSamples.sln). This sample shows how to use the PROSE
  Data Wrangling API.  
* Program synthesis samples in [ProseSamples.sln](ProseSamples.sln). This sample shows how to instantiate the
  framework to build a synthesizer for a new DSL. 
* PROSE DSL authoring Tutorial in [DslAuthoringTutorial](DslAuthoringTutorial). This sample demonstrates in a
  step-by-step manner how to instantiate the framework to build a synthesizer for a new DSL. It is mainly used
  during PROSE workshops.  

Find guides for these sample projects here: [https://microsoft.github.io/prose/](https://microsoft.github.io/prose/)

Optionally, you can get started quickly using [Docker](https://www.docker.com/get-started):
```sh
git clone https://github.com/microsoft/prose.git
cd prose
docker build -t prose-samples .
docker run -it --rm -v "$(pwd):/opt/prose-samples" -w "/opt/prose-samples" prose-samples bash
# Inside the Docker container
cd ProgramSynthesis/ProseSample  # ... or the directory for any other sample
dotnet run  # run the sample in the current directory
```

---
This project has adopted the [Microsoft Open Source Code of
Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct
FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com)
with any additional questions or comments.
