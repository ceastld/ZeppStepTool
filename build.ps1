#!/usr/bin/env pwsh
# Build ZeppStepTool WPF app using qkbuild

Write-Host "Building ZeppStepTool..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\ZeppStepTool.Wpf" @args
