param(
    [string]$TrainingRoot = "C:\Users\mru\ModelTraining\AviationAgent"
)

$source = Join-Path $TrainingRoot "models\onnx\aviation-agent-qwen2.5-0.5b-v2-fp32-cpu"
$destination = Join-Path $PSScriptRoot "src\MarcusRunge.AviationAgent.Console\Models\aviation-agent-qwen2.5-0.5b-v2-fp32-cpu"

if (-not (Test-Path $source -PathType Container)) { throw "Model source not found: $source" }
if (Test-Path $destination) { Remove-Item $destination -Recurse -Force }
New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
Copy-Item -Path $source -Destination $destination -Recurse -Force
Write-Host "Model copied to $destination"
