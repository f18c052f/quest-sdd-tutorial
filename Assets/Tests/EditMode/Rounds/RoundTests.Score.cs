using System.Numerics;
using MosquitoSpray.Core.Rounds;
using NUnit.Framework;

namespace MosquitoSpray.Tests.EditMode.Rounds
{
    // 撃墜数とスコア（撃墜 +1、刺される −1、下限 0）
    public sealed partial class RoundTests
    {
        // 撃墜と刺されたことを指定の回数だけ、この順に通知する
        static void NotifyDownedAndBitten(Round round, int downed, int bitten)
        {
            for (var i = 0; i < downed; i++) round.NotifyDowned();
            for (var i = 0; i < bitten; i++) round.NotifyBitten();
        }

        [Test]
        public void プレイ中に3回撃墜するとスコアと撃墜数が3になり1回刺されるとスコアだけ2になる()
        {
            var round = CreatePlayingRound();

            NotifyDownedAndBitten(round, 3, 0);

            Assert.That(round.DownedCount, Is.EqualTo(3));
            Assert.That(round.Score, Is.EqualTo(3));

            round.NotifyBitten();

            Assert.That(round.DownedCount, Is.EqualTo(3));
            Assert.That(round.Score, Is.EqualTo(2));
        }

        [Test]
        public void 得点0で刺されてもスコアは0のままで撃墜数も変わらない()
        {
            var round = CreatePlayingRound();

            round.NotifyBitten();

            Assert.That(round.Score, Is.EqualTo(0));
            Assert.That(round.DownedCount, Is.EqualTo(0));
        }

        [Test]
        public void 撃墜1回のあと3回刺されるとスコアは0で撃墜数は1のまま()
        {
            var round = CreatePlayingRound();

            NotifyDownedAndBitten(round, 1, 3);

            Assert.That(round.Score, Is.EqualTo(0));
            Assert.That(round.DownedCount, Is.EqualTo(1));
        }

        [Test]
        public void 得点0から刺されてから撃墜するとスコアは1になる()
        {
            var round = CreatePlayingRound();

            round.NotifyBitten();
            round.NotifyDowned();

            Assert.That(round.Score, Is.EqualTo(1));
            Assert.That(round.DownedCount, Is.EqualTo(1));
        }

        [Test]
        public void 得点0から撃墜してから刺されるとスコアは0になる()
        {
            var round = CreatePlayingRound();

            round.NotifyDowned();
            round.NotifyBitten();

            Assert.That(round.Score, Is.EqualTo(0));
            Assert.That(round.DownedCount, Is.EqualTo(1));
        }

        [Test]
        public void 待機では撃墜も刺されたことも無視され撃墜数とスコアが変わらない()
        {
            var round = CreateRound();

            round.NotifyDowned();
            round.NotifyBitten();

            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Waiting));
            Assert.That(round.DownedCount, Is.EqualTo(0));
            Assert.That(round.Score, Is.EqualTo(0));
        }

        [Test]
        public void 結果では撃墜も刺されたことも無視され撃墜数とスコアが変わらない()
        {
            var round = CreatePlayingRound();
            NotifyDownedAndBitten(round, 3, 1);
            round.Advance(60f);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Result));

            round.NotifyDowned();
            round.NotifyBitten();

            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Result));
            Assert.That(round.DownedCount, Is.EqualTo(3));
            Assert.That(round.Score, Is.EqualTo(2));
        }

        [Test]
        public void 時間切れで結果になっても撃墜数とスコアが残り受付待ちのあとの再開で待機に戻ると0に戻る()
        {
            var round = CreatePlayingRound();
            NotifyDownedAndBitten(round, 3, 1);

            round.Advance(60f);

            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Result));
            Assert.That(round.DownedCount, Is.EqualTo(3));
            Assert.That(round.Score, Is.EqualTo(2));

            round.Advance(1.0f);
            var changed = round.RequestStart(Position);

            Assert.That(changed, Is.True);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Waiting));
            Assert.That(round.DownedCount, Is.EqualTo(0));
            Assert.That(round.Score, Is.EqualTo(0));
        }

        [Test]
        public void プレイ中に撃墜したあとの開始要求は変わらなかったと返り撃墜数とスコアが変わらない()
        {
            var round = CreatePlayingRound();
            NotifyDownedAndBitten(round, 2, 0);

            var changed = round.RequestStart(new Vector3(-2f, 0.7f, 3f));

            Assert.That(changed, Is.False);
            Assert.That(round.DownedCount, Is.EqualTo(2));
            Assert.That(round.Score, Is.EqualTo(2));
        }
    }
}
