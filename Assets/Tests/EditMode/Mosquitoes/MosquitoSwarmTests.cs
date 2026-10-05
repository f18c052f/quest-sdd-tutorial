using System;
using System.Numerics;
using MosquitoSpray.Core.Mosquitoes;
using NUnit.Framework;

namespace MosquitoSpray.Tests.EditMode.Mosquitoes
{
    // 共通の準備と、開始・停止・経過秒数の検証。出現・接近・刺された判定は別の partial ファイルに置く
    public sealed partial class MosquitoSwarmTests
    {
        // 原点以外の顔の位置。差し引き忘れを見つける
        static readonly Vector3 Face = new Vector3(0.5f, 1.2f, -0.3f);

        // 仮の初期値
        static MosquitoRules DefaultRules() =>
            new MosquitoRules(0.3f, 2.0f, 1.0f, 2.0f, -0.5f, 0.5f, 3, 2.0f, 0.5f);

        // 仮の初期値のうち biteDuration だけ 1000 秒。刺された判定を確かめるテスト以外で使う
        static MosquitoRules NonBitingRules() =>
            new MosquitoRules(0.3f, 1000f, 1.0f, 2.0f, -0.5f, 0.5f, 3, 2.0f, 0.5f);

        static MosquitoSwarm CreateSwarm(MosquitoRules rules = null, IRandomSource random = null) =>
            new MosquitoSwarm(rules ?? NonBitingRules(), random ?? new FixedRandomSource(0f, 0f, 0f));

        [Test]
        public void 作った直後は出現中でなくMosquitoesが空で5秒進めても蚊がおらずSpawnedとBittenも空()
        {
            var swarm = CreateSwarm();

            Assert.That(swarm.IsSpawning, Is.False);
            Assert.That(swarm.Mosquitoes, Is.Empty);

            var result = swarm.Advance(5f, Face);

            Assert.That(swarm.Mosquitoes, Is.Empty);
            Assert.That(result.Spawned, Is.Empty);
            Assert.That(result.Bitten, Is.Empty);
        }

        [Test]
        public void 開始と停止でIsSpawningが切り替わる()
        {
            var swarm = CreateSwarm();

            swarm.Start();
            Assert.That(swarm.IsSpawning, Is.True);

            swarm.Stop();
            Assert.That(swarm.IsSpawning, Is.False);
        }

        [Test]
        public void 停止中の停止は何もしない()
        {
            var swarm = CreateSwarm();

            Assert.DoesNotThrow(() => swarm.Stop());

            Assert.That(swarm.IsSpawning, Is.False);
            Assert.That(swarm.Mosquitoes, Is.Empty);
        }

        [Test]
        public void 開始中の再開始も例外なく出現中のまま()
        {
            var swarm = CreateSwarm();
            swarm.Start();

            Assert.DoesNotThrow(() => swarm.Start());

            Assert.That(swarm.IsSpawning, Is.True);
        }

        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void 不正な経過秒数はdeltaTimeの範囲外として拒否し何も変えない(float deltaTime)
        {
            var swarm = CreateSwarm();
            swarm.Start();
            var spawningBefore = swarm.IsSpawning;
            var countBefore = swarm.Mosquitoes.Count;

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => swarm.Advance(deltaTime, Face));

            Assert.That(ex.ParamName, Is.EqualTo("deltaTime"));
            Assert.That(swarm.IsSpawning, Is.EqualTo(spawningBefore));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(countBefore));
        }

        [Test]
        public void 停止中でも不正な経過秒数は拒否する()
        {
            var swarm = CreateSwarm();

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => swarm.Advance(-1f, Face));

            Assert.That(ex.ParamName, Is.EqualTo("deltaTime"));
            Assert.That(swarm.IsSpawning, Is.False);
            Assert.That(swarm.Mosquitoes, Is.Empty);
        }

        [Test]
        public void rulesがnullならArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new MosquitoSwarm(null, new FixedRandomSource(0f)));

            Assert.That(ex.ParamName, Is.EqualTo("rules"));
        }

        [Test]
        public void randomがnullならArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new MosquitoSwarm(DefaultRules(), null));

            Assert.That(ex.ParamName, Is.EqualTo("random"));
        }

        [Test]
        public void Rulesは渡した設定値の同じ実体を返す()
        {
            var rules = DefaultRules();

            var swarm = CreateSwarm(rules);

            Assert.That(swarm.Rules, Is.SameAs(rules));
        }

        [Test]
        public void 経過秒数0は有効でAdvanceResultを返す()
        {
            var swarm = CreateSwarm();

            var result = swarm.Advance(0f, Face);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Spawned, Is.Empty);
            Assert.That(result.Bitten, Is.Empty);
        }

        [Test]
        public void FixedRandomSourceは決めた値を順に返し使い切ると先頭へ戻る()
        {
            var random = new FixedRandomSource(0.1f, 0.2f);

            Assert.That(random.NextFloat(), Is.EqualTo(0.1f));
            Assert.That(random.NextFloat(), Is.EqualTo(0.2f));
            Assert.That(random.NextFloat(), Is.EqualTo(0.1f));
            Assert.That(random.CallCount, Is.EqualTo(3));
        }
    }
}
