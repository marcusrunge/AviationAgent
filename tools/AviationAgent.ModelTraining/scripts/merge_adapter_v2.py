from __future__ import annotations

import argparse
import json
import shutil
from pathlib import Path

import torch
from peft import PeftModel
from transformers import AutoModelForCausalLM, AutoTokenizer


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base-model", required=True)
    parser.add_argument("--adapter", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--overwrite", action="store_true")
    arguments = parser.parse_args()
    if arguments.output.exists():
        if not arguments.overwrite:
            raise FileExistsError(f"Output exists: {arguments.output}")
        shutil.rmtree(arguments.output)
    model = AutoModelForCausalLM.from_pretrained(arguments.base_model, torch_dtype=torch.float16, device_map="cpu", low_cpu_mem_usage=True)
    merged = PeftModel.from_pretrained(model, arguments.adapter).merge_and_unload()
    arguments.output.mkdir(parents=True)
    merged.save_pretrained(arguments.output, safe_serialization=True)
    AutoTokenizer.from_pretrained(arguments.adapter, use_fast=True).save_pretrained(arguments.output)
    for name in ("agent_contract_v2.txt", "aviation_agent_metadata.json", "chat_template.jinja"):
        source = arguments.adapter / name
        if source.is_file():
            shutil.copy2(source, arguments.output / name)
    metadata = {"base_model": arguments.base_model, "adapter": str(arguments.adapter), "dtype": "float16"}
    (arguments.output / "aviation_agent_metadata.json").write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
