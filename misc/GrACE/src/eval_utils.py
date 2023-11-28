from fuzzywuzzy import fuzz
import csv
import json


def getTopEditSim(preds, targets, k=1):
    assert len(preds) == len(targets)
    editSimilarity = 0.0
    for predLis, target in zip(preds, targets):
        maxEditSim = 0.0
        # Ignoring Edit Similarity
        # for pred in predLis[: min(k, len(predLis))]:
        #     maxEditSim = max(maxEditSim, fuzz.ratio(pred.strip(), target.strip()))
        # editSimilarity += maxEditSim

    return round(editSimilarity / len(preds), 2)


def getTopKAcc(topPreds, targets, k=1):
    assert len(topPreds) == len(targets)
    exactMatch = 0.0
    for predLis, target in zip(topPreds, targets):
        for pred in predLis[: min(k, len(predLis))]:
            if pred.strip().split() == target.strip().split():
                exactMatch += 1
                break
    return round(exactMatch / len(topPreds) * 100, 2)

def getTopEditSimFV(preds, futureVersions, k=1):
    assert len(preds) == len(futureVersions)
    editSimilarity = 0.0
    for predLis, fv in zip(preds, futureVersions):
        maxEditSim = 0.0
        # IGNORE EDIT SIMILARITY FOR NOW
        # for target in fv:
        #     for pred in predLis[: min(k, len(predLis))]:
        #         maxEditSim = max(maxEditSim, fuzz.ratio(pred.strip(), target.strip()))
        #     if maxEditSim >= 99:
        #         break
        editSimilarity += maxEditSim

    return round(editSimilarity / len(preds), 2)


def getTopKAccFV(topPreds, futureVersions, k=1):
    assert len(topPreds) == len(futureVersions)
    exactMatch = 0.0
    for predLis, fv in zip(topPreds, futureVersions):
        for target in fv:
            matched = False
            for pred in predLis[: min(k, len(predLis))]:
                if pred.strip().split() == target.strip().split():
                    exactMatch += 1
                    matched = True
                    break
            if matched:
                break
    return round(exactMatch / len(topPreds) * 100, 2)


def getEvalDict(predDict):
    evalKs = [1, 3, 5]
    evalDict = {}
    for k in evalKs:
        evalDict[f"RawAccTop-{k}"] = getTopKAcc(
            predDict["topPreds"], predDict["targets"], k
        )
        evalDict[f"RawESTop-{k}"] = getTopEditSim(
            predDict["topPreds"], predDict["targets"], k
        )
        if predDict.get("futureVersions", None):
            evalDict[f"FVAccTop-{k}"] = getTopKAccFV(
                predDict["topPreds"], predDict["futureVersions"], k
            )
            evalDict[f"FVESTop-{k}"] = getTopEditSimFV(
                predDict["topPreds"], predDict["futureVersions"], k
            )
    return evalDict


def getAvgPerf(resDict, dumpFV = False):
    evalKs = [1, 3, 5]
    keys = [f"RawAccTop-{k}" for k in evalKs]
    keys += [f"RawESTop-{k}" for k in evalKs]
    if dumpFV:
        keys += [f"FVAccTop-{k}" for k in evalKs]
        keys += [f"FVESTop-{k}" for k in evalKs]

    avgDict = {}
    for key in keys:
        avgDict[key] = 0.0
    for i, id in enumerate(resDict):
        evalDict = resDict[id]["eval"]
        
        for key in keys:
            avgDict[key] = (avgDict[key] * i + evalDict[key]) / (i + 1)

    for key in avgDict:
        avgDict[key] = round(avgDict[key], 2)
    return avgDict


def getOverallPerf(overAllresDict, dumpFV = False):
    evalKs = [1, 3, 5]
    keys = [f"RawAccTop-{k}" for k in evalKs]
    keys += [f"RawESTop-{k}" for k in evalKs]
    if dumpFV:
        keys += [f"FVAccTop-{k}" for k in evalKs]
        keys += [f"FVESTop-{k}" for k in evalKs]
    avgDict = {}
    for key in keys:
        avgDict[key] = 0.0
    totIds = 0
    for i, ep in enumerate(overAllresDict):
        evalDict = overAllresDict[ep]["eval"]
        numIds = len(overAllresDict[ep]["id2pred"])
        for key in keys:
            avgDict[key] = (avgDict[key] * totIds + evalDict[key] * numIds) / (
                totIds + numIds
            )
        totIds += numIds

    for key in avgDict:
        avgDict[key] = round(avgDict[key], 2)
    return avgDict


