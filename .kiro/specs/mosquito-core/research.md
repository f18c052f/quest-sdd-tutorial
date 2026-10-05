# Research & Design Decisions

## Summary
- **Feature**: `mosquito-core`
- **Discovery Scope**: New Feature（ただし外部依存のない純粋計算のため light discovery。外部調査は行わず、リポジトリ内の確認のみ）
- **Key Findings**:
  - spray-hit-core が位置の型として `System.Numerics.Vector3` を採用済み。mosquito-core も同じ型を使えば、spec 間の依存なしで蚊の位置をそのまま `SprayVolume.Contains` に渡せる
  - `System.Numerics.Vector3` には上方向の決まりがない。「水平距離」「高さ」（要件 2.1, 2.2）を定めるには、Core として軸を決める必要がある
  - 出現間隔・同時数・最初の1匹（要件 1.2, 3.1–3.4）は、「直前の出現からの経過秒数」を1つ持ち、上限に達している間はそれを出現間隔で頭打ちにすれば、すべて同じ規則で表せる

## Research Log

### 既存の Core のパターン
- **Context**: Core に置かれている型は `SprayVolume` だけ。命名・検証・例外の前例をそろえたい
- **Sources Consulted**: `Assets/Scripts/Core/Spray/SprayVolume.cs`、`.kiro/archive/spray-hit-core/design.md`、`Assets/Scripts/Core/MosquitoSpray.Core.asmdef`、`ProjectSettings/ProjectSettings.asset`（`apiCompatibilityLevel: 6`）
- **Findings**:
  - 検証はコンストラクタに集め、「受け付ける範囲に入っていなければ拒否」の形で書いて NaN も拒否している
  - 不正な値は `ArgumentException` 系の例外と `ParamName` で知らせる
  - 境界ちょうどの比較には、公開しない許容誤差（`1e-5f` m）を入れている
  - 名前空間はフォルダと一致させている（`MosquitoSpray.Core.Spray`）
- **Implications**: mosquito-core も同じ形にそろえる。設定値の検証は値オブジェクトのコンストラクタで行い、例外と `ParamName` で知らせる

### 上方向の軸
- **Context**: 要件 2.1 / 2.2 は、顔の位置からの水平距離と高さの差で出現範囲を定める
- **Findings**:
  - Unity は Y 軸が上。Adapter は `UnityEngine.Vector3` の成分をそのまま写して `System.Numerics.Vector3` に変換する（spray-hit-core の Implementation Notes）
  - Core が Y を上とみなせば、Adapter で軸の入れ替えが要らない
- **Implications**: Core は **Y 軸を上**とする。水平距離は XZ 平面上の距離、高さの差は Y の差。これは下流（mosquito-view-adapter）にとっての契約なので、Revalidation Triggers に入れる

### 名前空間の名前
- **Context**: 蚊1匹の型名は用語集どおり `Mosquito` にしたい
- **Findings**: 名前空間を `MosquitoSpray.Core.Mosquito` にすると、型 `Mosquito` と名前空間の名前が同じになり、C# で参照があいまいになる
- **Implications**: フォルダ・名前空間を `Mosquitoes` にする（`MosquitoSpray.Core.Mosquitoes`）

### 乱数の差し替え
- **Context**: 要件 2.3 / 2.4 / 10.3 は、乱数を差し替えて出現位置を固定できることを求める
- **Findings**:
  - `System.Random` は Core で使えるが、テストで「この値を返す」と指定しにくい
  - `UnityEngine.Random` は Core から使えない
