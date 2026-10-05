param([string]$UnityData = 'C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Data')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    New-Item -ItemType Directory -Force 'Temp/PotionChecks' | Out-Null
    $mono = Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe'
    $compiler = Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
    & $mono $compiler /nologo /out:Temp/PotionChecks/GameRulesChecks.exe Assets/PotionPanic/PotionGame.cs Tests/GameRulesChecks.cs
    if ($LASTEXITCODE -ne 0) { throw 'Rule-check compilation failed.' }
    & $mono Temp/PotionChecks/GameRulesChecks.exe
    if ($LASTEXITCODE -ne 0) { throw 'Gameplay checks failed.' }
    $references = Get-ChildItem "$UnityData/Managed/UnityEngine/*.dll" | ForEach-Object { '/reference:' + $_.FullName }
    & $mono $compiler /nologo /target:library /out:Temp/PotionChecks/PotionPanic.dll @references "/reference:$UnityData/MonoBleedingEdge/lib/mono/4.5/Facades/netstandard.dll" /reference:Library/ScriptAssemblies/Unity.InputSystem.dll Assets/PotionPanic/PotionGame.cs Assets/PotionPanic/PotionPanicApp.cs Assets/PotionPanic/PotionPanicCapture.cs
    if ($LASTEXITCODE -ne 0) { throw 'Game compilation failed. Open the project in Unity first to import its Input System package.' }
    Write-Output 'PASS: Unity runtime code compilation'
} finally { Pop-Location }
