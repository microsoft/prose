from copy import deepcopy
import re




isTrivialLine = re.compile("^(\s|\{|\}|;)*\S*(\s|\{|\}|;)*$")
isCompleteLine = re.compile(".*\n$")
def TryGetFirstKNonTrivialLines(lineList, k):
    retLineList = []
    nonSpaceLines = 0
    for line in lineList:
        if k < 0:
            break
        elif isTrivialLine.match(line) is None:
            k-=1
            nonSpaceLines+=1
        retLineList.append(line)
    return retLineList, nonSpaceLines

def truncateLineListsByLineCount(currentLines: list[str], prefixLines: list[str], suffixLines: list[str], lineCount = 10):
    tokensLine = sum([isTrivialLine.match(line) is None for line in currentLines])
    if tokensLine > lineCount:
        truncatedPrefixLines = []
        truncatedSuffixLines = []
    else:
        lineCount -= tokensLine
        truncatedPrefixLines, nonTrivialLines = TryGetFirstKNonTrivialLines(prefixLines.__reversed__(), lineCount//2)
        truncatedPrefixLines.reverse()
        truncatedSuffixLines, _ = TryGetFirstKNonTrivialLines(suffixLines, lineCount - nonTrivialLines)
    return {
            "prefix": truncatedPrefixLines,
            "suffix": truncatedSuffixLines,
            "current": currentLines
        }


def truncateLineListsByLineCountAndGetDiffDict(prefix, suffix, before, after, lineCount):  
    before = truncateLineListsByLineCount(lineCount = lineCount, prefixLines= prefix, suffixLines=suffix, currentLines=before)
    after = truncateLineListsByLineCount(lineCount = lineCount, prefixLines= prefix, suffixLines=suffix, currentLines=after)
    suffix = before["suffix"] if len(before["suffix"]) < len(after["suffix"]) else after["suffix"]
    prefix = before["prefix"] if len(before["prefix"]) < len(after["prefix"]) else after["prefix"]
    return {
        "prefix": "".join(prefix),
        "suffix": "".join(suffix),
        "before": "".join(before["current"]),
        "after": "".join(after["current"])
    }

"""
Take the before and after versoin code lines as an input and localise the edit. 
Returns the prefix, suffix and the changelines 
"""
def LocalizeChanges(beforeLines: list[str], afterLines: list[str]):
    def match_prefix(beforeLines: list[str], afterLines: list[str]):
        matchIdx = 0
        for b, a in zip(beforeLines, afterLines):
            if b == a :
                matchIdx+=1
            else:
                break
        return {
            "prefix": beforeLines[:matchIdx],
            "before": beforeLines[matchIdx:],
            "after": afterLines[matchIdx:]
        }
    match = match_prefix(beforeLines, afterLines)
    prefix = match["prefix"]
    match = match_prefix(list(reversed(match["before"])), list(reversed(match["after"])))
    for k, v in match.items():
        v.reverse()
    suffix = match["prefix"]
    return {
        "prefix": prefix,
        "suffix": suffix,
        "before": match["before"],
        "after": match["after"],
    }

def ASTLocalizationToLineLocalization(edDict: dict[str,list[str]]):
    retEdDict = deepcopy(edDict)
    def TryGetFirst(lineList: list[str]):
        if len(lineList)> 0:
            return lineList[0]
        else:
            return None
    def TryGetLast(lineList: list[str]):
        if len(lineList)> 0:
            return lineList[-1]
        else:
            return None
    def ConcatToLineList(lineList: list[str], content: str, last: bool):
        if len(lineList) == 0:
            lineList.append("")
        if last:
            lineList[-1] = lineList[-1] + content
        else:
            lineList[0] = content + lineList[0]

    if (x:= TryGetLast(retEdDict["prefix"])) and not isCompleteLine.match(x):
        ConcatToLineList(retEdDict["before"], x, False)
        ConcatToLineList(retEdDict["after"], x, False)
        retEdDict["prefix"].pop()

    if ((b := TryGetLast(retEdDict["before"])) and not isCompleteLine.match(b)) or ((a := TryGetLast(retEdDict["after"])) and not isCompleteLine.match(a)):
        addLine = TryGetFirst(retEdDict["suffix"])
        if addLine:
            ConcatToLineList(retEdDict["before"], addLine, True)
            ConcatToLineList(retEdDict["after"], addLine, True)
            retEdDict["suffix"].pop(0)
    return retEdDict

    
def processEditSeq(editSeq):
    allEdits = [(edit[0].replace("\\n", "\n"), edit[1].replace("\\n", "\n")) for edit in editSeq["edits"] ]

    dataLis = []
    for k in range(len(allEdits)):  
        allBefore, allAfter = zip(*allEdits)
        allBefore, allAfter = list(allBefore),  list(allAfter)
        currAfter = allAfter[k]
        allAfter[k] = allBefore[k]
        localizedEdits = [LocalizeChanges("".join(allAfter[:i] + allBefore[i:]).splitlines(keepends=True),"".join(allAfter[:i+1] + allBefore[i+1:]).splitlines(keepends=True))  for i in range(k)]
        localizedEdits.append({
            "prefix": "".join(allAfter[:k]).splitlines(keepends=True),
            "suffix": "".join(allAfter[k+1:]).splitlines(keepends=True),
            "before": allBefore[k].splitlines(keepends=True),
            "after": currAfter.splitlines(keepends=True)
        })
        localizedEdits.extend([LocalizeChanges("".join(allAfter[:i] + allBefore[i:]).splitlines(keepends=True),"".join(allAfter[:i+1] + allBefore[i+1:]).splitlines(keepends=True))  for i in range(k+1,len(allBefore))])
        truncEdits = [truncateLineListsByLineCountAndGetDiffDict(lineCount= (8 if i != k else 15) , **ed) for i, ed in enumerate(localizedEdits)] 
        dataLis.append({
            "AssociatedEdits": truncEdits[:k] + truncEdits[k+1:],
            "Current" : truncEdits[k]
        })
    return {
        "id": editSeq["id"],
        "data": dataLis
    }

def processSession(sess):
    return [processEditSeq(editSeq) for editSeq in sess]
