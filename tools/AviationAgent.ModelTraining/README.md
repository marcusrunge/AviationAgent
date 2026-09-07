# AviationAgent.ModelTraining

Dieses Verzeichnis enthält den reproduzierbaren Python-Teil des Aviation-Agent-Projekts: Datenvalidierung, LoRA-Training, Evaluation, Merge und den **erfolgreichen FP32-CPU-Export** für ONNX Runtime GenAI. Trainingsdaten, Modellgewichte, Adapter, ONNX-Artefakte und Berichte werden absichtlich nicht in Git aufgenommen.

## Funktionsprinzip

Das Sprachmodell ist ausschließlich ein Intent-Router. Es übersetzt eine natürliche Wetteranfrage in genau ein kompaktes JSON-Objekt:

```json
{"action":"get_taf","station":"ETHS","focus":"wind"}
```

Die .NET-Anwendung übernimmt danach deterministisch Safety Guard, Validierung, Routing, Datenabruf sowie METAR-/TAF-Parsing und Formatierung. Das Modell trifft keine Start-, Lande-, Freigabe-, Minima- oder Go/No-Go-Entscheidung.

## Verzeichnisstruktur

```text
model-training/
├── config/training-v2.json
├── data/README.md
├── data/sample-v2.jsonl
├── scripts/
│   ├── validate_dataset_v2.py
│   ├── train_adapter_v2.py
│   ├── evaluate_model_v2.py
│   ├── merge_adapter_v2.py
│   ├── export_onnx_v2_fp32_cpu.py
│   └── verify_onnx_v2_fp32_cpu.py
├── src/aviation_agent_training/
├── tests/
├── requirements-training.txt
├── requirements-export.txt
├── LICENSE
├── NOTICE
└── THIRD-PARTY-NOTICES.md
```

## Voraussetzungen

- Windows 10 oder Windows 11
- Git
- Python 3.12 oder 3.13 für Training
- Python 3.14 für die nachweislich verwendete Exportumgebung
- Eine CUDA-fähige GPU wird für das Training empfohlen
- Genügend freier Speicher für Basismodell, Adapter, Merge und ungefähr 2 GB FP32-ONNX-Ausgabe

## 1. Trainingsumgebung erstellen

Alle PowerShell-Befehle sind Einzeiler:

```powershell
Set-Location C:\Users\mru\ModelTraining\AviationAgent
```

```powershell
py -3.12 -m venv .venv
```

```powershell
.\.venv\Scripts\Activate.ps1
```

