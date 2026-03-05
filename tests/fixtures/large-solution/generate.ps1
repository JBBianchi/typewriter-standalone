# Generates the large-solution fixture: 25 minimal .csproj projects, a .sln file,
# and 5 .tst template files spread across selected projects.
param()
$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ProjectCount = 25
$SlnName = 'LargeSolution'
$CsGuid = 'FAE04EC0-301F-11D3-BF4B-00C04F79EFBC'

# Clean previous output (keep scripts)
Get-ChildItem -Path $ScriptDir -Exclude 'generate.sh', 'generate.ps1' |
    Remove-Item -Recurse -Force

# ── helpers ──────────────────────────────────────────────────────────────────

function Get-ProjectGuid([int]$Index) {
    '{A1B2C3D4-0000-0000-0000-' + $Index.ToString('X12') + '}'
}

function Get-PaddedName([int]$Index) {
    'Project' + $Index.ToString('D2')
}

# ── generate projects ────────────────────────────────────────────────────────

$CsprojContent = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
'@

for ($i = 1; $i -le $ProjectCount; $i++) {
    $Name = Get-PaddedName $i
    $Dir = Join-Path $ScriptDir $Name
    New-Item -ItemType Directory -Path $Dir -Force | Out-Null

    # .csproj
    Set-Content -Path (Join-Path $Dir "$Name.csproj") -Value $CsprojContent -NoNewline

    # Class1.cs
    $ClassContent = "namespace $Name;`n`npublic class Class1 { }`n"
    Set-Content -Path (Join-Path $Dir 'Class1.cs') -Value $ClassContent -NoNewline
}

# ── generate .tst files in selected projects ─────────────────────────────────

# Project03 – Enums.tst
$EnumsTst = @'
$Enums(*)[
export enum $Name {
    $Values[
    $Name = $Value,]
}
]
'@
Set-Content -Path (Join-Path $ScriptDir 'Project03' 'Enums.tst') -Value $EnumsTst -NoNewline

# Project07 – Interfaces.tst
$InterfacesTst = @'
$Classes(*Model)[
export interface I$Name {
    $Properties[
    $name: $Type;]
}
]
'@
Set-Content -Path (Join-Path $ScriptDir 'Project07' 'Interfaces.tst') -Value $InterfacesTst -NoNewline

# Project12 – Models.tst
$ModelsTst = @'
$Classes(*)[
export class $Name {
    $Properties[
    $name: $Type;]
}
]
'@
Set-Content -Path (Join-Path $ScriptDir 'Project12' 'Models.tst') -Value $ModelsTst -NoNewline

# Project18 – Services.tst
$ServicesTst = @'
$Interfaces(*Service)[
export interface $Name {
    $Methods[
    $name($Parameters[$name: $Type][, ]): $Type;]
}
]
'@
Set-Content -Path (Join-Path $ScriptDir 'Project18' 'Services.tst') -Value $ServicesTst -NoNewline

# Project22 – AllTypes.tst
$AllTypesTst = @'
$Classes(*)[// class $FullName
]$Interfaces(*)[// interface $FullName
]$Enums(*)[// enum $FullName
]
'@
Set-Content -Path (Join-Path $ScriptDir 'Project22' 'AllTypes.tst') -Value $AllTypesTst -NoNewline

# ── generate .sln ────────────────────────────────────────────────────────────

$sb = [System.Text.StringBuilder]::new()

[void]$sb.AppendLine('Microsoft Visual Studio Solution File, Format Version 12.00')
[void]$sb.AppendLine('# Visual Studio Version 17')
[void]$sb.AppendLine('VisualStudioVersion = 17.0.31903.59')
[void]$sb.AppendLine('MinimumVisualStudioVersion = 10.0.40219.1')

for ($i = 1; $i -le $ProjectCount; $i++) {
    $Name = Get-PaddedName $i
    $Guid = Get-ProjectGuid $i
    [void]$sb.AppendLine("Project(""{$CsGuid}"") = ""$Name"", ""$Name\$Name.csproj"", ""$Guid""")
    [void]$sb.AppendLine('EndProject')
}

[void]$sb.AppendLine('Global')
[void]$sb.AppendLine('	GlobalSection(SolutionConfigurationPlatforms) = preSolution')
[void]$sb.AppendLine('		Debug|Any CPU = Debug|Any CPU')
[void]$sb.AppendLine('		Release|Any CPU = Release|Any CPU')
[void]$sb.AppendLine('	EndGlobalSection')
[void]$sb.AppendLine('	GlobalSection(ProjectConfigurationPlatforms) = postSolution')

for ($i = 1; $i -le $ProjectCount; $i++) {
    $Guid = Get-ProjectGuid $i
    foreach ($Cfg in @('Debug', 'Release')) {
        [void]$sb.AppendLine("		$Guid.$Cfg|Any CPU.ActiveCfg = $Cfg|Any CPU")
        [void]$sb.AppendLine("		$Guid.$Cfg|Any CPU.Build.0 = $Cfg|Any CPU")
    }
}

[void]$sb.AppendLine('	EndGlobalSection')
[void]$sb.AppendLine('	GlobalSection(SolutionProperties) = preSolution')
[void]$sb.AppendLine('		HideSolutionNode = FALSE')
[void]$sb.AppendLine('	EndGlobalSection')
[void]$sb.AppendLine('EndGlobal')

Set-Content -Path (Join-Path $ScriptDir "$SlnName.sln") -Value $sb.ToString() -NoNewline

Write-Host "Generated $ProjectCount projects and $SlnName.sln in $ScriptDir"
