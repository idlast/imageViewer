# Implementation Plan: タブ付き画像ビューア

**Branch**: `001-tabbed-image-viewer` | **Date**: 2025-12-03 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-tabbed-image-viewer/spec.md`

## Summary

Windows 11のメモ帳と同様のタブ機能を持った画像ビューアアプリケーション。複数の画像をタブで同時に開き、ウィンドウサイズに合わせた自動フィット表示、Ctrl+ホイールでの拡大縮小、常に最前面表示、セッション復元機能を提供する。WPF + .NET 8.0でMVVMアーキテクチャを採用して実装する。

## Technical Context

**Language/Version**: C# 12 / .NET 8.0  
**Primary Dependencies**: WPF, CommunityToolkit.Mvvm (MVVMフレームワーク), System.Text.Json (セッション保存)  
**Storage**: ローカルファイル（AppData\Local\ImgViewer\session.json）  
**Testing**: MSTest または xUnit（ViewModelのユニットテスト）  
**Target Platform**: Windows 10/11  
**Project Type**: single（WPFデスクトップアプリケーション）  
**Performance Goals**: 画像表示100ms以内、タブ切り替え50ms以内、セッション復元500ms以内  
**Constraints**: UIスレッドをブロックしない非同期処理、大容量画像のメモリ効率  
**Scale/Scope**: 同時20タブ程度、10000x10000px以上の画像対応

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| 原則 | 状態 | 確認事項 |
|------|------|----------|
| I. UX-First Design | ✅ PASS | 100ms応答目標設定済み、Windows標準操作（D&D、メニュー）対応 |
| II. Performance & Stability | ✅ PASS | 非同期処理設計、サポートフォーマット明確、エラー処理要件あり |
| III. MVVM Architecture | ✅ PASS | Model/View/ViewModel分離設計、CommunityToolkit.Mvvm採用 |

**ゲート判定**: ✅ 全原則に適合。Phase 0に進行可能。

## Project Structure

### Documentation (this feature)

```text
specs/001-tabbed-image-viewer/
├── plan.md              # このファイル
├── research.md          # Phase 0 出力
├── data-model.md        # Phase 1 出力
├── quickstart.md        # Phase 1 出力
├── contracts/           # Phase 1 出力
└── tasks.md             # Phase 2 出力（/speckit.tasksで作成）
```

### Source Code (repository root)

```text
ImgViewer/
├── App.xaml                    # アプリケーションエントリポイント
├── App.xaml.cs
├── MainWindow.xaml             # メインウィンドウ（View）
├── MainWindow.xaml.cs
├── Models/
│   ├── ImageTabModel.cs        # タブのデータモデル
│   └── SessionData.cs          # セッション情報モデル
├── ViewModels/
│   ├── MainViewModel.cs        # メインウィンドウのViewModel
│   └── ImageTabViewModel.cs    # 各タブのViewModel
├── Views/
│   └── ImageTabView.xaml       # タブコンテンツのView（UserControl）
├── Services/
│   ├── IImageService.cs        # 画像読み込みサービスインターフェース
│   ├── ImageService.cs         # 画像読み込み実装
│   ├── ISessionService.cs      # セッション管理インターフェース
│   └── SessionService.cs       # セッション管理実装
└── Converters/
    └── ImageScaleConverter.cs  # 画像スケール用コンバーター

Tests/
└── ImgViewer.Tests/
    ├── ViewModels/
    │   ├── MainViewModelTests.cs
    │   └── ImageTabViewModelTests.cs
    └── Services/
        ├── ImageServiceTests.cs
        └── SessionServiceTests.cs
```

**Structure Decision**: WPFアプリケーションとしてMVVM構造を採用。Models/ViewModels/Views/Servicesの4層構成で責務を分離。

## Complexity Tracking

> 憲法チェックに違反はないため、このセクションは空。
