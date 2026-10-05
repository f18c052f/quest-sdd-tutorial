# Brief: round-flow-adapter

## Problem
部品（入力、蚊、ラウンドの規則）がそろっても、それらをつなぐ役がいないとゲームとして進まない。
人の表にはこの役がなく、つなぎ込みのコードの置き場がなかった。

## Current State
なし。round-core と mosquito-view-adapter ができてから着手する。

## Desired Outcome
待機中にトリガーを引くとラウンドが始まり、定位置が記録され、蚊が出始める。
撃墜と刺されがスコアに反映され、制限時間が来ると蚊が止まって結果に移る。結果でトリガーを引くと待機に戻る。
頭の位置が離脱判定に渡され、警告の状態が外から読める。主要な経路に PlayMode テストがある。

## Approach
round-core を持つ MonoBehaviour が、毎フレーム経過時間と頭の位置を Core に渡す。
spray-input-adapter の入力インターフェースからトリガーを受け、状態に応じて開始・再開に使う。
mosquito-view-adapter に出現の開始・停止を指示し、撃墜・刺されのイベントを Core へ渡す。
表示のために、状態の変化を外へ公開する。

## Scope
- **In**: 開始・再開の入力処理、出現の開始・停止の指示、撃墜・刺されのスコアへの反映、頭の位置の取得（インターフェース越し）と離脱判定、表示向けの状態の公開
- **Out**: 表示そのもの（round-hud-adapter）、シーンへの配置（game-scene）

## Boundary Candidates
- 入力 → ラウンドの状態遷移
- 蚊のイベント → スコア
- 頭の位置 → 離脱判定

## Out of Boundary
- 離脱警告中にラウンドや蚊を止めること（外部設計では止めない）
- スコアの保存

## Upstream / Downstream
- **Upstream**: round-core、mosquito-view-adapter（spray-input-adapter の入力インターフェースは、これを通して使う）
- **Downstream**: round-hud-adapter、game-scene

## Existing Spec Touchpoints
- **Extends**: なし
- **Adjacent**: spray-input-adapter（入力インターフェースを使う）、round-hud-adapter（公開する状態の形）

## Constraints
- 直接の依存は2つ。spray-input-adapter への依存は、mosquito-view-adapter を通した間接的なものとして扱う。design で直接参照が必要になったら、依存の数え方として learning-log に記録する
- 頭の位置は mosquito-view-adapter と同じインターフェースから受け取る
