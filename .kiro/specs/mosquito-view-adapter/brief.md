# Brief: mosquito-view-adapter

## Problem
mosquito-core の状態は計算上のものなので、部屋の中に蚊として見えるようにする橋渡しが要る。
噴射を受けて命中を判定し、撃墜させる処理も、どこかが持たなければならない。

## Current State
なし。mosquito-core と spray-input-adapter ができてから着手する。

## Desired Outcome
出現ルールに従って蚊のプレハブが現れ、Core の飛行状態どおりに動く。
噴射イベントを受けると、各蚊の位置を spray-hit-core で判定し、命中した蚊は撃墜されて落ちる。
撃墜と刺されたことを、外へイベントとして通知する。主要な経路に PlayMode テストがある。

## Approach
蚊1匹ごとの MonoBehaviour が Core の状態を持ち、毎フレーム経過時間を渡して位置を反映する。
出現の管理役が、出現ルールに従ってプレハブを生成・回収する。
噴射イベントの購読と命中判定はこの spec が持つ。頭の位置は、インターフェース越しに受け取る。

## Scope
- **In**: 蚊のプレハブ（最低限の見た目）、出現の管理、Core の状態の反映、噴射を受けての命中判定と撃墜、落下の動き、撃墜・刺されのイベント通知、出現範囲などの値を持つ ScriptableObject
- **Out**: スコアへの反映（round-flow-adapter）、刺されたときの画面の赤（round-hud-adapter）

## Boundary Candidates
- 出現の管理（生成・回収）
- 蚊1匹の表示の同期と撃墜の動き
- 噴射と蚊をつなぐ命中判定

## Out of Boundary
- 蚊のモデル・アニメーションの作り込み（L4）
- 家具との当たり判定（対象外）
- ラウンドの開始・終了に合わせた出現の開始・停止の判断（round-flow-adapter が指示する）

## Upstream / Downstream
- **Upstream**: mosquito-core、spray-input-adapter（噴射イベントと噴射範囲。spray-hit-core はこれを通して使う）
- **Downstream**: round-flow-adapter

## Existing Spec Touchpoints
- **Extends**: なし
- **Adjacent**: spray-input-adapter（噴射イベントの形）、round-flow-adapter（撃墜・刺されのイベントの形）

## Constraints
- プレハブは Unity CLI 経由で作る。シーンは触らない
- 落下の時間などの数値は PlayMode で確かめ、見た目はシミュレータで人間が確認する
