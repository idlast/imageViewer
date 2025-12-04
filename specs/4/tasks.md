# Tasks: タブドラッグ並び替え

**Input**: `/specs/4/plan.md`
**Repository Root**: `g:/imageViewer`

タスクはフェーズ単位で順番に実施する。`[P]` は並列実行可能なタスクを表す。

---

## Phase: Setup

- [X] T401 仕様ドキュメント確認 — `specs/4/plan.md` を精読し影響範囲を洗い出す

## Phase: Tests

- [X] T402 手動テスト計画 — タブの追加/削除/ドラッグ操作パターンと回帰観点を列挙し `README.md` のメモに追記（任意、必要な場合のみ）

## Phase: Core Implementation

- [X] T403 `MainViewModel.cs` に `MoveTab` API を追加し、ObservableCollection の順序変更をカプセル化
- [X] T404 `MainWindow.xaml` の `TabItem` スタイルへドラッグ関連イベント (AllowDrop, PreviewMouseLeftButtonDown, PreviewMouseMove, Drop) を追加
- [X] T405 `MainWindow.xaml.cs` にドラッグ開始/終了ロジックと `DragDrop.DoDragDrop` を実装。ドロップ時に `MoveTab` を呼び出す

## Phase: Integration

- [X] T406 既存のコンテキストメニューや閉じるボタンと干渉しないことを確認し、必要ならヒットテストの除外処理を追加 (`MainWindow.xaml.cs`)

## Phase: Polish

- [ ] T407 セッション復元（`SessionService` 経由）で並びが保持されることを手動確認し、異常時ログやコメントを追加せず記録 (`SessionService.cs` には変更不要を確認)

## Phase: Visual Effects

- [X] T408 `plan.md` にブラウザ風ドラッグ＆スワップ戦略を追記
- [X] T409 `MainWindow.xaml` に TabControl の参照名、`TranslateTransform` セッター、ドラッグ解放イベントを追加
- [X] T410 `MainWindow.xaml.cs` で DragDrop を使わない追従ロジックを実装し、ポインタ位置に応じて `MoveTab` を即時呼び出す
- [X] T411 同ファイルにスワップ時のアニメーションとクリーンアップ処理を追加（ZIndex / MouseCapture / LostMouseCapture 対応）
- [ ] T412 ビルドと手動テストで視覚効果を確認し、README のメモを更新

---

**完了条件**: すべてのタスクが `[X]` になり、手動テストでドラッグ並び替えが安定して動作すること。