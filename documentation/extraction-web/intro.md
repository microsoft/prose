---
date: 2015-09-02T20:00:06-07:00
title: "Web Extraction"
---


**Extraction.Web** is a system that extracts data from web pages (HTML documents) using *examples*. The [Usage]({{ site.baseurl }}/documentation/extraction-web/usage) page and the `Extraction.Web.Sample` project show examples of how to use the Extraction.Web API.

#### Supported Extractions

Extraction.Web supports two kinds of extractions: (1) extract a single region from a web page, and (2) extract a sequence of regions from a web page.

- **Extract a single region**. For example, given a web page such as the one shown in Figure 1, we may want to only extract the first name in the table on the page. To do this we can give the example of the document node containing “Harriet” in this particular document. The program learnt by Extraction.Web can then be applied to a different document such as the one in Figure 2 in which it will extract the node containing the first name “Brandan”.

- **Extract a sequence of regions**. For example, for the page in Figure 1 we may want to extract all surnames. To do this we can give a few examples, such as the document nodes containing the names { “Briggs”, “Parsons” }. Extraction.Web may then generate a program which can extract all the surnames { “Briggs”, “Parsons”, “Cameron”, “Owens”, “Garner”, “Booth”, “Dobson”, “Perry” } from this document or other similarly formatted documents such as in Figure 2.


<image src="/img/extraction-web/ex1.jpg" style="float: left;">
*Figure 1. Sample document available at Extraction.Web\Sample\SampleDocuments\sample-document-1.html*

<image src="/img/extraction-web/ex2.jpg" style="float: left;">
*Figure 2. Sample document available at Extraction.Web\Sample\SampleDocuments\sample-document-2.html*

