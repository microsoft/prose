#!/usr/bin/env python
# coding=utf-8
# Copyright 2021 The HuggingFace Team All rights reserved.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.



# Code modified from https://github.com/huggingface/transformers 
"""
Pretraining the library models for T5-like span-masked language modeling on C3PO dataset.

Here is the full list of checkpoints on the hub that can be pretrained by this script:
https://huggingface.co/models?filter=t5
"""
import json
import logging
import math
import os
import random
import sys
from dataclasses import asdict, dataclass, field

# You can also adapt this script on your own masked language modeling task. Pointers for this are left as comments.
from enum import Enum
from typing import Optional

import torch
import transformers
from accelerate import Accelerator, DistributedType
from accelerate.logging import get_logger
from accelerate.utils import set_seed
from torch.utils.data import DataLoader
from tqdm import tqdm
from transformers import (
    CONFIG_MAPPING,
    MODEL_MAPPING,
    Adafactor,
    HfArgumentParser,
    get_scheduler,
)
from src.eval_utils import (
    getEvalDict,
    getAvgPerf,
    dumpid2pred,
    getOverallPerf,
    dumpOverallresDict,
)

from src.dataset import AssociatedEditsDataset
from torch.utils.data import ConcatDataset, Subset
from transformers import AutoModelForPreTraining, AutoConfig, AutoTokenizer
import time

logger = get_logger(__name__)
MODEL_CONFIG_CLASSES = list(MODEL_MAPPING.keys())
MODEL_TYPES = tuple(conf.model_type for conf in MODEL_CONFIG_CLASSES)

AMLT_DATA_DIR = os.environ.get("AMLT_DATA_DIR", ".")
AMLT_OUTPUT_DIR = os.environ.get("AMLT_OUTPUT_DIR", "../outputs")


@dataclass
class TrainingArguments:
    output_dir: str = field(
        default=AMLT_OUTPUT_DIR,
        metadata={
            "help": "The output directory where the model predictions and checkpoints will be written."
        },
    )
    overwrite_output_dir: bool = field(
        default=False,
        metadata={
            "help": (
                "Overwrite the content of the output directory. "
                "Use this to continue training if output_dir points to a checkpoint directory."
            )
        },
    )
    do_train: bool = field(default=False, metadata={"help": "Whether to run training."})
    do_eval: bool = field(
        default=False, metadata={"help": "Whether to run eval on the dev set."}
    )
    do_test: bool = field(
        default=False, metadata={"help": "Whether to run eval on the Test set."}
    )
    per_device_train_batch_size: int = field(
        default=8, metadata={"help": "Batch size per GPU/TPU core/CPU for training."}
    )
    per_device_eval_batch_size: int = field(
        default=8, metadata={"help": "Batch size per GPU/TPU core/CPU for evaluation."}
    )
    per_device_test_batch_size: int = field(
        default=8, metadata={"help": "Batch size per GPU/TPU core/CPU for testing."}
    )
    learning_rate: float = field(
        default=5e-5, metadata={"help": "The initial learning rate for AdamW."}
    )
    gradient_accumulation_steps: int = field(
        default=1,
        metadata={
            "help": "Number of updates steps to accumulate before performing a backward/update pass."
        },
    )
    weight_decay: float = field(
        default=0.0, metadata={"help": "Weight decay for AdamW if we apply some."}
    )
    adam_beta1: float = field(
        default=0.9, metadata={"help": "Beta1 for AdamW optimizer"}
    )
    adam_beta2: float = field(
        default=0.999, metadata={"help": "Beta2 for AdamW optimizer"}
    )
    adam_epsilon: float = field(
        default=1e-8, metadata={"help": "Epsilon for AdamW optimizer."}
    )
    adafactor: bool = field(
        default=False,
        metadata={"help": "Whether or not to replace AdamW by Adafactor."},
    )
    num_train_epochs: float = field(
        default=3.0, metadata={"help": "Total number of training epochs to perform."}
    )
    warmup_steps: int = field(
        default=0, metadata={"help": "Linear warmup over warmup_steps."}
    )
    logging_steps: int = field(
        default=500, metadata={"help": "Log every X updates steps."}
    )
    save_steps: int = field(
        default=500, metadata={"help": "Save checkpoint every X updates steps."}
    )
    eval_steps: int = field(
        default=None, metadata={"help": "Run an evaluation every X steps."}
    )
    seed: int = field(
        default=42,
        metadata={"help": "Random seed that will be set at the beginning of training."},
    )
    with_tracking: bool = field(
        default=True,
        metadata={"help": "Whether to enable experiment trackers for logging."},
    )
    report_to: str = field(
        default="tensorboard", metadata={"help": "The service to report results to."}
    )
    max_train_steps: Optional[int] = field(
        default=None,
        metadata={
            "help": "If set, overrides num_train_epochs to compute the number of training steps."
        },
    )
    lr_scheduler_type: str = field(
        default="linear",
        metadata={
            "help": """
                    The scheduler type to use. It must be one of ['linear', 'cosine', 'cosine_with_restarts',
                    'polynomial', 'constant', 'constant_with_warmup'].
                """
        },
    )
    resume_from_checkpoint: Optional[str] = field(
        default=None,
        metadata={
            "help": "The path to a directory containing model and tokenizer files to resume training from"
        },
    )
    fp16: bool = field(
        default=False,
        metadata={
            "help": "Whether to use 16-bit (mixed) precision (through NVIDIA apex) instead of 32"
        },
    )

    def __post_init__(self):
        if self.output_dir is not None:
            self.output_dir = os.path.expanduser(self.output_dir)
        self.model_dump_dir = os.path.join(self.output_dir, "ModelWeights")

    def to_dict(self):
        """
        Serializes this instance while replace `Enum` by their values (for JSON serialization support). It obfuscates
        the token values by removing their value.
        """
        d = asdict(self)
        for k, v in d.items():
            if isinstance(v, Enum):
                d[k] = v.value
            if isinstance(v, list) and len(v) > 0 and isinstance(v[0], Enum):
                d[k] = [x.value for x in v]
            if k.endswith("_token"):
                d[k] = f"<{k.upper()}>"
        return d


