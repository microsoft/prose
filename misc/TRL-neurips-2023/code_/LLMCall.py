import openai
import json
import time
import re
import os
from typing import List, Dict
from openai.embeddings_utils import get_embedding

DEFAULT_KEY = os.env("OPENAI_API_KEY")


def set_openai_key_attribute(open_api_key: str):
    openai.api_type = "open_ai"
    openai.api_base = "https://api.openai.com/v1"
    openai.api_key = open_api_key
    openai.api_version = None


def openapi_call_completions(prompt: str, modelName="text-davinci-003", temp=0.7, maxTok=500, num_n=1, open_api_key: str = DEFAULT_KEY):

    set_openai_key_attribute(open_api_key)
    max_attempts = 100000
    attempt = 1

    while attempt <= max_attempts:
        try:
            response = openai.Completion.create(
                model=modelName,
                prompt=prompt,
                temperature=temp,
                max_tokens=maxTok,
                top_p=1,
                frequency_penalty=0,
                presence_penalty=0,
                n=num_n,
                logprobs=1
            )

            generation_list_all = []
            response = json.loads(str(response))
            meta_info = {"prompt": prompt, "response": response}
            for i in range(len(response["choices"])):
                output = response["choices"][i]["text"].strip()
                generation_list_all.append(output)
            meta_info["generations"] = generation_list_all
            return generation_list_all, meta_info

        except openai.error.RateLimitError as e:
            # Rate limit error occurred, wait for a while before retrying
            wait_duration = 1  # Wait for 60 seconds
            print(
                f"Rate limit exceeded. Retrying in {wait_duration} seconds...")
            time.sleep(wait_duration)

        attempt += 1

    return
