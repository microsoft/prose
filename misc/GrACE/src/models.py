import copy
import openai
import os
from dotenv import load_dotenv
import time
import torch
from transformers import AutoModelForPreTraining, AutoConfig, AutoTokenizer
import re

load_dotenv()

# Variables used to store OpenAI API Key and other login information in .env
OPENAI_API_KEY_VAR = "OPENAI_API_KEY"
AZURE_OPENAI_API_BASE_VAR = "AZURE_OPENAI_API_BASE"
AZURE_OPENAI_API_VERSION_VAR = "AZURE_OPENAI_API_VERSION"


class OAIInsertModel:
    def __init__(self, use_azure_oai=False, data=None):
        if use_azure_oai:
            print("Using Azure OpenAI API")
            openai.api_type = "azure"
            openai.api_base = os.environ.get(AZURE_OPENAI_API_BASE_VAR)
            openai.api_version = os.environ.get(AZURE_OPENAI_API_VERSION_VAR)
        else:
            print("Using OpenAI API")
        openai.api_key = os.environ.get(OPENAI_API_KEY_VAR)

        self.data = {
            "engine": "code-davinci-002",
            "temperature": 0.1,
            "n": 5,
            "max_tokens": 1024,
            "suffix": "\n",
        }

        if data is not None:
            for key in data:
                self.data[key] = data[key]

    def evaluate(self, prefix, suffix):
        data = copy.deepcopy(self.data)
        # trailing new lines can cause issues with OAI predictions
        data["prompt"] = prefix.strip()
        data["suffix"] = suffix.strip()
       # Retry every 2 second, if rate limitted
        while True:
            try:
                completion = openai.Completion.create(**data)
                break
            except Exception as e:
                print(
                    f"API Request failed with an error {e} . . . Re requesting in 2 seconds . . ."
                )
                time.sleep(2)
                continue
        return [choice.text for choice in completion.choices]


class HFModel:
    def __init__(self, model_name_or_path, max_seq_length=1024, topK=5):
        print("Using HuggingFace Model")
        self.max_seq_length = max_seq_length
        self.topK = topK
        self.model, self.tokenizer = self.get_model_tokenizer_from_path(
            model_name_or_path, self.max_seq_length
        )

    def evaluate(self, prefix, suffix):
        prompt = prefix + " <extra_id_0> " + suffix
        input_ids = self.tokenizer(
            prompt,
            padding="max_length",
            max_length=self.max_seq_length,
            truncation=True,
            return_tensors="pt",
        ).input_ids
        self.model.eval()
        with torch.no_grad():
            output = self.model.generate(
                input_ids,
                num_beams=self.topK,
                max_length=self.max_seq_length,
                num_return_sequences=self.topK,
            )
            output = torch.nn.functional.pad(
                output,
                (0, self.max_seq_length - output.shape[-1]),
                value=self.tokenizer.pad_token_id,
            )

        return [
            self.get_span(pred)
            for pred in self.tokenizer.batch_decode(output, skip_special_tokens=False)
        ]

    def get_span(self, text, sentinelTokenId=0):
        text = " " + text
        for tok in ["<s>", "</s>", "<pad>"]:
            text = text.replace(tok, " ")
        splits = text.split("<extra_id")
        return ">".join(splits[sentinelTokenId + 1].split(">")[1:])

    def get_model_tokenizer_from_path(self, model_path, max_seq_length=1024):
        config = AutoConfig.from_pretrained(model_path)
        print("Loaded config from model path: ", model_path)

        tokenizer_kwargs = {
            "use_fast": True,
            "additional_special_tokens": [
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
            ],
        }
        tokenizer = AutoTokenizer.from_pretrained(model_path, **tokenizer_kwargs)
        print("Loaded tokenizer from model path: ", model_path)

        model = AutoModelForPreTraining.from_pretrained(
            model_path,
            from_tf=bool(".ckpt" in model_path),
            config=config,
        )
        print("Loaded model from model path: ", model_path)
        return model, tokenizer
