# Brief: round-hud-adapter

## Problem
状態がプレイヤーに見えないと遊べない。外部設計 03 では、場面ごとに見えるものが決まっている
（待機の案内、プレイ中の残り時間とスコア、刺されたときの赤、離脱警告、結果）。

## Current State
なし。人の表では、HUD の配置が game-scene に含まれていた。

## Desired Outcome
round-flow-adapter が公開する状態に合わせて、空間に浮かぶ表示が出たり消えたりする。
表示は視界に貼り付かず、部屋の中に浮かぶ。文字は日本語。
表示までの距離・角度・文字の高さは仕様の数値どおりで、PlayMode テストで確かめられる。

## Approach
ワールド空間の UI を、場面ごとのパネル（案内、プレイ中、結果、離脱警告）と、刺されたときの周辺の赤に分ける。
状態の変化を購読して表示を切り替える。表示の配置値は ScriptableObject に出す。

## Scope
- **In**: 待機の案内、残り時間とスコア（視界の下のほう）、刺されたときの周辺の赤、離脱警告（正面）、結果（撃墜数とスコア）、表示のプレハブ、配置値の ScriptableObject
- **Out**: 状態の計算（round-core / round-flow-adapter）、シーンへの配置（game-scene）

## Boundary Candidates
- 場面ごとのパネルの出し入れ
- 刺されたときの赤い縁
- 表示の配置（距離・角度・文字高）

## Out of Boundary
- 視界に貼り付く表示（外部設計で使わないと決めている）
- 配色・フォントの作り込み（L4）
- 日本語以外の表示

## Upstream / Downstream
- **Upstream**: round-flow-adapter
- **Downstream**: game-scene

## Existing Spec Touchpoints
- **Extends**: なし
- **Adjacent**: round-flow-adapter（購読する状態の形）

## Constraints
- 視点が勝手に動く演出は使わない（酔い対策）
- プレハブは Unity CLI 経由で作る。uGUI の操作は公式プラグインの `/ui-ugui` を使う
- 刺されたときの赤は「周辺が一瞬」。秒数は数値で仕様に書き、見た目は人間が確認する
