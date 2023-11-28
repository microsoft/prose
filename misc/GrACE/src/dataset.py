import json
import random
from pathlib import Path

from torch.utils.data import Dataset


class AssociatedEditsDataset(Dataset):
    def __init__(
        self,
        json_addr,
        assc_edit_prompt=lambda x: "<edit>\n{}</edit>".format(
            "\n".join(
                [
                    "<{}>\n{}\n</{}>".format(k, x[k], k)
                    for k in ["prefix", "before", "after", "suffix"]
                ]
            )
        ),
        predict_prompt=lambda x: x["after"],
        curr_edit_prefix=lambda x: "<edit>\n"
        + "".join(["<{}>\n{}\n</{}>".format(k, x[k], k) for k in ["prefix", "before"]])
        + "\n<after>\n",
        curr_edit_suffix=lambda x: "\n</after>\n<suffix>\n{}\n</suffix>\n</edit>".format(
            x["suffix"]
        ),
        use_all=False,
        shuffle=False,
        use_ascEdits=True,
    ):
        self.dataLis = []
        filePath = Path(json_addr)
        self.mode = "lineComp"
        if filePath.suffix == ".json":
            with filePath.open() as f:
                sessionData = json.load(f)
            for editSeqData in sessionData:
                editSeqId = editSeqData["id"]
                for i, editData in enumerate(editSeqData["data"]):
                    ######### Remove this line if you want to use all 3 configs ##############
                    if len(editSeqData["data"]) > 1 and not use_all and i != 1:
                        continue
                    id = f"{editSeqId}.{i}"
                    prefix = curr_edit_prefix(editData["Current"])
                    suffix = curr_edit_suffix(editData["Current"])
                    if use_ascEdits:
                        if shuffle:
                            random.shuffle(editData["AssociatedEdits"])
                        ascEditCtx = "\n".join(
                            [assc_edit_prompt(ed) for ed in editData["AssociatedEdits"]]
                        )
                        suffix += f"<ctxEdits>{ascEditCtx}</ctxEdits>"
                    predictText = predict_prompt(editData["Current"])
                    full_prefix = editData["Current"]["prefix"]
                    if "FullPrefix" in editData["Current"]:
                        full_prefix = "".join(editData["Current"]["FullPrefix"])
                    full_suffix = editData["Current"]["suffix"]
                    if "FullSuffix" in editData["Current"]:
                        full_suffix = "".join(editData["Current"]["FullSuffix"])
                    self.dataLis.append(
                        {
                            "Spctx": prefix,
                            "FutureCtx": suffix,
                            "ExpectedText": predictText,
                            "Id": id,
                            "current_prefix": full_prefix,
                            "current_suffix": full_suffix,
                        }
                    )
                    futureVersions = editData.get("FutureVersions", [])
                    if futureVersions:
                        self.dataLis[-1]["FutureVersions"] = [futureVersions]
        self.__len = len(self.dataLis)

    def __len__(self):
        return self.__len

    def __getitem__(self, idx):
        return self.dataLis[idx]


class AssociatedEditsFewShotDataset(Dataset):
    def __init__(
        self,
        json_addr,
        assc_edit_prompt=lambda x: "<edit>\n{}</edit>".format(
            "\n".join(
                [
                    "<{}>\n{}\n</{}>".format(k, x[k], k)
                    for k in ["prefix", "before", "after", "suffix"]
                ]
            )
        ),
        predict_prompt=lambda x: x["after"],
        curr_edit_prefix=lambda x: "<edit>\n"
        + "".join(["<{}>\n{}\n</{}>".format(k, x[k], k) for k in ["prefix", "before"]])
        + "\n<after>\n",
        curr_edit_suffix=lambda x: "\n</after>\n<suffix>\n{}\n</suffix>\n</edit>".format(
            x["suffix"]
        ),
        use_all=False,
        shuffle=False,
        use_asc_edits=True,
        few_shot_key="few_shot_samples_same_repo",
    ):
        self.dataLis = []
        filePath = Path(json_addr)
        self.mode = "lineComp"
        if filePath.suffix == ".json":
            with filePath.open() as f:
                sessionData = json.load(f)
            for editSeqData in sessionData:
                editSeqId = editSeqData["id"]
                for i, editData in enumerate(editSeqData["data"]):
                    ######### Remove this line if you want to use all 3 configs ##############
                    if len(editSeqData["data"]) > 1 and not use_all and i != 1:
                        continue
                    id = f"{editSeqId}.{i}"
                    prefix = curr_edit_prefix(editData["Current"])
                    suffix = curr_edit_suffix(editData["Current"])
                    if use_asc_edits:
                        if shuffle:
                            random.shuffle(editData[few_shot_key])
                        asc_edits = [
                            fewShotSample["Assoc_Edit"]
                            for fewShotSample in editData[few_shot_key]
                        ]
                        asc_edit_ids = [
                            fewShotSample["sample_id"]
                            for fewShotSample in editData[few_shot_key]
                        ]
                        # ascEditCtx = "\n".join([assc_edit_prompt(ed) for ed in asc_edits])
                        # suffix += f'<ctxEdits>{ascEditCtx}</ctxEdits>'
                        ################## FEWSHOT NO ASSOC EDIT
                        fewshot_prefix = "\n\n".join(
                            [assc_edit_prompt(ed) for ed in asc_edits]
                        )
                        prefix = fewshot_prefix + "\n\n" + prefix
                    predictText = predict_prompt(editData["Current"])
                    full_prefix = editData["Current"]["prefix"]
                    if "FullPrefix" in editData["Current"]:
                        full_prefix = "".join(editData["Current"]["FullPrefix"])
                    full_suffix = editData["Current"]["suffix"]
                    if "FullSuffix" in editData["Current"]:
                        full_suffix = "".join(editData["Current"]["FullSuffix"])
                    self.dataLis.append(
                        {
                            "Spctx": prefix,
                            "FutureCtx": suffix,
                            "ExpectedText": predictText,
                            "Id": id,
                            "current_prefix": full_prefix,
                            "current_suffix": full_suffix,
                        }
                    )
                    futureVersions = editData.get("FutureVersions", [])
                    if futureVersions:
                        self.dataLis[-1]["FutureVersions"] = [futureVersions]
                    self.dataLis[-1]["k-shotAscEditIds"] = asc_edit_ids
        self.__len = len(self.dataLis)

    def __len__(self):
        return self.__len

    def __getitem__(self, idx):
        return self.dataLis[idx]


