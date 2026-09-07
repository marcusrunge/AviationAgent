from __future__ import annotations

import argparse
import json
from pathlib import Path

import torch
from peft import PeftModel
from transformers import AutoModelForCausalLM, AutoTokenizer

from aviation_agent_training.contract import SYSTEM_PROMPT
from aviation_agent_training.dataset import load_jsonl


def generate(model, tokenizer, user: str) -> str:
    messages = [{"role": "system", "content": SYSTEM_PROMPT}, {"role": "user", "content": user}]
    inputs = tokenizer.apply_chat_template(messages, add_generation_prompt=True, return_tensors="pt").to(model.device)
    with torch.inference_mode():
        output = model.generate(inputs, max_new_tokens=96, do_sample=False, pad_token_id=tokenizer.eos_token_id)
    return tokenizer.decode(output[0][inputs.shape[-1]:], skip_special_tokens=True).strip()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base-model", required=True)
    parser.add_argument("--adapter")
    parser.add_argument("--dataset", type=Path, required=True)
    parser.add_argument("--report", type=Path, required=True)
    arguments = parser.parse_args()
    tokenizer_path = arguments.adapter or arguments.base_model
    tokenizer = AutoTokenizer.from_pretrained(tokenizer_path, use_fast=True)
    model = AutoModelForCausalLM.from_pretrained(arguments.base_model, torch_dtype=torch.float16, device_map="auto")
    if arguments.adapter:
        model = PeftModel.from_pretrained(model, arguments.adapter)
    model.eval()
    results = []
    for record in load_jsonl(arguments.dataset):
        actual = generate(model, tokenizer, record["user"])
        results.append({"input": record["user"], "expected": record["assistant"], "actual": actual, "identical": actual == record["assistant"]})
    report = {"total": len(results), "identical": sum(result["identical"] for result in results), "identical_rate": sum(result["identical"] for result in results) / len(results), "results": results}
    arguments.report.parent.mkdir(parents=True, exist_ok=True)
    arguments.report.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: value for key, value in report.items() if key != "results"}, indent=2))


if __name__ == "__main__":
    main()
