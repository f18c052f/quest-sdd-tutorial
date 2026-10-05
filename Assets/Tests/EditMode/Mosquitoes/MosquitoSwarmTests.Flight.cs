using System.Linq;
using System.Numerics;
using MosquitoSpray.Core.Mosquitoes;
using NUnit.Framework;

namespace MosquitoSpray.Tests.EditMode.Mosquitoes
{
    // 接近（顔へまっすぐ近づく）のテスト。刺さない設定値（biteDuration 1000 秒）を使う
    public sealed partial class MosquitoSwarmTests
    {
        const float Tolerance = 1e-4f;

        // 乱数 (0,0,0) で顔 + (radius, height, 0) に出る設定値。刺さない
        static MosquitoRules FlightRules(float radius, float height = 0f, int maxCount = 1, float spawnInterval = 2.0f, float approachSpeed = 0.5f) =>
            new MosquitoRules(0.3f, 1000f, radius, radius, height, height, maxCount, spawnInterval, approachSpeed);

        // 開始して経過秒数 0 で進め、顔 + (radius, height, 0) に 1 匹を出す
        static (MosquitoSwarm swarm, Mosquito mosquito) SpawnAt(Vector3 face, MosquitoRules rules)
        {
            var swarm = CreateSwarm(rules);
            swarm.Start();
            var mosquito = swarm.Advance(0f, face).Spawned.Single();
            return (swarm, mosquito);
        }

