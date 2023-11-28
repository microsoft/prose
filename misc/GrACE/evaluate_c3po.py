from argparse import ArgumentParser
from pathlib import Path
import logging
from tqdm import tqdm
import time
import json
from src.dataset import AssociatedEditsDataset, AssociatedEditsFewShotDataset, AssociatedEditsFewShotAblationDataset
from src.eval_utils import (
    getAvgPerf,
    getOverallPerf,
    getEvalDict,
)
from src.eval_utils import dumpid2pred, dumpOverallresDict
from src.models import OAIInsertModel, HFModel

def predict(
    input_dict,
    model
):
    predDict = {}
    current_prefix = input_dict["Spctx"]
    target = input_dict["ExpectedText"]
    prediction_paths = model.evaluate(current_prefix, input_dict.get("FutureCtx", "\n"))

    # for prediction_path in prediction_paths:
    #     print(prediction_path)
    #     print("---------------------------")
    # print("=====================================")

    predDict["targets"] = [target]
    predDict["topPreds"] = [prediction_paths]

    # if "FutureVersions" in input_dict:
    #     prefix = input_dict["current_prefix"]
    #     suffix = input_dict["current_suffix"]
    #     snippet_prediction_paths = [prefix + prediction_path + suffix for prediction_path in prediction_paths]
    #     predDict["topPreds"] = [snippet_prediction_paths]
    #     predDict["futureVersions"] = input_dict["FutureVersions"]
    
    if "k-shotAscEditIds" in input_dict:
        predDict["k-shotAscEditIds"] = input_dict["k-shotAscEditIds"]

    predDict["promptPrefix"] = current_prefix
    predDict["promptSuffix"] = input_dict.get("FutureCtx", "\n")
    evalDict = getEvalDict(predDict)
    return {"eval": evalDict, "pred": predDict}

def predictOnDataset(
    testDataset,
    model,
):
    id2preds = {}
    LOGGER.info("========================")
    LOGGER.info("Start Prediction")
    for sample in tqdm(testDataset):
        id = sample["Id"]
        id2preds[id] = predict(sample, model)
    LOGGER.info("========================")
    return id2preds