def dumpid2pred(id2pred, target_file, dumpFV = False):
    evalKs = [1, 3, 5]
    evalKeys = [f"RawAccTop-{k}" for k in evalKs]
    evalKeys += [f"RawESTop-{k}" for k in evalKs]
    if dumpFV:
        evalKeys += [f"FVAccTop-{k}" for k in evalKs]
        evalKeys += [f"FVESTop-{k}" for k in evalKs]
    with open(target_file, "w+", newline="", encoding='utf-8') as csvfile:
        fieldnames = [
            "id",
            "targets",
            "RawPredTop-1",
            "RawPredTop-2",
            "RawPredTop-3",
            "RawPredTop-4",
            "RawPredTop-5",
            "promptPrefix",
            "promptSuffix"
        ] + evalKeys
        if dumpFV:
            fieldnames += ["futureVersions"]
        if len(id2pred) > 0 and "k-shotAscEditIds" in id2pred[list(id2pred.keys())[0]]["pred"]:
            fieldnames += ["k-shotAscEditIds"]
        writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
        writer.writeheader()
        for id in id2pred:
            rowDict = {
                "id": id,
                "targets": id2pred[id]["pred"]["targets"],
                "RawPredTop-1": [
                    rawPred[0] if len(rawPred) > 0  else " " for rawPred in id2pred[id]["pred"]["topPreds"] 
                ],
                "RawPredTop-2": [
                    rawPred[1]  if len(rawPred) > 1 else " " for rawPred in id2pred[id]["pred"]["topPreds"]
                ],
                "RawPredTop-3": [
                    rawPred[2] if len(rawPred) > 2 else " " for rawPred in id2pred[id]["pred"]["topPreds"] 
                ],
                "RawPredTop-4": [
                    rawPred[3] if len(rawPred) > 3 else " " for rawPred in id2pred[id]["pred"]["topPreds"] 
                ],
                "RawPredTop-5": [
                    rawPred[4] if len(rawPred) > 4 else " " for rawPred in id2pred[id]["pred"]["topPreds"] 
                ],
                "promptPrefix": id2pred[id]["pred"].get("promptPrefix", ""),
                "promptSuffix": id2pred[id]["pred"].get("promptSuffix", "")
            }
            if dumpFV:
                rowDict["futureVersions"] = id2pred[id]["pred"]["futureVersions"]
            if "k-shotAscEditIds" in id2pred[id]["pred"]:
                rowDict["k-shotAscEditIds"] = id2pred[id]["pred"]["k-shotAscEditIds"]
            for key in evalKeys:
                rowDict[key] = id2pred[id]["eval"][key]
            writer.writerow(rowDict)
        avgPerf = getAvgPerf(id2pred, dumpFV)
        rowDict = {
            "id": "Overall",
            "targets": "-",
            "RawPredTop-1": "-",
            "RawPredTop-2": "-",
            "RawPredTop-3": "-",
            "RawPredTop-4": "-",
            "RawPredTop-5": "-",
        }
        for key in evalKeys:
            rowDict[key] = avgPerf[key]
        writer.writerow(rowDict)


def dumpOverallresDict(resDict, target_file, dumpFV = False):
    evalKs = [1, 3, 5]
    evalKeys = [f"RawAccTop-{k}" for k in evalKs]
    evalKeys += [f"RawESTop-{k}" for k in evalKs]
    if dumpFV:
        evalKeys += [f"FVAccTop-{k}" for k in evalKs]
        evalKeys += [f"FVESTop-{k}" for k in evalKs]
    totTime = 0
    with open(target_file, "w+", newline="", encoding='utf-8') as csvfile:
        fieldnames = ["id", "time"] + evalKeys
        writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
        writer.writeheader()
        for id in resDict:
            timeRow = round(resDict[id]["time"], 2)
            totTime += timeRow
            rowDict = {"id": id, "time": timeRow}
            for key in evalKeys:
                rowDict[key] = resDict[id]["eval"][key]
            writer.writerow(rowDict)
        overallPerf = getOverallPerf(resDict, dumpFV)
        rowDict = {"id": "Overall", "time": totTime}
        for key in evalKeys:
            rowDict[key] = overallPerf[key]
        writer.writerow(rowDict)