        static void AssertNear(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.X, Is.EqualTo(expected.X).Within(Tolerance));
            Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(Tolerance));
            Assert.That(actual.Z, Is.EqualTo(expected.Z).Within(Tolerance));
        }

        [Test]
        public void 顔から1_5mの蚊は1秒で0_5m顔へまっすぐ近づく()
        {
            var (swarm, mosquito) = SpawnAt(Face, FlightRules(1.5f));

            swarm.Advance(1.0f, Face);

            AssertNear(mosquito.Position, Face + new Vector3(1.0f, 0f, 0f));
            Assert.That(Vector3.Distance(mosquito.Position, Face), Is.EqualTo(1.0f).Within(Tolerance));
            Assert.That(mosquito.State, Is.EqualTo(MosquitoState.Approaching));
        }

        [Test]
        public void 高さがある蚊も顔を結ぶ線分の上を近づく()
        {
            // 顔 + (1.2, 1.6, 0) は顔まで 2.0m。1秒で 0.5m 近づくと残り 1.5m
            var (swarm, mosquito) = SpawnAt(Face, FlightRules(1.2f, 1.6f));
            var start = mosquito.Position;

            swarm.Advance(1.0f, Face);

            AssertNear(mosquito.Position, Face + new Vector3(1.2f, 1.6f, 0f) * 0.75f);
            Assert.That(Vector3.Distance(mosquito.Position, Face), Is.EqualTo(1.5f).Within(Tolerance));
            Assert.That(Vector3.Distance(start, mosquito.Position), Is.EqualTo(0.5f).Within(Tolerance));
            // 出現位置・新しい位置・顔が同じ直線上にある
            Assert.That(Vector3.Cross(start - Face, mosquito.Position - Face).Length(), Is.LessThan(Tolerance));
        }

        [Test]
        public void 顔から0_2mの蚊は1秒で顔の位置に止まり通り過ぎない()
        {
            var (swarm, mosquito) = SpawnAt(Face, FlightRules(0.2f));

            swarm.Advance(1.0f, Face);

            Assert.That(mosquito.Position, Is.EqualTo(Face));
        }

        [Test]
        public void 距離ちょうど移動量の蚊は顔の位置に着く()
        {
            var (swarm, mosquito) = SpawnAt(Face, FlightRules(0.5f));

            swarm.Advance(1.0f, Face);

            Assert.That(mosquito.Position, Is.EqualTo(Face));
        }

        [Test]
        public void 顔を動かすと新しい顔の位置へ向かう()
        {
            var faceA = Face;
            var faceB = new Vector3(-3.0f, 0.8f, 4.0f);
            var (swarm, mosquito) = SpawnAt(faceA, FlightRules(1.5f));
            var before = Vector3.Distance(mosquito.Position, faceB);
            var towardA = faceA + new Vector3(1.0f, 0f, 0f); // 古い顔の位置へ向かった場合の位置

            swarm.Advance(1.0f, faceB);

            Assert.That(Vector3.Distance(mosquito.Position, faceB), Is.EqualTo(before - 0.5f).Within(Tolerance));
            Assert.That(Vector3.Distance(mosquito.Position, towardA), Is.GreaterThan(0.1f));
        }

        [Test]
        public void 経過秒数0のAdvanceで既存の蚊の位置と状態は変わらない()
        {
            var (swarm, mosquito) = SpawnAt(Face, FlightRules(1.5f));
            var position = mosquito.Position;
            var biteTime = mosquito.BiteTime;

            swarm.Advance(0f, Face);

            Assert.That(mosquito.Position, Is.EqualTo(position));
            Assert.That(mosquito.State, Is.EqualTo(MosquitoState.Approaching));
            Assert.That(mosquito.BiteTime, Is.EqualTo(biteTime));
        }

        [Test]
        public void 出現した時間経過では出現位置から動かず次の時間経過で動く()
        {
            var swarm = CreateSwarm(FlightRules(1.5f));
            swarm.Start();

            var mosquito = swarm.Advance(1.0f, Face).Spawned.Single();

            AssertNear(mosquito.Position, Face + new Vector3(1.5f, 0f, 0f));

            swarm.Advance(1.0f, Face);

            AssertNear(mosquito.Position, Face + new Vector3(1.0f, 0f, 0f));
        }

        [Test]
        public void 顔の位置ちょうどにいる蚊は動かずNaNにもならない()
        {
            var (swarm, mosquito) = SpawnAt(Face, FlightRules(0f));
            Assert.That(mosquito.Position, Is.EqualTo(Face));

            swarm.Advance(1.0f, Face);
            Assert.That(mosquito.Position, Is.EqualTo(Face));

            swarm.Advance(1.0f, Face);
            Assert.That(mosquito.Position, Is.EqualTo(Face));
        }

        [Test]
        public void 複数の蚊がそれぞれ顔へ近づく()
        {
            // 乱数の並び: 蚊 1 は角度 0（顔 + (1.5,0,0)）、蚊 2 は角度 0.5（顔 + (-1.5,0,0)）
            var random = new FixedRandomSource(0f, 0f, 0f, 0.5f, 0f, 0f);
            var swarm = CreateSwarm(FlightRules(1.5f, 0f, 3, 1.0f), random);
            swarm.Start();
            var first = swarm.Advance(0f, Face).Spawned.Single();
            var second = swarm.Advance(1.0f, Face).Spawned.Single();

            // 蚊 1 は 2 回目の時間経過で 0.5m 進み、蚊 2 は出現した時間経過なので動かない
            AssertNear(first.Position, Face + new Vector3(1.0f, 0f, 0f));
            AssertNear(second.Position, Face + new Vector3(-1.5f, 0f, 0f));

            swarm.Advance(0.5f, Face); // 0.25m 進む。出現間隔 1.0 秒には届かず出ない

            AssertNear(first.Position, Face + new Vector3(0.75f, 0f, 0f));
            AssertNear(second.Position, Face + new Vector3(-1.25f, 0f, 0f));
        }

        [Test]
        public void 接近の速さはRulesのApproachSpeedに従う()
        {
            var (swarm, mosquito) = SpawnAt(Face, FlightRules(3.0f, 0f, 1, 2.0f, 2.0f));

            swarm.Advance(1.0f, Face);

            AssertNear(mosquito.Position, Face + new Vector3(1.0f, 0f, 0f));
        }
    }
}