def evaluate(args):
    api_engine = args["api_engine"]
    if api_engine == "hf":
        model = HFModel(model_name_or_path=args["model_name_or_path"], max_seq_length=args["max_tokens"], topK=args["num_generations"])
    else:
        use_azure_oai = api_engine == "azureoai"
        model_args = {
            "data": {
                "engine": args["model_name_or_path"],
                "n": args["num_generations"],
                "max_tokens": args["max_tokens"],
                "temperature": args["temperature"],
                "stop": "</after>"
            }
        }
        print("Model args: ", model_args)
        model = OAIInsertModel(use_azure_oai=use_azure_oai, **model_args)
    input_dir = Path(args["input_dir"])
    output_dir = Path(args["output_dir"])
    dumpFV = args["dumpFV"]
    use_ascEdits = args["use_ascEdits"]
    use_fewshot_base = args["use_fewshot_base"]
    use_fewshot_ablation = args["use_fewshot_ablation"]
    few_shot_key = args["few_shot_key"]
    splits_path = args["splits"]

    print("Using associated edits: ", use_ascEdits)

    with open(splits_path) as f:
        splits = json.load(f)

    test_split = splits["test"]

    output_dir.mkdir(exist_ok=True)
    LOGGER.info("=====================================")
    LOGGER.info("=====================================")
    overallResDict = {}
    timeTaken = []
    for path in input_dir.iterdir():
        if path.suffix == ".json" and path.is_file() and path.stem in test_split:
            if use_fewshot_base:
                print("Using fewshot base")
                testDataset = AssociatedEditsFewShotDataset(
                    json_addr=path,
                    use_asc_edits=use_ascEdits,
                    few_shot_key=few_shot_key,
                )
            elif use_fewshot_ablation:
                print("Using fewshot ablation")
                testDataset = AssociatedEditsFewShotAblationDataset(
                    json_addr=path,
                    use_asc_edits=use_ascEdits,
                    few_shot_key=few_shot_key,
                )
            else:
                print("Using normal dataset")
                testDataset = AssociatedEditsDataset(json_addr=path, use_ascEdits=use_ascEdits)

            log_dir = output_dir / (str(path.stem) + ".csv")
            start_time = time.time()
            id2preds = predictOnDataset(
                testDataset=testDataset,
                model=model,
            )
            timeTaken.append(time.time() - start_time)
            dumpid2pred(id2preds, log_dir, dumpFV=dumpFV)
            avgPerf = getAvgPerf(id2preds)
            overallResDict[path.stem] = {}
            overallResDict[path.stem]["id2pred"] = id2preds
            overallResDict[path.stem]["eval"] = avgPerf
            overallResDict[path.stem]["time"] = timeTaken[-1]
            LOGGER.info("=====================================")
            LOGGER.info(f"Evaluation metrics - EP {path.stem}")
            LOGGER.info("=====================================")
            LOGGER.info(
                "Raw Top-1 Acc || Raw Top-5 Acc || Raw Top-1 ES || Raw Top-5 ES ||"
            )
            LOGGER.info(
                f"     {avgPerf['RawAccTop-1']}     ||     {avgPerf['RawAccTop-5']}     ||"
                + f"     {avgPerf['RawESTop-1']}     ||     {avgPerf['RawESTop-5']}     ||"
            )
            LOGGER.info(f"Time Taken: {round(timeTaken[-1],2)}s")
            LOGGER.info("=====================================")

    log_dir = output_dir / "OverallResults.csv"
    dumpOverallresDict(overallResDict, log_dir)
    overallPerf = getOverallPerf(overallResDict)
    LOGGER.info("=====================================")
    LOGGER.info(f"Evaluation metrics - Overall")
    LOGGER.info("=====================================")
    LOGGER.info("Raw Top-1 Acc || Raw Top-5 Acc || Raw Top-1 ES || Raw Top-5 ES ||")
    LOGGER.info(
        f"     {overallPerf['RawAccTop-1']}     ||     {overallPerf['RawAccTop-5']}     ||\
                {overallPerf['RawESTop-1']}     ||     {overallPerf['RawESTop-5']}     ||"
    )
    LOGGER.info(f"Time Taken: {round(sum(timeTaken),2)}s")
    LOGGER.info("=====================================")

def parse_args():
    parser = ArgumentParser(description="Evaluate Language Models")
    parser.add_argument(
        "--input_dir", type=str, help="Directory containing the input JSONs"
    )
    parser.add_argument("--splits", type=str, help="JSON file containing the train/val/test splits")
    parser.add_argument(
        "--output_dir", type=str, help="Output Directory to dump the precition data."
    )
    parser.add_argument("--use_ascEdits", action="store_true", help="Use Associated Edits in the Prompt")
    parser.add_argument("--dumpFV", action="store_true", help="Also consider Future Versions of the code while evaluating (used for Overwatch evaluations)")
    parser.add_argument("--use_fewshot_base", action="store_true", help="Set to true to run basic Fewshot experiments")
    parser.add_argument("--use_fewshot_ablation", action="store_true", help="Set to true to run Fewshot 2+1 ablations")
    parser.add_argument("--few_shot_key", type=str, default="few_shot_samples_same_repo", help="Few Shot Key for choosing associated edits")
    parser.add_argument("--api_engine", type=str, choices=["oai", "azureoai", "hf"], default="openai", help="Either oai, azureoai or hf for OpenAI, Azure OpenAI or HuggingFace API Engine respectively. Use hf for CodeT5 models.")
    parser.add_argument("--model_name_or_path", type=str, default="text-davinci-003", help="Either name of the OpenAI/HuggingFace model or path to the model directory")
    parser.add_argument("--num_generations", type=int, default=5, help="Number of predictions to generate per sample")
    parser.add_argument("--max_tokens", type=int, default=256, help="Maximum number of tokens in the generation, longer sequences will be truncated")
    parser.add_argument("--temperature", type=float, default=0.1, help="Temperature used for sampling")
    return vars(parser.parse_args())


if __name__ == "__main__":
    args = parse_args()

    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)-15s %(name)-5s %(levelname)-8s %(message)s",
    )
    LOGGER = logging.getLogger(__name__)
    print(evaluate(args))
