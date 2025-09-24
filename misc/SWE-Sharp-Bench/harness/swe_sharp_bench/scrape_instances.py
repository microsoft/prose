import os
import json 
from multiprocessing import Pool
from swebench.collect.get_tasks_pipeline import construct_data_files, split_instances

def read_json_file(file_path):
    with open(file_path, 'r') as file:
        data = json.load(file)
    return data 

def read_jsonl_file(file_path):
    with open(file_path, 'r') as file:
        data = [json.loads(line) for line in file]
    return data

repos = read_json_file("top_csharp_repos.jsonl")
repos = [repo['repo'] for repo in repos]
print(f"Number of existing repos: {len(repos)}")


path_prs = "data/prs/"
path_tasks = "data/tasks/"
max_pulls = 10000
cutoff_date = None 
tokens = os.getenv("GITHUB_TOKENS")
tokens = tokens.split(",")
data_task_lists = split_instances(repos, len(tokens))

data_pooled = [
    {
        "repos": repos,
        "path_prs": path_prs,
        "path_tasks": path_tasks,
        "max_pulls": max_pulls,
        "cutoff_date": cutoff_date,
        "token": token
    }
    for repos, token in zip(data_task_lists, tokens)
]

with Pool(len(tokens)) as pool:
    pool.map(
        construct_data_files,
        data_pooled,
    )