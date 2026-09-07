from __future__ import annotations

import argparse
import json
import time
from pathlib import Path

import numpy as np
import onnxruntime_genai as og

from aviation_agent_training.contract import SYSTEM_PROMPT

CASES = (
    ("TAF ETHS. Schwerpunkt: Wind und Böen.", '{"action":"get_taf","station":"ETHS","focus":"wind"}'),
    ("METAR EDDH. Schwerpunkt: Sichtweite.", '{"action":"get_metar","station":"EDDH","focus":"visibility"}'),
    ("Zeige Beobachtung und Prognose für EDDF.", '{"action":"get_metar_and_taf","station":"EDDF","focus":"full"}'),
    ("Wie ist das Wetter?", '{"action":"unknown","station":null,"focus":"none"}'),
)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", type=Path, default=Path("models/onnx/aviation-agent-qwen2.5-0.5b-v2-fp32-cpu"))
    arguments = parser.parse_args()
    model = og.Model(str(arguments.model.resolve()))
    tokenizer = og.Tokenizer(model)
    results = []
    for user, expected in CASES:
        prompt = f"<|im_start|>system\n{SYSTEM_PROMPT}<|im_end|>\n<|im_start|>user\n{user}<|im_end|>\n<|im_start|>assistant\n"
        input_tokens = tokenizer.encode(prompt)
        parameters = og.GeneratorParams(model)
        parameters.set_search_options(max_length=len(input_tokens) + 96, do_sample=False)
        generator = og.Generator(model, parameters)
        generator.append_tokens(np.asarray(input_tokens, dtype=np.int32))
        started = time.perf_counter()
        while not generator.is_done():
            generator.generate_next_token()
        actual = tokenizer.decode(generator.get_sequence(0)[len(input_tokens):]).strip()
        results.append({"input": user, "expected": expected, "actual": actual, "passed": actual == expected, "elapsed_seconds": round(time.perf_counter() - started, 3)})
    report = {"total": len(results), "passed": sum(result["passed"] for result in results), "results": results}
    Path("reports").mkdir(exist_ok=True)
    Path("reports/onnx-v2-fp32-cpu-smoke.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    if report["passed"] != report["total"]:
        raise RuntimeError("ONNX smoke test failed.")


if __name__ == "__main__":
    main()
