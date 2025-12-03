# Data Model: タブ付き画像ビューア

**Feature**: 001-tabbed-image-viewer  
**Date**: 2025-12-03

## エンティティ定義

### ImageTabModel

1つのタブで開かれた画像を表すデータモデル。

| フィールド | 型 | 説明 | 制約 |
|-----------|-----|------|------|
| FilePath | string | 画像ファイルの絶対パス | 必須、存在するファイル |
| FileName | string | ファイル名（タブタイトル用） | FilePathから自動取得 |
| Image | BitmapSource? | 読み込まれた画像データ | 遅延読み込み、null可 |
| ZoomLevel | double | 現在のズーム倍率 | デフォルト1.0、範囲0.1〜10.0 |
| IsZoomed | bool | ユーザーがズーム操作したか | ズーム中true、ウィンドウリサイズでfalseにリセット |
| ScrollOffset | Point | スクロール位置 | ズーム時のみ有効 |
| IsLoading | bool | 画像読み込み中フラグ | 読み込み中true |
| LoadError | string? | 読み込みエラーメッセージ | エラー時のみ設定 |

**バリデーション**:
- FilePathは空でないこと
- ZoomLevelは0.1以上10.0以下

---

### SessionData

アプリケーションのセッション情報を永続化するためのモデル。

| フィールド | 型 | 説明 | 制約 |
|-----------|-----|------|------|
| WindowWidth | double | ウィンドウ幅 | 最小200、デフォルト800 |
| WindowHeight | double | ウィンドウ高さ | 最小200、デフォルト450 |
| WindowLeft | double | ウィンドウX座標 | 画面内に収まる値 |
| WindowTop | double | ウィンドウY座標 | 画面内に収まる値 |
| IsMaximized | bool | 最大化状態 | デフォルトfalse |
| OpenTabs | List\<string\> | 開いているタブのファイルパス一覧 | 順序を保持 |
| ActiveTabIndex | int | アクティブなタブのインデックス | OpenTabs範囲内、デフォルト0 |
| Version | int | セッションデータのバージョン | マイグレーション用 |

**バリデーション**:
- WindowWidth/Heightは最小値以上
- ActiveTabIndexはOpenTabsの範囲内
- 復元時に存在しないファイルはOpenTabsから除外

---

### ViewerSettings（将来拡張用）

アプリケーションの設定情報。初期スコープでは最小限。

| フィールド | 型 | 説明 | 制約 |
|-----------|-----|------|------|
| LastOpenDirectory | string? | 最後に開いたディレクトリ | ファイル選択ダイアログ用 |

---

## 状態遷移

### ImageTabModel 状態遷移

```
[未読み込み] ---(読み込み開始)---> [読み込み中]
     ↑                              |
     |                    +---------+---------+
     |                    |                   |
     |              (成功)↓             (失敗)↓
     |            [表示中]              [エラー]
     |                |                       |
     +---(タブ閉じ)---+                       |
     +---(タブ閉じ)---------------------------+
```

### ZoomLevel 状態遷移

```
[自動フィット] ---(Ctrl+ホイール)---> [ズーム中]
      ↑                                   |
      +---(ウィンドウリサイズ)------------+
```

---

## リレーションシップ

```
MainViewModel (1) -----> (*) ImageTabViewModel
      |                          |
      | has                      | wraps
      ↓                          ↓
SessionData              ImageTabModel
```

- MainViewModelは複数のImageTabViewModelを保持（ObservableCollection）
- ImageTabViewModelはImageTabModelをラップし、UI向けプロパティを公開
- SessionDataはMainViewModelの状態をシリアライズ可能な形式で保持
