# Service Contracts: タブ付き画像ビューア

**Feature**: 001-tabbed-image-viewer  
**Date**: 2025-12-03

## IImageService

画像ファイルの読み込みと処理を担当するサービス。

```csharp
public interface IImageService
{
    /// <summary>
    /// 画像ファイルを非同期で読み込む
    /// </summary>
    /// <param name="filePath">画像ファイルの絶対パス</param>
    /// <param name="maxDecodeWidth">デコード時の最大幅（省略時は元サイズ）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>読み込まれたBitmapSource</returns>
    Task<BitmapSource> LoadImageAsync(
        string filePath, 
        int? maxDecodeWidth = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ファイルがサポートされている画像形式かどうかを判定
    /// </summary>
    bool IsSupportedFormat(string filePath);

    /// <summary>
    /// サポートされているファイル拡張子の一覧を取得
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }
}
```

### 振る舞い

| メソッド | 入力 | 出力 | エラー時 |
|---------|------|------|----------|
| LoadImageAsync | 有効なファイルパス | BitmapSource | FileNotFoundException, InvalidOperationException |
| IsSupportedFormat | ファイルパス | true/false | - |

---

## ISessionService

セッション情報の保存と復元を担当するサービス。

```csharp
public interface ISessionService
{
    /// <summary>
    /// セッション情報を保存する
    /// </summary>
    Task SaveSessionAsync(SessionData session);

    /// <summary>
    /// セッション情報を読み込む
    /// </summary>
    /// <returns>セッション情報、存在しない場合はデフォルト値</returns>
    Task<SessionData> LoadSessionAsync();

    /// <summary>
    /// セッションファイルが存在するか
    /// </summary>
    bool SessionExists { get; }

    /// <summary>
    /// セッションをクリアする
    /// </summary>
    Task ClearSessionAsync();
}
```

### 振る舞い

| メソッド | 入力 | 出力 | エラー時 |
|---------|------|------|----------|
| SaveSessionAsync | SessionData | void | IOException（書き込み失敗） |
| LoadSessionAsync | - | SessionData | デフォルト値を返す（例外投げない） |
| ClearSessionAsync | - | void | 例外投げない |

---

## Commands (MainViewModel)

ユーザーアクションに対応するコマンド。

| コマンド | パラメータ | 説明 | CanExecute条件 |
|---------|-----------|------|----------------|
| OpenFileCommand | - | ファイル選択ダイアログを開く | 常にtrue |
| CloseTabCommand | ImageTabViewModel | 指定タブを閉じる | タブが存在する |
| CloseCurrentTabCommand | - | 現在のタブを閉じる | タブが存在する |
| ToggleAlwaysOnTopCommand | - | 最前面表示を切り替え | 常にtrue |
| ZoomInCommand | - | 拡大 | 画像が表示されている |
| ZoomOutCommand | - | 縮小 | 画像が表示されている |
| ResetZoomCommand | - | ズームをリセット | ズーム中 |

---

## Events

### MainViewModel Events

| イベント | 発火タイミング | データ |
|---------|---------------|--------|
| TabAdded | タブ追加時 | ImageTabViewModel |
| TabRemoved | タブ削除時 | ImageTabViewModel |
| ActiveTabChanged | アクティブタブ変更時 | ImageTabViewModel? |

### ImageTabViewModel Events

| イベント | 発火タイミング | データ |
|---------|---------------|--------|
| ImageLoaded | 画像読み込み完了時 | BitmapSource |
| ImageLoadFailed | 画像読み込み失敗時 | Exception |
| ZoomChanged | ズーム倍率変更時 | double |