- **Implications**: `[0, 1)` の float を返すだけのインターフェースを Core に置く。本番の実装は Adapter が持つ（`System.Random` か `UnityEngine.Random` を包む）。テストでは、決めた値を順に返す偽物を使う

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| 群れ1つが蚊の一覧と出現を持つ | `MosquitoSwarm` が出現・時間経過・命中を受け、蚊1匹の振る舞いは `Mosquito` に置く | 同時数・出現間隔・刺された判定が同じ時間経過の中で一貫する。Adapter の呼び出し口が1つ | `MosquitoSwarm` のテストが多くなる | 採用 |
| 出現ルール・蚊・刺された判定を別々の公開型にする | brief の Boundary Candidates と1対1 | 部品ごとにテストできる | 同時数を数えるには蚊の状態を知る必要があり、結局どこかで束ねる。束ね役なしで Adapter に順序を任せると、順序の誤りがテストで捕まらない | 不採用（Simplification） |
| 刺された判定を別 spec に出す | brief の Constraints にある選択肢 | spec が小さくなる | 刺された判定は「累積秒数を蚊1匹ごとに持つ」ので、蚊の状態と切り離せない。分けると両 spec が蚊の状態を共有する | 不採用。tasks の行数は design 時点で 300 行に届かない見込み |

## Design Decisions

### Decision: 撃墜・刺した蚊は、その時点で一覧から外す
- **Context**: 要件 3.5（同時数に数えない）、5.2 / 7.1（動かさない、判定の対象にしない）、8.1（各蚊の状態を読める）
- **Alternatives Considered**:
  1. 撃墜・刺した蚊も一覧に残し、状態で区別する
  2. 状態が変わった時点で一覧から外し、その蚊の参照を結果として渡す
- **Selected Approach**: 2。`Mosquitoes` は接近中の蚊だけになる。撃墜・刺した蚊は `TryHit` の `out` 引数と `AdvanceResult.Bitten` で渡し、その `Mosquito` の `State` と `Position` は最後の値のまま変わらない
- **Rationale**: 同時数＝一覧の数になり、数え間違いが起きない。ラウンド中に一覧が増え続けない。撃墜・刺した蚊は Core にとってもう何も起きないので、持ち続ける理由がない
- **Trade-offs**: 落下の演出のために撃墜した蚊を覚えておくのは Adapter の役目になる（もともと Adapter の範囲）

### Decision: 出現のタイミングは「直前の出現からの経過秒数」1つで決める
- **Context**: 要件 1.2, 3.1–3.4
- **Selected Approach**:
  - 出現を開始したとき、経過秒数を出現間隔と同じ値にしておく（最初の時間経過で1匹目が出る。1.2）
  - 時間経過のたびに経過秒数を足し、それが出現間隔以上で、かつ接近中の数が上限未満の間、1匹出して出現間隔を引く（3.1, 3.4）
  - 出現のあと接近中の数が上限に達していたら、経過秒数を出現間隔で頭打ちにする（3.2）。空きができたら、次の時間経過で1匹だけ出る（3.3）。2匹同時に空いても、2匹目は出現間隔を待つ
- **Rationale**: カウンターが1つで済み、1回の時間経過が長くても短くても同じ結果になる
- **Trade-offs**: 上限に達している間の待ち時間は「貯まらない」。外部設計にはこの点の記述がなく、要件 3.1（直前の出現から出現間隔以上）の文言どおりに読んだ

### Decision: 1回の時間経過の中の順序は「移動 → 刺された判定 → 出現」
- **Context**: 要件 4.1, 6.1, 3.1。順序で結果が変わる
- **Selected Approach**: 接近中の蚊を動かし、その位置で刺された判定をし、刺した蚊を外してから出現を判定する
- **Rationale**:
  - 6.1 は「その時間経過のあとの位置」で判定すると定めている
  - 刺して消えた枠を同じ時間経過で埋められる（3.3 の「次に時間が進められたとき」は、刺して消えた場合は同じ時間経過の出現判定になる）
  - 出現したばかりの蚊は、その時間経過では動かない。出現位置のテストが顔の位置だけで決まる
- **Trade-offs**: 出現した時間経過の分だけ、蚊が顔へ着くのが最大1フレーム遅れる。ゲームとして気にならない

