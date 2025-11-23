# Visual Studioなしで開発環境をセットアップする方法

## ✅ 方法1: .NET SDKのみを使用（推奨・最も軽量）

.NET SDKをインストールすれば、MSBuildが含まれているため、Visual Studioなしでビルドできます。
**これが最も軽量で推奨される方法です。**

### 1. .NET SDKのインストール

1. [.NET SDK ダウンロードページ](https://dotnet.microsoft.com/download)から最新の.NET SDKをダウンロード
   - **推奨**: .NET 8 SDK（最新版）
   - または .NET 6 SDK（LTS版）
2. インストーラーを実行してインストール
3. インストール後、**PowerShellを再起動**（重要！）

**インストールサイズ**: 約200-300MB（Visual Studioの数GBと比較して非常に軽量）

### 2. .NET Framework Developer Packのインストール

.NET Framework 4.5のターゲットパックが必要です：

1. [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net48)をダウンロード
   - **推奨**: 4.8 Developer Pack（4.5も含まれます）
   - または [.NET Framework 4.5 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net45)（古いバージョン）
2. インストール

**注意**: 
- .NET Framework 4.5はサポートが終了しています
- 4.8 Developer Packをインストールすれば、4.5プロジェクトもビルド可能です
- 可能であれば、プロジェクトを.NET Framework 4.8に移行することを推奨します

### 3. 環境の確認

```powershell
# .NET SDKの確認
dotnet --version

# MSBuildの確認（.NET SDKに含まれています）
dotnet msbuild -version

# または直接MSBuildを使用
msbuild -version
```

### 4. 環境の確認スクリプト

プロジェクトルートで以下のスクリプトを実行して、環境が正しくセットアップされているか確認できます：

```powershell
.\check-dotnet-sdk.ps1
```

### 5. ビルドの実行

```powershell
# .NET SDKのMSBuildを使用（推奨）
dotnet msbuild DesktopMinidamWorking.sln /p:Configuration=Debug

# または、Cursor内で Ctrl+Shift+B を押す
# （.vscode/tasks.jsonで dotnet msbuild を使用するように設定済み）
```

### 6. アプリケーションの実行

ビルド後、生成されたexeファイルを実行：

```powershell
.\DesktopMinidamWorking\bin\Debug\DesktopMinidamWorking.exe
```

## 方法2: Visual Studio Build Tools（代替案）

Visual Studio Build Toolsは、Visual Studio IDEなしでMSBuildだけをインストールする方法です。

1. [Visual Studio Build Tools](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022)をダウンロード
2. インストール時に「.NET desktop build tools」ワークロードのみを選択
3. IDEはインストールされませんが、MSBuildは使用可能になります

**インストールサイズ**: 約1-2GB（方法1より大きい）

## 推奨される方法

**現在のプロジェクト（.NET Framework 4.5）の場合：**
- ✅ **方法1（.NET SDK + .NET Framework Developer Pack）を強く推奨**
  - 最も軽量（約200-300MB）
  - Cursorでの開発に最適
  - モダンな開発ツールチェーン

**インストール手順のまとめ：**
1. [.NET SDK](https://dotnet.microsoft.com/download)をインストール（約200-300MB）
2. [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net48)をインストール
3. PowerShellを再起動
4. `.\check-dotnet-sdk.ps1`で環境を確認
5. Cursorで `Ctrl+Shift+B` でビルド

**将来的な改善：**
- プロジェクトを.NET 6/7/8に移行すれば、.NET SDKのみで完結します
- ただし、WPFアプリの場合は.NET 6以降が必要です
- .NET Framework 4.5は既にサポート終了のため、移行を検討することをお勧めします

## トラブルシューティング

### MSBuildが見つからない場合

.NET SDKをインストールしてもMSBuildが見つからない場合：

1. PowerShellを再起動
2. 環境変数PATHを確認：
```powershell
$env:PATH -split ';' | Select-String -Pattern "dotnet|msbuild"
```

3. .NET SDKのインストールパスを確認：
```powershell
where.exe dotnet
```

通常は `C:\Program Files\dotnet\` にインストールされます。

### .NET Framework 4.5のターゲットが見つからない場合

エラーメッセージ: `error MSB3644: The reference assemblies for framework ".NETFramework,Version=v4.5" were not found.`

解決方法：
1. [.NET Framework 4.5 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net45)をインストール
2. または、.NET Framework 4.8 Developer Packをインストール（4.5も含まれます）

