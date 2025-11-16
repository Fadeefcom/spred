param(
  [Parameter(Mandatory=$true)][string]$TemplatePath,
  [Parameter(Mandatory=$true)][string]$TargetRoot,
  [Parameter(Mandatory=$true)][string]$ServiceName,
  [string]$AppName,
  [string]$AppBasePath,
  [string]$OutputDir,
  [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function To-Kebab([string]$s){
  if ([string]::IsNullOrWhiteSpace($s)) { return $s }
  ($s -creplace '([a-z0-9])([A-Z])','$1-$2').ToLower()
}

function Get-AppBasePath([string]$serviceName) {
  $k = To-Kebab $serviceName
  ($k -split '-')[0]
}

$ServiceNamePascal = ($ServiceName -replace '\s','')
if (-not $AppName)     { $AppName     = To-Kebab $ServiceNamePascal }
if (-not $AppBasePath) { $AppBasePath = Get-AppBasePath $ServiceNamePascal }

if (-not (Test-Path $TemplatePath)) { throw "Template not found: $TemplatePath" }

if ($OutputDir) {
  if (-not (Test-Path $OutputDir)) { throw "OutputDir must exist: $OutputDir" }
  $dest = (Resolve-Path $OutputDir).Path
} else {
  $dest = Join-Path $TargetRoot $ServiceNamePascal
  if (Test-Path $dest) { throw "Target already exists: $dest (use -OutputDir to point to an existing folder)" }
  if (-not $DryRun) { $null = New-Item -ItemType Directory -Path $dest -Force }
}

Write-Host "Copying template: $TemplatePath -> $dest"
if (-not $DryRun) {
  $rc = (Start-Process -FilePath robocopy -ArgumentList @("$TemplatePath","$dest","/E","/NFL","/NDL","/NJH","/NJS","/NP","/R:1","/W:1") -PassThru -NoNewWindow -Wait).ExitCode
  if ($rc -gt 7) { throw "robocopy failed with code $rc" }
}

$replacements = @(
  @{ From='${ServiceName}';   To=$ServiceNamePascal },
  @{ From='${service-name}';  To=$AppName },
  @{ From='${app-name}';      To=$AppName },
  @{ From='${app-base-path}'; To=$AppBasePath }
)

function Replace-In-String([string]$text, $map){
  $out = $text
  foreach($m in $map){ $out = $out.Replace($m.From, $m.To) }
  $out
}

Write-Host "Replacing content tokens..."
$TextExtensions = @(
  '.cs','.csproj','.sln','.json','.jsonc','.yml','.yaml','.md','.txt','.xml','.config','.props','.targets',
  '.ts','.tsx','.js','.jsx','.scss','.css','.ps1','.psm1','.sh','.dockerfile','.env','.editorconfig',
  '.tf','.tfvars','.ini','.cmd','.bat','.yarnrc','.npmrc'
)
$allFiles = Get-ChildItem -LiteralPath $dest -Recurse -File -Force
$files = $allFiles | Where-Object {
  $ext = $_.Extension
  if ([string]::IsNullOrEmpty($ext)) {
    $_.Name -match '^(Dockerfile|\.env.*|\.gitignore|\.gitattributes)$'
  } else {
    $TextExtensions -contains $ext.ToLower()
  }
}
foreach($f in $files){
  try { $raw = Get-Content -LiteralPath $f.FullName -Raw -ErrorAction Stop } catch { continue }
  if ($null -ne $raw -and ($replacements | Where-Object { $raw.Contains($_.From) })){
    $new = Replace-In-String $raw $replacements
    if (-not $DryRun) { [System.IO.File]::WriteAllText($f.FullName, $new, [System.Text.UTF8Encoding]::new($true)) }
  }
}

Write-Host "Renaming directories..."
$dirs = Get-ChildItem -LiteralPath $dest -Recurse -Directory -Force | Sort-Object FullName -Descending
foreach($d in $dirs){
  $newName = Replace-In-String $d.Name $replacements
  if ($newName -ne $d.Name){
    $newPath = Join-Path ($d.Parent.FullName) $newName
    if (-not $DryRun) { Move-Item -LiteralPath $d.FullName -Destination $newPath -Force }
  }
}

Write-Host "Renaming files..."
$filesAll = Get-ChildItem -LiteralPath $dest -Recurse -File -Force
foreach($f in $filesAll){
  $newName = Replace-In-String $f.Name $replacements
  if ($newName -ne $f.Name){
    $newPath = Join-Path ($f.DirectoryName) $newName
    if (-not $DryRun) { Move-Item -LiteralPath $f.FullName -Destination $newPath -Force }
  }
}

Write-Host "Done. ServiceName=$ServiceNamePascal app-name=$AppName app-base-path=$AppBasePath output=$dest"