@dataclass
class ModelArguments:
    """
    Arguments pertaining to which model/config/tokenizer we are going to fine-tune, or train from scratch.
    """

    model_name_or_path: Optional[str] = field(
        default=None,
        metadata={
            "help": (
                "The model checkpoint for weights initialization.Don't set if you want to train a model from scratch."
            )
        },
    )
    model_type: Optional[str] = field(
        default=None,
        metadata={
            "help": "If training from scratch, pass a model type from the list: "
            + ", ".join(MODEL_TYPES)
        },
    )
    config_name: Optional[str] = field(
        default=None,
        metadata={
            "help": "Pretrained config name or path if not the same as model_name"
        },
    )
    tokenizer_name: Optional[str] = field(
        default=None,
        metadata={
            "help": "Pretrained tokenizer name or path if not the same as model_name"
        },
    )
    cache_dir: Optional[str] = field(
        default=None,
        metadata={
            "help": "Where do you want to store the pretrained models downloaded from s3"
        },
    )
    use_fast_tokenizer: bool = field(
        default=True,
        metadata={
            "help": "Whether to use one of the fast tokenizer (backed by the tokenizers library) or not."
        },
    )
    dtype: Optional[str] = field(
        default="float32",
        metadata={
            "help": (
                "Floating-point format in which the model weights should be initialized and trained. Choose one of"
                " `[float32, float16, bfloat16]`."
            )
        },
    )
    ag_tokenizer: bool = field(
        default=False, metadata={"help": "Whether to augment tokenizer or not"}
    )


@dataclass
class DataTrainingArguments:
    """
    Arguments pertaining to what data we are going to input our model for training and eval.
    """

    data_path: str = field(default=None, metadata={"help": "The Input Data Path"})
    splits_path: Optional[str] = field(
        default="splits_50.json", metadata={"help": "Train-Dev-Eval Split path"}
    )
    overwrite_cache: bool = field(
        default=False,
        metadata={"help": "Overwrite the cached training and evaluation sets"},
    )
    validation_split_percentage: Optional[float] = field(
        default=5,
        metadata={
            "help": "The percentage of the Eval set used for checkpoint validation"
        },
    )
    max_seq_length: Optional[int] = field(
        default=None,
        metadata={
            "help": (
                "The maximum total input sequence length after tokenization and masking. Sequences longer than this"
                " will be truncated. Default to the max input length of the model."
            )
        },
    )
    pad_to_max_length: bool = field(
        default=False,
        metadata={
            "help": (
                "Whether to pad all samples to `max_seq_length`. "
                "If False, will pad the samples dynamically when batching to the maximum length in the batch."
            )
        },
    )
    preprocessing_num_workers: Optional[int] = field(
        default=None,
        metadata={"help": "The number of processes to use for the preprocessing."},
    )
    predict_complete_sequence: bool = field(
        default=False,
        metadata={
            "help": "Whether or not to predict the complete sequence. If False, only predict the masked tokens."
        },
    )
    use_all: bool = field(
        default=False,
        metadata={
            "help": "Choose whether to use all edits in an edit sequence as target edit or only the one originally intended to be"
        },
    )
    shuffle: bool = field(
        default=False,
        metadata={
            "help": "Shuffle the order in which associated edits appear randomly"
        },
    )
    no_ascEdits: bool = field(default=False)

    def __post_init__(self):
        self.data_path = os.path.join(AMLT_DATA_DIR, self.data_path)


