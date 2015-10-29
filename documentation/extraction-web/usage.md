---
date: 2015-09-02T20:00:16-07:00
title: Web Extraction - Usage
toc: true
---


The Extraction.Web learning APIs are accessed through the `Extraction.Web.Learner` class. The two primary methods are `LearnRegion()` and `LearnSequence()` which take a set of examples
and learn a Extraction.Web program consistent with those examples.

The Extraction.Web program is defined in `Extraction.Web.Program` class.
A program is either a region extraction program or a sequence extraction program.
The key method is `Run()` to execute the program on some input region to obtain the extracted output.
Other important methods include serialization (`Serialize()`) and deserialization (`Load()`) of a program.

In order to use
Extraction.Web, you need assembly references to `Microsoft.ProgramSynthesis.Extraction.dll`, `Microsoft.ProgramSynthesis.Extraction.Web.Learner.dll`
and `Microsoft.ProgramSynthesis.Extraction.Web.Semantics.dll`. Again, the sample project `Extraction.Web.Sample` illustrates our API usage and contains the sample HTML documents discussed in this tutorial.




HtmlDoc and WebRegion
===

 Extraction.Web operates on regions of HTML documents. An HTML document is represented by the class ``HtmlDoc``. These can be created from the HTML markup of a document as follows:

``` csharp
string s = File.ReadAllText(@"..\..\SampleDocuments\sample-document-1.html");
HtmlDoc doc = HtmlDoc.Create(s);
```

A region within a document is represented by the ``WebRegion`` class which refers to a specific node in the HTML document, such as a table node, paragraph node, or the root node representing the whole document. Web regions can be created using the ``GetRegion`` function, by specifying a CSS selector that determines a unique node within a given HTML document. For example, the following code generates a region that refers to the second cell in the first row of the table in the document (containing the surname "Briggs").

``` csharp
WebRegion region = doc.GetRegion("tr:nth-child(1) td:nth-child(2)")
```




Learning a program to extract a single region
===================

Extraction.Web learns programs from examples of regions that it is given. A `ExampleSpec` is a pair consisting of the example region and a “reference” region which is some ancestor of the example region, which may be a whole document or a part of the document from which the example is extracted. Extraction.Web learns programs for the example region using the reference region as the anchor point. The reference region can be any ancestor node of the example region in the HTML document. For instance, in our running example, to extract a surname from a given table row, we may specify an example with the table row as a reference region:

``` csharp
WebRegion referenceRegion = doc.GetRegion("tr:nth-child(1)");  //1st table row
WebRegion exampleRegion = doc.GetRegion("tr:nth-child(1) td:nth-child(2)");  //2nd cell in 1st table row
ExampleSpec<WebRegion> exampleSpec = new ExampleSpec<WebRegion>(referenceRegion, exampleRegion);
```

The reference region represents some knowledge that we have on splitting the file, which we leverage to learn a new field. In the example above, we may perform an extraction with respect to the table row as we may already know how to extract table rows from the document. If we do not have any such information (for instance, when we learn the first field), then we can use the entire document region as the reference.



We next illustrate how we learn the first surname in a document from  a single positive example.

``` csharp
WebRegion referenceRegion = new WebRegion(doc);
WebRegion exampleRegion = doc.GetRegion("tr:nth-child(1) td:nth-child(2)");  //2nd cell in 1st table row
ExampleSpec<WebRegion> exampleSpec = new ExampleSpec<WebRegion>(referenceRegion, exampleRegion);
Extraction.Web.Program prog = Learner.Instance.LearnRegion(new[] { exampleSpec }, Enumerable.Empty<ExampleSpec<WebRegion>>());
```

**Multiple Positive Examples:** The API takes multiple positive examples because we allow users to give examples that come from multiple documents, or multiple regions in a document. For example, here is how we learn the first surname in a document using two examples in two different documents.

