# Roadmap

## Overview

外部設計 `docs/external/mosquito-spray.md`（v1.0、2026-10-04 確定）のゲームを作る。
パススルーで見える自分の部屋を蚊が飛び、右手コントローラーのトリガーで噴射して撃墜する。1ラウンド1分。
蚊に顔の近くにとどまられると刺されて減点される。定位置から離れると離脱警告が出る。

ガイドライン第6章の切り口（レイヤー → シーン → 技術 → ドメイン）の順に分け、
Core を3つ、Adapter を4つ、シーンを1つの、計8 spec にする。Core だけで閉じる spec を先に作る。

## Approach Decision

- **Chosen**: 案A「層ごとに8 spec」。Core 3（spray-hit / mosquito / round）→ Adapter 4（spray-input / mosquito-view / round-flow / round-hud）→ scene 1
- **Why**:
  - どの spec も直接の依存が2つ以下（3つ以上なら分割を間違えている、の決まりを守れる）
  - UI（round-hud-adapter）を別 spec にできる（切り口3）
  - `-scene` の spec がシーンだけを触る形を保てる。つなぎ込みのコードは round-flow-adapter が持つ
  - spec ごとの大きさがおおむねそろい、最後に重い作業が固まらない
- **Rejected alternatives**:
  - 案B「人の表（`docs/learning-roadmap.md` 3-2）を最小限だけ直す7 spec」：game-scene が、シーン・HUD・つなぎ込みを抱えて特大になる。mosquito-view-adapter の依存が3つになる。つなぎ込みのコードの置き場がない
  - 縦切り（ドメインごとに core→adapter→scene を通す）：ガイドラインの切り口1（レイヤー優先）に反する。シーン1つを複数の spec が順に触ることになる

### 人の表（learning-roadmap 3-2）との差分

| 人の表 | この roadmap | 理由 |
| --- | --- | --- |
| game-session-core | round-core | 用語集の「ラウンド（Round）」に揃えた。離脱判定もここに入れた |
| mosquito-flight-core と mosquito-spawn-core | mosquito-core に統合 | 難易度の変化を外すと出現は小さすぎる。分けたままだと mosquito-view-adapter の依存が3つになる |
| 飛行の「逃避」 | 入れない | 外部設計に書かれていない |
| 出現の「難易度の上がり方」 | 入れない | 外部設計の対象外「難易度の選択：ラウンドの条件は1種類」 |
| spray-input-adapter の「スプレー残量」 | 入れない | 外部設計に書かれていない |
| （なし） | 刺された判定 → mosquito-core | 外部設計で必須だが、表のどこにもなかった |
| （なし） | 離脱判定 → round-core、離脱警告の表示 → round-hud-adapter | 表のどこにもなかった。定位置は「ラウンドを始めた位置」なので、ラウンドに属する |
| （なし） | round-flow-adapter | トリガーで開始し、撃墜・刺されをスコアにつなぐ役が表になかった |
| game-scene の HUD 配置 | round-hud-adapter に分離 | UI は別 spec（切り口3） |

## Scope

- **In**: 外部設計 06「対応する」の全項目。パススルー表示、右トリガーによる噴射、蚊の出現・飛行・撃墜、刺されたことによる減点、離脱警告、1分間のラウンドと結果表示、Meta XR Simulator での動作
- **Out**: 外部設計 06「対象外」の全項目。実機での確認、ハンドトラッキング、複数人プレイ、部屋の形の認識（蚊は家具をすり抜ける）、難易度の選択と変化、スコアの保存、音・エフェクト・3Dモデルの作り込み（L4）、日本語以外の表示、ストア公開。外部設計に書いていないもの（スプレー残量、逃避など）も作らない

## Constraints

- Unity 6 / OpenXR / Meta XR SDK（Core + Interaction）。確認は Meta XR Simulator だけで行う
- 依存の向きは Core ← Adapter ← XR。`MosquitoSpray.Core` は UnityEngine を参照しない
- シーンは `Assets/Scenes/Game.unity` の1つだけ。シーン・プレハブ・.meta は Unity CLI 経由でしか変更しない
- 調整しうる数値は `Assets/Settings/` の ScriptableObject に出す。仕様に書くのは、テストが1本書ける値だけ
- `/kiro-spec-batch` と `/kiro-spec-quick` は使わない（ガイドライン第7章）。spec は1つずつ `/kiro-spec-init` から始める
- 1 spec は約1週間で閉じる大きさにする。上限の目安は requirements 250行・design 800行・tasks 300行

## Boundary Strategy

- **Why this split**:
  - Core の3つは互いに依存しない。命中・蚊・ラウンドは、それぞれ単独で EditMode テストが書ける
  - Adapter は「入力 → 蚊 → ラウンド進行 → 表示」の一本の鎖にした。各 spec は直前の spec と、自分の Core だけに依存する
  - Meta XR SDK に触るのは spray-input-adapter（入力）と game-scene（カメラリグ、パススルー）に限る
- **Shared seams to watch**:
  - spray-hit-core ↔ mosquito-view-adapter：噴射範囲の型を、蚊の側でどう受け取るか
  - mosquito-core ↔ round-core：撃墜・刺されのイベントの形。Core 同士は直接参照せず、round-flow-adapter が橋渡しする
  - spray-input-adapter の入力インターフェース：round-flow-adapter が、ラウンド開始のトリガーとしても使う
  - 頭の位置（顔の位置）：mosquito-core の刺された判定と、round-core の離脱判定の両方が使う。どこから供給するかを design で揃える
  - round-core の離脱判定は、ラウンドの状態遷移に影響しない（警告中もラウンドは続く）。混ぜないように注意する

## Specs (dependency order)

- [x] spray-hit-core -- 噴射範囲（半角・到達距離）に蚊の位置が入るかを判定する純粋な計算。Dependencies: none
- [x] mosquito-core -- 蚊の出現ルール、飛行（出現直後から顔へ近づく）、撃墜後の状態、刺された判定（顔からの距離×秒数）。Dependencies: none
- [x] round-core -- ラウンドの状態遷移（待機→プレイ→結果→待機。結果から待機へは受付待ちの秒数のあと）、制限時間、スコア（撃墜+1・刺され-1、0で止める）、定位置と離脱判定（水平距離、警告を出す距離と消す距離を分ける）。Dependencies: none
- [ ] spray-input-adapter -- 右トリガーと右手の姿勢をインターフェース越しに受け取り、噴射イベントと噴射範囲を出す。Meta XR の実装は XR アセンブリ。Dependencies: spray-hit-core
- [ ] mosquito-view-adapter -- 蚊のプレハブと出現・飛行の反映。噴射を受けて命中・撃墜させ、刺されたことを通知する。Dependencies: mosquito-core, spray-input-adapter
- [ ] round-flow-adapter -- トリガーでラウンドを開始し、撃墜・刺されをスコアに反映し、頭の位置を離脱判定に渡す。Dependencies: round-core, mosquito-view-adapter
- [ ] round-hud-adapter -- 空間に浮かぶ表示。案内、残り時間とスコア、刺されたときの赤い縁、離脱警告、結果。Dependencies: round-flow-adapter
- [ ] game-scene -- Game.unity の構成。カメラリグ、パススルー、各プレハブと表示の配置、シミュレータでの通し確認。Dependencies: round-hud-adapter
