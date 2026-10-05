# Brief: game-scene

## Problem
部品とプレハブがそろっても、1つのシーンに置いてパススルーとカメラリグを整えないと、シミュレータで遊べない。

## Current State
`Assets/Scenes/` にシーンがある（骨組み）。ゲームの部品はまだ置かれていない。

## Desired Outcome
`Assets/Scenes/Game.unity` を開いてシミュレータで再生すると、パススルーの部屋に案内が出る。
トリガーで1ラウンド遊べて、結果が出て、もう一度遊べる。
外部設計 03 の各場面を、シミュレータで確かめた記録が `docs/verification/` に残る。

## Approach
Meta XR のカメラリグとパススルーを設定し、これまでの spec のプレハブと ScriptableObject を配置して、参照をつなぐ。
変更はすべて Unity CLI 経由で、Game.unity を開いた状態で行う。最後に外部設計の流れを通しで確認する。

## Scope
- **In**: カメラリグ、パススルー、入力の XR 実装の配置、蚊・スプレー・ラウンド進行・表示のプレハブ配置と参照の接続、シミュレータでの通し確認と記録
- **Out**: 新しいロジック（必要になったら該当する Adapter spec に戻す）

## Boundary Candidates
- XR の土台（カメラリグとパススルー）
- 部品の配置と接続
- 通しの確認

## Out of Boundary
- 実機での確認（対象外）
- L4 の作り込み（マテリアル、ライティング、エフェクト、音）
- シーンを増やすこと（シーンは1つだけ）

## Upstream / Downstream
- **Upstream**: round-hud-adapter（ここまでの全 spec の成果を、これを通して使う）
- **Downstream**: なし（最後の spec）

## Existing Spec Touchpoints
- **Extends**: なし
- **Adjacent**: spray-input-adapter（XR 実装の置き方）

## Constraints
- `-scene` なので、触るのは Game.unity だけ。シーン・プレハブ・.meta を直接編集しない
- Meta XR SDK の設定は AI が誤りやすい。L3 の仕上げは人間がシミュレータで行う
- シミュレータで直した数値は、コードより先に該当 spec の design.md を直す
