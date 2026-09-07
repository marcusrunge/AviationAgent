# Model training and export

The executable Python project is located under `tools/AviationAgent.ModelTraining`. Run every Python command from that directory.

## Pipeline

1. Validate JSONL datasets and the routing contract.
2. Fine-tune `Qwen/Qwen2.5-0.5B-Instruct` with LoRA.
3. Mask system and user tokens so loss is computed only for the assistant JSON response.
4. Evaluate exact JSON equality on the validation set.
5. Merge the accepted adapter into the base model.
6. Re-evaluate the merged model.
7. Export the merged model to ONNX Runtime GenAI FP32 CPU.
8. Run the four-case ONNX smoke test.

## Local artifact locations

The operational training workspace remains separate from the source repository:

```text
C:/Users/mru/ModelTraining/AviationAgent
```

The .NET application currently loads:

```text
C:/Users/mru/ModelTraining/AviationAgent/models/onnx/aviation-agent-qwen2.5-0.5b-v2-fp32-cpu
```

## First setup

```powershell
Set-Location C:\Users\mru\source\repos\AviationAgent\tools\AviationAgent.ModelTraining
```

```powershell
py -3.12 -m venv .venv
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& '.\.venv\Scripts\Activate.ps1'; python -m pip install --upgrade pip; python -m pip install -r requirements-training.txt; python -m pip install -e ."
```

Provide `data/train-v2.jsonl` and `data/validation-v2.jsonl`, then validate:

```powershell
.\.venv\Scripts\python.exe .\scripts\validate_dataset_v2.py .\data\train-v2.jsonl .\data\validation-v2.jsonl
```

Train:

```powershell
.\.venv\Scripts\python.exe .\scripts\train_adapter_v2.py --config .\config\training-v2.json
```

Evaluate adapter:

```powershell
.\.venv\Scripts\python.exe .\scripts\evaluate_model_v2.py --base-model Qwen/Qwen2.5-0.5B-Instruct --adapter .\models\adapters\aviation-agent-qwen2.5-0.5b-v2 --dataset .\data\validation-v2.jsonl --report .\reports\adapter-validation-v2.json
```

Merge:

```powershell
.\.venv\Scripts\python.exe .\scripts\merge_adapter_v2.py --base-model Qwen/Qwen2.5-0.5B-Instruct --adapter .\models\adapters\aviation-agent-qwen2.5-0.5b-v2 --output .\models\merged\aviation-agent-qwen2.5-0.5b-v2
```

## Export environment

```powershell
py -3.14 -m venv .venv-export
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& '.\.venv-export\Scripts\Activate.ps1'; python -m pip install --upgrade pip; python -m pip install -r requirements-export.txt"
```

Export FP32 CPU:

```powershell
.\.venv-export\Scripts\python.exe .\scripts\export_onnx_v2_fp32_cpu.py
```

Verify ONNX:

```powershell
.\.venv-export\Scripts\python.exe .\scripts\verify_onnx_v2_fp32_cpu.py
```

## Acceptance criteria

- Dataset validation succeeds.
- Adapter and merged-model evaluations produce exact contract JSON.
- The ONNX smoke test passes all four cases.
- Only the FP32 CPU export is used by the .NET application.