class AssociatedEditsFewShotAblationDataset(Dataset):
    def __init__(
        self,
        json_addr,
        assc_edit_prompt=lambda x: "<edit>\n{}</edit>".format(
            "\n".join(
                [
                    "<{}>\n{}\n</{}>".format(k, x[k], k)
                    for k in ["prefix", "before", "after", "suffix"]
                ]
            )
        ),
        predict_prompt=lambda x: x["after"],
        curr_edit_prefix=lambda x: "<edit>\n"
        + "".join(["<{}>\n{}\n</{}>".format(k, x[k], k) for k in ["prefix", "before"]])
        + "\n<after>\n",
        curr_edit_suffix=lambda x: "\n</after>\n<suffix>\n{}\n</suffix>\n</edit>".format(
            x["suffix"]
        ),
        use_all=False,
        shuffle=False,
        use_asc_edits=True,
        few_shot_key="few_shot_samples_same_repo",
    ):
        self.dataLis = []
        filePath = Path(json_addr)
        self.mode = "lineComp"
        if filePath.suffix == ".json":
            with filePath.open() as f:
                sessionData = json.load(f)
            for editSeqData in sessionData:
                editSeqId = editSeqData["id"]
                for i, editData in enumerate(editSeqData["data"]):
                    ######### Remove this line if you want to use all 3 configs ##############
                    if len(editSeqData["data"]) > 1 and not use_all and i != 1:
                        continue
                    id = f"{editSeqId}.{i}"
                    prefix = curr_edit_prefix(editData["Current"])
                    suffix = curr_edit_suffix(editData["Current"])
                    if use_asc_edits:
                        if shuffle:
                            random.shuffle(editData[few_shot_key])
                        asc_edits = editData["AssociatedEdits"]
                        asc_edits.append(editData[few_shot_key][0]["Assoc_Edit"])
                        asc_edit_ids = [id, id]
                        asc_edit_ids.append(editData[few_shot_key][0]["sample_id"])
                        ascEditCtx = "\n".join(
                            [assc_edit_prompt(ed) for ed in asc_edits]
                        )
                        suffix += f"<ctxEdits>{ascEditCtx}</ctxEdits>"
                    predictText = predict_prompt(editData["Current"])
                    full_prefix = editData["Current"]["prefix"]
                    if "FullPrefix" in editData["Current"]:
                        full_prefix = "".join(editData["Current"]["FullPrefix"])
                    full_suffix = editData["Current"]["suffix"]
                    if "FullSuffix" in editData["Current"]:
                        full_suffix = "".join(editData["Current"]["FullSuffix"])
                    self.dataLis.append(
                        {
                            "Spctx": prefix,
                            "FutureCtx": suffix,
                            "ExpectedText": predictText,
                            "Id": id,
                            "current_prefix": full_prefix,
                            "current_suffix": full_suffix,
                        }
                    )
                    futureVersions = editData.get("FutureVersions", [])
                    if futureVersions:
                        self.dataLis[-1]["FutureVersions"] = [futureVersions]
                    self.dataLis[-1]["k-shotAscEditIds"] = asc_edit_ids
        self.__len = len(self.dataLis)

    def __len__(self):
        return self.__len

    def __getitem__(self, idx):
        return self.dataLis[idx]
