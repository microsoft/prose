# SWE-Sharp-Bench 

**The First Software Engineering Benchmark for C#**

SWE-Sharp-Bench is a comprehensive benchmark suite for evaluating software engineering capabilities of AI agents and models on C# and .NET codebases. This benchmark extends the SWE-Bench framework to the C# ecosystem, providing real-world software engineering tasks from popular open-source repositories.

## Leaderboard

| Model | Resolution Rate | Evaluation Date |
|-------|-----------------|-----------------|
| OpenHands + GPT-5 | 47.3 | 2025-08-31 |
| SWE-Agent + Claude Sonnet 4 | 44.7 | 2025-08-31 |
| SWE-Agent + GPT-5 | 43.3 | 2025-08-31 |
| OpenHands + Claude Sonnet 4 | 40.6 | 2025-08-31 |
| OpenHands + o3 | 35.0 | 2025-08-31 |
| SWE-Agent + o3 | 33.3 | 2025-08-31 |
| SWE-Agent + Claude Sonnet 3.7 | 31.3 | 2025-08-31 |
| OpenHands + Claude Sonnet 3.7 | 31.3 | 2025-08-31 |
| OpenHands + o4-mini | 26.0 | 2025-08-31 |
| SWE-Agent + o4-mini | 25.3 | 2025-08-31 |
| OpenHands + GPT-4.1 | 23.3 | 2025-08-31 |
| OpenHands + Claude Sonnet 3.5 | 22.6 | 2025-08-31 |
| SWE-Agent + GPT-4.1 | 22.0 | 2025-08-31 |
| SWE-Agent + Claude Sonnet 3.5 | 20.0 | 2025-08-31 |
| SWE-Agent + o3-mini | 19.3 | 2025-08-31 |
| OpenHands + o3-mini | 19.3 | 2025-08-31 |
| SWE-Agent + GPT-4o | 11.3 | 2025-08-31 |
| OpenHands + GPT-4o | 8.0 | 2025-08-31 |

### Repository Structure

#### `data/`
- **`benchmark/`** - Contains the core benchmark files and datasets used for evaluation
- **`supplementary_data/`** - Additional supporting data and resources

#### `experiments/`
- **`analysis/`** - Helper scripts and tools for analyzing benchmark results and performance metrics
- **`evaluation/`** - Agent runs and evaluation results, organized by date and model type
  - Contains subdirectories for different AI agents (OpenHands, SWE-Agent) tested with various models:
    - GPT-4 variants (gpt-4-1, gpt-4o)
    - GPT-5 and O-series models (gpt-5, o3, o4-mini)
    - Claude Sonnet models (sonnet-3-5, sonnet-3-7, sonnet-4)
  - Each subdirectory contains the complete run logs, outputs, and evaluation metrics for that specific agent-model combination

#### `harness/`
The benchmark harness and core evaluation infrastructure. See the [harness README](./harness/README.md) for detailed information about:
- Repository scraping and instance collection
- Task filtering and validation
- Benchmark execution and evaluation procedures

## License

Code in this GitHub repository is licensed under the [MIT License](LICENSE).

## Leaderboard Submissions

To submit your results to the SWE-Sharp-Bench leaderboard, please follow these instructions carefully to ensure your submission is properly evaluated and merged:

### Submission Process

1. **Fork this repository**
2. **Create a submission folder** under `experiments/evaluation/` with the format: `<date>_<agent/system>_<model>` (e.g., `20241120_myagent_gpt4`)
3. **Provide the required assets** within your submission folder (see below)
4. **Create a pull request** to this repository with your new submission folder

### 📋 Required Assets

Within your submission folder (`experiments/evaluation/<date>_<agent>_<model>/`), please provide the following:

#### Core Results
- **`results.json`**: Aggregated evaluation results and metrics
- **`metadata.yaml`**: Model and system information (see requirements below)
- **`README.md`**: Documentation of your approach and methodology

#### Per-Instance Evaluation Data
For each task instance (e.g., `ardalis__cleanarchitecture-530/`):
- **`eval_report.json`**: Summary of evaluation outcomes for this instance
- **`source_metadata.json`**: Instance metadata and configuration
- **`trajectories/`**: Reasoning traces showing how your system solved the task
- **`logs/`**: Complete evaluation logs and artifacts

### Metadata Requirements

Your `metadata.yaml` should include:

```yaml
# Model Information
model_name: "gpt-4-turbo"  # The specific model used
model_version: "2024-04-09"  # Model version or date
agent_system: "swe-agent"  # Your agent/system name
agent_version: "1.0.0"  # Version of your agent

# Evaluation Details
evaluation_date: "2024-11-20"
total_instances: 150  # Number of instances evaluated
resolved_instances: 45  # Number successfully resolved
resolution_rate: 0.30  # Success rate (resolved/total)

# System Configuration
temperature: 0.0
max_tokens: 8000
timeout_minutes: 30

# Additional Info
contact: "your-email@example.com"
paper_url: "https://arxiv.org/abs/your-paper"  # Optional
code_url: "https://github.com/your/repo"  # Optional
```

### Validation

Before submitting, ensure:
- [ ] All required files are present for each evaluated instance
- [ ] `metadata.yaml` contains all required fields
- [ ] Results are consistent between `results.json` and per-instance reports
- [ ] Trajectories are provided for resolved instances
- [ ] No sensitive information (API keys, etc.) is included


## Contact

For any suggestions or questions, please raise a Github issue or contact us at t-smhatre@microsoft.com

