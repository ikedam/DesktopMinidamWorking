# MSBuildのパスを検出するスクリプト
$msbuildPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe"
)

foreach ($path in $msbuildPaths) {
    if (Test-Path $path) {
        Write-Output $path
        exit 0
    }
}

# 環境変数から検索
$vsPath = $env:VSINSTALLDIR
if ($vsPath) {
    $msbuildPath = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
    if (Test-Path $msbuildPath) {
        Write-Output $msbuildPath
        exit 0
    }
}

# 見つからない場合
Write-Error "MSBuildが見つかりません。Visual Studio Build ToolsまたはVisual Studio Communityをインストールしてください。"
exit 1


