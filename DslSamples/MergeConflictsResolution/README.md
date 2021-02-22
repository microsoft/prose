

This project contains the code and data for the paper **Can Program Synthesis be Used to Learn Merge Conflict Resolutions? An Empirical Analysis**, appeared in International Conference on Software Engineering (ICSE'21) . This work has been done by [Rangeet Pan](https://rangeetpan.github.io/), [Vu Le](https://www.microsoft.com/en-us/research/people/levu/), [Nachiappan Nagappan](https://nachinagappan.github.io/), [Sumit Gulwani](https://www.microsoft.com/en-us/research/people/sumitg/), [Shuvendu Lahiri](https://www.microsoft.com/en-us/research/people/shuvendu/), and [Mike Kaufman](https://www.linkedin.com/in/mike-kaufman-439622/).

**Abstract**
 In this paper, we study the problem of textual merge conflicts from the perspective of Microsoft Edge, a large, highly collaborative fork off the main Chromium branch with significant merge conflicts. 
Broadly, this study is divided into two sections. First, we empirically evaluate textual merge conflicts in Microsoft Edge and classify them based on the type of files, location of conflicts in a file, and the size of conflicts. We found that ~28% of the merge conflicts are 1-2 line changes, and many resolutions have frequent patterns.  Second, driven by these findings, we explore Program Synthesis (for the first time) to learn patterns and resolve structural merge conflicts. 
We propose a novel domain-specific language (DSL) that captures many of the repetitive merge conflict resolution patterns and learn resolution strategies as programs in this DSL from example resolutions. We found that the learned strategies can resolve 11.4% of the conflicts (~41% of 1-2 line changes) that arise in the  C++ files with 93.2% accuracy.

## Set Up and Run

1. Install .NET Core 3.1 SDK (https://www.microsoft.com/net/download/)

2. Clone this repository:
    ```
    git clone https://github.com/Microsoft/prose.git
    ```
3. Build the solution
    ```
    cd prose\DslSamples\MergeConflictsResolution
    dotnet build
    ```
4. Run the tool
    ```
    .\MergeConflictsResolutionConsole\bin\Debug\netcoreapp3.1\MergeConflictsResolutionConsole.exe
    ```
	
#### Build and Run Using Visual Studio 
1. Open the project solution `MergeConflictsResolution.sln` in Visual Studio.

1. Build -> Build Solution.

1. Right click MergeConflictsResolutionConsole project (in Solution Explorer) -> Set as Startup Project.

1. Debug -> Start Debugging.

## Understanding the Synthesizer

The synthesizer has the following main components:

- [LanguageGrammar.cs](MergeConflictsResolution/LanguageGrammar.cs): The grammar of our domain specific language (DSL).
- [Semantics.cs](MergeConflictsResolution/Semantics.cs): Defines the semantics of our functions in the DSL.
- [WitnessFunctions.cs](MergeConflictsResolution/WitnessFunctions.cs): The witness functions (aka inverse functions) that are used to learn programs from examples. [Read more.](https://www.microsoft.com/en-us/research/publication/flashmeta-framework-inductive-program-synthesis/)
- [RankingScore.cs](MergeConflictsResolution/RankingScore.cs): The ranking functions.


## Contact Us:

Rangeet Pan (rangeet@iastate.edu)