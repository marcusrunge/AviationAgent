from __future__ import annotations

import argparse
import json
from collections import Counter
from pathlib import Path

from aviation_agent_training.dataset import load_jsonl
from aviation_agent_training.contract import validate_answer


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("paths", nargs="+", type=Path)
    arguments = parser.parse_args()
    for path in arguments.paths:
        records = load_jsonl(path)
        actions = Counter(validate_answer(record["assistant"])["action"] for record in records)
        print(json.dumps({"path": str(path), "records": len(records), "actions": actions}, ensure_ascii=False, indent=2, default=dict))


if __name__ == "__main__":
    main()
