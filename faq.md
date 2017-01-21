---
date: 2015-10-28T12:19:15-07:00
title: FAQ
---

## How do I use it?

The SDK is used through .NET APIs.
See the [tutorial]({{ site.baseurl }}/documentation/prose/tutorial) for how to get
started and the documentation links on the sidebar for more details.

Also, the [Playground]({{ site.baseurl }}/playground) provides a web interface for trying
out the text and web extraction features.


## How do I install it?

Install the `Microsoft.ProgramSynthesis` and (optionally) `Microsoft.ProgramSynthesis.Compiler` NuGet packages in Visual Studio.

You can also generate a template DSL project by running
```
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
[the license](https://prose-playground.cloudapp.net/data/SDKLicense.pdf)
for details.


## Where can I find sample code?

Our samples are located in the [PROSE GitHub repository](https://github.com/microsoft/prose).

## How can I contact you if I have any questions or feedback?

If you run into any bugs or issues, please [open an issue](https://github.com/microsoft/prose/issues) in our GitHub repository.
Feel free also to [email us](mailto:prose-contact@microsoft.com).
