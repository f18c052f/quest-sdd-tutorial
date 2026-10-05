using System.Numerics;
using MosquitoSpray.Core.Rounds;
using NUnit.Framework;

namespace MosquitoSpray.Tests.EditMode.Rounds
{
    // 定位置と離脱警告（1.3, 5.1, 5.6, 5.7, 5.8）
    public sealed partial class RoundTests
    {
        // 定位置から水平方向に distance だけ離れた位置（高さは変えない）
        static Vector3 OffsetFrom(Vector3 home, float distance) => home + new Vector3(distance, 0f, 0f);

        // 警告を立てたプレイ中のラウンド
        static Round CreateWarningRound()
        {
            var round = CreatePlayingRound();
            Assert.That(round.UpdateStray(OffsetFrom(Position, 0.6f)), Is.True); // 警告が立っていることを先に確かめる
            return round;
        }

        [Test]
        public void 作った直後は離脱警告が出ていない()
        {
            var round = CreateRound();

            Assert.That(round.IsStrayWarning, Is.False);
        }

        [Test]
        public void 待機で遠い位置を渡しても警告は出ない()
        {
            var round = CreateRound();

            var warning = round.UpdateStray(OffsetFrom(Position, 5f));

            Assert.That(warning, Is.False);
            Assert.That(round.IsStrayWarning, Is.False);
        }

        [Test]
        public void プレイ中に水平0_6mで警告が出て0_45mでも続き0_3mで解ける()
        {
            var round = CreatePlayingRound();

            Assert.That(round.UpdateStray(OffsetFrom(Position, 0.6f)), Is.True);
            Assert.That(round.IsStrayWarning, Is.True);

            Assert.That(round.UpdateStray(OffsetFrom(Position, 0.45f)), Is.True);
            Assert.That(round.IsStrayWarning, Is.True);

            Assert.That(round.UpdateStray(OffsetFrom(Position, 0.3f)), Is.False);
            Assert.That(round.IsStrayWarning, Is.False);
        }

        [Test]
        public void 警告が出たままラウンドが終わると警告が消え結果で遠い位置を渡しても出ない()
        {
            var round = CreateWarningRound();

            round.Advance(60f);

            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Result));
            Assert.That(round.IsStrayWarning, Is.False);
            Assert.That(round.UpdateStray(OffsetFrom(Position, 5f)), Is.False);
            Assert.That(round.IsStrayWarning, Is.False);
        }

        [Test]
        public void 警告が出たまま結果から待機へ戻り次の開始で新しい定位置から警告なしで始まる()
        {
            var round = CreateWarningRound();
            round.Advance(60f);
            round.Advance(1.0f);
            round.RequestStart(Position);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Waiting));
            Assert.That(round.IsStrayWarning, Is.False);

            var newHome = new Vector3(-2f, 0.7f, 3f);
            var changed = round.RequestStart(newHome);

            Assert.That(changed, Is.True);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Playing));
            Assert.That(round.IsStrayWarning, Is.False);
            Assert.That(round.HomePosition, Is.EqualTo(newHome));
        }

        [Test]
        public void 新しい定位置の近くなら古い定位置から遠くても警告は出ない()
        {
            var round = CreateWarningRound();
            round.Advance(60f);
            round.Advance(1.0f);
            round.RequestStart(Position);
            var newHome = new Vector3(-2f, 0.7f, 3f);
            round.RequestStart(newHome);

            var warning = round.UpdateStray(OffsetFrom(newHome, 0.1f));

            Assert.That(warning, Is.False);
            Assert.That(round.IsStrayWarning, Is.False);
        }

        [Test]
        public void 警告が出ていても残り時間の進み方は警告のないラウンドと同じ()
        {
            var warned = CreateWarningRound();
            var twin = CreatePlayingRound();

            for (var i = 0; i < 64; i++)
            {
                warned.Advance(1f / 64f);
                twin.Advance(1f / 64f);
            }

            Assert.That(warned.IsStrayWarning, Is.True);
            Assert.That(warned.RemainingTime, Is.EqualTo(twin.RemainingTime));
            Assert.That(warned.RemainingTime, Is.EqualTo(59f));
            Assert.That(warned.Phase, Is.EqualTo(twin.Phase));
        }

        [Test]
        public void 警告が出ていても撃墜と刺されたことは警告のないラウンドと同じに数える()
        {
            var warned = CreateWarningRound();
            var twin = CreatePlayingRound();

            foreach (var round in new[] { warned, twin })
            {
                round.NotifyBitten(); // スコア 0 では減らない
                round.NotifyDowned();
                round.NotifyDowned();
                round.NotifyBitten();
            }

            Assert.That(warned.IsStrayWarning, Is.True);
            Assert.That(warned.DownedCount, Is.EqualTo(twin.DownedCount));
            Assert.That(warned.Score, Is.EqualTo(twin.Score));
            Assert.That(warned.DownedCount, Is.EqualTo(2));
            Assert.That(warned.Score, Is.EqualTo(1));
        }

        [Test]
        public void 離脱を更新しても状態と残り時間と撃墜数とスコアと定位置が変わらない()
        {
            var round = CreatePlayingRound();
            round.NotifyDowned();
            round.NotifyDowned();
            round.NotifyBitten();
            round.Advance(1.5f);

            var phase = round.Phase;
            var remaining = round.RemainingTime;
            var downed = round.DownedCount;
            var score = round.Score;
            var home = round.HomePosition;

            // 警告を立てる更新と、解く更新の両方で確かめる
            foreach (var distance in new[] { 0.6f, 0.45f, 0.3f, 0f })
            {
                round.UpdateStray(OffsetFrom(Position, distance));

                Assert.That(round.Phase, Is.EqualTo(phase));
                Assert.That(round.RemainingTime, Is.EqualTo(remaining));
                Assert.That(round.DownedCount, Is.EqualTo(downed));
                Assert.That(round.Score, Is.EqualTo(score));
                Assert.That(round.HomePosition, Is.EqualTo(home));
            }
        }
    }
}
