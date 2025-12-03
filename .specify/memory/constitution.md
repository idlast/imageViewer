<!--
==========================================================================
SYNC IMPACT REPORT
==========================================================================
Version change: N/A → 1.0.0 (initial ratification)
Modified principles: N/A (initial creation)
Added sections:
  - Core Principles (3 principles)
  - Technical Standards
  - Development Workflow
  - Governance
Removed sections: N/A
Templates requiring updates:
  - .specify/templates/plan-template.md: ✅ No changes required
  - .specify/templates/spec-template.md: ✅ No changes required
  - .specify/templates/tasks-template.md: ✅ No changes required
Follow-up TODOs: None
==========================================================================
-->

# ImgViewer Constitution

## Core Principles

### I. UX-First Design

WPF画像ビューアとして、ユーザー体験を最優先する。
- 画像の表示・操作はレスポンシブでなければならない（目標: 100ms以内の応答）
- Windowsの規定アプリケーションとして、OS標準の操作感との一貫性を保つ
- キーボードショートカット、マウス操作、タッチ操作のすべてに対応する
    - 変更。タッチ操作は不要。
- ファイル関連付け登録によるシームレスな起動体験を提供する

### II. Performance & Stability

大量の画像ファイルや大きなサイズの画像を扱う際も、安定したパフォーマンスを維持する。
- メモリ効率を考慮した画像キャッシュ戦略を採用する
- 非同期処理によりUIスレッドをブロックしない
- サポート対象フォーマット: JPEG, PNG, GIF, BMP, TIFF, WebP, HEIC
- エラー発生時もアプリケーションがクラッシュしない堅牢性を確保する

### III. MVVM Architecture

WPFアプリケーションとして、MVVM（Model-View-ViewModel）パターンを厳守する。
- ViewはXAMLで定義し、コードビハインドは最小限に抑える
- ViewModelはINotifyPropertyChangedを実装し、データバインディングを活用する
- Modelはビジネスロジックとデータアクセスを担当する
- 依存性注入によりテスタビリティを確保する

## Technical Standards

**プラットフォーム**: Windows 10/11  
**フレームワーク**: .NET 8.0 + WPF  
**言語**: C# 12  
**アーキテクチャ**: MVVM  
**Nullable参照型**: 有効（必須）  
**ファイル関連付け**: Windows Registry / MSIX パッケージ対応  

## Development Workflow

1. 機能追加は仕様書（spec.md）から開始する
2. 実装計画（plan.md）でMVVM構造を設計する
3. ViewModelのユニットテストを作成する（UIテストはオプション）
4. 実装後、手動でのUXテストを実施する
5. Windowsの規定アプリ設定でテストを行う

## Governance

本憲法はImgViewerプロジェクトの最上位の指針である。
- 新機能追加時は必ずCore Principlesとの整合性を確認する
- パフォーマンスに影響する変更は、ベンチマーク測定を行う
- 憲法の修正は、変更理由・影響範囲の文書化を必須とする
- バージョニング: MAJOR.MINOR.PATCH（セマンティックバージョニング準拠）

**Version**: 1.0.0 | **Ratified**: 2025-12-03 | **Last Amended**: 2025-12-03
