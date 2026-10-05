# Brief: mosquito-core

## Problem
蚊がいつ・どこに現れ、どう飛び、いつ「刺された」になるかの規則がないと、ゲームの判断軸
（撃墜を狙うか、顔に近づけさせないか）が成立しない。外部設計では「放っておくと顔に近づく」「近くにとどまられると刺される」と文章で決まっている。

## Current State
コードはない。人の表（learning-roadmap 3-2）では flight と spawn が別 spec だった。
難易度の変化を外すと spawn は小さすぎるため、1つにまとめた。

## Desired Outcome
時間を進めると、蚊の出現・位置・状態（飛行中／撃墜／刺した）が決まった規則で変わる。
規則はすべて EditMode テストで確かめられる。
刺された判定は、顔からの距離と、その範囲にとどまった秒数で決まる。

## Approach
蚊1匹の状態機械（例：漂う → 顔へ近づく → 撃墜、または刺した）と、出現ルール（出現範囲の半径と高さ、同時数、間隔）を Core に置く。
時間は呼び出し側から経過秒数として渡し、Core は時計を持たない。乱数はインターフェースで差し替えられるようにし、テストでは固定する。

## Scope
- **In**: 出現ルール（範囲・同時数・間隔）、飛行の状態遷移と速度、撃墜を受けたときの状態変化、刺された判定（距離×秒数）、刺したあとの蚊の扱い
- **Out**: 蚊の見た目と落下の演出、噴射の命中判定そのもの（spray-hit-core）、スコアへの反映（round-core）

## Boundary Candidates
- 出現ルール
- 蚊1匹の飛行と状態遷移
- 刺された判定

## Out of Boundary
- 難易度の選択や、ラウンド中の難易度変化（外部設計の対象外）
- 逃避などの、外部設計に書かれていない飛び方
- 家具や壁との当たり判定（蚊は家具をすり抜ける）
- 減点の量（round-core）

## Upstream / Downstream
- **Upstream**: なし
- **Downstream**: mosquito-view-adapter（状態を GameObject に反映）、round-flow-adapter（撃墜・刺されのイベントをスコアへ渡す）

## Existing Spec Touchpoints
- **Extends**: なし
- **Adjacent**: spray-hit-core（位置の型を揃える）、round-core（撃墜・刺されのイベントの形。直接参照はしない）

## Constraints
- `MosquitoSpray.Core` に置き、UnityEngine を参照しない
- Core の中では一番大きい。tasks が300行に近づいたら、刺された判定を別 spec に出すことを検討する
- 蚊は手の届く範囲より外にも出るが、プレイヤーが追いかけなくて済む範囲にとどめる（外部設計 05 の安全）
