from __future__ import annotations

import argparse
import json
import shutil
import time
from pathlib import Path

from onnxruntime_genai.models.builder import create_model, parse_extra_options


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", type=Path, default=Path("models/merged/aviation-agent-qwen2.5-0.5b-v2"))
    parser.add_argument("--output", type=Path, default=Path("models/onnx/aviation-agent-qwen2.5-0.5b-v2-fp32-cpu"))
    parser.add_argument("--overwrite", action="store_true")
    arguments = parser.parse_args()
    if arguments.output.exists():
        if not arguments.overwrite:
            raise FileExistsError(f"Output directory is not empty: {arguments.output}")
        shutil.rmtree(arguments.output)
    cache = Path(".cache/onnxruntime-genai")
    started = time.perf_counter()
    extra_options = parse_extra_options("qwen2", str(arguments.input.resolve()), str(arguments.output.resolve()), "fp32", "cpu", str(cache.resolve()), [])
    create_model("qwen2", str(arguments.input.resolve()), str(arguments.output.resolve()), "fp32", "cpu", str(cache.resolve()), **extra_options)
    for name in ("agent_contract_v2.txt", "aviation_agent_metadata.json", "chat_template.jinja"):
        source = arguments.input / name
        if source.is_file():
            shutil.copy2(source, arguments.output / name)
    report = {"model_name": "qwen2", "precision": "fp32", "execution_provider": "cpu", "elapsed_seconds": round(time.perf_counter() - started, 3), "total_bytes": sum(path.stat().st_size for path in arguments.output.rglob("*") if path.is_file())}
    Path("reports").mkdir(exist_ok=True)
    Path("reports/onnx-export-v2-fp32-cpu.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
