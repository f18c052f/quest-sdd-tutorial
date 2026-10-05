# Research & Design Decisions

## Summary
- **Feature**: `spray-hit-core`
- **Discovery Scope**: New Feature（ただし小さな純粋計算のため minimal discovery。外部調査は行わず、リポジトリ内の確認のみ）
- **Key Findings**:
  - `MosquitoSpray.Core` は `noEngineReferences: true`、参照なし。API 互換レベルは .NET Standard 系で、`System.Numerics.Vector3` を追加の依存なしで使える
  - Core / Tests ともに既存コードはない。命名・配置の前例はステアリング（tech.md / structure.md）だけ
  - 他の Core spec（mosquito-core）も位置を扱う。Core 同士は直接参照しないので、spec 間で共有できる位置の型が要る

## Research Log

### 位置と向きの型
- **Context**: brief で「Core 内の自前の型か System.Numerics か、design で決める」とされた
- **Sources Consulted**: `Assets/Scripts/Core/MosquitoSpray.Core.asmdef`、`ProjectSettings/ProjectSettings.asset`（`apiCompatibilityLevel: 6`）、roadmap.md の依存関係
- **Findings**:
  - `System.Numerics.Vector3` は .NET Standard 2.0/2.1 に含まれ、UnityEngine に依存しない
  - 自前の型を spray-hit-core に置くと、mosquito-core がそれを使うには spray-hit-core への依存が生まれる（roadmap では両者とも依存なし）
- **Implications**: `System.Numerics.Vector3` を採用する。Adapter 側で `UnityEngine.Vector3` と相互変換する（成分を写すだけ）

### 境界ちょうどと浮動小数点
- **Context**: Requirement 2.4 / 2.5 は「ちょうど半角」「ちょうど到達距離」を命中とする。float の計算では、角度ちょうどの点を作っても誤差で外側に出ることがある
- **Findings**: 許容誤差なしで比較すると、2.4 のテストが値の作り方しだいで通ったり落ちたりする
- **Implications**: 距離と角度の比較に小さな許容誤差を入れ、「許容誤差の内側は境界上」とみなす。テストの「わずかに外」はこの許容誤差より十分大きく離す

### 不正な値の知らせ方
- **Context**: Requirement 3 は「作らず、どの値が不正かを呼び出し側に知らせる」
- **Findings**: C# では、コンストラクタで `ArgumentException` 系の例外を投げ、`ParamName` で引数を示すのが一般的。Result 型を自作すると、Core に余分な型が増える
- **Implications**: コンストラクタで例外を投げる（Fail Fast）。不正な値は設定ミスかバグであり、ゲーム中の通常経路ではないので例外が適切

## Architecture Pattern Evaluation

| Option | Description | Strengths | Risks / Limitations | Notes |
|--------|-------------|-----------|---------------------|-------|
| 値オブジェクト1つ（形＋判定） | `SprayVolume` が形を持ち、`Contains` で判定する | 型が1つで済む。壊れた形が存在しないことを型で保証できる | 判定の種類が増えると肥大化する | 採用。現状の要件では判定は1種類 |
| 形と判定を分ける | `SprayVolume`（値）＋ `SprayHitJudge`（静的計算） | brief の Boundary Candidates と1対1 | 実装が1つしかない間接層になる | 不採用（Simplification） |

## Design Decisions

### Decision: `System.Numerics.Vector3` を使う
- **Context**: Core は UnityEngine を使えない。蚊の位置の型を mosquito-core と揃えたい
- **Alternatives Considered**:
  1. Core に自前の `Vec3` 構造体を定義する
  2. `System.Numerics.Vector3` を使う
- **Selected Approach**: 2
- **Rationale**: spec 間の依存を作らずに型を揃えられる。内積・長さ・正規化が揃っており、自作の計算コードとそのテストが不要（Build vs Adopt → Adopt）
- **Trade-offs**: Adapter で `UnityEngine.Vector3` と名前が衝突するため、using エイリアスが要る
- **Follow-up**: mosquito-core の design で同じ型を使うことを確認する

### Decision: `SprayVolume` は sealed class にする
- **Context**: Requirement 3 は「壊れた形の噴射範囲は作れない」
- **Alternatives Considered**:
  1. readonly struct
  2. sealed class
- **Selected Approach**: 2
- **Rationale**: struct は `default(SprayVolume)` でコンストラクタの検証を通らずに作れてしまい、向きが (0,0,0) の値ができる。class ならコンストラクタを必ず通る
- **Trade-offs**: 噴射ごとにヒープ割り当てが1回発生する。噴射の頻度（トリガー操作）では問題にならない

### Decision: 範囲チェックは「受け付ける範囲に入っているか」で書く
- **Context**: Requirement 1.4 は受け付ける範囲（0 < 半角 < 90、0 < 到達距離）を定める。NaN は「0 以下」「90 以上」のどちらの比較でも偽になる
- **Selected Approach**: 「範囲内でなければ拒否」の形で検査する。結果として NaN の半角・到達距離も拒否される。向きは正規化した結果が有限の単位ベクトルにならなければ拒否する（長さ 0、NaN、無限大を含む）
- **Rationale**: 1.4 が受け付ける範囲の外はすべて拒否する、という読み方で要件と矛盾しない。新しい振る舞いを足さずに済む

### Decision: 許容誤差
- **Selected Approach**: 距離は `到達距離 + 1e-5 m` 以下を命中、角度は `cos(なす角) ≥ cos(半角) − 1e-6` を命中とする
- **Rationale**: 1e-5 m は 0.01 mm。cos の 1e-6 は半角 15° で約 0.0002° に相当する。どちらもゲームの見た目では区別できず、float の丸め誤差よりは大きい

## Risks & Mitigations
- Adapter での `Vector3` の名前衝突 — design に using エイリアスの方針を書き、spray-input-adapter / mosquito-view-adapter に引き継ぐ
- 起点や蚊の位置が NaN のとき — 比較がすべて偽になり「命中しない」になる。要件外なので保証はせず、design に挙動だけ記す
- シミュレータで半角・到達距離を調整したとき — 規則は値に依存しないので Core の変更は不要。仮の初期値の記述（requirements.md / design.md）だけを直す

## References
- `.kiro/steering/tech.md` — レイヤー境界、単位、テスト規約、禁止事項
- `.kiro/steering/structure.md` — `Assets/Scripts/Core/`、`Assets/Tests/EditMode/` の責務
- `docs/external/mosquito-spray.md` — 「噴射はコントローラーの向いた先」「届く範囲にしか当たらない」
