# 一键发布 + 打包安装包
# 用法（在解决方案根目录）：
#   .\build-release.ps1                  # 用 csproj 里的版本，发布并打包
#   .\build-release.ps1 -Version 0.2.0   # 先改版本号再发布打包
#   .\build-release.ps1 -SkipInstaller   # 只发布，不编译安装包
#
# 版本号以 src\PhotoAlbum.Presentation\PhotoAlbum.Presentation.csproj 的 <Version> 为准，
# 脚本会自动把它同步到 installer\PhotoAlbum.iss 的 MyAppVersion。

param(
    [string]$Version,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$csproj = Join-Path $root "src\PhotoAlbum.Presentation\PhotoAlbum.Presentation.csproj"
$iss = Join-Path $root "installer\PhotoAlbum.iss"
$publishDir = Join-Path $root "publish"

# 1. 若指定了版本号，先写回 csproj
if ($Version) {
    $csprojText = Get-Content $csproj -Raw
    $csprojText = $csprojText -replace '<Version>.*?</Version>', "<Version>$Version</Version>"
    Set-Content -Path $csproj -Value $csprojText -Encoding UTF8 -NoNewline
    Write-Host "[csproj] 版本 -> $Version"
}

# 2. 从 csproj 读版本号
[xml]$xml = Get-Content $csproj -Raw
$version = $xml.Project.PropertyGroup.Version
if (-not $version) { throw "csproj 未设置 <Version>" }
Write-Host "[版本] $version"

# 3. 同步 .iss 的 MyAppVersion
$issText = Get-Content $iss -Raw
$issNew = $issText -replace '(?m)^#define MyAppVersion ".*?"', "#define MyAppVersion `"$version`""
if ($issNew -ne $issText) {
    Set-Content -Path $iss -Value $issNew -Encoding UTF8 -NoNewline
    Write-Host "[.iss] MyAppVersion -> $version"
} else {
    Write-Host "[.iss] 版本已是 $version"
}

# 4. 发布自包含程序
Write-Host "[发布] dotnet publish ..."
dotnet publish $csproj -c Release -r win-x64 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败" }

# 5. 编译安装包（需要 Inno Setup）
if (-not $SkipInstaller) {
    $iscc = (Get-Command iscc.exe -ErrorAction SilentlyContinue).Source
    if (-not $iscc) {
        foreach ($p in @(
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
        )) {
            if (Test-Path $p) { $iscc = $p; break }
        }
    }

    if ($iscc) {
        Write-Host "[打包] $iscc ..."
        & $iscc $iss | Out-Host
        Write-Host "安装包 -> installer\output\PhotoAlbum-Setup-$version.exe"
    } else {
        Write-Warning "未找到 Inno Setup 的 ISCC.exe，请手动用 Inno Setup 编译 $iss"
    }
}

Write-Host "完成。"
