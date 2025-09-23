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


## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit [Contributor License Agreements](https://cla.opensource.microsoft.com).

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft
trademarks or logos is subject to and must follow
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.

## License

Code in this GitHub repository is licensed under the [MIT License](LICENSE).

## Contact

For any suggestions or questions, please raise a Github issue or contact us at t-smhatre@microsoft.com

