# GrACE

This repo provides the code for reproducing the experiments in "[GrACE: Language Models Meet Code Edits](https://dl.acm.org/doi/10.1145/3611643.3616253)" (ESEC/FSE 2023). 

## Reproducing C3PO experiments

### Requirements

Please install the required dependencies by running:

```
pip install -r requirements.txt
```

### Pre-process C3PO dataset

Download the splits_50 dataset as described in the [c3po repository](https://github.com/tech-srl/c3po) and run the following script to get the data in the desired format:
```
python preprocess\run.py --input_dir <SPLITS_50_DATASET_PATH> --output_dir <PATH_TO_C3PO_PROCESSED_DATASET_DIR>
```

### OpenAI Authentication (OPTIONAL, only for evaluating OpenAI models)

Store the OpenAI API Key in .env as follows:
```
OPENAI_API_KEY=<Your key>
```

If you're using Azure OpenAI, additionally add the following to .env:
```
AZURE_OPENAI_API_BASE=<Azure OpenAI API endpoint>
AZURE_OPENAI_API_VERSION=<Azure OpenAI endpoint version>
```

### Evaluating on the C3PO dataset

The paper presents evaluation results with the `code-davinci-002` OpenAI model and finetuned CodeT5 models. 

#### Evaluating code-davinci-002
While the `code-davinci-002` model is no longer available via the OpenAI public API, it can be accessed via Azure OpenAI. Deploy the `code-davinci-002` model on Azure OpenAI and run the following:

To evaluate the model without associated edits
```
python evaluate_c3po.py --input_dir <PATH_TO_C3PO_PROCESSED_DATASET_DIR> --output_dir <PATH_TO_RESULTS_DIR> --splits splits_50.json --api_engine azure_oai --model_name_or_path <NAME_OF_MODEL_DEPLOYED_ON_AZURE>
```

To evaluate the model with spatial associated edits
```
python evaluate_c3po.py --input_dir <PATH_TO_C3PO_PROCESSED_DATASET_DIR> --output_dir <PATH_TO_RESULTS_DIR> --splits splits_50.json --api_engine azure_oai --model_name_or_path <NAME_OF_MODEL_DEPLOYED_ON_AZURE> --use_ascEdits
```

**NOTE:** Please contact us if you'd like to get access to our `code-davinci-002` predictions.

#### Evaluating other OpenAI models
You can also choose to run OpenAI models that are still available via the OpenAI public API by setting `--api_engine oai` in the commands discussed above.

#### Evaluating finetuned CodeT5 models
The paper also presents results with finetuned variants of the `CodeT5` model. We open-source two such models under `aka.ms/GrACE-Code`.
```
python evaluate_c3po.py --input_dir <PATH_TO_C3PO_PROCESSED_DATASET_DIR> --output_dir <PATH_TO_RESULTS_DIR> --splits splits_50.json --api_engine hf --model_name_or_path <PATH_TO_CODET5_WEIGHTS> --use_ascEdits
```

### Training CodeT5 models

To train the model without associated edits
```
accelerate launch --config_file accelerate.yaml train_t5.py --model_name_or_path "Salesforce/codet5-base" --data_path <data_path> --do_eval --do_test --per_device_train_batch_size 8  --per_device_eval_batch_size 16 --learning_rate 3e-4 --preprocessing_num_workers 96 --num_train_epoch 8 --overwrite_output_dir --pad_to_max_length --max_seq_length 1024 --warmup_steps 500 --logging_steps 10000 --save_steps 10000 --seed 42 --report_to tensorboard --use_fast_tokenizer --with_tracking --do_train --ag_tokenizer --no_ascEdits
```

To train the model with associated edits
```
accelerate launch --config_file accelerate.yaml train_t5.py --model_name_or_path "Salesforce/codet5-base" --data_path <data_path> --do_eval --do_test --per_device_train_batch_size 8  --per_device_eval_batch_size 16 --learning_rate 3e-4 --preprocessing_num_workers 96 --num_train_epoch 8 --overwrite_output_dir --pad_to_max_length --max_seq_length 1024 --warmup_steps 500 --logging_steps 10000 --save_steps 10000 --seed 42 --report_to tensorboard --use_fast_tokenizer --with_tracking --do_train --ag_tokenizer 
```

The pretrained codet5 models for the c3po configurations can be found at ``aka.ms/GrACE-Code``

### Few-shot experiments

The paper also discusses how using associated edits differs from few-shot prompting with randomly sampled edits. The results can be reproduced by following these steps:

#### Pre-process the Unfiltered C3PO dataset
```
python preprocess\run.py --input_dir <SPLITS_50_DATASET_PATH> --output_dir <PATH_TO_UNFILTERED_C3PO_PROCESSED_DATASET_DIR> --unfiltered
```

#### Generate the few-shot dataset
```
python preprocess\few_shot.py --filtered_input_dir <PATH_TO_C3PO_PROCESSED_DATASET_DIR> --unfiltered_input_dir <PATH_TO_UNFILTERED_C3PO_PROCESSED_DATASET_DIR> --output_dir <PATH_TO_FEWSHOT_DATASET_DIR> --num_few_shot_samples 2
```

#### Evaluate few-shot performance
For evaluating `code-davinci-002` which is available via an Azure OpenAI endpoint:

***Few-shot samples from the same repo in the filtered dataset***
```
python evaluate_c3po.py --input_dir <PATH_TO_C3PO_PROCESSED_DATASET_DIR> --output_dir <PATH_TO_RESULTS_DIR> --splits splits_50.json --api_engine azure_oai --model_name_or_path <NAME_OF_MODEL_DEPLOYED_ON_AZURE> --use_ascEdits --use_fewshot_base --few_shot_key few_shot_samples_same_repo
```

***Few-shot samples from any repo in the filtered dataset***
```
python evaluate_c3po.py --input_dir <PATH_TO_C3PO_PROCESSED_DATASET_DIR> --output_dir <PATH_TO_RESULTS_DIR> --splits splits_50.json --api_engine azure_oai --model_name_or_path <NAME_OF_MODEL_DEPLOYED_ON_AZURE> --use_ascEdits --use_fewshot_base --few_shot_key few_shot_samples_any_repo
```

***Few-shot samples from the same repo in the unfiltered dataset***
```
python evaluate_c3po.py --input_dir <PATH_TO_C3PO_PROCESSED_DATASET_DIR> --output_dir <PATH_TO_RESULTS_DIR> --splits splits_50.json --api_engine azure_oai --model_name_or_path <NAME_OF_MODEL_DEPLOYED_ON_AZURE> --use_ascEdits --use_fewshot_base --few_shot_key few_shot_samples_same_repo
```

***Few-shot samples from any repo in the unfiltered dataset***
```
python evaluate_c3po.py --input_dir <PATH_TO_C3PO_PROCESSED_DATASET_DIR> --output_dir <PATH_TO_RESULTS_DIR> --splits splits_50.json --api_engine azure_oai --model_name_or_path <NAME_OF_MODEL_DEPLOYED_ON_AZURE> --use_ascEdits --use_fewshot_base --few_shot_key few_shot_samples_any_repo
```

## Tutorial on using our technique in an IDE-based edit suggestions tool

We have created an interactive tutorial on how our technique can be used in an IDE-based edit suggestions tool. Readers can follow the steps below to go through the tutorial:

**Requirements and setup:**

You would need python (3 or above) and Jupyter Notebook support for running the tutorial notebook. Please see https://jupyter.org/install#jupyter-notebook for instructions on installing Jupter Notebook. You would additionally need the `torch` and `tranformers` python modules to run the CodeT5 inference which can be installed by calling

```
pip install -r tutorial_requirements.txt
```

Note that the tutorial also uses utilities from the `src` and `tutorial_utils` directories and loads the CodeT5 model weights. Moving the `tutorial.ipynb` file to some other location could lead to these utlities and files not being discoverable.

**Running the tutorial:**

After ensuring that the requirements are met, launch the `tutorial.ipynb` file. The tutorial content is presented as a series of markdown cells that describe each step and code cells that let you simulate the step (a step here could be identifying the target edit location, processing associated edits, etc.). The notebook can be run multiple times with different examples and the instructions for the same are included in the notebook.

# Citation

If you find our work useful in your research, please consider citing the paper:
```
@inproceedings{10.1145/3611643.3616253,
author = {Gupta, Priyanshu and Khare, Avishree and Bajpai, Yasharth and Chakraborty, Saikat and Gulwani, Sumit and Kanade, Aditya and Radhakrishna, Arjun and Soares, Gustavo and Tiwari, Ashish},
title = {Grace: Language Models Meet Code Edits},
year = {2023},
isbn = {9798400703270},
publisher = {Association for Computing Machinery},
address = {New York, NY, USA},
url = {https://doi.org/10.1145/3611643.3616253},
doi = {10.1145/3611643.3616253},
abstract = {Developers spend a significant amount of time in editing code for a variety of reasons such as bug fixing or adding new features. Designing effective methods to predict code edits has been an active yet challenging area of research due to the diversity of code edits and the difficulty of capturing the developer intent. In this work, we address these challenges by endowing pre-trained large language models (LLMs) with the knowledge of relevant prior associated edits, which we call the Grace (Generation conditioned on Associated Code Edits) method. The generative capability of the LLMs helps address the diversity in code changes and conditioning code generation on prior edits helps capture the latent developer intent. We evaluate two well-known LLMs, codex and CodeT5, in zero-shot and fine-tuning settings respectively. In our experiments with two datasets, Grace boosts the performance of the LLMs significantly, enabling them to generate 29\% and 54\% more correctly edited code in top-1 suggestions relative to the current state-of-the-art symbolic and neural approaches, respectively.},
booktitle = {Proceedings of the 31st ACM Joint European Software Engineering Conference and Symposium on the Foundations of Software Engineering},
pages = {1483–1495},
numpages = {13},
keywords = {Programming language processing, Pre-trained model, Large language models, Associated edits, Code editing},
location = {, San Francisco, CA, USA, },
series = {ESEC/FSE 2023}
}
```
# Contact
For any questions or issues, please submit repository issues or reach us at `priyansgupta@microsoft.com`