param([string]$UnityData = (Join-Path $env:LOCALAPPDATA 'Unity/Editors/2022.1.7f1/Editor/Data'))
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    if (-not (Test-Path -LiteralPath $UnityData)) {
        $UnityData = 'C:/Program Files/Unity/Hub/Editor/2022.1.7f1/Editor/Data'
    }
    $mono = Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe'
    $compiler = Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
    if (-not (Test-Path -LiteralPath $compiler)) { throw 'Pass -UnityData with the Data folder of your Unity 2022.1.7f1 installation.' }
    New-Item -ItemType Directory -Force 'Temp/PotionChecks' | Out-Null
    & $mono $compiler /nologo /out:Temp/PotionChecks/GameRulesChecks.exe Assets/PotionPanic/PotionGame.cs Tests/GameRulesChecks.cs
    if ($LASTEXITCODE -ne 0) { throw 'Rule-check compilation failed.' }
    & $mono Temp/PotionChecks/GameRulesChecks.exe
    if ($LASTEXITCODE -ne 0) { throw 'Gameplay checks failed.' }
    # The full Canvas UI is checked by building and running the game in Unity, not by this rule-only test.
} finally { Pop-Location }
