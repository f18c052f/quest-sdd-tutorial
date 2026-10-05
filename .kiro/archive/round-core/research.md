# Research & Design Decisions

## Summary
- **Feature**: `round-core`
- **Discovery Scope**: New Feature（ただし外部依存のない純粋計算のため light discovery。外部調査は行わず、リポジトリ内の確認のみ）
- **Key Findings**:
  - spray-hit-core と mosquito-core が、検証・例外・許容誤差・位置の型（`System.Numerics.Vector3`、Y 軸が上）の前例を作っている。round-core もそろえれば、Adapter は頭の位置を mosquito-core と同じ値のまま渡せる
  - 離脱判定は「警告を出す距離」と「消す距離」を分けた（要件 5.1–5.4）ため、直前に警告していたかを入力にとる。判定そのものは純粋な関数にできるが、「直前に警告していたか」はどこかが持つ必要がある
  - 撃墜・刺されたことの知らせ（要件 4）は、mosquito-core の `TryHit` の戻り値と `AdvanceResult.Bitten` の要素1つずつに対応させれば、Core 同士が参照しあわずに済む

## Research Log

### 既存の Core のパターン
- **Context**: Core には `MosquitoSpray.Core.Spray` と `MosquitoSpray.Core.Mosquitoes` がある。命名・検証・例外の前例をそろえたい
- **Sources Consulted**: `Assets/Scripts/Core/Spray/SprayVolume.cs`、`Assets/Scripts/Core/Mosquitoes/MosquitoRules.cs`・`MosquitoSwarm.cs`・`AdvanceResult.cs`、`.kiro/archive/mosquito-core/design.md`、`Assets/Tests/EditMode/MosquitoSpray.Tests.EditMode.asmdef`
- **Findings**:
  - 設定値は `sealed class` の `...Rules` にまとめ、コンストラクタで検証する。「受け付ける範囲に入っていなければ拒否」の形で書き、NaN と無限大も拒否する。最初に見つかった不正な値の引数名を `ArgumentOutOfRangeException.ParamName` で知らせる。既定値は持たない
  - 時間は `Advance(float deltaTime, ...)` で外から渡す。負・NaN・無限大は `ArgumentOutOfRangeException`（`ParamName = "deltaTime"`）で、何も変えない
  - 境界ちょうどの比較には、公開しない許容誤差 `1e-5f`（m と秒）を入れる
  - 名前空間はフォルダと一致させ、型名と衝突する名前は複数形にする（`Mosquitoes`）
  - テストは `MosquitoSpray.Tests.EditMode` の下に、Core と同じフォルダ名で置く。大きいテストクラスは partial に分ける
- **Implications**: round-core も同じ形にそろえる。名前空間は `MosquitoSpray.Core.Rounds`（型 `Round` と衝突させない）

### mosquito-core との継ぎ目
- **Context**: roadmap の Shared seams「mosquito-core ↔ round-core：撃墜・刺されのイベントの形」
- **Sources Consulted**: `.kiro/archive/mosquito-core/design.md` の `AdvanceResult` と `MosquitoSwarm.TryHit`
- **Findings**:
  - 撃墜は `TryHit(id, out downed)` が `true` を返したときに1件起きる。同じ蚊で2回 `true` にはならない
  - 刺されたことは `AdvanceResult.Bitten` の要素1つが1件。同じ蚊が2回入ることはない
  - mosquito-core の design は「刺されたときの減点は、round-flow-adapter が `Bitten.Count` 回だけ round-core に渡す」と書いている
- **Implications**: round-core は、撃墜1件・刺された1件をそれぞれ引数なしのメソッド1回で受け取る。mosquito-core の型（`Mosquito` など）を引数にとらない。どの蚊だったかはスコアの規則に関係しない

