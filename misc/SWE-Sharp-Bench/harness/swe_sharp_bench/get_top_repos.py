import requests
import json

def get_top_csharp_repos(k):
    url = "https://api.github.com/search/repositories"
    headers = {"Accept": "application/vnd.github.v3+json"}
    params = {
        "q": "language:C#",
        "sort": "stars",
        "order": "desc",
        "per_page": k
    }

    response = requests.get(url, headers=headers, params=params)
    if response.status_code == 200:
        return response.json().get("items", [])
    else:
        print(f"Failed to fetch repositories: {response.status_code}")
        return []

def save_repos_to_jsonl(repos, filename):
    with open(filename, "w") as file:
        for repo in repos:
            json.dump(repo, file)
            file.write("\n")

if __name__ == "__main__":
    K = 100  # Number of top repositories to fetch
    output_file = "top_csharp_repos.jsonl"

    top_repos = get_top_csharp_repos(K)
    if top_repos:
        save_repos_to_jsonl(top_repos, output_file)
        print(f"Saved top {K} C# repositories to {output_file}")
    else:
        print("No repositories found.")