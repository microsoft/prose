# Inference utils
import torch
from transformers import AutoModelForPreTraining, AutoConfig, AutoTokenizer

def get_span(text, sentinelTokenId = 0):
    text = " " + text
    for tok in ["<s>", "</s>", "<pad>"]: 
        text = text.replace(tok, " ")
    splits = text.split("<extra_id")
    return ">".join(splits[sentinelTokenId+1].split(">")[1:])

def get_topK_predictions(model, tokenizer, sample, max_seq_length, topK = 5):
    topPreds = None
    prompt = sample["Spctx"] + " <extra_id_0> " + sample["FutureCtx"]
    input_ids = tokenizer(prompt, padding="max_length", max_length=max_seq_length, truncation=True, return_tensors="pt").input_ids
    model.eval()
    with torch.no_grad():
        output = model.generate(input_ids, num_beams = topK, max_length = max_seq_length, num_return_sequences = topK)
        output = torch.nn.functional.pad(output, (0, max_seq_length - output.shape[-1]), value = tokenizer.pad_token_id)
    topPreds = [get_span(pred)for pred in tokenizer.batch_decode(output, skip_special_tokens=False)]
    return topPreds

def get_model_tokenizer_from_path(model_path, max_seq_length=1024):
    config = AutoConfig.from_pretrained(model_path)
    print("Loaded config from model path: ", model_path)

    tokenizer_kwargs = {
        "use_fast": True,
        "additional_special_tokens": ["<before>", "</before>","<prefix>", "</prefix>", "<suffix>", "</suffix>","<after>", "</after>","<edit>", "</edit>","<ctxEdits>", "</ctxEdits>"]
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

def print_topK_predictions(predictions, num_target_lines=1, k=5):
    # print the first k predictions
    for i, prediction in enumerate(predictions[:k]):
        print(f"Prediction {i+1}:")
        # ignore part of the prediction after the target line
        print("\n".join(prediction.split("\n")[:num_target_lines]))
        print("")
        