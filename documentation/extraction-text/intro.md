---
date: 2017-02-02
title: "Text Extraction"
---
{% include toc.liquid.md %}

**Extraction.Text** extracts data from semi-structured text files using *examples*.
The [Usage]({{ site.baseurl }}/documentation/extraction-text/usage) page and the [Sample project](https://github.com/Microsoft/prose/tree/master/Extraction.Text) illustrate the API usage.

**Extraction.Text** supports two kinds of extraction: 

1.  Extract.Text.Region extracts a substring from an input string.
2.  Extract.Text.Sequence extracts a sequence of substrings from an input string.

**Read more:** ["FlashExtract: A Framework for Data Extraction by Examples"](http://research.microsoft.com/en-us/um/people/sumitg/pubs/pldi14-flashextract.pdf)


# Substring Extraction

From input/output example(s) in the form of `<input string, substring of input string>`, **Extraction.Text.Region** learns a program to extract a substring from an input string. The program can be run on new input strings to obtain new output substrings.

For instance, given this example:

|        Input      | Example output |
|:------------------|:---------------|
| Carrie `Dodson` 100 | `Dodson`   |

**Extraction.Text.Region** generates a program to extract the last name in similar strings such as the one below:

|        Input      | Program output |
|:------------------|:---------------|
| Leonard `Robledo` 75 | `Robledo`   |


# Sequence Extraction

From input/output example(s) in the form of `<input string, subsequence of the intended sequence>`, **Extraction.Text.Sequence** learns a program to extract a *complete* sequence of substrings from an input string. The program can be run on the same training input string to obtain the complete output sequence, or on new input strings to obtain new output sequences.

For instance, given this example that contains a subsequence of the intended sequence of first names:

|        Input      | Example output |
|:------------------|:---------------|
| United States<br/> &nbsp;`Carrie` Dodson 100<br/> &nbsp;`Leonard` Robledo 75<br/>&nbsp;Margaret Cook 320<br/>Canada<br/> &nbsp;Concetta Beck 350<br/> &nbsp;Nicholas Sayers 90<br/> &nbsp;Francis Terrill 2430<br/> Great Britain<br/> &nbsp;Nettie Pope 50<br/> &nbsp;Mack Beeson 1070 | `Carrie`<br/> `Leonard` |

**Extraction.Text.Sequence** generates a program to extract the sequence of all first names:

|        Input      | Program output |
|:------------------|:---------------|
| United States<br/> &nbsp;`Carrie` Dodson 100<br/> &nbsp;`Leonard` Robledo 75<br/> &nbsp;`Margaret` Cook 320<br/>Canada<br/> &nbsp;`Concetta` Beck 350<br/> &nbsp;`Nicholas` Sayers 90<br/> &nbsp;`Francis` Terrill 2430<br/>Great Britain<br/> &nbsp;`Nettie` Pope 50<br/> &nbsp;`Mack` Beeson 1070 | `Carrie`<br/> `Leonard`<br/> `Margaret`<br/>`Concetta` <br/>`Nicholas` <br/>`Francis` <br/>`Nettie` <br/>`Mack` |


# Applications

Clients may use these two APIs directly, or combine them to extract nested/hierarchical data (*i.e*, tree) from documents. Below are several real usages of **Extraction.Text**.

## Operations Management Suite (OMS)

The [**Custom Fields**](https://docs.microsoft.com/en-us/azure/log-analytics/log-analytics-custom-fields) feature in OMS allows users to create a new *custom* field based on an existing field in the log. **Custom Fields** uses the **Extraction.Text.Region** API because each cell in the new field is a substring of a cell in the existing field.

Users highlight a text in one cell of the source field that they want to create a new field with. 

<a href="{{ site.baseurl }}/img/custom-field-example.png" class="popup-image mfp-image"
   title="Create a new custom field by selecting the example.">
    <img class="pure-img mx-auto" src="{{ site.baseurl }}/img/custom-field-example.png">
</a>

The UI calls the **Extraction.Text.Region** API to learn a substring extraction program, and populates other cells of the new field. Users may give more examples if necessary.

<a href="{{ site.baseurl }}/img/custom-field-result.png" class="popup-image mfp-image"
   title="The UI returns similar extracted entries.">
    <img class="pure-img mx-auto" src="{{ site.baseurl }}/img/custom-field-result.png">
</a>

 **Read more:** [Create your own fields in OMS with Custom Fields!](https://blogs.technet.microsoft.com/momteam/2015/08/18/create-your-own-fields-in-oms-with-custom-fields/).

The [**Custom Log**](https://docs.microsoft.com/en-us/azure/log-analytics/log-analytics-data-sources-custom-logs) feature in OMS allows users to split the input text stream into records, which can be split further into fields using **Custom Fields**.

<a href="{{ site.baseurl }}/img/custom-log.png" class="popup-image mfp-image"
   title="Custom Log.">
    <img class="pure-img mx-auto" src="{{ site.baseurl }}/img/custom-log.png">
</a>


## PowerShell ConvertFrom-String

[**ConvertFrom-String**](https://msdn.microsoft.com/en-us/powershell/reference/5.0/microsoft.powershell.utility/convertfrom-string) allows users to extract hierarchical data from a document from an example template, which is a sample of the complete document.

Users mark extracted fields in the template using pairs of curly brackets { }. The following template extracts three fields, each of which was given 2 examples.

```
{[string]Name*:Phoebe Cat}, {[string]phone:425-123-6789}, {[int]age:6}
{[string]Name*:Lucky Shot}, {[string]phone:(206) 987-4321}, {[int]age:12}
```

**ConvertFrom-String** learns the fields one by one using one of the two **Extraction.Text** APIs. The fields are learned based on their document order. That is, a field appear first in the document will be learned first. 

While learning a field, **ConvertFrom-String** uses one of the already learned fields as a *reference*. Depending on the nature of the field, it learns a substring program or a sequence program.

In the above template, **ConvertFrom-String** learns a sequence of *Name* (also indicated by the * next to *Name*), a substring of *phone* based on *Name*, and a substring of *age* also based on *Name*. Note that *age* can also be learned *w.r.t* *phone*.

Now we can pass the template to **ConvertFrom-String** to extract nested data from the complete input document.

``` powershell
$template = @'
{[string]Name*:Phoebe Cat}, {[string]phone:425-123-6789}, {[int]age:6}
{[string]Name*:Lucky Shot}, {[string]phone:(206) 987-4321}, {[int]age:12}
'@

$testText = @'
Phoebe Cat, 425-123-6789, 6
Lucky Shot, (206) 987-4321, 12
Elephant Wise, 425-888-7766, 87
Wild Shrimp, (111)  222-3333, 1
'@

$testText  |
    ConvertFrom-String -TemplateContent $template -OutVariable PersonalData | Out-Null

Write-Output ("Pet items found: " + ($PersonalData.Count))
$PersonalData
```

Output:

```
Pet items found: 4

Name          phone           age
----          -----           ---
Phoebe Cat    425-123-6789      6
Lucky Shot    (206) 987-4321   12
Elephant Wise 425-888-7766     87
Wild Shrimp   (111)  222-3333   1
```

**Read more:** [ConvertFrom-String: Example-based text parsing](https://blogs.msdn.microsoft.com/powershell/2014/10/31/convertfrom-string-example-based-text-parsing/).


