# Tasks: タブ付き画像ビューア

**Input**: Design documents from `/specs/001-tabbed-image-viewer/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: テストはオプション。明示的に要求された場合のみ含める。

**Organization**: タスクはユーザーストーリーごとにグループ化され、各ストーリーを独立して実装・テスト可能。

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 並列実行可能（異なるファイル、依存関係なし）
- **[Story]**: 対象ユーザーストーリー（US1, US2, US3...）
- ファイルパスは正確に記載

## Path Conventions

- **プロジェクトルート**: 既存の `ImgViewer.csproj` がある場所
- **新規ディレクトリ**: `Models/`, `ViewModels/`, `Views/`, `Services/`, `Converters/`

---

## Phase 1: セットアップ（共通基盤）

**Purpose**: プロジェクト構造の整備と依存パッケージの追加

- [x] T001 CommunityToolkit.Mvvm パッケージを追加（`dotnet add package CommunityToolkit.Mvvm`）
- [x] T002 [P] Magick.NET パッケージを追加（WebP/HEIC対応: `dotnet add package Magick.NET-Q16-AnyCPU`）
- [x] T003 [P] プロジェクト構造のディレクトリ作成（Models/, ViewModels/, Views/, Services/, Converters/）
- [x] T004 [P] App.xaml.cs に DI コンテナ設定を追加

---

## Phase 2: 基盤構築（ブロッキング前提条件）

**Purpose**: すべてのユーザーストーリーに必要なコア基盤

**⚠️ CRITICAL**: このフェーズが完了するまでユーザーストーリーの実装は開始不可

- [x] T005 [P] ImageTabModel クラスを作成（Models/ImageTabModel.cs）
- [x] T006 [P] SessionData クラスを作成（Models/SessionData.cs）
- [x] T007 [P] IImageService インターフェースを作成（Services/IImageService.cs）
- [x] T008 [P] ISessionService インターフェースを作成（Services/ISessionService.cs）
- [x] T009 ImageService クラスを実装（Services/ImageService.cs）- 画像読み込み、サポートフォーマット判定
- [x] T010 SessionService クラスを実装（Services/SessionService.cs）- JSON保存/読み込み
- [x] T011 [P] ImageTabViewModel クラスを作成（ViewModels/ImageTabViewModel.cs）- ImageTabModelをラップ
- [x] T012 MainViewModel クラスの基本構造を作成（ViewModels/MainViewModel.cs）- ObservableCollection<ImageTabViewModel>

**Checkpoint**: 基盤完了 - ユーザーストーリー実装開始可能

---

## Phase 3: User Story 1 & 2 - 画像表示とタブ管理 (Priority: P1) 🎯 MVP

**Goal**: 画像ファイルを開いてタブで管理できる基本機能を実現する

**Independent Test**: 
- ファイルメニューから画像を開き、ウィンドウに収まるサイズで表示される
- 複数の画像をタブで開き、クリックで切り替えられる

### Implementation for User Story 1 & 2

- [x] T013 [US1] MainWindow.xaml にメニューバーを追加（ファイル > 開く）
- [x] T014 [US1] MainWindow.xaml に TabControl を追加（ItemsSource バインディング）
- [x] T015 [US1] [P] ImageTabView.xaml を作成（Views/ImageTabView.xaml）- 画像表示用UserControl
- [x] T016 [US1] MainViewModel に OpenFileCommand を実装
- [x] T017 [US1] MainViewModel にファイルダイアログ処理を追加
- [x] T018 [US1] ImageTabViewModel に画像読み込みロジックを追加（非同期）
- [x] T019 [US1] ImageTabView に Image コントロールと Viewbox を追加（自動フィット）
- [x] T020 [US2] MainViewModel に AddTab / RemoveTab メソッドを実装
- [x] T021 [US2] MainViewModel に SelectedTab プロパティを追加
- [x] T022 [US2] TabControl の TabItem テンプレートを作成（ファイル名 + 閉じるボタン）
- [x] T023 [US2] MainViewModel に CloseTabCommand を実装
- [x] T024 [US1] MainWindow.xaml にドラッグ＆ドロップ処理を追加（AllowDrop、Dropイベント）
- [x] T025 [US1] MainWindow.xaml.cs にドロップハンドラーを追加（コードビハインド最小限）

**Checkpoint**: 画像を開いてタブで管理できる状態

---

## Phase 4: User Story 3 - 画像の拡大・縮小 (Priority: P2)

**Goal**: Ctrl+ホイールで画像を拡大縮小、ウィンドウリサイズで自動フィットにリセット

**Independent Test**: 
- Ctrl+ホイールで拡大縮小できる
- ウィンドウサイズ変更で自動フィットに戻る

### Implementation for User Story 3

- [x] T026 [US3] ImageTabViewModel に ZoomLevel プロパティを追加
- [x] T027 [US3] ImageTabViewModel に IsZoomed プロパティを追加
- [x] T028 [US3] ImageTabView に ScaleTransform を追加（ZoomLevel バインディング）
- [x] T029 [US3] ImageTabView に PreviewMouseWheel イベントハンドラーを追加
- [x] T030 [US3] Ctrl+ホイールでズーム倍率を変更するロジックを実装
- [x] T031 [US3] ImageTabView に ScrollViewer を追加（ズーム時のスクロール対応）
- [x] T032 [US3] MainWindow の SizeChanged イベントでズームリセットを実装
- [x] T033 [US3] MainViewModel に ZoomIn / ZoomOut / ResetZoom コマンドを追加（メニュー用）

**Checkpoint**: ズーム機能が動作する状態

---

## Phase 5: User Story 5 - セッション復元 (Priority: P2)

**Goal**: アプリ終了時の状態を保存し、次回起動時に復元する

**Independent Test**: 
- タブを開いてウィンドウサイズを変更し、終了→再起動で状態復元

### Implementation for User Story 5

- [x] T034 [US5] MainViewModel に CreateSessionData メソッドを追加
- [x] T035 [US5] MainViewModel に RestoreFromSession メソッドを追加
- [x] T036 [US5] App.xaml.cs の OnStartup でセッション復元を呼び出し
- [x] T037 [US5] MainWindow の Closing イベントでセッション保存を呼び出し
- [x] T038 [US5] SessionService で存在しないファイルをフィルタリングするロジックを追加
- [x] T039 [US5] MainWindow に WindowState / Left / Top バインディングを追加
- [x] T040 [US5] SessionData のバリデーション（画面外チェック）を追加

**Checkpoint**: セッション復元が動作する状態

---

## Phase 6: User Story 4 - 常に最前面表示 (Priority: P3)

**Goal**: メニューから最前面表示をトグルできる

**Independent Test**: 
- メニューから設定切り替え、他のウィンドウをクリックしても前面に留まる

### Implementation for User Story 4

- [x] T041 [US4] MainViewModel に IsAlwaysOnTop プロパティを追加
- [x] T042 [US4] MainViewModel に ToggleAlwaysOnTopCommand を追加
- [x] T043 [US4] MainWindow.xaml に「表示」メニューを追加
- [x] T044 [US4] MainWindow.Topmost を IsAlwaysOnTop にバインド
- [x] T045 [US4] メニュー項目に IsChecked バインディングを追加

**Checkpoint**: 最前面表示が動作する状態

---

## Phase 7: 仕上げ・横断的対応

**Purpose**: 複数ストーリーにまたがる改善

- [x] T046 [P] エラーハンドリングの統一（画像読み込み失敗時のUI表示）
- [x] T047 [P] ローディング表示の追加（IsLoading プロパティ活用）
- [x] T048 大容量画像のメモリ効率化（DecodePixelWidth 設定）
- [x] T049 [P] タブ多数時のタブバースクロール対応
- [x] T050 WebP/HEIC 画像のサポート確認（Magick.NET 統合テスト）
- [x] T051 quickstart.md のチェックリストで動作確認

---

## Phase 8: 既定アプリ連携の改善

**Purpose**: 既定のアプリとして複数ファイルを開いた際も単一ウィンドウでタブ追加のみを行う

- [x] T052 単一インスタンスコーディネーターを追加（Services/SingleInstanceCoordinator.cs）
- [x] T053 App.xaml.cs に単一インスタンス制御と引数転送処理を組み込む
- [x] T054 既存ウィンドウをアクティブ化しタブ追加するハンドラを共通化

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (セットアップ)**: 依存なし - 即座に開始可能
- **Phase 2 (基盤構築)**: Phase 1 完了後 - すべてのユーザーストーリーをブロック
- **Phase 3 (US1 & US2)**: Phase 2 完了後 - MVP として最優先
- **Phase 4 (US3)**: Phase 3 完了後 - ズーム機能は画像表示が必要
- **Phase 5 (US5)**: Phase 3 完了後 - タブ機能が必要
- **Phase 6 (US4)**: Phase 2 完了後 - 他のストーリーと独立
- **Phase 7 (仕上げ)**: すべてのストーリー完了後

### Parallel Opportunities

```text
Phase 1 完了後:
  T005 ─┬─ T006 ─┬─ T007 ─┬─ T008  (すべて並列可能)
        │        │        │
        └────────┴────────┴──────> Phase 2 完了

Phase 3 完了後:
  Phase 4 (US3) ─┬─ Phase 5 (US5)  (並列可能)
                 │
  Phase 6 (US4) ─┘  (Phase 2 完了後から並列可能)
```

---

## Implementation Strategy

### MVP スコープ（推奨）

**最小限で価値を届ける場合**: Phase 1 + Phase 2 + Phase 3 (US1 & US2)

これにより以下が実現:
- 画像をタブで開ける
- 複数タブの管理
- 自動フィット表示
- ドラッグ＆ドロップ対応

### 完全スコープ

すべての Phase を順次実装

---

## Summary

| 項目 | 値 |
|------|-----|
| 総タスク数 | 54 |
| Phase 1 (セットアップ) | 4 タスク |
| Phase 2 (基盤構築) | 8 タスク |
| Phase 3 (US1 & US2) | 13 タスク |
| Phase 4 (US3) | 8 タスク |
| Phase 5 (US5) | 7 タスク |
| Phase 6 (US4) | 5 タスク |
| Phase 7 (仕上げ) | 6 タスク |
| Phase 8 (既定アプリ) | 3 タスク |
| 並列可能タスク | 23 タスク（[P] マーク付き） |
| MVP タスク数 | 25 タスク（Phase 1〜3） |
