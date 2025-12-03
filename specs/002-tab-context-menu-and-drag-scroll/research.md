# Research: タブ付き画像ビューア

**Feature**: 001-tabbed-image-viewer  
**Date**: 2025-12-03

## 調査項目

### 1. WPFでのタブコントロール実装

**Decision**: TabControlを使用し、ItemsSourceでViewModelコレクションにバインド

**Rationale**: 
- WPF標準のTabControlはMVVMパターンと相性が良い
- ItemTemplateとContentTemplateでタブヘッダーとコンテンツを分離可能
- ObservableCollectionとの連携で動的なタブ追加・削除が容易

**Alternatives considered**:
- サードパーティライブラリ（MahApps.Metro等）→ 学習コストと依存性増加のため却下
- 独自TabControl実装 → 標準機能で十分なため却下

---

### 2. 画像フォーマットのサポート

**Decision**: WPF標準のBitmapImage + 追加ライブラリでWebP/HEIC対応

**Rationale**:
- JPEG, PNG, GIF, BMP, TIFFはWPF標準でサポート
- WebPはMicrosoft.Toolkit.Uwp.UI.Controls または Magick.NETで対応
- HEICはMagick.NET（ImageMagick）で対応

**Alternatives considered**:
- SkiaSharp → クロスプラットフォーム向けで今回は過剰
- 標準のみ → WebP/HEIC未対応のため要件を満たさない

---

### 3. 非同期画像読み込み

**Decision**: async/awaitでバックグラウンド読み込み、BitmapImage.BeginInitで遅延読み込み

**Rationale**:
- Task.Runでファイル読み込みをバックグラウンド化
- BitmapImage.CacheOption = OnLoadでメモリにキャッシュ
- DecodePixelWidth/Heightで表示サイズに応じた効率的なデコード

**Alternatives considered**:
- BackgroundWorker → 旧式、async/awaitの方がモダン
- ReactiveExtensions → 今回のスコープには過剰

---

### 4. セッション保存形式

**Decision**: System.Text.JsonでJSON形式、AppData\Local\ImgViewer\session.json

**Rationale**:
- .NET標準ライブラリで追加依存なし
- 人間が読める形式でデバッグ容易
- スキーマ変更に柔軟に対応可能

**Alternatives considered**:
- XML (XmlSerializer) → JSONより冗長
- バイナリ形式 → デバッグ困難、メリット薄い
- SQLite → 単純なセッション情報には過剰

---

### 5. MVVMフレームワーク選定

**Decision**: CommunityToolkit.Mvvm（旧 Microsoft.Toolkit.Mvvm）

**Rationale**:
- Microsoft公式サポート
- Source Generatorによるボイラープレート削減
- [ObservableProperty]、[RelayCommand]属性で簡潔な記述
- 軽量で学習コストが低い

**Alternatives considered**:
- Prism → 大規模アプリ向け、今回は過剰
- ReactiveUI → リアクティブプログラミング習熟が必要
- 自前実装 → 車輪の再発明

---

### 6. 画像スケーリングとズーム

**Decision**: Viewbox + ScaleTransformの組み合わせ

**Rationale**:
- ViewboxのStretch="Uniform"でウィンドウフィット
- ScaleTransformで拡大縮小を重ねがけ
- RenderTransformOriginで拡大中心を制御

**Alternatives considered**:
- ScrollViewerのみ → ズーム機能の実装が複雑
- Image.Stretchのみ → 拡大縮小の細かい制御が困難

---

### 7. ドラッグ＆ドロップ実装

**Decision**: WPF標準のDragDropイベント + AllowDrop="True"

**Rationale**:
- PreviewDragOver、Dropイベントでファイルパス取得
- DataFormats.FileDropでファイルパス配列を取得
- 複数ファイル同時ドロップにも対応可能

**Alternatives considered**:
- GongSolutions.Wpf.DragDrop → 内部D&D向け、ファイルドロップには過剰

---

## 技術スタック確定

| カテゴリ | 選定技術 |
|---------|---------|
| フレームワーク | .NET 8.0 + WPF |
| MVVM | CommunityToolkit.Mvvm |
| 画像処理 | BitmapImage + Magick.NET (WebP/HEIC) |
| シリアライズ | System.Text.Json |
| テスト | xUnit + Moq |
| ビルド | dotnet CLI / Visual Studio |
