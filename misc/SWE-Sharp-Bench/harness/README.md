## SWE-Sharp-Bench

An Extension of SWE-Bench Python to extend support for C# and .NET ecosystem. The extended package contains support to scrape, build and evaluate SWE-Bench like instances for C#.

## Setup

### Prerequisites
- Python 3.8+
- Docker
- Docker Compose (optional)

### Installation

1. **Install dependencies**
   ```bash
   pip install -e .
   ```

2. **Prepare the dataset**
   
   The evaluation system requires dataset instances in JSON format. Convert the CSV dataset to JSON:
   ```bash
   python -m swe_sharp_bench.convert_csv_to_json
   ```
   
   This will create `data/tasks/swe-sharp-bench.json` from the CSV file in `../data/benchmark/swe-sharp-bench.csv`.

## Usage

### Running Evaluations

Use the CLI to run evaluations on C# instances:

```bash
python -m swe_sharp_bench.cli \
    --dataset_name data/tasks/swe-sharp-bench.json \
    --predictions_path gold \
    --max_workers 1 \
    --instance_ids <instance_id> \
    --run_id validate-gold
```

**Example with a real instance:**
```bash
python -m swe_sharp_bench.cli \
    --dataset_name data/tasks/swe-sharp-bench.json \
    --predictions_path gold \
    --max_workers 1 \
    --instance_ids ardalis__cleanarchitecture-546 \
    --run_id validate-gold
```

Add --namespace="" to enable local build.

### Available Options

- `--dataset_name`: Path to the JSON dataset file
- `--predictions_path`: Path to predictions file (use `gold` for gold predictions)
- `--max_workers`: Number of parallel workers (default: 4)
- `--instance_ids`: Space-separated list of instance IDs to evaluate
- `--run_id`: Unique identifier for this evaluation run
- `--timeout`: Timeout in seconds for each instance (default: 1800)
- `--force_rebuild`: Force rebuild of Docker images (default: False)
- `--cache_level`: Cache level - none, base, env, instance (default: env)

### Finding Instance IDs

List available instance IDs from the dataset:
```bash
python -c "import json; data=json.load(open('data/tasks/swe-sharp-bench.json')); [print(item['instance_id']) for item in data[:10]]"
```

### Important Scripts

1 - `get_top_repos.py` - fetch top repositories from GitHub 

2 - `scrape_instances.py` - scrape valid tasks from selected repositories 

3 - `run_instances.py` - filter out valid instances by execution filtering

4 - `convert_csv_to_json.py` - convert CSV dataset to required JSON format

## Dataset

The SWE-Sharp-Bench dataset contains 150 real-world C# software engineering problems from popular open-source repositories. The dataset is available in both CSV format (`../data/benchmark/swe-sharp-bench.csv`) and the converted JSON format required for evaluation (`data/tasks/swe-sharp-bench.json`).
