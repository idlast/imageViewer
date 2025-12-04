# Implementation Plan: タブドラッグによる並び替え

**Branch**: `4` | **Date**: 2025-12-04

## Summary

既存のタブ付き画像ビューアに「タブ見出しをドラッグ＆ドロップして順序を入れ替える」操作を追加する。Windows 11 メモ帳と同じ感覚でタブ順を調整できるようにし、ファイルを開いた順に依存しない柔軟なワークフローを提供する。

## Technical Context

- **UI レイヤー**: WPF / .NET 8、TabControl + TabPanel をカスタムスタイルで利用
- **状態管理**: `MainViewModel.Tabs` (ObservableCollection) がタブ順を表現
- **永続化**: セッション保存は Tabs の順序をそのまま JSON に記録するため、新機能でも追加作業なしで整合
- **制約**: 既存のタブクローズボタンやコンテキストメニューと競合せず、コードビハインドは必要最小限に保つ

## Architecture & File Impact

| 層 | ファイル | 変更点 |
|----|----------|--------|
| View | `MainWindow.xaml` | TabItem スタイルにドラッグ系イベント (PreviewMouseLeftButtonDown/Move/Drop) を追加。ドラッグ中のヒットテストを助ける添付プロパティを最小限で導入。 |
| View (code-behind) | `MainWindow.xaml.cs` | ドラッグ状態のトラッキング、`DragDrop.DoDragDrop` 呼び出し、ドロップ時に対象タブと挿入位置を算出するヘルパーを追加。 |
| ViewModel | `MainViewModel.cs` | コレクション順序を変更する `MoveTab(int fromIndex, int toIndex)` API と、`ImageTabViewModel` を受け取る `MoveTab(ImageTabViewModel source, ImageTabViewModel target)` オーバーロードを実装。 |

## Risks & Mitigations

1. **Drop 位置の曖昧さ**: TabPanel は ItemsControl ではないため、挿入インデックス計算をヒットした TabItem から求める。`ItemsControl.ContainerFromElement` を使い、タブ要素基準で before/after を判断する。
2. **ドラッグが誤発火する**: 短いクリックで DragDrop が開始されないよう、`SystemParameters.MinimumHorizontalDragDistance` を利用してドラッグ閾値を判定。
3. **ViewModel との同期**: `ObservableCollection.Move` を利用し、INotifyCollectionChanged を正しく発火させる。

## Implementation Steps

1. **入力ハンドリングの準備**
   - TabItem に `MouseLeftButtonDown` でドラッグ候補を記録し、`PreviewMouseMove` で閾値超え時に `DragDrop.DoDragDrop` を開始。
   - DragDrop データには `ImageTabViewModel` を格納し、`Effects.Move` を設定。

2. **ドロップ処理**
   - `Drop` イベントでソース/ターゲットタブを取得。
   - 視覚的にタブの左半分へドロップされた場合はターゲットの前、右半分なら後ろに挿入する簡易ロジックを採用。

3. **ViewModel 移動メソッド**
   - `MoveTab(ImageTabViewModel tab, int newIndex)` を追加し、境界チェックを実施。
   - 既存の `SelectedTab` を維持するため、移動後に `SelectedTab` をソースタブへ再設定。

4. **フォーカスとアクセシビリティ**
   - ドロップ後に対象タブを選択状態にし、キーボード操作でも順序が反映されるようにする。

## Visual Switch Animation (追加強化)

**目的**: ブラウザのようにタブをドラッグした距離だけ追従させ、隣接タブの中央を越えたタイミングで滑らかに位置を入れ替える。

1. **DragDrop 非依存の入力処理**
   - TabItem の `PreviewMouseLeftButtonDown/Move/Up` を用いてポインタ位置を継続追跡。
   - `Mouse.Capture` でドラッグ中のタブを固定し、`TranslateTransform` で水平移動を表現。

2. **閾値判定と即時スワップ**
   - TabPanel 上の座標系でドラッグ中タブの中心を算出し、右/左隣のタブ中心と比較。
   - 閾値を越えた瞬間に `ObservableCollection.Move` を呼び、実際の順序も即時更新。

3. **アニメーション効果**
   - ドラッグ中タブ: 直接 `TranslateTransform.X` を更新して追従。
   - 押しのけられるタブ: `TranslateTransform` にオフセットを与え、`DoubleAnimation` (120〜150ms, EaseOut) でゼロへ復帰させ、滑らかな「くるっと」切り替えを表現。
   - ドラッグ終了時はタブをアニメーションで最終位置へ戻し、`ZIndex` やキャプチャを解除。

4. **フォールバックと安全策**
   - 予期せぬ `LostMouseCapture` やウィンドウ外へ出た場合でもクリーンアップする共通メソッドを用意。
   - 既存のドラッグ＆ドロップでファイルを開く処理には影響しないよう、メインウィンドウ側の `OnDrop` ハンドラはそのまま維持。

## Validation Strategy

- 複数タブを開き、ドラッグ&ドロップで順序が変わることを UI 上で確認。
- タブを閉じたり再度開いた後も、保存された順序がセッション復元時に再現されることを確認。
- 右クリックメニューやタブクローズボタン操作との競合がないか手動テスト。`
