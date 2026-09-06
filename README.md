# Aviation Agent .NET Reference

A new .NET 10 solution for the local Aviation Agent V2 using ONNX Runtime GenAI 0.15.2 and the validated FP32 CPU model.

## Setup

1. Run `./Copy-Model.ps1` from PowerShell.
2. Run `dotnet restore AviationAgent.slnx`.
3. Run `dotnet test AviationAgent.slnx`.
4. Run `dotnet run --project src/MarcusRunge.AviationAgent.Console`.

The ONNX model is intentionally not included in this archive.
