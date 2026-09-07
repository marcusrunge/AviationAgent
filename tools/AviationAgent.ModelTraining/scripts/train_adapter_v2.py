from __future__ import annotations

import argparse
import json
from pathlib import Path

import torch
from datasets import Dataset
from peft import LoraConfig, get_peft_model
from transformers import AutoModelForCausalLM, AutoTokenizer, Trainer, TrainingArguments, set_seed

from aviation_agent_training.collator import SupervisedCollator
from aviation_agent_training.dataset import encode_record, load_jsonl


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", type=Path, default=Path("config/training-v2.json"))
    arguments = parser.parse_args()
    configuration = json.loads(arguments.config.read_text(encoding="utf-8"))
    set_seed(configuration["seed"])

    tokenizer = AutoTokenizer.from_pretrained(configuration["base_model"], use_fast=True)
    if tokenizer.pad_token_id is None:
        tokenizer.pad_token = tokenizer.eos_token
    model = AutoModelForCausalLM.from_pretrained(configuration["base_model"], torch_dtype=torch.float16, device_map="auto")
    model.config.use_cache = False
    lora = LoraConfig(r=configuration["lora_rank"], lora_alpha=configuration["lora_alpha"], lora_dropout=configuration["lora_dropout"], target_modules=["q_proj", "k_proj", "v_proj", "o_proj", "gate_proj", "up_proj", "down_proj"], task_type="CAUSAL_LM")
    model = get_peft_model(model, lora)

    def create_dataset(path: str) -> Dataset:
        records = load_jsonl(Path(path))
        return Dataset.from_list([encode_record(record, tokenizer, configuration["max_length"]) for record in records])

    output = Path(configuration["adapter_output"])
    output.mkdir(parents=True, exist_ok=True)
    training_arguments = TrainingArguments(output_dir=str(output), num_train_epochs=configuration["epochs"], learning_rate=configuration["learning_rate"], per_device_train_batch_size=configuration["batch_size"], per_device_eval_batch_size=configuration["batch_size"], gradient_accumulation_steps=configuration["gradient_accumulation_steps"], eval_strategy="epoch", save_strategy="epoch", logging_steps=5, fp16=torch.cuda.is_available(), report_to="none", seed=configuration["seed"], load_best_model_at_end=True, metric_for_best_model="eval_loss")
    trainer = Trainer(model=model, args=training_arguments, train_dataset=create_dataset(configuration["train_file"]), eval_dataset=create_dataset(configuration["validation_file"]), data_collator=SupervisedCollator(tokenizer))
    result = trainer.train()
    trainer.save_model(str(output))
    tokenizer.save_pretrained(output)
    (output / "agent_contract_v2.txt").write_text(__import__("aviation_agent_training.contract", fromlist=["SYSTEM_PROMPT"]).SYSTEM_PROMPT + "\n", encoding="utf-8")
    (Path("reports")).mkdir(exist_ok=True)
    (Path("reports") / "training-v2.json").write_text(json.dumps(result.metrics, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
