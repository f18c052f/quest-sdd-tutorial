# tech — 技術の前提と規約

## 構成

| 項目 | 固定する値 |
| --- | --- |
| エンジン | Unity 6 |
| XR プラグイン | OpenXR。Oculus XR Plugin は使わない（非推奨・削除予定） |
| SDK | Meta XR SDK（Core SDK + Interaction SDK） |
| 動作の確認 | Meta XR Simulator。実機は使わない |
| 入力 | 右手コントローラーのトリガー |
| テスト | Unity Test Framework（UTF） |
| Editor 操作 | Unity 公式 Claude Code プラグイン（Unity CLI）。サードパーティ製の Unity MCP は併用しない |

バージョン番号は README に書く。ここには書かない（更新のたびに嘘になるため）。

## レイヤー境界

ガイドライン第2章の L1〜L4 を、このプロジェクトでは次のアセンブリに対応させる。

| 層 | アセンブリ | 中身 | 検証 |
| --- | --- | --- | --- |
| L1 Core | `MosquitoSpray.Core` | 命中判定の計算、ラウンドの状態遷移、スコア、蚊の飛行状態、出現ルール | EditMode |
| L2 Adapter | `MosquitoSpray.Adapter` | Core と GameObject の橋渡し。入力イベント、Core の状態の反映 | EditMode + PlayMode |
| L2/L3 XR | `MosquitoSpray.XR` | Meta XR SDK に直接触る実装。カメラリグ、パススルー、コントローラー入力 | シミュレータ |
| L3 Spatial | シーンとプレハブ | 配置、寸法、当たり判定の形、アニメーション時間 | シミュレータ（数値は PlayMode） |
| L4 Presentation | マテリアル、エフェクト、音 | 見た目と演出 | 人間の目のみ |

依存の向きは **Core ← Adapter ← XR** の一方向。逆向きの参照は作らない。

`MosquitoSpray.Core` は `noEngineReferences: true` なので `UnityEngine` を参照できない。
Core に Unity の型が要ると感じたら、それは **設計が間違っている合図**。
asmdef を緩めず、インターフェースを切って Adapter 側に実装を出す。

`MosquitoSpray.XR` を Adapter から分けてあるのは、Meta XR SDK への依存を1つのアセンブリに
閉じ込め、Adapter をテスト可能に保つため。XR SDK の型を Adapter に持ち込まない。

L1 は AI に完成品を求めてよい。L2・L3 で AI が出すのは 7〜8割の叩き台で、残りは人間が
Editor とシミュレータで仕上げる。見積もりもレビューもこの前提で行う。

詰まったときも同じ線で分ける。**L1 は AI が直す**（テストがあるので機械的に判定できる）。
**L2・L3 と Editor 周りは人間が直す**。ここを直すことが Unity の習得そのものなので AI に渡さない。
どちらの場合も、直した内容と理由を `docs/learning-log.md` に残す。

## 単位

| 種類 | 単位 | 例 |
| --- | --- | --- |
| 長さ | メートル | `1.2f` は 1.2m |
| 角度 | 度 | `15f` は 15° |
| 時間 | 秒 | `1.5f` は 1.5秒 |

単位はここで一つに決めてあるので、変数名に単位を入れない（`distanceMeters` ではなく `distance`）。

## 仕様に書く値・書かない値

判定はひとつ。**その記述からテストが1本書けるか。**

| 書く | 書かない |
| --- | --- |
| 噴射の半角と到達距離 | 噴射エフェクトの見た目 |
| 蚊の出現範囲の半径と高さ | 蚊の飛び方の「うっとうしさ」 |
| 刺された判定の距離と秒数 | 刺されたときの演出 |
| HUD の距離・角度・文字高 | HUD の配色やフォント |

書けないものは仕様ではなく、シミュレータでの確認項目に回す。
L4 の参照画像・動画は `docs/references/` に置き、design.md からはパスで示すだけにする。

## テスト

| 項目 | 規約 |
| --- | --- |
| フレームワーク | Unity Test Framework のみ |
| アセンブリ | `MosquitoSpray.Tests.EditMode` と `MosquitoSpray.Tests.PlayMode`。同じアセンブリに混ぜない |
| 属性 | 原則 `[Test]`。フレーム待ちや時間経過が要るときだけ `[UnityTest]` |
| 命名 | テストクラスは `<対象クラス名>Tests` |
| Core の必須ライン | public メソッドにテストがある。なければ完了にしない |
| Adapter の必須ライン | 主要経路のみ |
| XR 依存 | XR の入力はインターフェース越しに受け、PlayMode テストでは差し替える |

カバレッジの数値目標は置かない。Core にテストが書けないなら、テストを諦めず設計を直す。
tasks.md の各タスクには、通すべきテストを完了条件として書く。

実行する場所を分ける。

| 場所 | 走るテスト | 方法 |
| --- | --- | --- |
| Stop フック（AI の作業終了時） | EditMode のみ | Unity CLI 経由で、開いている Editor で実行 |
| 人間 | 任意 | Test Runner ウィンドウ |

PlayMode テストを Stop フックで走らせない。開いている Editor が Play モードに入り、
人間の作業を止めてしまうため。PlayMode は Test Runner から手で流す。

## 禁止事項

1. **`MosquitoSpray.Core` に `UnityEngine` を持ち込まない。** asmdef がコンパイルを落とす
2. **シーン（`*.unity`）・プレハブ（`*.prefab`）・`*.meta` の中身を編集しない。**
   変更は Unity CLI 経由で、Editor に対象シーンを開いた状態で行う。
   `.claude/settings.json` が Edit / Write を拒否する
3. **2 を Bash（`sed`・リダイレクト）で迂回しない。**
   権限設定はここを塞げていない。機械は止めないので、AI が自分で守る

   ただし **Unity が生成したファイルを、中身を変えずに扱うのは可**（コピー・移動・削除）。
   禁じているのは YAML を手で書き換えて GUID や参照を壊すことであって、丸ごとのコピーは
   むしろ手で作り直すより安全。テンプレートから設定を持ち込むときに必要になる。
4. **L4 の値（マテリアル、ライティング、エフェクト、音）を AI が変更しない**
5. **AI の「テストが通りました」を完了の根拠にしない。**
   根拠は Stop フックの終了コードと Test Runner の結果だけ
6. **シミュレータで直した数値は、コードより先に design.md を直す**
7. **ルールが足りないと感じても、spec の途中で steering に足さない。**
   spec を閉じてから `docs/learning-log.md` に書き、まとめて検討する
