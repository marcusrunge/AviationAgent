from __future__ import annotations

from typing import Any

import torch


class SupervisedCollator:
    def __init__(self, tokenizer: Any) -> None:
        self._tokenizer = tokenizer

    def __call__(self, features: list[dict[str, list[int]]]) -> dict[str, torch.Tensor]:
        max_length = max(len(feature["input_ids"]) for feature in features)
        pad_id = self._tokenizer.pad_token_id
        input_ids, attention_masks, labels = [], [], []
        for feature in features:
            padding = max_length - len(feature["input_ids"])
            input_ids.append(feature["input_ids"] + [pad_id] * padding)
            attention_masks.append(feature["attention_mask"] + [0] * padding)
            labels.append(feature["labels"] + [-100] * padding)
        return {"input_ids": torch.tensor(input_ids), "attention_mask": torch.tensor(attention_masks), "labels": torch.tensor(labels)}
