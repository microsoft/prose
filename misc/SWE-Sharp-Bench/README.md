# SWE-Sharp-Bench 

**The First Software Engineering Benchmark for C#**

SWE-Sharp-Bench is a comprehensive benchmark suite for evaluating software engineering capabilities of AI agents and models on C# and .NET codebases. This benchmark extends the SWE-Bench framework to the C# ecosystem, providing real-world software engineering tasks from popular open-source repositories.

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

## Contact

For any suggestions or questions, please raise a Github issue or contact us at t-smhatre@microsoft.com

