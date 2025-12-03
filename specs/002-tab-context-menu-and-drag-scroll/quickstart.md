# Quickstart: タブ付き画像ビューア

**Feature**: 001-tabbed-image-viewer  
**Date**: 2025-12-03

## 前提条件

- .NET 8.0 SDK
- Visual Studio 2022 または VS Code + C# Dev Kit
- Windows 10/11

## セットアップ

### 1. 依存パッケージのインストール

```powershell
cd ImgViewer
dotnet add package CommunityToolkit.Mvvm
dotnet add package Magick.NET-Q16-AnyCPU
```

### 2. プロジェクト構造の作成

```powershell
mkdir Models, ViewModels, Views, Services, Converters
```

### 3. ビルドと実行

```powershell
dotnet build
dotnet run
```

## 開発の流れ

### Phase 1: 基盤構築

1. `Models/ImageTabModel.cs` - タブのデータモデル
2. `Models/SessionData.cs` - セッション情報モデル
3. `Services/IImageService.cs` + `ImageService.cs` - 画像読み込み
4. `Services/ISessionService.cs` + `SessionService.cs` - セッション管理

### Phase 2: ViewModel

1. `ViewModels/ImageTabViewModel.cs` - タブのViewModel
2. `ViewModels/MainViewModel.cs` - メインウィンドウのViewModel

### Phase 3: View

1. `Views/ImageTabView.xaml` - タブコンテンツのUserControl
2. `MainWindow.xaml` - メニュー、タブ、画像表示エリア

### Phase 4: 機能実装

1. ファイルを開く（メニュー/D&D）
2. タブ管理（追加/削除/切り替え）
3. 画像スケーリング（自動フィット）
4. ズーム機能（Ctrl+ホイール）
5. 最前面表示
6. セッション保存/復元

## テスト実行

```powershell
cd Tests/ImgViewer.Tests
dotnet test
```

## 動作確認チェックリスト

- [ ] アプリケーションが起動する
- [ ] ファイルメニューから画像を開ける
- [ ] ドラッグ＆ドロップで画像を開ける
- [ ] タブが追加される
- [ ] タブを閉じられる
- [ ] タブ切り替えで画像が変わる
- [ ] ウィンドウリサイズで画像がフィットする
- [ ] Ctrl+ホイールでズームできる
- [ ] 表示メニューで最前面表示を切り替えられる
- [ ] アプリ終了→再起動でセッションが復元される