``` csharp
WebRegion referenceRegion1 = new WebRegion(doc1);
WebRegion referenceRegion2 = new WebRegion(doc2);
WebRegion exampleRegion1 = doc1.GetRegion("tr:nth-child(1) td:nth-child(2)");  //2nd cell in 1st table row of doc1
WebRegion exampleRegion2 = doc2.GetRegion("tr:nth-child(1) td:nth-child(2)");  //2nd cell in 1st table row of doc2
ExampleSpec<WebRegion> exampleSpec1 = new ExampleSpec<WebRegion>(referenceRegion1, exampleRegion1);
ExampleSpec<WebRegion> exampleSpec2 = new ExampleSpec<WebRegion>(referenceRegion2, exampleRegion2);

Extraction.Web.Program prog = Learner.Instance.LearnRegion(new[] { exampleSpec1, exampleSpec2 }, Enumerable.Empty<ExampleSpec<WebRegion>>());
```

If we know a way to separate the document into multiple regions, we may learn a program for each of the separated regions. For instance, suppose we can split the document into table rows. We can learn the surname with respect to the table row, so that given any table row, our program will return the surname contained in the row.

``` csharp
WebRegion referenceRegion1 = doc.GetRegion("tr:nth-child(1)");  //1st table row
WebRegion exampleRegion1 = doc.GetRegion("tr:nth-child(1) td:nth-child(2)");  //2nd cell in 1st table row
WebRegion referenceRegion2 = doc.GetRegion("tr:nth-child(2)");  //2nd table row
WebRegion exampleRegion2 = doc.GetRegion("tr:nth-child(2) td:nth-child(2)");  //2nd cell in 2nd table row
ExampleSpec<WebRegion> exampleSpec1 = new ExampleSpec<WebRegion>(referenceRegion1, exampleRegion1);
ExampleSpec<WebRegion> exampleSpec2 = new ExampleSpec<WebRegion>(referenceRegion2, exampleRegion2);

Extraction.Web.Program prog = Learner.Instance.LearnRegion(new[] { exampleSpec1, exampleSpec2 }, Enumerable.Empty<ExampleSpec<WebRegion>>());
```




Learning a program to extract a sequence of regions
===================


We can use Extraction.Web to learn programs that extract a sequence of WebRegions within a given WebRegion. For example, here is how we learn the list of all surnames in a document.

``` csharp
WebRegion referenceRegion = new WebRegion(doc);
WebRegion exampleRegion1 = doc.GetRegion("tr:nth-child(1) td:nth-child(2)");  //2nd cell in 1st table row of doc
WebRegion exampleRegion2 = doc.GetRegion("tr:nth-child(2) td:nth-child(2)");  //2nd cell in 2nd table row of doc
ExampleSpec<WebRegion> exampleSpec1 = new ExampleSpec<WebRegion>(referenceRegion, exampleRegion1);
ExampleSpec<WebRegion> exampleSpec2 = new ExampleSpec<WebRegion>(referenceRegion, exampleRegion2);

Extraction.Web.Program prog = Learner.Instance.LearnSequence(new[] { exampleSpec1, exampleSpec2 }, Enumerable.Empty<ExampleSpec<WebRegion>>());
```

Similar to the single WebRegion extraction API, it is possible to give examples from different documents or different regions within documents. We just need to specify the reference field accordingly. The negative examples also work similarly to the single WebRegion case.

Serializing and deserializing programs
===================


We can serialize our program to an XML file using the `Serialize()` method, and the `Load()` method to create a program from serialized XML.

``` csharp
Extraction.Web.Program program = …; // learn extraction program
string progText = prog.Serialize();   //serialize the program to XML
Extraction.Web.Program loadProg = Extraction.Web.Program.Load(progText); //deserialize
```

Executing programs
===================


Once we learn an extraction program, we can execute it on any new inputs to perform extractions using the `Run()` method:

``` csharp
Extraction.Web.Program program = …; // learn an extraction program
WebRegion region = …; //define the input region to perform the extraction on
IEnumerable<WebRegion> executionResult = program.Run(region);  //execute the program on the input region
```


