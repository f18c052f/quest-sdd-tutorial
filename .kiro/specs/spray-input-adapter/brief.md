# Brief: spray-input-adapter

## Problem
右手コントローラーのトリガーと向きを読み取って噴射に変える部分がないと、プレイヤーが何もできない。
Meta XR SDK の型が Adapter に漏れると、PlayMode テストで入力を差し替えられなくなる。

## Current State
`MosquitoSpray.Adapter` と `MosquitoSpray.XR` の asmdef があるだけ。Meta XR Simulator は導入済み。

## Desired Outcome
トリガーを引くと噴射イベントが出て、そのときの右手の位置と向きから噴射範囲（spray-hit-core の型）が作られる。
入力はインターフェース越しに受け取る。PlayMode テストでは擬似入力に差し替えて、噴射イベントを確かめられる。
シミュレータで右手のトリガーを操作すると、噴射が出る。

## Approach
入力インターフェース（トリガーの状態、右手の姿勢）を Adapter に定義し、Meta XR SDK による実装を `MosquitoSpray.XR` に置く。
Adapter の MonoBehaviour が、入力から噴射イベントと噴射範囲を作る。
Meta XR SDK の API は、design の段階でガイドライン付録Bの verify-api を通して確かめる。

## Scope
- **In**: 入力インターフェース、Meta XR による実装、噴射イベント、噴射範囲の生成、噴射範囲の値を持つ ScriptableObject、スプレーのプレハブ（最低限の見た目）
- **Out**: 命中した蚊の処理（mosquito-view-adapter）、ラウンド開始の判断（round-flow-adapter）

## Boundary Candidates
- 入力インターフェースと XR 実装
- 噴射イベントと噴射範囲の生成

## Out of Boundary
- スプレー残量（外部設計に書かれていない）
- トリガー以外のボタン、左手、ハンドトラッキング（対象外）
- 噴射エフェクトの作り込み（L4）

## Upstream / Downstream
- **Upstream**: spray-hit-core
- **Downstream**: mosquito-view-adapter（噴射イベントを受ける）、round-flow-adapter（トリガーをラウンド開始にも使う）

## Existing Spec Touchpoints
- **Extends**: なし
- **Adjacent**: round-flow-adapter（同じ入力インターフェースを使う）

## Constraints
- XR SDK の型は `MosquitoSpray.XR` に閉じ込め、Adapter に持ち込まない
- プレハブは Unity CLI 経由で作る
- Unity 公式プラグインには XR 向けのスキルがない。AI が誤りやすい箇所なので、L2 の仕上げは人間がシミュレータで行う
