---
date: 2017-02-01T20:00:06-07:00
title: "Text Splitting"
---

**Split.Text** is a system for splitting data in plain text format, where there may be multiple fields that need to be separated into different columns. The [Usage]({{ site.baseurl }}/documentation/split-text/usage) page and the [`Split.Text` sample project](https://github.com/Microsoft/prose/tree/master/Split.Text) show examples of how to use the Split.Text API. The Split.Text system supports purely predictive as well as interactive techniques to learn programs for splitting textual data. 

## Predictive Splitting

The predictive learning technique attempts to infer a program given only the input data and no other constraints from the user (such as output examples). It analyses the properties of the input data to infer the most regular pattern of fields and delimiters that have good alignment with one another. For instance, if we are given the following input data without any output examples:

| Input                               | 
|:------------------------------------|
| PE5 Leonard Robledo (Australia)     |
| U109 Adam Jay Lucas (New Zealand) |
| R342 Carrie Dodson (United States)  |
| TS51 Naomi Cole (Canada)            |
| Y722 Owen Murphy (United States)              |
| UP335 Zoe Erin Rees (UK)            |


Split.Text will predictively generate a program to perform the following three-column splitting:

| Split Column 1    | Split Column 2    | Split Column 3               |
|:--------------|:----------------------|:----------------------|
|       PE5     |       Leonard Robledo |       Australia       |
|       U109    |       Adam Jay Lucas  |       New Zealand   |
|       R342    |       Carrie Dodson   |       United States   |
|       TS51    |       Naomi Cole      |       Canada          |
|       Y722    |       Owen Murphy     |       United States             |
|       UP335   |       Zoe Erin Rees   |       UK              |

In this case it determines the space as well as open/close brackets as probable delimiters given the pattern in the inputs. However, not all occurrences of the space character is a delimiter, as there are varying number of spaces inside the person names (some including middle names) and countries as well. Hence we cannot simply split by all spaces. The Split.Text DSL and learning algorithm handles such scenarios by analyzing the patterns within the inferred data fields as well as supporting *contextual delimiters*, which look at data patterns around occurrences of possible delimiting substrings. More information about the DSL and learning techniques can be found in our [recent publication on predictive program synthesis.](https://research.microsoft.com/en-us/um/people/sumitg/pubs/aaai17.pdf)

## Interactive Splitting

The predictive inference of Split.Text can handle many common practical scenarios for text splitting. However, in many cases different users may have different preferences for the kind of splitting they want, especially with respect to how they want to split a particular field into subfields. For example, in the above scenario, one user may want to separate the first names into a separate column while another may prefer to have just the last name in its own column. Split.Text supports such scenarios with interactive features that permit the user to provide various constraints on the program that will be learnt. 

The most powerful constraint is to provide examples of the desired splitting on some inputs. For instance, if the user wants first names to be split into a separate column, she may provide the following examples on the first two inputs:

| Input                               | Split Column 1    | Split Column 2    | Split Column 3 | Split Column 4 |
|:------------------------------------|:--------------|:----------------------|:----------------------|:------|
| PE5 Leonard Robledo (Australia)     |      PE5     |       Leonard |       Robledo |       Australia       |
| U109 Adam Jay Lucas (New Zealand) |      U109    |       Adam    |       Jay Lucas       |       New Zealand   |

The system will then learn a program that can perform the same splitting on the rest of the data:

| Input                               | Split Column 1    | Split Column 2    | Split Column 3 | Split Column 4 |
|:------------------------------------|:--------------|:----------------------|:----------------------|:------|
| PE5 Leonard Robledo (Australia)     |      PE5     |       Leonard |       Robledo |       Australia       |
| U109 Adam Jay Lucas (New Zealand) |      U109    |       Adam    |       Jay Lucas       |       New Zealand   |
| R342 Carrie Dodson (United States)  |       R342    |       Carrie  |       Dodson  |       United States   |
| TS51 Naomi Cole (Canada)            |TS51    |       Naomi   |       Cole    |       Canada  |
| Y722 Owen Murphy (United States)              |Y722    |       Owen    |       Murphy  |       United States     |
| UP335 Zoe Erin Rees (UK)            | UP335   |       Zoe     |       Erin Rees       |       UK      |


If another user wants last names to be in a separate column, then he can similarly provide the corresponding examples to achieve that splitting:

| Input                               | Split Column 1    | Split Column 2    | Split Column 3 | Split Column 4 |
|:------------------------------------|:--------------|:----------------------|:----------------------|:------|
| PE5 Leonard Robledo (Australia)     |      PE5     |       Leonard |       Robledo |       Australia       |
| U109 Adam Jay Lucas (New Zealand) |      U109    |       Adam Jay   |       Lucas       |       New Zealand   |

The system will then learn a program that can perform the same splitting on the rest of the data:

| Input                               | Split Column 1    | Split Column 2    | Split Column 3 | Split Column 4 |
|:------------------------------------|:--------------|:----------------------|:----------------------|:------|
| PE5 Leonard Robledo (Australia)     |      PE5     |       Leonard |       Robledo |       Australia       |
| U109 Adam Jay Lucas (New Zealand) |      U109    |       Adam Jay  |       Lucas       |       New Zealand   |
| R342 Carrie Dodson (United States)  |       R342    |       Carrie  |       Dodson  |       United States   |
| TS51 Naomi Cole (Canada)            |TS51    |       Naomi   |       Cole    |       Canada  |
| Y722 Owen Murphy (United States)              |Y722    |       Owen    |       Murphy  |       United States     |
| UP335 Zoe Erin Rees (UK)            | UP335   |       Zoe Erin    |       Rees       |       UK      |


As well as the ability to provide examples, Split.Text supports various other constraints, such as whether the user wants to keep the delimiters in separate columns or not. 
