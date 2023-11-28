from argparse import ArgumentParser
from copy import deepcopy
import os
import json
import random
import tqdm

def fetch_dataset(dir_path):
    print(f"Fetching dataset from {dir_path}")
    repo_json_dict = {file_name.split(".")[0]: os.path.join(dir_path, file_name) for file_name in os.listdir(dir_path) if file_name.endswith(".json")}

    print(f"Found repos: {list(repo_json_dict.keys())}")
    
    data = {}
    for repo in repo_json_dict:
        with open(repo_json_dict[repo]) as f:
            data[repo] = json.load(f)
    return data

def tag_unfiltered_data_with_filtered_ids(filtered_data, unfiltered_data):
    # Find the mapping between filtered and unfiltered ids
    repos = list(filtered_data.keys())
    
    for repo in repos:
        print(f"Tagging unfiltered data with filtered ids for {repo}")
        
        for sample in tqdm.tqdm(unfiltered_data[repo], desc="Clearing filtered ids"):
            sample["filtered_id"] = None

        for sample in tqdm.tqdm(filtered_data[repo], desc="Matching samples, adding filtered ids"):
            # Find all samples in unfiltered data that match the current sample
            match = [unfiltered_sample for unfiltered_sample in unfiltered_data[repo] if unfiltered_sample["data"][1]["Current"] == sample["data"][1]["Current"]]
            for m in match:
                m["filtered_id"] = sample["id"]

        # Check if there are any duplicates
        # if len(match) > 1:
        #     num_duplicates += 1

def get_random_assoc_edits(data, id_key, target_id, num_samples, seed=42):
    # Get num_samples random samples from the data such that data["id_key"] != target_id
    candidate_samples = [sample for sample in data if sample[id_key] != target_id]

    # When the associated few shot edits are sampled from the unfiltered set, the id_key is "filtered_id"
    sample_source = "filtered" if id_key == "id" else "unfiltered"

    if len(candidate_samples) < num_samples:
        print(f"Warning: Not enough candidate samples to pick {num_samples} samples for {target_id}. Picking {len(candidate_samples)} samples instead.")
        return [{"sample_id": sample["id"], "Assoc_Edit": sample["data"][1]["Current"], "sample_source": sample_source} for sample in candidate_samples]
    
    # Pick num_samples random samples from the candidate samples
    random.seed(seed)
    picked_samples = random.sample(candidate_samples, num_samples)
    return [{"sample_id": sample["id"], "Assoc_Edit": sample["data"][1]["Current"], "sample_source": sample_source} for sample in picked_samples]

def create_and_dump_few_shot_dataset(target_samples_by_repo, few_shot_candidate_samples_by_repo, num_few_shot_samples, candidate_id_key, target_id_key="id", output_dir=None, seed=42):
    target_repos = list(target_samples_by_repo.keys())

    # Collect all candidate samples from all candidate repos
    all_few_shot_candidate_samples = []
    for repo in few_shot_candidate_samples_by_repo:
        all_few_shot_candidate_samples.extend(few_shot_candidate_samples_by_repo[repo])
    
    # Each target sample has num_few_shot_samples such that target_sample[target_id_key] != candidate_sample[candidate_id_key]
    for repo in target_repos:
        print(f"Creating few-shot dataset for {repo}")
        for target_sample in tqdm.tqdm(target_samples_by_repo[repo], desc="Creating few-shot dataset"):
            target_id = target_sample[target_id_key]
            target_sample["data"][1]["few_shot_samples_same_repo"] = get_random_assoc_edits(few_shot_candidate_samples_by_repo[repo], candidate_id_key, target_id, num_few_shot_samples)
            target_sample["data"][1]["few_shot_samples_any_repo"] = get_random_assoc_edits(all_few_shot_candidate_samples, candidate_id_key, target_id, num_few_shot_samples)

        # Dump the data
        if output_dir is not None:
            os.makedirs(output_dir, exist_ok=True)
            with open(os.path.join(output_dir, f"{repo}.json"), "w") as f:
                json.dump(target_samples_by_repo[repo], f)
            print(f"Dumped few-shot dataset for {repo} in {output_dir} with {len(target_samples_by_repo[repo])} samples.")

if __name__ == "__main__":
    parser = ArgumentParser(description="Collect few-shot examples for C3PO.")
    parser.add_argument(
        "--filtered_input_dir", type=str, help="Directory containing the filtered C3PO dataset."
    )
    parser.add_argument(
        "--unfiltered_input_dir", type=str, help="Directory containing the unfiltered C3PO dataset."
    )
    parser.add_argument(
        "--output_dir", type=str, help="Output Directory to dump the processed data in json format."
    )
    parser.add_argument(
        "--num_few_shot_samples", type=int, default=2, help="Number of few-shot samples to collect for each sample."
    )
    parser.add_argument(
        "--seed", type=int, default=42, help="Random seed."
    )
    args = parser.parse_args()

    filtered_input_root = args.filtered_input_dir
    unfiltered_input_root = args.unfiltered_input_dir
    output_root = args.output_dir
    num_few_shot_samples = args.num_few_shot_samples
    seed = args.seed

    filtered_data = fetch_dataset(filtered_input_root)
    unfiltered_data = fetch_dataset(unfiltered_input_root)

    tag_unfiltered_data_with_filtered_ids(filtered_data, unfiltered_data)

    # Get few-shot examples for each filtered sample
    print("Creating few-shot dataset for filtered data")
    filtered_output_root = os.path.join(output_root, "filtered")
    os.makedirs(filtered_output_root, exist_ok=True)
    create_and_dump_few_shot_dataset(filtered_data, filtered_data, num_few_shot_samples=num_few_shot_samples, candidate_id_key="id", output_dir=filtered_output_root, seed=seed)

    # Get few-shot examples for each unfiltered sample
    print("Creating few-shot dataset for unfiltered data")
    unfiltered_output_root = os.path.join(output_root, "unfiltered")
    os.makedirs(unfiltered_output_root, exist_ok=True)
    create_and_dump_few_shot_dataset(filtered_data, unfiltered_data, num_few_shot_samples=num_few_shot_samples, candidate_id_key="filtered_id", output_dir=unfiltered_output_root, seed=seed)