def GetSpan(text, sentinelTokenId=0):
    text = " " + text
    for tok in ["<s>", "</s>", "<pad>"]:
        text = text.replace(tok, " ")
    splits = text.split("<extra_id")
    return ">".join(splits[sentinelTokenId + 1].split(">")[1:])


def batch_generate(
    batch, model, max_seq_length, topK=5, beam_search=False, temperature=0.2
):
    batch_input_ids = batch["input_ids"]
    if beam_search:
        outputs = model.module.generate(
            batch_input_ids,
            num_beams=topK,
            max_length=max_seq_length,
            num_return_sequences=topK,
        )
    else:
        outputs = model.module.generate(
            batch_input_ids,
            do_sample=True,
            max_length=max_seq_length,
            temperature=temperature,
            num_return_sequences=topK,
        )
    return outputs


def get_batch_size(num_data_pts, input_batch_size, num_device):
    # Each device should have atleast 1 batch
    if num_data_pts < num_device:
        return 1
    if num_data_pts / input_batch_size < num_device:
        return num_data_pts // (num_device - 1) if num_device > 1 else num_data_pts
    return input_batch_size


def GetCodeEditDatasetDicts(root_dir, repos, dataset_args: DataTrainingArguments):
    return {
        repo: AssociatedEditsDataset(
            os.path.join(root_dir, repo + ".json"),
            use_all=dataset_args.use_all,
            shuffle=dataset_args.shuffle,
            use_ascEdits=not dataset_args.no_ascEdits,
        )
        for repo in repos
    }


def FlattenDatasetDict(datasetDict):
    return ConcatDataset([v for _, v in datasetDict.items()])