### 頭の位置（プレイヤーの位置）
- **Context**: roadmap の Shared seams「頭の位置：mosquito-core の刺された判定と、round-core の離脱判定の両方が使う。どこから供給するかを design で揃える」
- **Findings**:
  - mosquito-core は顔の位置を `System.Numerics.Vector3`（Y が上）で毎フレーム受け取る
  - 離脱判定は水平距離だけを使う（要件 5.5）。座る・立つで変わるのは主に Y なので、頭の位置をそのまま使っても座位・立位で警告しない
- **Implications**: round-core が受け取る「プレイヤーの位置」は、mosquito-core の顔の位置と**同じ値**（頭の位置、同じ座標空間、Y が上）とする。round-flow-adapter は頭の位置を1回取得し、両方に渡せばよい

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| A. `Round` 1つにすべて持たせる | 状態遷移・スコア・離脱判定を1つの型のメソッドで行う | 型が少ない | 離脱の規則がラウンドの状態遷移に埋もれ、brief の「混ぜない」に反する。ヒステリシスの規則だけを単独でテストしにくい | 不採用 |
| B. 離脱判定を純粋関数にし、警告フラグは呼び出し側が持つ | `StrayJudge.ShouldWarn(home, pos, wasWarning)` だけを公開し、`Round` は定位置だけ持つ | 判定とラウンドが完全に独立 | 「ラウンド開始時に警告なしから始める」（5.7）、「定位置がなければ警告しない」（5.6）を Adapter が守る必要があり、規則が Core の外に漏れる | 不採用 |
| C. 純粋関数 + `Round` が警告フラグを持つ | 判定は `StrayJudge`（純粋関数）。`Round` は定位置と警告フラグを持ち、`UpdateStray` で判定を呼ぶだけ。フラグはラウンドの状態遷移・時間・スコアに影響しない | 判定の規則を単独でテストできる。定位置の寿命（開始で記録、結果で消去）と警告フラグの初期化が同じ場所にある | `Round` に離脱のメンバーが入る | **採用** |

## Design Decisions

### Decision: 警告フラグを `Round` が持つ
- **Context**: 要件 5.1–5.4 のヒステリシスには「直前に警告していたか」が要る。5.6 / 5.7 は、定位置の有無とラウンドの開始にフラグを結びつける
- **Alternatives Considered**:
  1. 呼び出し側（round-flow-adapter）がフラグを持つ（上の表の B）
  2. 離脱判定用の別のクラス（`StrayMonitor`）を作り、`Round` とは別に持たせる
- **Selected Approach**: 判定の式は `StrayJudge`（static、純粋関数）に置く。`Round` は定位置と警告フラグを持ち、`UpdateStray(position)` で `StrayJudge` を呼んでフラグを更新する。ラウンドの開始でフラグを下ろし、結果に入るとき定位置とともに消す
- **Rationale**: 定位置は「ラウンドを始めた位置」で、記録・消去の時機はラウンドの状態遷移が決める。フラグの初期化も同じ時機なので、同じ型に置くのがいちばん漏れが少ない。2 は、定位置を `Round` と `StrayMonitor` のどちらが持つかがあいまいになる（No Hidden Shared Ownership に反する）
- **Trade-offs**: brief の「状態遷移とは独立に置く」は、判定の式を純粋関数にすることと、フラグが状態・時間・スコアに影響しないこと（5.8）で満たす。`Round` の公開メンバーは2つ増える
- **Follow-up**: `UpdateStray` が状態・残り時間・撃墜数・スコアを変えないことをテストで固定する（5.8）

### Decision: 知らせは戻り値で返し、C# の event は使わない
- **Context**: 要件 2.4（ラウンドが終わったことを読み取れる）と 6.3（開始の指示で状態が変わったか）
- **Alternatives Considered**:
  1. `event Action<RoundPhase> PhaseChanged` を公開する
  2. `Advance` と `RequestStart` の戻り値で知らせる
