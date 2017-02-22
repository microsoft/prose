---
date: 2015-10-28T12:19:15-07:00
title: FAQ
---
{% include toc.liquid.md fullwidth=true %}

# General questions

## How do I use it?

The SDK is used through .NET APIs.
See the [tutorial]({{ site.baseurl }}/documentation/prose/tutorial) for how to get
started and the documentation links on the sidebar for more details.

Also, the [Playground]({{ site.baseurl }}/playground) provides a web interface for trying
out the text and web extraction features.


## How do I install it?

Install the `Microsoft.ProgramSynthesis` and (optionally) `Microsoft.ProgramSynthesis.Compiler` NuGet packages in Visual Studio.

You can also generate a template DSL project by running

``` terminal
npm install -g yo
npm install -g generator-prose
yo prose
```

## Is it cross-platform?
Yes, PROSE SDK is supported on Windows, Linux, and macOS.
Currently, the package is guaranteed to work on .NET 4.5+ and Mono.
Support for .NET Core is forthcoming as soon as its MSBuild packaging stabilizes.


## Where can I use it?

The SDK is released under a _non-commercial license_ for use in
research, education, and non-commercial applications. See
[the license]({{ site.baseurl }}/SDKLicense.pdf)
for details.


## Where can I find sample code?

Our samples are located in the [PROSE GitHub repository](https://github.com/microsoft/prose).

## How can I contact you if I have any questions or feedback?

If you run into any bugs or issues, please [open an issue](https://github.com/microsoft/prose/issues) in our GitHub repository.
Feel free also to [email us](mailto:prose-contact@microsoft.com).

# Visual Studio Code on Linux

## How do I restore NuGet packages for a PROSE solution (a sample or a `yo`-generated template)?

``` terminal
sudo apt install mono-complete nuget
sudo nuget update -self
nuget restore YourSolution.sln
```

## How do I build a solution in VS Code?

Press <kbd>Ctrl+Shift+P</kbd> and [configure a task runner](https://code.visualstudio.com/docs/editor/tasks). Pick the `msbuild` task. In the generated `tasks.json`, replace `msbuild` with `xbuild`.

## When I try to run xbuild on my yo-generated solution, it fails with an error about `@(AssemblyPaths -> Replace('/', '/'))`.

As it turns out, `xbuild` does not support the entirety of the `msbuild` language. This means that you won't be able to recompile your grammar automatically on each build out of the box. We are working on fixing this. In the meantime, please regenerate your solution with `yo prose` but **answer "No" to the last question**.

## How do I launch a program or debug it in VS Code?

Install the [mono-debug](http://marketplace.visualstudio.com/items?itemName=ms-vscode.mono-debug) extension for VS Code and follow the instructions on its webpage.
