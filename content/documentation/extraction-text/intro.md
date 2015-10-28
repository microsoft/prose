---
date: 2015-09-02T20:00:06-07:00
title: "Text Extraction"
---

**Extraction.Text** is a system that extracts data from semi-structured text files using *examples*.
The [Usage](/documentation/extraction-text/usage) page and the `Extraction.Text.Sample` project show examples of how to use the Extraction.Text API.

Extraction.Text supports two kinds of extraction: (1) extract a substring from an input string, and (2) extract a sequence of substrings from an input string.

#### Substring Extraction

Given an example like

|        Input      | Example output |
|:------------------|:---------------|
| Carrie Dodson 100 | Dodson   |

Extraction.Text will generate a program to extract the last name given any other similar strings:

|        Input      | Program output |
|:------------------|:---------------|
| Leonard Robledo 75 | Robledo   |


#### Sequence Extraction

Given an example like

|        Input      | Example output |
|:------------------|:---------------|
| United States<br/>Carrie Dodson 100<br/>Leonard Robledo 75<br/>Margaret Cook 320<br/>Canada<br/>Concetta Beck 350<br/>Nicholas Sayers 90<br/>Francis Terrill 2430<br/>Great Britain<br/>Nettie Pope 50<br/>Mack Beeson 1070 | Carrie<br/> Leonard |

Extraction.Text will generate a program to extract the sequence of all first names in this string or in other similar strings:

|        Input      | Program output |
|:------------------|:---------------|
| United States<br/>Carrie Dodson 100<br/>Leonard Robledo 75<br/>Margaret Cook 320<br/>Canada<br/>Concetta Beck 350<br/>Nicholas Sayers 90<br/>Francis Terrill 2430<br/>Great Britain<br/>Nettie Pope 50<br/>Mack Beeson 1070 | Carrie<br/> Leonard<br/> Margaret<br/>Concetta <br/>Nicholas <br/>Francis <br/>Nettie <br/>Mack |

