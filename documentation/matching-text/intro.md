---
date: 2018-05-23
title: "Matching Text"
---

Have you ever written a script to perform a string transformation and have it either crash or
produce wrong results silently due to input data being in unexpected formats? Or do you want 
to figure out how many different cases you need to handle in your standardization procedure.
**Matching.Text** to the rescue!

**Matching.Text** automatically identifies different formats and patterns in
string data.  Given a set of input strings, **Matching.Text** produces a small
number of disjoint regular expressions such that they together match all the input
strings, except possibly a small fraction of outliers.  Additional
documentation and usage can be found [here]({{ site.baseurl }}/documentation/matching-text/usage).

Scenario
----------------

Consider a list of names below which from which you want to extract last names.

|Full Name      | 
|:------        |
|Laia Sanchis   |
|Gwilym Jones   |
|Cai Huws       |
|Tomi Elis      |
|Geraint Llwyd  |
|...            |

A simple looking task, if there was one -- the python function below is a good
attempt.
```python
def extract_last_name(name):
    return name[name.find(' ')+1:]
```

However, while the first 10 names look standard, running
**Matching.Text** provides more insight into the different formats, further
identifies outliers that do not fall into any of the other formats.


|Pattern Name             |Regex Pattern                         |Frequency |Examples                                    |
|:----                    |:------                               |   ------:|:------                                     |
|Word_Word                |`[A-Z][a-z]+ [A-Z][a-z]+`             |  0.84    |"Laia Sanchis", "Gwilym Jones"              |
|Word_Word_Hyphen_Word    |`[A-Z][a-z]+ [A-Z][a-z]+-[A-Z][a-z]+` |  0.06    |"Tulga Bat-Erdene", "Dabir Al-Zuhairi"      |
|Word_Word_Word           |`[A-Z][a-z]+ [A-Z][a-z]+ [A-Z][a-z]+` |  0.06    |"Yue Ying Jen", "Rolf Van Eeuwijk"          |
|Word                     |`[A-Z][a-z]+`                         |  0.04    |"Danlami", "Isioma"                         |
|Outliers                 |                                      | <0.01    |"UNKNOWN", "NULL"                           |

Given this new insight, it can be seen that `extract_last_name` may not always
return the right answer, and you may want to handle the last name extraction task
quite differently.
Further, to make the writing the procedure easier, **Matching.Text** can also generate
a switch-case like template to match against the different patterns.
```python
regex_word_word = re.compile(r'[A-Z][a-z]+ [A-Z][a-z]+')
regex_word_word_hyphen_word = re.compile(r'[A-Z][a-z]+ [A-Z][a-z]+-[A-Z][a-z]+')
regex_word_word_word = re.compile(r'[A-Z][a-z]+ [A-Z][a-z]+ [A-Z][a-z]+')
regex_word = re.compile(r'[A-Z][a-z]+')

def extract_last_name(name):
    if regex_word_word.match(name):
        return "TitleWord & TitleWord"                                     # Modify
    elif regex_word_word_hyphen_word.match(name):
        return "TitleWord & TitleWord & Const[-] & TitleWord"              # Modify
    elif regex_word_word_word.match(name):
        return "TitleWord & TitleWord & TitleWord"                         # Modify
    elif regex_word.match(name):
        return "TitleWord"                                                 # Modify
    else:
        return "Others"                                                    # Modify
```