Falls die Ausführungsrichtlinie lokale Skripte blockiert, aktiviere die Umgebung ohne systemweite Policy-Änderung:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& '.\.venv\Scripts\Activate.ps1'; python -m pip install --upgrade pip; python -m pip install -r requirements-training.txt"
```

Bei bereits aktiver Umgebung:

```powershell
python -m pip install --upgrade pip
```

```powershell
python -m pip install -r requirements-training.txt
```

```powershell
python -m pip install -e .
```

## 2. Trainingsdaten bereitstellen und validieren

Kopiere die geprüften Dateien nach:

```text
data/train-v2.jsonl
data/validation-v2.jsonl
```

Validierung:

```powershell
python .\scripts\validate_dataset_v2.py .\data\train-v2.jsonl .\data\validation-v2.jsonl
```

Die Validierung prüft JSONL-Struktur, erlaubte Aktionen und Fokusse, ICAO-Format sowie erforderliche Stationen.

## 3. LoRA-Adapter trainieren

```powershell
python .\scripts\train_adapter_v2.py --config .\config\training-v2.json
```

Ziel:

```text
models/adapters/aviation-agent-qwen2.5-0.5b-v2
```

Wichtig: Die Label-Maske setzt alle System- und User-Tokens auf `-100`. Der Loss wird damit nur für die erwartete Assistant-JSON-Antwort berechnet. Das verhindert, dass das Modell den Prompt statt der Routingantwort lernt.

## 4. Adapter evaluieren

```powershell
python .\scripts\evaluate_model_v2.py --base-model Qwen/Qwen2.5-0.5B-Instruct --adapter .\models\adapters\aviation-agent-qwen2.5-0.5b-v2 --dataset .\data\validation-v2.jsonl --report .\reports\adapter-validation-v2.json
```

Akzeptanzkriterium für die feste Referenzsuite ist eine exakte JSON-Übereinstimmung. Semantisch ähnliche Freitextausgaben zählen absichtlich nicht als bestanden.

## 5. Adapter mit dem Basismodell zusammenführen

```powershell
python .\scripts\merge_adapter_v2.py --base-model Qwen/Qwen2.5-0.5B-Instruct --adapter .\models\adapters\aviation-agent-qwen2.5-0.5b-v2 --output .\models\merged\aviation-agent-qwen2.5-0.5b-v2
```

Ziel:

```text
models/merged/aviation-agent-qwen2.5-0.5b-v2
```

## 6. Zusammengeführtes Modell verifizieren

```powershell
python .\scripts\evaluate_model_v2.py --base-model .\models\merged\aviation-agent-qwen2.5-0.5b-v2 --dataset .\data\validation-v2.jsonl --report .\reports\merged-v2-comparison.json
```

Der erfolgreiche Projektstand erreichte auf der festen Vergleichssuite 20 von 20 identische Antworten.

## 7. Separate Exportumgebung erstellen

Training und ONNX-Export bleiben getrennt:

```powershell
py -3.14 -m venv .venv-export
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& '.\.venv-export\Scripts\Activate.ps1'; python -m pip install --upgrade pip; python -m pip install -r requirements-export.txt"
```

Für interaktive Folgeaufrufe kann die Umgebung im aktuellen Prozess aktiviert werden, sofern die lokale Policy dies erlaubt. Andernfalls starte die einzelnen Exportbefehle über `powershell.exe -NoProfile -ExecutionPolicy Bypass -Command`.

Prüfung:

```powershell
.\.venv-export\Scripts\python.exe -m pip check
```

## 8. Erfolgreichen FP32-CPU-ONNX-Export erzeugen

```powershell
.\.venv-export\Scripts\python.exe .\scripts\export_onnx_v2_fp32_cpu.py
```

Ziel:

```text
models/onnx/aviation-agent-qwen2.5-0.5b-v2-fp32-cpu
```

Der erfolgreiche Referenzexport verwendete:

```text
Python 3.14.7
Olive 0.13.0
ONNX Runtime 1.29.0
ONNX Runtime GenAI 0.15.2
Requests 2.34.2
Safetensors 0.8.0
Precision fp32
Execution Provider cpu
```

`parse_extra_options(...)` muss vor `create_model(...)` aufgerufen werden. Ohne diesen Schritt fehlt `hf_details`.

## 9. ONNX-Smoke-Test

```powershell
.\.venv-export\Scripts\python.exe .\scripts\verify_onnx_v2_fp32_cpu.py
```

Der Test prüft vier Referenzfälle: TAF/Wind, METAR/Sicht, kombinierter Abruf und fehlende Station. Alle Antworten müssen exakt übereinstimmen.

## Nicht freigegebene Exportvarianten

- INT4 CPU lädt, zerstörte im Versuch jedoch die JSON-Ausgabequalität und wurde verworfen.
- INT8 CPU erzeugte mit der verwendeten Builder-Version einen ungültigen ONNX-Graphen (`lm_head.MatMul.weight_Q4`) und wurde verworfen.
- Deploymentkandidat ist deshalb ausschließlich `aviation-agent-qwen2.5-0.5b-v2-fp32-cpu`.

## Einbindung in die .NET-Anwendung

Die .NET-Anwendung lädt direkt diesen Ordner:

```text
C:/Users/mru/ModelTraining/AviationAgent/models/onnx/aviation-agent-qwen2.5-0.5b-v2-fp32-cpu
```

Das Modellverzeichnis darf nicht in Git aufgenommen werden. Die Python-`.gitignore` schließt Modelle, Adapter, Berichte, virtuelle Umgebungen und private Trainingsdaten aus.

## Tests

```powershell
python -m pytest
```

Syntaxprüfung aller Skripte:

```powershell
Get-ChildItem .\scripts -Filter *.py | ForEach-Object { python -m py_compile $_.FullName }
```

## Lizenz und Modellherkunft

Der Quellcode des Gesamtprojekts steht unter der MIT-Lizenz im Repository-Stamm. Das Standard-Basismodell `Qwen/Qwen2.5-0.5B-Instruct` wird ebenfalls als Apache-2.0-Modell veröffentlicht. `../../NOTICE` und `../../THIRD-PARTY-NOTICES.md` dokumentieren die Modellherkunft und die nicht mitgelieferten Artefakte.

Vor einer Veröffentlichung eigener Trainingsdaten oder erzeugter Modellgewichte müssen deren Rechte und die jeweils geltenden Lizenzen separat geprüft werden.
