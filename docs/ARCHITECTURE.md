# Architecture

## Purpose

AviationAgent is a local aviation-weather routing agent. The language model recognizes the requested product, ICAO station and subject focus. It does not interpret weather operationally and does not make flight decisions.

## Runtime flow

```text
Natural-language request
  -> deterministic operational-decision guard
  -> local Qwen ONNX model
  -> strict JSON validation
  -> typed AgentDecision
  -> deterministic router
  -> Aviation Weather Center client
  -> deterministic METAR or TAF parser
  -> localized focus formatter
  -> formatted result plus unchanged raw report
```

## Safety boundary

Requests for take-off, landing, release, personal minima, alternate or go/no-go decisions are rejected before model inference and before any tool routing. Model output must contain exactly `action`, `station` and `focus`. Unsupported values, malformed JSON and missing stations for weather actions are rejected.

## Projects

- `MarcusRunge.AviationAgent.Core` contains domain contracts, safety guard, routing, parsers and formatters.
- `MarcusRunge.AviationAgent.OnnxRuntime` encapsulates ONNX Runtime GenAI.
- `MarcusRunge.AviationAgent.AviationWeather` encapsulates the external weather data source.
- `MarcusRunge.AviationAgent.Console` is the composition root and reference UI.
- `MarcusRunge.AviationAgent.Tests` contains deterministic unit tests.
- `tools/AviationAgent.ModelTraining` contains the reproducible local training and export pipeline.

## Model artifacts

The validated deployment model is the FP32 CPU ONNX export. INT4 was rejected because output quality collapsed. INT8 was rejected because the builder generated an invalid graph. Model artifacts remain outside Git.
