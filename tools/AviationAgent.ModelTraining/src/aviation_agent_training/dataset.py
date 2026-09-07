from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from .contract import SYSTEM_PROMPT, validate_answer


def load_jsonl(path: Path) -> list[dict[str, str]]:
    records: list[dict[str, str]] = []
    for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if not line.strip():
            continue
        record = json.loads(line)
        if set(record) != {"user", "assistant"} or not all(isinstance(record[key], str) for key in record):
            raise ValueError(f"{path}:{line_number}: expected string properties user and assistant.")
        validate_answer(record["assistant"])
        records.append(record)
    if not records:
        raise ValueError(f"Dataset is empty: {path}")
    return records


def encode_record(record: dict[str, str], tokenizer: Any, max_length: int) -> dict[str, list[int]]:
    prompt_messages = [{"role": "system", "content": SYSTEM_PROMPT}, {"role": "user", "content": record["user"]}]
    full_messages = [*prompt_messages, {"role": "assistant", "content": record["assistant"]}]
    prompt = tokenizer.apply_chat_template(prompt_messages, tokenize=False, add_generation_prompt=True)
    full_text = tokenizer.apply_chat_template(full_messages, tokenize=False, add_generation_prompt=False)
    prompt_ids = tokenizer(prompt, add_special_tokens=False)["input_ids"]
    encoded = tokenizer(full_text, add_special_tokens=False, truncation=True, max_length=max_length)
    input_ids = encoded["input_ids"]
    labels = [-100] * min(len(prompt_ids), len(input_ids)) + input_ids[len(prompt_ids):]
    if not labels or all(label == -100 for label in labels):
        raise ValueError("The assistant answer was truncated completely. Increase max_length.")
    return {"input_ids": input_ids, "attention_mask": encoded["attention_mask"], "labels": labels}
