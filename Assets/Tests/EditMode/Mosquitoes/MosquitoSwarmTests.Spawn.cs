using System.Linq;
using System.Numerics;
using MosquitoSpray.Core.Mosquitoes;
using NUnit.Framework;

namespace MosquitoSpray.Tests.EditMode.Mosquitoes
{
    // 出現の位置、最初の1匹、出現間隔と同時数の上限
    public sealed partial class MosquitoSwarmTests
    {
        const float SpawnTolerance = 1e-4f;

        // 乱数を System.Random から作るテスト用の供給元。同じ種なら同じ並びになる
        sealed class SeededRandomSource : IRandomSource
        {
            readonly System.Random _random;
            public SeededRandomSource(int seed) { _random = new System.Random(seed); }
            public float NextFloat() => (float)_random.NextDouble();
        }

        static void AssertPosition(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.X, Is.EqualTo(expected.X).Within(SpawnTolerance));
            Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(SpawnTolerance));
            Assert.That(actual.Z, Is.EqualTo(expected.Z).Within(SpawnTolerance));
        }

        static void AssertInSpawnRange(Vector3 position, Vector3 face)
        {
            var dx = position.X - face.X;
            var dz = position.Z - face.Z;
            var horizontal = (float)System.Math.Sqrt(dx * dx + dz * dz);
            var height = position.Y - face.Y;
            Assert.That(horizontal, Is.InRange(1.0f - SpawnTolerance, 2.0f + SpawnTolerance));
            Assert.That(height, Is.InRange(-0.5f - SpawnTolerance, 0.5f + SpawnTolerance));
        }

        static Mosquito SpawnOne(MosquitoSwarm swarm, Vector3 face)
        {
            swarm.Start();
            return swarm.Advance(0f, face).Spawned.Single();
        }

        [Test]
        public void 乱数が0_0_0なら顔から水平に1m前方で高さは下限の位置に出る()
        {
            var swarm = CreateSwarm(DefaultRules(), new FixedRandomSource(0f, 0f, 0f));

            var mosquito = SpawnOne(swarm, Face);

            AssertPosition(mosquito.Position, Face + new Vector3(1.0f, -0.5f, 0f));
        }

        [Test]
        public void 乱数が0_25_1_1なら角度90度で水平距離2mで高さは上限の位置に出る()
        {
            var swarm = CreateSwarm(DefaultRules(), new FixedRandomSource(0.25f, 1f, 1f));

            var mosquito = SpawnOne(swarm, Face);

            AssertPosition(mosquito.Position, Face + new Vector3(0f, 0.5f, 2.0f));
        }

        [Test]
        public void 乱数は角度_水平距離_高さの順に使う()
        {
            AssertPosition(
                SpawnOne(CreateSwarm(DefaultRules(), new FixedRandomSource(0.25f, 0f, 0f)), Face).Position,
                Face + new Vector3(0f, -0.5f, 1.0f));
            AssertPosition(
                SpawnOne(CreateSwarm(DefaultRules(), new FixedRandomSource(0f, 0.25f, 0f)), Face).Position,
                Face + new Vector3(1.25f, -0.5f, 0f));
            AssertPosition(
                SpawnOne(CreateSwarm(DefaultRules(), new FixedRandomSource(0f, 0f, 0.25f)), Face).Position,
                Face + new Vector3(1.0f, -0.25f, 0f));
        }

        [Test]
        public void 乱数は1匹につき3回だけ引く()
        {
            var random = new FixedRandomSource(0.1f, 0.2f, 0.3f);
            var swarm = CreateSwarm(DefaultRules(), random);
            swarm.Start();

            Assert.That(random.CallCount, Is.EqualTo(0));

            swarm.Advance(0f, Face);

            Assert.That(random.CallCount, Is.EqualTo(3));

            // 出現しない Advance では引かない
            swarm.Advance(1f, Face);

            Assert.That(random.CallCount, Is.EqualTo(3));
        }

        [TestCase(1.5f)]
        [TestCase(-0.5f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void 乱数が範囲外でも出現位置は範囲内に収まる(float value)
        {
            var swarm = CreateSwarm(DefaultRules(), new FixedRandomSource(value, value, value));

            var mosquito = SpawnOne(swarm, Face);

            AssertInSpawnRange(mosquito.Position, Face);
        }

        [Test]
        public void 範囲外の乱数は0から1に丸めて使う()
        {
            // 1.5→1, -0.5→0, NaN→0: 角度 2π（=0度）、水平距離 2m、高さ下限
            var swarm = CreateSwarm(DefaultRules(), new FixedRandomSource(1.5f, -0.5f, float.NaN));
            AssertPosition(SpawnOne(swarm, Face).Position, Face + new Vector3(1.0f, -0.5f, 0f));

            // +∞→1, NaN→0, -∞→0
            var swarm2 = CreateSwarm(DefaultRules(), new FixedRandomSource(float.PositiveInfinity, float.NaN, float.NegativeInfinity));
            AssertPosition(SpawnOne(swarm2, Face).Position, Face + new Vector3(1.0f, -0.5f, 0f));

            // 水平距離だけ 1.5→1 に丸まり 2m になる
            var swarm3 = CreateSwarm(DefaultRules(), new FixedRandomSource(0f, 1.5f, 0f));
            AssertPosition(SpawnOne(swarm3, Face).Position, Face + new Vector3(2.0f, -0.5f, 0f));

            // 高さだけ 1.5→1 に丸まり +0.5 になる
            var swarm4 = CreateSwarm(DefaultRules(), new FixedRandomSource(0f, 0f, 1.5f));
            AssertPosition(SpawnOne(swarm4, Face).Position, Face + new Vector3(1.0f, 0.5f, 0f));
        }

        [Test]
        public void 顔の位置を変えて100回出しても全部範囲内()
        {
            var swarm = CreateSwarm(DefaultRules(), new SeededRandomSource(12345));
            var faceRandom = new System.Random(777);

            for (var i = 0; i < 100; i++)
            {
                var face = new Vector3(
                    (float)(faceRandom.NextDouble() * 10 - 5),
                    (float)(faceRandom.NextDouble() * 3),
                    (float)(faceRandom.NextDouble() * 10 - 5));

                var mosquito = SpawnOne(swarm, face);

                AssertInSpawnRange(mosquito.Position, face);
            }
        }

        [Test]
        public void 同じ乱数の並びなら2つの群れで同じ位置が同じ順に出る()
        {
            var swarmA = CreateSwarm(DefaultRules(), new SeededRandomSource(42));
            var swarmB = CreateSwarm(DefaultRules(), new SeededRandomSource(42));

            for (var i = 0; i < 10; i++)
            {
                var a = SpawnOne(swarmA, Face);
                var b = SpawnOne(swarmB, Face);

                Assert.That(a.Position, Is.EqualTo(b.Position));
            }
        }

        [Test]
        public void 出現位置は渡した顔の位置を基準にする()
        {
            var faceA = new Vector3(0.5f, 1.2f, -0.3f);
            var faceB = new Vector3(-3f, 0.4f, 7f);
            var swarmA = CreateSwarm(DefaultRules(), new FixedRandomSource(0.1f, 0.6f, 0.9f));
            var swarmB = CreateSwarm(DefaultRules(), new FixedRandomSource(0.1f, 0.6f, 0.9f));

            var a = SpawnOne(swarmA, faceA);
            var b = SpawnOne(swarmB, faceB);

            AssertPosition(b.Position - faceB, a.Position - faceA);
        }

        [Test]
        public void Startのあとの最初のAdvanceで1匹だけ出る()
        {
            var swarm = CreateSwarm();
            swarm.Start();

            var result = swarm.Advance(0.5f, Face);

            Assert.That(result.Spawned.Count, Is.EqualTo(1));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(1));
        }

        [Test]
        public void 経過秒数0のAdvanceでも最初の1匹は出る()
        {
            var swarm = CreateSwarm();
            swarm.Start();

            var result = swarm.Advance(0f, Face);

            Assert.That(result.Spawned.Count, Is.EqualTo(1));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(1));
        }

        [Test]
        public void 出現間隔に満たない2回目のAdvanceでは出ない()
        {
            var swarm = CreateSwarm();
            swarm.Start();
            swarm.Advance(0f, Face);

            var second = swarm.Advance(0.1f, Face);

            Assert.That(second.Spawned, Is.Empty);
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(1));
        }

        [Test]
        public void SpawnedにはそのAdvanceで出た蚊だけが入りBittenは空()
        {
            var swarm = CreateSwarm();
            swarm.Start();

            var first = swarm.Advance(0f, Face);
            var second = swarm.Advance(1f, Face);

            Assert.That(first.Spawned.Single(), Is.SameAs(swarm.Mosquitoes[0]));
            Assert.That(first.Bitten, Is.Empty);
            Assert.That(second.Spawned, Is.Empty);
            Assert.That(second.Bitten, Is.Empty);
        }

        [Test]
        public void 出た蚊は接近中でBiteTimeは0でIdは1から始まる()
        {
            var swarm = CreateSwarm();

            var mosquito = SpawnOne(swarm, Face);

            Assert.That(mosquito.Id, Is.EqualTo(1));
            Assert.That(mosquito.State, Is.EqualTo(MosquitoState.Approaching));
            Assert.That(mosquito.BiteTime, Is.EqualTo(0f));
            Assert.That(mosquito.Position, Is.Not.EqualTo(Vector3.Zero));
        }

        [Test]
        public void IdはStartをまたいで1ずつ増える()
        {
            var swarm = CreateSwarm();

            var ids = new[] { SpawnOne(swarm, Face).Id, SpawnOne(swarm, Face).Id, SpawnOne(swarm, Face).Id };

            Assert.That(ids, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Stopで蚊が空になり以後Advanceしても出ない()
        {
            var swarm = CreateSwarm();
            swarm.Start();
            swarm.Advance(0f, Face);

            swarm.Stop();

            Assert.That(swarm.Mosquitoes, Is.Empty);
            var result = swarm.Advance(5f, Face);
            Assert.That(result.Spawned, Is.Empty);
            Assert.That(swarm.Mosquitoes, Is.Empty);
        }

        [Test]
        public void 開始前の停止中は出現しない乱数も引かない()
        {
            var random = new FixedRandomSource(0.5f);
            var swarm = CreateSwarm(DefaultRules(), random);

            var result = swarm.Advance(0f, Face);

            Assert.That(result.Spawned, Is.Empty);
            Assert.That(random.CallCount, Is.EqualTo(0));
        }

        [Test]
        public void Stopのあと開始し直すと1匹だけ出てIdは前より大きい()
        {
            var swarm = CreateSwarm();
            var before = SpawnOne(swarm, Face);
            swarm.Stop();

            swarm.Start();
            var result = swarm.Advance(0f, Face);

            Assert.That(result.Spawned.Count, Is.EqualTo(1));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(1));
            Assert.That(result.Spawned[0].Id, Is.GreaterThan(before.Id));
            Assert.That(result.Spawned[0].Id, Is.EqualTo(2));
        }

        [Test]
        public void 開始中の再開始はやり直しで蚊が空に戻り次のAdvanceで1匹出る()
        {
            var swarm = CreateSwarm();
            var before = SpawnOne(swarm, Face);

            swarm.Start();

            Assert.That(swarm.Mosquitoes, Is.Empty);
            Assert.That(swarm.IsSpawning, Is.True);

            var result = swarm.Advance(0f, Face);

            Assert.That(result.Spawned.Count, Is.EqualTo(1));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(1));
            Assert.That(result.Spawned[0].Id, Is.GreaterThan(before.Id));
        }

        [Test]
        public void IdはStartとStopをまたいで増え続け戻らない()
        {
            var swarm = CreateSwarm();
            var last = 0;

            for (var i = 0; i < 5; i++)
            {
                var id = SpawnOne(swarm, Face).Id;
                Assert.That(id, Is.GreaterThan(last));
                last = id;
                if (i % 2 == 0) swarm.Stop();
            }

            Assert.That(last, Is.EqualTo(5));
        }

        [Test]
        public void 不正な経過秒数で最初のAdvanceを拒否しても出現は消費されない()
        {
            var random = new FixedRandomSource(0.5f);
            var swarm = CreateSwarm(DefaultRules(), random);
            swarm.Start();

            Assert.Throws<System.ArgumentOutOfRangeException>(() => swarm.Advance(-1f, Face));

            Assert.That(random.CallCount, Is.EqualTo(0));
            Assert.That(swarm.Advance(0f, Face).Spawned.Count, Is.EqualTo(1));
        }

        static MosquitoRules RulesWith(int maxCount, float spawnInterval) =>
            new MosquitoRules(0.3f, 1000f, 1.0f, 2.0f, -0.5f, 0.5f, maxCount, spawnInterval, 0.5f);

        [Test]
        public void 一六四分の一秒ずつ進めると開始直後と2秒後と4秒後に出て4匹目は出ない()
        {
            const float Step = 1f / 64f; // 2 のべき乗なので float でも誤差が出ない
            var swarm = CreateSwarm();
            swarm.Start();
            var spawnTimes = new System.Collections.Generic.List<float>();
            var simulated = 0f;

            for (var i = 1; i <= 640; i++)
            {
                var before = swarm.Mosquitoes.Count;
                var result = swarm.Advance(Step, Face);
                simulated += Step;

                // 出た蚊だけが入り、出ていなければ空
                Assert.That(result.Spawned.Count, Is.EqualTo(swarm.Mosquitoes.Count - before));
                Assert.That(result.Spawned.Count, Is.LessThanOrEqualTo(1));
                if (result.Spawned.Count == 1) spawnTimes.Add(simulated);
            }

            // 最初は開始後の最初の時間経過（1 刻み目）、以降は経過時間がちょうど 2.0 秒、4.0 秒になる時間経過
            Assert.That(spawnTimes, Is.EqualTo(new[] { Step, 2.0f, 4.0f }));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(3));
        }

        [Test]
        public void 各AdvanceのSpawnedにはそのAdvanceで出た蚊だけが入る()
        {
            var swarm = CreateSwarm();
            swarm.Start();

            var first = swarm.Advance(5f, Face);
            var second = swarm.Advance(0f, Face);

            Assert.That(first.Spawned, Is.EqualTo(swarm.Mosquitoes));
            Assert.That(first.Spawned.Select(m => m.Id), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(second.Spawned, Is.Empty);
        }

        [Test]
        public void 開始直後に5秒進めると3匹出る()
        {
            var swarm = CreateSwarm();
            swarm.Start();

            var result = swarm.Advance(5f, Face);

            Assert.That(result.Spawned.Count, Is.EqualTo(3));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(3));
        }

        [Test]
        public void 開始直後に100秒進めても上限の3匹しか出ない()
        {
            var swarm = CreateSwarm();
            swarm.Start();

            var result = swarm.Advance(100f, Face);

            Assert.That(result.Spawned.Count, Is.EqualTo(3));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(3));
        }

        [Test]
        public void 上限1なら1匹で止まる()
        {
            var swarm = CreateSwarm(RulesWith(1, 2.0f));
            swarm.Start();

            var result = swarm.Advance(100f, Face);

            Assert.That(result.Spawned.Count, Is.EqualTo(1));
            Assert.That(swarm.Advance(100f, Face).Spawned, Is.Empty);
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(1));
        }

        [Test]
        public void 出現間隔が短いと1回のAdvanceで複数匹が順に出て乱数は続けて引く()
        {
            var random = new FixedRandomSource(0f, 0f, 0f, 0.25f, 1f, 1f);
            var swarm = CreateSwarm(RulesWith(3, 0.1f), random);
            swarm.Start();

            var result = swarm.Advance(0.1f, Face);

            Assert.That(result.Spawned.Count, Is.EqualTo(2));
            Assert.That(result.Spawned.Select(m => m.Id), Is.EqualTo(new[] { 1, 2 }));
            AssertPosition(result.Spawned[0].Position, Face + new Vector3(1.0f, -0.5f, 0f));
            AssertPosition(result.Spawned[1].Position, Face + new Vector3(0f, 0.5f, 2.0f));
            Assert.That(random.CallCount, Is.EqualTo(6));
            Assert.That(swarm.Mosquitoes, Is.EqualTo(result.Spawned));
        }

        [Test]
        public void 蚊が3匹いる状態で停止すると空になり以後Advanceしても出ない()
        {
            var swarm = CreateSwarm();
            swarm.Start();
            swarm.Advance(5f, Face);
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(3));

            swarm.Stop();

            Assert.That(swarm.Mosquitoes, Is.Empty);
            Assert.That(swarm.Advance(100f, Face).Spawned, Is.Empty);
            Assert.That(swarm.Mosquitoes, Is.Empty);
        }

        [Test]
        public void 開始し直すと経過秒数を引き継がず最初のAdvanceで1匹出る()
        {
            var swarm = CreateSwarm();
            swarm.Start();
            swarm.Advance(0f, Face);
            swarm.Advance(1.5f, Face); // 次の出現まで 0.5 秒の状態
            swarm.Stop();

            swarm.Start();
            var first = swarm.Advance(0f, Face);

            Assert.That(first.Spawned.Count, Is.EqualTo(1));
            Assert.That(swarm.Mosquitoes.Count, Is.EqualTo(1));
            // 開始し直した時点から数え直す。1.75 + 0.25 でちょうど 2 秒（どちらも float で厳密）
            Assert.That(swarm.Advance(1.75f, Face).Spawned, Is.Empty);
            Assert.That(swarm.Advance(0.25f, Face).Spawned.Count, Is.EqualTo(1));
        }
    }
}
