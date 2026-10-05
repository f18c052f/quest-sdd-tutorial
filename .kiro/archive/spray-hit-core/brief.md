# Brief: spray-hit-core

## Problem
プレイヤーが噴射したとき、どの蚊に当たったかを決める規則がないと、撃墜が成立しない。
この判定が GameObject や物理演算に埋もれると、テストで確かめられず、シミュレータで見た目から推測するしかなくなる。

## Current State
`MosquitoSpray.Core` の asmdef があるだけで、コードはない。命中の規則は外部設計に文章で書かれているだけ
（「噴射はコントローラーの向いた先に出る」「手を伸ばした先の届く範囲にしか当たらない」）。

## Desired Outcome
噴射の起点・向き・噴射範囲の形（半角と到達距離）と、蚊の位置を渡すと、命中かどうかが返る。
すべての public メソッドに EditMode テストがあり、境界値（角度ちょうど、距離ちょうど、真後ろ、起点と同じ位置）がテストで決まっている。

## Approach
噴射範囲（SprayVolume）を、起点から向きの方向に伸びる円錐として扱う。半角と到達距離で形を決める。
UnityEngine を使わないので、ベクトルは Core 内の自前の型か System.Numerics で表す。どちらにするかは design で決める。

## Scope
- **In**: 噴射範囲の形の表現、点が噴射範囲に入るかの判定、判定に使う値（半角・到達距離）の定義
- **Out**: 噴射の入力（トリガー）、噴射の見た目、蚊の状態変更（撃墜にするのは mosquito-core / mosquito-view-adapter）

## Boundary Candidates
- 噴射範囲の形（値オブジェクト）
- 命中判定の計算（形 × 点 → 命中か）

## Out of Boundary
- 噴射を出すタイミング、連射の扱い（spray-input-adapter）
- 命中した蚊を撃墜状態にすること（mosquito-core）
- 当たり判定に Unity の Collider や Physics を使うこと

## Upstream / Downstream
- **Upstream**: なし
- **Downstream**: spray-input-adapter（噴射範囲を作る）、mosquito-view-adapter（蚊の位置を判定にかける）

## Existing Spec Touchpoints
- **Extends**: なし
- **Adjacent**: mosquito-core（蚊の位置の型と揃える）

## Constraints
- `MosquitoSpray.Core` に置き、UnityEngine を参照しない
- 単位はメートルと度
- 半角・到達距離の具体的な値は requirements で決め、調整用の値は後で Adapter 側の ScriptableObject に出す
- 最初の spec。ガイドラインの流れに慣れることを優先し、範囲を広げない
