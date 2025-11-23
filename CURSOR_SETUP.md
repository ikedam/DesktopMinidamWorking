# CursorでC#.NET開発を完結するために必要なもの

## 必須ツール

### 1. Visual Studio Build Tools または Visual Studio Community
このプロジェクトは.NET Framework 4.5を使用しているため、MSBuildが必要です。

**推奨インストール方法：**
- [Visual Studio Build Tools](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022)をダウンロード
- インストール時に「.NET desktop build tools」ワークロードを選択

または

- [Visual Studio Community](https://visualstudio.microsoft.com/ja/downloads/)をインストール
- 「.NET デスクトップ開発」ワークロードを選択

### 2. C#拡張機能（Cursor内）
Cursorには通常、C#のIntelliSenseとデバッグ機能が含まれていますが、必要に応じて：
- C# Dev Kit拡張機能（Microsoft提供）
- C#拡張機能（OmniSharp）

## 開発環境の確認

インストール後、以下のコマンドで確認できます：

```powershell
# MSBuildのパスを確認
& "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" -version

# または、Visual Studio Communityの場合
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" -version

# MSBuildがPATHに追加されているか確認
msbuild -version
```

## MSBuildのパス設定

Visual Studio Build Toolsをインストールすると、通常はMSBuildがPATHに自動追加されます。

**MSBuildがPATHに追加されていない場合：**

1. `find-msbuild.ps1`スクリプトを実行してMSBuildのパスを検出：
```powershell
.\find-msbuild.ps1
```

2. 検出されたパスを環境変数PATHに追加するか、`.vscode/tasks.json`の`command`を直接パスに変更：
```json
"command": "C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe"
```

3. または、Visual Studio Developer Command Promptを使用する場合、以下のコマンドでPATHを設定：
```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"
```

## ビルド方法

### Cursor内でのビルド（推奨）
1. `Ctrl+Shift+P` (または `Cmd+Shift+P`) でコマンドパレットを開く
2. "Tasks: Run Build Task" を選択
3. または `Ctrl+Shift+B` で直接ビルド

### コマンドラインからビルド
```powershell
# Debugビルド
msbuild DesktopMinidamWorking.sln /p:Configuration=Debug

# Releaseビルド
msbuild DesktopMinidamWorking.sln /p:Configuration=Release
```

### Cursor内での開発フロー
1. **コード編集**：Cursorのエディタで直接編集可能（IntelliSense、構文ハイライト対応）
2. **ビルド**：`Ctrl+Shift+B` またはコマンドパレットから実行
3. **実行**：ビルド後、`DesktopMinidamWorking\bin\Debug\DesktopMinidamWorking.exe` を直接実行
4. **デバッグ**：.NET Frameworkアプリの場合は、Visual Studioを使用するか、外部デバッガーをアタッチ

## 現在のプロジェクト構成

- **フレームワーク**: .NET Framework 4.5
- **プロジェクトタイプ**: WPFアプリケーション
- **ビルドシステム**: MSBuild（旧形式の.csproj）
- **設定ファイル**: `.vscode/tasks.json`（ビルドタスク）、`.vscode/settings.json`（エディタ設定）

## 作成された設定ファイル

- `.vscode/tasks.json` - ビルドタスクの定義
- `.vscode/settings.json` - エディタ設定（XAMLファイルの認識など）
- `.vscode/launch.json` - デバッグ設定（.NET Framework用）
- `find-msbuild.ps1` - MSBuildパス検出スクリプト

## 動作確認

セットアップが完了したら、以下の手順で動作確認してください：

1. **MSBuildの確認**
```powershell
msbuild -version
```
バージョン情報が表示されればOKです。

2. **プロジェクトのビルド**
   - Cursorで `Ctrl+Shift+B` を押す
   - または、コマンドパレット（`Ctrl+Shift+P`）から "Tasks: Run Build Task" を選択
   - ビルドが成功すれば、`DesktopMinidamWorking\bin\Debug\DesktopMinidamWorking.exe` が生成されます

3. **アプリケーションの実行**
   - ビルド後、生成されたexeファイルをダブルクリックして実行
   - または、ターミナルから：
```powershell
.\DesktopMinidamWorking\bin\Debug\DesktopMinidamWorking.exe
```

## 今後の改善提案

もし将来的にモダンな開発環境に移行したい場合：
- .NET 6/7/8への移行を検討（SDKスタイルのプロジェクト形式）
- これにより`dotnet build`コマンドが使用可能になります
- ただし、.NET Framework固有の機能（WPF、Windows Formsなど）を使用している場合は、.NET Frameworkのままでも問題ありません