- **Selected Approach**: 2。`Advance` はその呼び出しでラウンドが終わったら `true`、`RequestStart` は状態が変わったら `true` を返す。変わった先は `Phase` で読む
- **Rationale**: mosquito-core（`AdvanceResult`、`TryHit` の戻り値）と同じ形。購読の解除忘れがなく、テストで書きやすい
- **Trade-offs**: 呼び出し側は戻り値を毎回見る必要がある。round-flow-adapter は毎フレーム呼ぶので負担にならない

### Decision: 時間と距離の境界に許容誤差を入れる
- **Context**: 要件 2.3（残り時間ちょうど）、3.1（受付待ちちょうど）、5.1 / 5.3 / 5.4（距離ちょうど）、8.3
- **Selected Approach**: mosquito-core と同じく `TimeTolerance = 1e-5f`（秒）、`DistanceTolerance = 1e-5f`（m）を `private const` で持つ。終了は `RemainingTime ≤ TimeTolerance`、受付は `経過 ≥ RestartDelay − TimeTolerance`、警告は `距離 ≥ WarnDistance − DistanceTolerance`、解除は `距離 < ClearDistance − DistanceTolerance`
- **Rationale**: float の引き算・平方根で、ちょうどの値がわずかにずれても、要件の「以上」「未満」の側に倒れるようにする
- **Trade-offs**: 許容誤差より小さい差は区別しない。ゲームの体験に影響しない大きさ

### Decision: スコアの点数を設定値にしない
- **Context**: requirements の Introduction で、点数の規則（+1 / -1、0 で止める）は固定と決めた
- **Selected Approach**: `RoundRules` に点数を入れない。`Round` の中で決まった値として扱う
- **Rationale**: 調整する予定のない値を設定値にすると、検証とテストが増えるだけになる（Simplification）
- **Follow-up**: 点数を変えたくなったら、requirements から直す

## Synthesis

- **Generalization**: 「待機・プレイ・結果のどの状態で、どの入力を受け付けるか」は、すべて `Round` の状態ごとの分岐として1か所で表せる。開始と再開の2つの入口を作らず、どちらも `RequestStart` 1つにした（トリガーという入力が1つしかないため）
- **Build vs. Adopt**: 状態機械のライブラリは使わない。状態は3つ、遷移は3本で、`switch` で足りる。Core は外部パッケージを参照しない（tech.md）
- **Simplification**:
  - 撃墜・刺されたことの知らせは戻り値を返さない（`void`）。スコアが変わったかを読みたい要件は無く、赤い縁は刺されたことの知らせから出す（requirements の Adjacent expectations）
  - 離脱判定の設定値を別の型に分けず、`RoundRules` にまとめる。値は4つで、どれもラウンドの1回分の条件
  - `StrayJudge` 以外の補助クラスを作らない

## Risks & Mitigations
- 残り時間を float の引き算で減らすため、毎フレーム（1/72 秒など）の経過秒数を60秒ぶん足すと、誤差が許容誤差を超えることがある。終わる時刻が1フレームずれるだけで、体験には影響しない。テストの「ちょうど」は、float で誤差なく表せる刻み（1/64 秒）か、残り時間と同じ値1回で確かめる
- 呼び出し側が、ラウンドが終わったフレームで蚊の出現を止め忘れると、結果の表示中に蚊が飛ぶ。round-core は知らせを無視するのでスコアは壊れないが、見た目は round-flow-adapter の責任。Implementation Notes に呼ぶ順の推奨を書く
- プレイヤーの位置に NaN が渡されると、距離の比較がすべて false になり、警告が出ない・消えない。mosquito-core の顔の位置と同じく、検証しない（要件外）

## References
- `.kiro/archive/mosquito-core/design.md` — 撃墜・刺されの形、座標の決まり、許容誤差
- `.kiro/archive/spray-hit-core/design.md` — 検証と例外の前例
- `docs/external/mosquito-spray.md` 02・03・05 — 体験の流れ、離脱警告、「一歩以上」