### Decision: 出現位置は角度・水平距離・高さを一様乱数から作る
- **Context**: 要件 2.1–2.4
- **Selected Approach**: 乱数を順に3回引き、角度 `θ = 2π·r1`、水平距離 `d = 内側 + (外側 − 内側)·r2`、高さの差 `h = 下限 + (上限 − 下限)·r3` とする。位置は `顔 + (d·cosθ, h, d·sinθ)`
- **Rationale**: 範囲の内側に必ず入り、乱数を固定すれば位置が決まる。引く順番を固定するので 2.4 を満たす
- **Trade-offs**: 水平距離を一様に引くので、面積あたりでは内側に寄る。外部設計に密度の決まりはなく、テストで確かめる対象でもない
- **Follow-up**: 乱数の実装が `[0, 1)` の外の値を返しても範囲を守れるよう、Core で `[0, 1]` に切り詰める

### Decision: 設定値は `MosquitoRules` という検証済みの値オブジェクトにまとめる
- **Context**: 要件 9.1–9.4。spray-hit-core は値をコンストラクタ引数で受けたが、mosquito-core の値は9つある
- **Alternatives Considered**:
  1. `MosquitoSwarm` のコンストラクタに9つの値を並べる
  2. `MosquitoRules`（sealed class）にまとめ、そのコンストラクタで検証する
- **Selected Approach**: 2
- **Rationale**: 検証が1か所になり、`MosquitoRules` だけを対象にテストできる。Adapter 側の ScriptableObject から1対1で写せる
- **Trade-offs**: 型が1つ増える。名前は Adapter 側の ScriptableObject（Settings）と区別するため `Rules` にした

### Decision: 単体の蚊は `Mosquito` に置くが、変更する操作は公開しない
- **Context**: brief の Boundary Candidates「蚊1匹の飛行と状態遷移」。Adapter が蚊を直接動かせると、同時数や刺された判定の順序が崩れる
- **Selected Approach**: `Mosquito` は読み取り専用のプロパティだけを公開し、移動・刺された判定・撃墜は `internal` にする。テストは `MosquitoSwarm` を通して行う
- **Rationale**: 公開の入口が `MosquitoSwarm` だけになり、規則を迂回できない。`InternalsVisibleTo` を使わないので、asmdef と AssemblyInfo を増やさずに済む
- **Trade-offs**: 蚊1匹の規則も群れを通してテストすることになる。同時数の上限を 1 にすれば、1匹だけを扱うテストになる

### Decision: 蚊の識別子は群れの中で単調に増やし、開始し直しても戻さない
- **Context**: 要件 8.4（使い回さない）と 1.4（前回を引き継がない）
- **Selected Approach**: 識別子は `int`。`MosquitoSwarm` ごとに 1 から数え、`Stop` / `Start` で戻さない
- **Rationale**: 前のラウンドの蚊の参照を Adapter が持っていても、新しい蚊と取り違えない。1.4 が求めるのは規則の引き継ぎがないことで、識別子の値は規則に含まれない

## Risks & Mitigations
- 浮動小数の累積誤差で、累積秒数が「ちょうど 2.0 秒」の手前で止まる — 秒数の比較に許容誤差（`1e-5f` 秒）を入れる。テストの「わずかに足りない」は 0.01 秒以上離す
- 顔の位置ちょうどの蚊は、顔への方向が決まらない — 距離 0 のときは動かさない（4.2 の「顔の位置で止める」と同じ結果）
- `MosquitoSwarmTests` が大きくなる — partial class で関心ごと（出現・接近と撃墜・刺された判定）にファイルを分け、命名規約 `<対象クラス名>Tests` は保つ
- tasks.md が 300 行に近づく — 近づいたら、brief の Constraints どおり刺された判定の分離を再検討する。design 時点の見込みは 100 行前後

## References
- `.kiro/archive/spray-hit-core/design.md` — 位置の型、検証と例外、許容誤差の前例
- `Assets/Scripts/Core/Spray/SprayVolume.cs` — 実装の前例
- `.kiro/steering/tech.md` — L1 Core の制約、単位、テスト規約
- `.kiro/steering/roadmap.md` — Shared seams（頭の位置の供給、撃墜・刺されのイベントの形）