def TestAndDumpResults(
    accelerator,
    model,
    tokenizer,
    dataLoaderDict,
    max_seq_length,
    dump_path,
    topK=5,
    beam_search=False,
    temperature=0.2,
):
    idx2idmap = {}
    idx2prefixmap = {}
    idx2suffixmap = {}
    idx2fvmap = None
    os.makedirs(dump_path, exist_ok=True)
    for k, dataloader in dataLoaderDict.items():
        idx2idmap[k] = [id for batch in dataloader for id in batch["id"]]
        idx2prefixmap[k] = [
            prefix for batch in dataloader for prefix in batch["prefix"]
        ]
        idx2suffixmap[k] = [
            suffix for batch in dataloader for suffix in batch["suffix"]
        ]
        idx2fvmapK = [
            fv for batch in dataloader for fv in batch.get("futureVersions", [])
        ]

        if len(idx2fvmapK) > 0:
            if idx2fvmap is None:
                idx2fvmap = dict()
            idx2fvmap[k] = idx2fvmapK

    for k in dataLoaderDict:
        dataLoaderDict[k] = accelerator.prepare(dataLoaderDict[k])
     
        accelerator.wait_for_everyone()
    if accelerator.is_main_process:
        logger.info({k: len(v) for k, v in dataLoaderDict.items()})
    overallResDict = {}
    timeTaken = []
    model.eval()
    for k, dataloader in dataLoaderDict.items():
        start_time = time.time()
        predDict = {"topPreds": [], "targets": []}
        progress_bar = tqdm(
            range(len(dataloader)),
            disable=not accelerator.is_local_main_process,
            desc=f"Evaluating {k}",
        )
        for step, batch in enumerate(dataloader):
            with torch.no_grad():
                outputs = batch_generate(
                    batch, model, max_seq_length, topK, beam_search, temperature
                )
                labels = batch["labels"]
                labels[labels == -100] = tokenizer.pad_token_id
                outputs = torch.nn.functional.pad(
                    outputs,
                    (0, max_seq_length - outputs.shape[-1]),
                    value=tokenizer.pad_token_id,
                )

            predDict["topPreds"].extend(
                [
                    GetSpan(pred)
                    for pred in tokenizer.batch_decode(
                        accelerator.gather(outputs), skip_special_tokens=False
                    )
                ]
            )
            predDict["targets"].extend(
                [
                    GetSpan(pred)
                    for pred in tokenizer.batch_decode(
                        accelerator.gather(labels), skip_special_tokens=False
                    )
                ]
            )

            progress_bar.update(1)
        timeTaken.append(time.time() - start_time)
        if accelerator.is_main_process:
            # Create json for eval:

            print(f"{accelerator.process_index}: Decoding Preds")

            predDict["topPreds"] = [
                predDict["topPreds"][i * topK : i * topK + topK]
                for i in range(len(predDict["topPreds"]) // topK)
            ]
            predDict["topPreds"] = [
                [prefix + pred + suffix for pred in preds]
                for prefix, suffix, preds in zip(
                    idx2prefixmap[k], idx2suffixmap[k], predDict["topPreds"]
                )
            ]
            predDict["targets"] = [
                prefix + tgt + suffix
                for prefix, suffix, tgt in zip(
                    idx2prefixmap[k], idx2suffixmap[k], predDict["targets"]
                )
            ]
            logger.info(f"{accelerator.process_index}: Decoding Labels")
            dumpFV = False
            if idx2fvmap is not None:
                iwPredDicts = [
                    {"topPreds": [tp], "targets": [tgt], "futureVersions": fv}
                    for tp, tgt, fv in zip(
                        predDict["topPreds"], predDict["targets"], idx2fvmap[k]
                    )
                ]
                dumpFV = True
            else:
                iwPredDicts = [
                    {"topPreds": [tp], "targets": [tgt]}
                    for tp, tgt in zip(predDict["topPreds"], predDict["targets"])
                ]
            iwEvalDicts = [getEvalDict(iwPredDict) for iwPredDict in iwPredDicts]
            resDict = {
                id: {"eval": evalDict, "pred": predDict}
                for id, evalDict, predDict in zip(
                    idx2idmap[k], iwEvalDicts, iwPredDicts
                )
            }
            avgPerf = getAvgPerf(resDict, dumpFV)
            print(avgPerf)
            logger.info(f"{accelerator.process_index}: Dumping Preds")
            dumpid2pred(resDict, os.path.join(dump_path, f"Results-{k}.csv"), dumpFV)
            overallResDict[k] = {}
            overallResDict[k]["id2pred"] = resDict
            overallResDict[k]["eval"] = avgPerf
            overallResDict[k]["time"] = timeTaken[-1]

        accelerator.wait_for_everyone()

    if accelerator.is_main_process:
        log_dir = os.path.join(dump_path, "OverallResults.csv")
        dumpOverallresDict(overallResDict, log_dir, dumpFV)
        for k, v in overallResDict.items():
            avgPerf = v["eval"]
            logger.info("=====================================")
            logger.info(f"Evaluation metrics - Repository: {k}")
            logger.info("=====================================")
            if avgPerf.get("FVAccTop-1", None):
                logger.info(
                    "Raw Top-1 Acc || Raw Top-5 Acc || FV Top-1 Acc || FV Top-5 Acc ||"
                )
                logger.info(
                    f"     {avgPerf['RawAccTop-1']}     ||     {avgPerf['RawAccTop-5']}     ||"
                    + f"     {avgPerf['FVAccTop-1']}     ||     {avgPerf['FVAccTop-5']}     ||"
                )
            else:
                logger.info("Raw Top-1 Acc || Raw Top-5 Acc ||")
                logger.info(
                    f"     {avgPerf['RawAccTop-1']}     ||     {avgPerf['RawAccTop-5']}     ||"
    
                )
            logger.info(f"Time Taken: {round(v['time'],2)}s")
            logger.info("=====================================")
        overallPerf = getOverallPerf(overallResDict, dumpFV)
        logger.info("=====================================")
        logger.info(f"Evaluation metrics - Overall")
        logger.info("=====================================")
        if dumpFV:
            logger.info(
                "Raw Top-1 Acc || Raw Top-5 Acc || FV Top-1 Acc || FV Top-5 Acc ||"
            )
            logger.info(
                f"     {overallPerf['RawAccTop-1']}     ||     {overallPerf['RawAccTop-5']}     ||\
                        {overallPerf['FVAccTop-1']}     ||     {overallPerf['FVAccTop-5']}     ||"
            )
        else:
            logger.info("Raw Top-1 Acc || Raw Top-5 Acc ||")
            logger.info(
                f"     {overallPerf['RawAccTop-1']}     ||     {overallPerf['RawAccTop-5']}     ||"
            )
        logger.info(f"Time Taken: {round(sum(timeTaken),2)}s")
        logger.info("=====================================")


def dump_model(accelerator, model, tokenizer, dump_dir):
    if accelerator.is_main_process:
        os.makedirs(dump_dir, exist_ok=True)
    accelerator.wait_for_everyone()
    unwrapped_model = accelerator.unwrap_model(model)
    unwrapped_model.save_pretrained(
        dump_dir,
        is_main_process=accelerator.is_main_process,
        save_function=accelerator.save,
    )
    if accelerator.is_main_process:
        tokenizer.save_pretrained(dump_dir)
        for filenm in os.listdir("configs/"):
            if filenm.startswith("codeT5"):
                path = os.path.join("configs", filenm)
                configDict = json.load(open(path, "r"))
                configDict["model_config"]["engine"] = dump_dir
                json.dump(configDict, open(path, "w"))


def main():
    parser = HfArgumentParser(
        (ModelArguments, DataTrainingArguments, TrainingArguments)
    )
    if len(sys.argv) == 2 and sys.argv[1].endswith(".json"):
        # If we pass only one argument to the script and it's the path to a json file,
        # let's parse it to get our arguments.
        model_args, data_args, training_args = parser.parse_json_file(
            json_file=os.path.abspath(sys.argv[1])
        )
    else:
        model_args, data_args, training_args = parser.parse_args_into_dataclasses()

    # Initialize the accelerator. We will let the accelerator handle device placement for us in this example.
    # If we're using tracking, we also need to initialize it here and it will by default pick up all supported trackers
    # in the environment
    accelerator_log_kwargs = {}

    if training_args.with_tracking:
        accelerator_log_kwargs["log_with"] = training_args.report_to
        accelerator_log_kwargs["logging_dir"] = training_args.output_dir

    accelerator = Accelerator(
        gradient_accumulation_steps=training_args.gradient_accumulation_steps,
        **accelerator_log_kwargs,
    )

    # Make one log on every process with the configuration for debugging.
    logging.basicConfig(
        format="%(asctime)s - %(levelname)s - %(name)s - %(message)s",
        datefmt="%m/%d/%Y %H:%M:%S",
        level=logging.INFO,
    )
    logger.info(accelerator.state, main_process_only=False)
    if accelerator.is_local_main_process:
        transformers.utils.logging.set_verbosity_info()
    else:
        transformers.utils.logging.set_verbosity_error()

    # If passed along, set the training seed now.
    if training_args.seed is not None:
        set_seed(training_args.seed)

    # Handle the repository creation
    if accelerator.is_main_process:
        log_path = os.path.join(training_args.output_dir, "t5_trainer")
        if training_args.output_dir is not None:
            os.makedirs(log_path, exist_ok=True)
        logger.info("In Main Process!!")
    accelerator.wait_for_everyone()

    # Load pretrained model and tokenizer
    #
    # Distributed training:
    # The .from_pretrained methods guarantee that only one local process can concurrently
    # download model & vocab.
    config_kwargs = {"cache_dir": model_args.cache_dir}
    if model_args.config_name:
        config = AutoConfig.from_pretrained(model_args.config_name, **config_kwargs)
    elif model_args.model_name_or_path:
        config = AutoConfig.from_pretrained(
            model_args.model_name_or_path, **config_kwargs
        )
    else:
        config = CONFIG_MAPPING[model_args.model_type]()
        logger.warning("You are instantiating a new config instance from scratch.")
        if model_args.config_overrides is not None:
            logger.info(f"Overriding config: {model_args.config_overrides}")
            config.update_from_string(model_args.config_overrides)
            logger.info(f"New config: {config}")

    tokenizer_kwargs = {
        "cache_dir": model_args.cache_dir,
        "use_fast": model_args.use_fast_tokenizer,
    }
    if model_args.ag_tokenizer:
        # Add special tokens
        tokenizer_kwargs["additional_special_tokens"] = tokenizer_kwargs.get(
            "additional_special_tokens", []
        ) + [
            "<before>",
            "</before>",
            "<prefix>",
            "</prefix>",
            "<suffix>",
            "</suffix>",
            "<after>",
            "</after>",
            "<edit>",
            "</edit>",
            "<ctxEdits>",
            "</ctxEdits>",
        ]
    if model_args.tokenizer_name:
        tokenizer = AutoTokenizer.from_pretrained(
            model_args.tokenizer_name, **tokenizer_kwargs
        )
    elif model_args.model_name_or_path:
        tokenizer = AutoTokenizer.from_pretrained(
            model_args.model_name_or_path, **tokenizer_kwargs
        )
    else:
        raise ValueError(
            "You are instantiating a new tokenizer from scratch. This is not supported by this script."
            "You can do it from another script, save it, and load it from here, using --tokenizer_name."
        )

    if model_args.model_name_or_path:
        model = AutoModelForPreTraining.from_pretrained(
            model_args.model_name_or_path,
            from_tf=bool(".ckpt" in model_args.model_name_or_path),
            config=config,
            cache_dir=model_args.cache_dir,
        )
    else:
        logger.info("Training new model from scratch")
        model = AutoModelForPreTraining.from_config(config)

    # We resize the embeddings only when necessary to avoid index errors. If you are creating a model from scratch
    # on a small vocab and want a smaller embedding size, remove this test.
    embedding_size = model.get_input_embeddings().weight.shape[0]
    if len(tokenizer) > embedding_size:
        model.resize_token_embeddings(len(tokenizer))
    with open(data_args.splits_path, "r") as fp:
        splits = json.load(fp)
        train_repos = splits["train"]
        eval_repos = splits["dev"]
        test_repos = splits["test"]

    def data_collator(batch):
        def process_item(item):
            prompt = item["Spctx"] + " <extra_id_0> " + item["FutureCtx"]
            label = f"<extra_id_0> {item['ExpectedText']} <extra_id_1>"
            ret_item = {"prompt": prompt, "labels": label, "id": item["Id"]}
            if item.get("FutureVersions", None):
                ret_item["futureVersions"] = item["FutureVersions"]
            if item.get("current_prefix", None):
                ret_item["prefix"] = item["current_prefix"]
            else:
                ret_item["prefix"] = item["Spctx"]
            if item.get("current_suffix", None):
                ret_item["suffix"] = item["current_suffix"]
            else:
                ret_item["suffix"] = item["FutureCtx"]
            return ret_item

        processed_batch = [process_item(item) for item in batch]
        batch_dict = {
            key: [item[key] for item in processed_batch] for key in processed_batch[0]
        }
        tokenized_batch_dict = {
            k: tokenizer(
                batch_dict[k],
                padding="max_length" if data_args.pad_to_max_length else "longest",
                max_length=data_args.max_seq_length,
                truncation=True,
                return_tensors="pt",
            )
            for k in ["prompt", "labels"]
        }
        tokenized_batch_dict["labels"].input_ids[
            tokenized_batch_dict["labels"].input_ids == tokenizer.pad_token_id
        ] = -100

        retDict = {
            "input_ids": tokenized_batch_dict["prompt"].input_ids,
            "attention_mask": tokenized_batch_dict["prompt"].attention_mask,
            "labels": tokenized_batch_dict["labels"].input_ids,
            "id": batch_dict["id"],
        }
        if batch_dict.get("futureVersions", None):
            retDict["futureVersions"] = batch_dict["futureVersions"]
        if batch_dict.get("prefix", None):
            retDict["prefix"] = batch_dict["prefix"]
        if batch_dict.get("suffix", None):
            retDict["suffix"] = batch_dict["suffix"]
        return retDict

    # Optimizer
    # Split weights in two groups, one with weight decay and the other not.
    no_decay = ["bias", "LayerNorm.weight"]
    optimizer_grouped_parameters = [
        {
            "params": [
                p
                for n, p in model.named_parameters()
                if not any(nd in n for nd in no_decay)
            ],
            "weight_decay": training_args.weight_decay,
        },
        {
            "params": [
                p
                for n, p in model.named_parameters()
                if any(nd in n for nd in no_decay)
            ],
            "weight_decay": 0.0,
        },
    ]
    optimizer = Adafactor(
        optimizer_grouped_parameters,
        lr=training_args.learning_rate,
        clip_threshold=1.0,
        scale_parameter=False,
        relative_step=False,
    )

    # Note -> the training dataloader needs to be prepared before we grab his length below (cause its length will be
    # shorter in multiprocess)

    # Scheduler and math around the number of training steps.

    # Prepare everything with our `accelerator`.
    model, optimizer = accelerator.prepare(model, optimizer)
    print(f"Dist. Models {accelerator.process_index}")
    accelerator.wait_for_everyone()
    # On TPU, the tie weights in our model have been disconnected, so we need to restore the ties.
    if accelerator.distributed_type == DistributedType.TPU:
        model.tie_weights()

    if training_args.do_train:
        # DataLoaders creation:
        train_dataset = FlattenDatasetDict(
            GetCodeEditDatasetDicts(data_args.data_path, train_repos, data_args)
        )
        if len(eval_repos) != 0:
            eval_dataset = FlattenDatasetDict(
                GetCodeEditDatasetDicts(data_args.data_path, eval_repos, data_args)
            )
            all_idx = [i for i in range(len(eval_dataset))]
            random.shuffle(all_idx)
            eval_dataset = Subset(
                eval_dataset,
                all_idx[
                    : int(
                        len(eval_dataset) * data_args.validation_split_percentage / 100
                    )
                ],
            )
        else:
            eval_num = int(
                len(train_dataset) * data_args.validation_split_percentage * 0.01
            )
            train_dataset, eval_dataset = torch.utils.data.random_split(
                train_dataset, [len(train_dataset) - eval_num, eval_num]
            )
        train_dataloader = DataLoader(
            train_dataset,
            shuffle=True,
            collate_fn=data_collator,
            batch_size=training_args.per_device_train_batch_size,
        )
        eval_dataloader = DataLoader(
            eval_dataset,
            shuffle=True,
            collate_fn=data_collator,
            batch_size=training_args.per_device_train_batch_size * 2,
        )
        overrode_max_train_steps = False
        num_update_steps_per_epoch = math.ceil(
            len(train_dataloader) / training_args.gradient_accumulation_steps
        )
        if training_args.max_train_steps is None:
            training_args.max_train_steps = (
                training_args.num_train_epochs * num_update_steps_per_epoch
            )
            overrode_max_train_steps = True

        lr_scheduler = get_scheduler(
            name=training_args.lr_scheduler_type,
            optimizer=optimizer,
            num_warmup_steps=training_args.warmup_steps
            * training_args.gradient_accumulation_steps,
            num_training_steps=training_args.max_train_steps
            * training_args.gradient_accumulation_steps,
        )

        train_dataloader, eval_dataloader, lr_scheduler = accelerator.prepare(
            train_dataloader, eval_dataloader, lr_scheduler
        )

        # We need to recalculate our total training steps as the size of the training dataloader may have changed.
        num_update_steps_per_epoch = math.ceil(
            len(train_dataloader) / training_args.gradient_accumulation_steps
        )
        if overrode_max_train_steps:
            training_args.max_train_steps = (
                training_args.num_train_epochs * num_update_steps_per_epoch
            )
        # Afterwards we recalculate our number of training epochs
        training_args.num_train_epochs = math.ceil(
            training_args.max_train_steps / num_update_steps_per_epoch
        )

        # Figure out how many steps we should save the Accelerator states
        checkpointing_steps = training_args.save_steps

        # We need to initialize the trackers we use, and also store our configuration.
        # The trackers initializes automatically on the main process.
        if training_args.with_tracking:
            experiment_config = vars(training_args)
            accelerator.init_trackers("t5_trainer", experiment_config)

        def RunEvaluation():
            model.eval()
            losses = []

            for step, batch in enumerate(eval_dataloader):
                with torch.no_grad():
                    outputs = model(
                        input_ids=batch["input_ids"],
                        attention_mask=batch["attention_mask"],
                        labels=batch["labels"],
                    )

                loss = outputs.loss

                losses.append(
                    accelerator.gather_for_metrics(
                        loss.repeat(batch["input_ids"].shape[0])
                    )
                )
            losses = torch.cat(losses)
            try:
                eval_loss = torch.mean(losses)
                perplexity = math.exp(eval_loss)
            except OverflowError:
                perplexity = float("inf")
            model.train()
            return eval_loss, perplexity

        # Train!
        total_batch_size = (
            training_args.per_device_train_batch_size
            * accelerator.num_processes
            * training_args.gradient_accumulation_steps
        )

        logger.info("***** Running training *****")
        logger.info(f"  Num examples = {len(train_dataset)}")
        logger.info(f"  Num Epochs = {training_args.num_train_epochs}")
        logger.info(
            f"  Instantaneous batch size per device = {training_args.per_device_train_batch_size}"
        )
        logger.info(
            f"  Total train batch size (w. parallel, distributed & accumulation) = {total_batch_size}"
        )
        logger.info(
            f"  Gradient Accumulation steps = {training_args.gradient_accumulation_steps}"
        )
        logger.info(f"  Total optimization steps = {training_args.max_train_steps}")
        # Only show the progress bar once on each machine.
        progress_bar = tqdm(
            range(int(training_args.max_train_steps)),
            disable=not accelerator.is_local_main_process,
        )
        completed_steps = 0
        starting_epoch = 0

        # Potentially load in the weights and states from a previous save
        if training_args.resume_from_checkpoint:
            if (
                training_args.resume_from_checkpoint is not None
                or training_args.resume_from_checkpoint != ""
            ):
                accelerator.print(
                    f"Resumed from checkpoint: {training_args.resume_from_checkpoint}"
                )
                accelerator.load_state(training_args.resume_from_checkpoint)
                path = os.path.basename(training_args.resume_from_checkpoint)
            else:
                # Get the most recent checkpoint
                dirs = [f.name for f in os.scandir(os.getcwd()) if f.is_dir()]
                dirs.sort(key=os.path.getctime)
                path = dirs[
                    -1
                ]  # Sorts folders by date modified, most recent checkpoint is the last
            # Extract `epoch_{i}` or `step_{i}`
            training_difference = os.path.splitext(path)[0]

            if "epoch" in training_difference:
                starting_epoch = int(training_difference.replace("epoch_", "")) + 1
                resume_step = None
            else:
                # need to multiply `gradient_accumulation_steps` to reflect real steps
                resume_step = (
                    int(training_difference.replace("step_", ""))
                    * training_args.gradient_accumulation_steps
                )
                starting_epoch = resume_step // len(train_dataloader)
                resume_step -= starting_epoch * len(train_dataloader)

        # update the progress_bar if load from checkpoint
        progress_bar.update(starting_epoch * num_update_steps_per_epoch)
        completed_steps = starting_epoch * num_update_steps_per_epoch
        best_eval_perplexity = float("inf")
        best_step = 0
        for epoch in range(starting_epoch, training_args.num_train_epochs):
            
            model.train()
            if training_args.with_tracking:
                total_loss = 0
            for step, batch in enumerate(train_dataloader):
                
                # We need to skip steps until we reach the resumed step
                if training_args.resume_from_checkpoint and epoch == starting_epoch:
                    if resume_step is not None and step < resume_step:
                        if step % training_args.gradient_accumulation_steps == 0:
                            progress_bar.update(1)
                            completed_steps += 1
                        continue

                with accelerator.accumulate(model):
                    outputs = model(
                        input_ids=batch["input_ids"],
                        attention_mask=batch["attention_mask"],
                        labels=batch["labels"],
                    )
                    loss = outputs.loss
                    # We keep track of the loss at each epoch
                    if training_args.with_tracking:
                        total_loss += loss.detach().float()
                    accelerator.backward(loss)
                    optimizer.step()
                    lr_scheduler.step()
                    optimizer.zero_grad()

                # Checks if the accelerator has performed an optimization step behind the scenes
                if accelerator.sync_gradients:
                    progress_bar.update(1)
                    completed_steps += 1

                if isinstance(checkpointing_steps, int):
                    if completed_steps % checkpointing_steps == 0:
                        output_dir = f"step_{completed_steps }"
                        if training_args.output_dir is not None:
                            output_dir = os.path.join(
                                training_args.output_dir, output_dir
                            )
                        accelerator.save_state(output_dir)
                        eval_loss, perplexity = RunEvaluation()
                        logger.info(f"Step {completed_steps} perplexity: {perplexity}")
                        if training_args.with_tracking:
                            accelerator.log(
                                {
                                    "perplexity": perplexity,
                                    "eval_loss": eval_loss,
                                },
                                step=completed_steps,
                            )
                     # Best model selection
                        if best_eval_perplexity > perplexity:
                            logger.info("Found Best perplexity, dumping model weights")
                            dump_model(
                                accelerator,
                                model,
                                tokenizer,
                                training_args.model_dump_dir,
                            )
                            best_step = completed_steps
                            best_eval_perplexity = perplexity

                if (
                    completed_steps % training_args.logging_steps == 0
                    and completed_steps > 0
                    and step > 0
                ):
                    accelerator.log(
                        {
                            "train_loss": total_loss.item() / step,
                        },
                        step=completed_steps,
                    )

                if completed_steps >= training_args.max_train_steps:
                    break

            eval_loss, perplexity = RunEvaluation()
            logger.info(f"epoch {epoch}: perplexity: {perplexity}")

            if training_args.with_tracking:
                accelerator.log(
                    {
                        "perplexity": perplexity,
                        "eval_loss": eval_loss,
                        "epoch_train_loss": total_loss.item() / len(train_dataloader),
                    },
                    step=completed_steps,
                )

            output_dir = f"epoch_{epoch}"
            if training_args.output_dir is not None:
                output_dir = os.path.join(training_args.output_dir, output_dir)
        if best_step != 0:
            accelerator.load_state(
                os.path.join(training_args.output_dir, f"step_{best_step}")
            )
        elif not os.path.exists(training_args.model_dump_dir):
            dump_model(accelerator, model, tokenizer, training_args.model_dump_dir)

        if training_args.with_tracking:
            accelerator.end_training()

    accelerator.wait_for_everyone()
    if training_args.do_eval:
        eval_datasets = GetCodeEditDatasetDicts(
            data_args.data_path, eval_repos, data_args
        )
        eval_dataloaders = {
            k: DataLoader(
                v,
                collate_fn=data_collator,
                batch_size=get_batch_size(
                    len(v),
                    training_args.per_device_eval_batch_size,
                    accelerator.num_processes,
                ),
            )
            for k, v in eval_datasets.items()
        }
        TestAndDumpResults(
            accelerator,
            model,
            tokenizer,
            eval_dataloaders,
            data_args.max_seq_length,
            os.path.join(training_args.output_dir, "DevEvaluationBeamSearch"),
            5,  # Beam width 5
            True, # Using Beam search
        )

    if training_args.do_test:
        logger.info(f"Evaluating with Beam Search")
        test_datasets = GetCodeEditDatasetDicts(
            data_args.data_path, test_repos, data_args
        )
        test_dataloaders = {
            k: DataLoader(
                v,
                collate_fn=data_collator,
                batch_size=get_batch_size(
                    len(v),
                    training_args.per_device_eval_batch_size,
                    accelerator.num_processes,
                ),
            )
            for k, v in test_datasets.items()
        }

        TestAndDumpResults(
            accelerator,
            model,
            tokenizer,
            test_dataloaders,
            data_args.max_seq_length,
            os.path.join(training_args.output_dir, "TestEvaluationBeamSearch"),
            5,
            True,
        )


if __name__ == "__main__":
    main()
