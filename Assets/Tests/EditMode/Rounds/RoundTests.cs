using System;
using System.Numerics;
using MosquitoSpray.Core.Rounds;
using NUnit.Framework;

namespace MosquitoSpray.Tests.EditMode.Rounds
{
    // 共通の準備と、作成・開始・経過秒数の検証。時間切れ・結果・採点・離脱は別の partial ファイルに置く
    public sealed partial class RoundTests
    {
        // 原点以外の位置。差し引き忘れや取り違えを見つける
        static readonly Vector3 Position = new Vector3(0.5f, 1.2f, -0.3f);

        // 仮の初期値
        static RoundRules DefaultRules() => new RoundRules(60f, 1.0f, 0.5f, 0.4f);

        static Round CreateRound(RoundRules rules = null) => new Round(rules ?? DefaultRules());

        [Test]
        public void 作った直後は待機で残り時間が制限時間で撃墜数とスコアが0で定位置がなく渡した設定値が読める()
        {
            var rules = DefaultRules();
            var round = CreateRound(rules);

            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Waiting));
            Assert.That(round.RemainingTime, Is.EqualTo(60f));
            Assert.That(round.DownedCount, Is.EqualTo(0));
            Assert.That(round.Score, Is.EqualTo(0));
            Assert.That(round.HomePosition, Is.Null);
            Assert.That(round.Rules, Is.SameAs(rules));
        }

        [Test]
        public void 待機で開始するとプレイになり変わったと返り残り時間が制限時間で定位置が渡した位置になる()
        {
            var round = CreateRound();

            var changed = round.RequestStart(Position);

            Assert.That(changed, Is.True);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Playing));
            Assert.That(round.RemainingTime, Is.EqualTo(60f));
            Assert.That(round.DownedCount, Is.EqualTo(0));
            Assert.That(round.Score, Is.EqualTo(0));
            Assert.That(round.HomePosition, Is.EqualTo(Position));
        }

        [Test]
        public void プレイ中にもう一度開始すると変わらなかったと返り状態と残り時間と定位置が変わらない()
        {
            var round = CreateRound();
            round.RequestStart(Position);

            var changed = round.RequestStart(new Vector3(-2f, 0.7f, 3f));

            Assert.That(changed, Is.False);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Playing));
            Assert.That(round.RemainingTime, Is.EqualTo(60f));
            Assert.That(round.HomePosition, Is.EqualTo(Position));
        }

        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void 不正な経過秒数は待機で拒否され値が変わらない(float deltaTime)
        {
            var round = CreateRound();

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => round.Advance(deltaTime));

            Assert.That(ex.ParamName, Is.EqualTo("deltaTime"));
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Waiting));
            Assert.That(round.RemainingTime, Is.EqualTo(60f));
            Assert.That(round.DownedCount, Is.EqualTo(0));
            Assert.That(round.Score, Is.EqualTo(0));
            Assert.That(round.HomePosition, Is.Null);
        }

        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void 不正な経過秒数はプレイ中に拒否され値が変わらない(float deltaTime)
        {
            var round = CreateRound();
            round.RequestStart(Position);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => round.Advance(deltaTime));

            Assert.That(ex.ParamName, Is.EqualTo("deltaTime"));
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Playing));
            Assert.That(round.RemainingTime, Is.EqualTo(60f));
            Assert.That(round.DownedCount, Is.EqualTo(0));
            Assert.That(round.Score, Is.EqualTo(0));
            Assert.That(round.HomePosition, Is.EqualTo(Position));
        }

        [Test]
        public void 設定値が無いと拒否され引数名がrulesになる()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new Round(null));

            Assert.That(ex.ParamName, Is.EqualTo("rules"));
        }

        [Test]
        public void 既定でない設定値でも開始後の残り時間は渡した制限時間になる()
        {
            var round = CreateRound(new RoundRules(10f, 0f, 1.0f, 1.0f));

            round.RequestStart(Position);

            Assert.That(round.RemainingTime, Is.EqualTo(10f));
        }

        // ---- 制限時間でラウンドを終える（2.1〜2.7, 8.3, 8.4） ----

        static Round CreatePlayingRound(RoundRules rules = null)
        {
            var round = CreateRound(rules);
            round.RequestStart(Position);
            return round;
        }

        [Test]
        public void プレイ中に1_64秒を64回進めると残り時間が59になりプレイのまま()
        {
            var round = CreatePlayingRound();
            var finished = false;

            for (var i = 0; i < 64; i++)
                finished |= round.Advance(1f / 64f);

            Assert.That(finished, Is.False);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Playing));
            Assert.That(round.RemainingTime, Is.EqualTo(59f)); // 1/64 は float で正確に足せる
            Assert.That(round.HomePosition, Is.EqualTo(Position));
        }

        [Test]
        public void 残り時間ちょうどを進めると終わったと返り結果になり残り時間が0で定位置がなくなる()
        {
            var round = CreatePlayingRound();

            var finished = round.Advance(60f);

            Assert.That(finished, Is.True);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Result));
            Assert.That(round.RemainingTime, Is.EqualTo(0f));
            Assert.That(round.HomePosition, Is.Null);
        }

        [Test]
        public void 残り時間に許容誤差以内で届かない経過でも終わり結果になる()
        {
            var round = CreatePlayingRound();

            var finished = round.Advance(59.999996f);

            Assert.That(finished, Is.True);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Result));
            Assert.That(round.RemainingTime, Is.EqualTo(0f));
        }

        [Test]
        public void 残り時間に許容誤差より大きく届かない経過ではプレイのまま終わらない()
        {
            var round = CreatePlayingRound();

            var finished = round.Advance(59.99f);

            Assert.That(finished, Is.False);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Playing));
            Assert.That(round.RemainingTime, Is.EqualTo(0.01f).Within(1e-4f));
            Assert.That(round.HomePosition, Is.EqualTo(Position));
        }

        [Test]
        public void 結果で時間を進めても変わらず終わったと返らない()
        {
            var round = CreatePlayingRound();
            round.Advance(60f);

            var finished = round.Advance(5f);

            Assert.That(finished, Is.False);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Result));
            Assert.That(round.RemainingTime, Is.EqualTo(0f));
            Assert.That(round.HomePosition, Is.Null);
        }

        [Test]
        public void 制限時間2_5秒の設定でも2_5秒進めると結果になる()
        {
            var round = CreatePlayingRound(new RoundRules(2.5f, 0.25f, 0.75f, 0.25f));

            var finished = round.Advance(2.5f);

            Assert.That(finished, Is.True);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Result));
            Assert.That(round.RemainingTime, Is.EqualTo(0f));
        }

        [Test]
        public void 結果で経過0秒は何も変えない()
        {
            var round = CreatePlayingRound();
            round.Advance(60f);

            var finished = round.Advance(0f);

            Assert.That(finished, Is.False);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Result));
            Assert.That(round.RemainingTime, Is.EqualTo(0f));
            Assert.That(round.HomePosition, Is.Null);
        }

        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void 不正な経過秒数は結果で拒否され値が変わらない(float deltaTime)
        {
            var round = CreatePlayingRound();
            round.Advance(60f);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Result)); // 結果に入っていることを先に確かめる

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => round.Advance(deltaTime));

            Assert.That(ex.ParamName, Is.EqualTo("deltaTime"));
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Result));
            Assert.That(round.RemainingTime, Is.EqualTo(0f));
            Assert.That(round.HomePosition, Is.Null);
        }

        // 以下は回帰の確認（実装前から通る）
        [Test]
        public void 回帰の確認_待機で時間を進めても待機のまま残り時間が変わらない()
        {
            var round = CreateRound();

            var finished = round.Advance(100f);

            Assert.That(finished, Is.False);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Waiting));
            Assert.That(round.RemainingTime, Is.EqualTo(60f));
        }

        [Test]
        public void 回帰の確認_待機で経過0秒は何も変えない()
        {
            var round = CreateRound();

            var finished = round.Advance(0f);

            Assert.That(finished, Is.False);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Waiting));
            Assert.That(round.RemainingTime, Is.EqualTo(60f));
        }

        [Test]
        public void 回帰の確認_プレイ中に経過0秒は何も変えない()
        {
            var round = CreatePlayingRound();

            var finished = round.Advance(0f);

            Assert.That(finished, Is.False);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Playing));
            Assert.That(round.RemainingTime, Is.EqualTo(60f));
            Assert.That(round.HomePosition, Is.EqualTo(Position));
        }

        // ---- 結果から待機へ戻る（2.2, 3.1〜3.5, 8.3, 8.4） ----

        static Round CreateResultRound(RoundRules rules = null)
        {
            var round = CreatePlayingRound(rules);
            round.Advance(round.Rules.TimeLimit);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Result)); // 結果に入っていることを先に確かめる
            return round;
        }

        [Test]
        public void 結果で0_5秒を2回進めてから開始すると待機に戻り変わったと返り残り時間が制限時間になる()
        {
            var round = CreateResultRound();
            round.Advance(0.5f);
            round.Advance(0.5f);

            var changed = round.RequestStart(Position);

            Assert.That(changed, Is.True);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Waiting));
            Assert.That(round.RemainingTime, Is.EqualTo(60f));
            Assert.That(round.DownedCount, Is.EqualTo(0));
            Assert.That(round.Score, Is.EqualTo(0));
            Assert.That(round.HomePosition, Is.Null);
        }

        [Test]
        public void 結果で受付待ちに許容誤差以内で届かない経過でも開始すると待機に戻る()
        {
            var round = CreateResultRound();
            round.Advance(0.999995f);

            var changed = round.RequestStart(Position);

            Assert.That(changed, Is.True);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Waiting));
        }

        [Test]
        public void 待機に戻した同じ開始ではプレイを始めず次の開始で時間を進めなくてもプレイになる()
        {
            var round = CreateResultRound();
            round.Advance(1.0f);

            round.RequestStart(Position);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Waiting));
            Assert.That(round.HomePosition, Is.Null);

            var changed = round.RequestStart(Position);

            Assert.That(changed, Is.True);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Playing));
            Assert.That(round.RemainingTime, Is.EqualTo(60f));
            Assert.That(round.HomePosition, Is.EqualTo(Position));
        }

        [Test]
        public void 受付待ち0秒の設定値では結果に入った直後の開始で待機に戻る()
        {
            var round = CreateResultRound(new RoundRules(10f, 0f, 1.0f, 1.0f));

            var changed = round.RequestStart(Position);

            Assert.That(changed, Is.True);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Waiting));
            Assert.That(round.RemainingTime, Is.EqualTo(10f));
        }

        [Test]
        public void 回帰の確認_結果で受付待ちに届かない63_64秒の経過で開始しても結果のまま変わらなかったと返る()
        {
            var round = CreateResultRound();
            round.Advance(63f / 64f); // 1.0 − 1e-5 より小さい

            var changed = round.RequestStart(Position);

            Assert.That(changed, Is.False);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Result));
            Assert.That(round.RemainingTime, Is.EqualTo(0f));
            Assert.That(round.HomePosition, Is.Null);
        }

        [Test]
        public void 回帰の確認_残り時間を超えた経過で結果に入った直後の開始は超えた分を数えず結果のまま()
        {
            var round = CreatePlayingRound();
            round.Advance(61f); // 超えた 1 秒は受付待ちに数えない

            var changed = round.RequestStart(Position);

            Assert.That(changed, Is.False);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Result));
            Assert.That(round.RemainingTime, Is.EqualTo(0f));
        }
    }
}
