using System;
using MosquitoSpray.Core.Mosquitoes;
using NUnit.Framework;

namespace MosquitoSpray.Tests.EditMode.Mosquitoes
{
    public class MosquitoRulesTests
    {
        // 仮の初期値。引数を名前で上書きして、1つだけ不正な値にする
        static MosquitoRules Create(
            float biteDistance = 0.3f,
            float biteDuration = 2.0f,
            float spawnMinRadius = 1.0f,
            float spawnMaxRadius = 2.0f,
            float spawnMinHeight = -0.5f,
            float spawnMaxHeight = 0.5f,
            int maxCount = 3,
            float spawnInterval = 2.0f,
            float approachSpeed = 0.5f)
        {
            return new MosquitoRules(
                biteDistance, biteDuration, spawnMinRadius, spawnMaxRadius,
                spawnMinHeight, spawnMaxHeight, maxCount, spawnInterval, approachSpeed);
        }

        static void AssertRejected(TestDelegate create, string paramName)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(create);

            Assert.That(ex.ParamName, Is.EqualTo(paramName));
        }

        [Test]
        public void 仮の初期値で作ると9つのプロパティが渡した値と等しい()
        {
            var rules = Create();

            Assert.That(rules.BiteDistance, Is.EqualTo(0.3f));
            Assert.That(rules.BiteDuration, Is.EqualTo(2.0f));
            Assert.That(rules.SpawnMinRadius, Is.EqualTo(1.0f));
            Assert.That(rules.SpawnMaxRadius, Is.EqualTo(2.0f));
            Assert.That(rules.SpawnMinHeight, Is.EqualTo(-0.5f));
            Assert.That(rules.SpawnMaxHeight, Is.EqualTo(0.5f));
            Assert.That(rules.MaxCount, Is.EqualTo(3));
            Assert.That(rules.SpawnInterval, Is.EqualTo(2.0f));
            Assert.That(rules.ApproachSpeed, Is.EqualTo(0.5f));
        }

        [Test]
        public void 仮の初期値以外の組でも作れて内側と外側が等しい水平距離と下限と上限が等しい高さも受け付ける()
        {
            var rules = Create(
                biteDistance: 0.1f, biteDuration: 0.5f,
                spawnMinRadius: 0f, spawnMaxRadius: 0f,
                spawnMinHeight: 0f, spawnMaxHeight: 0f,
                maxCount: 1, spawnInterval: 0.1f, approachSpeed: 3f);

            Assert.That(rules.BiteDistance, Is.EqualTo(0.1f));
            Assert.That(rules.BiteDuration, Is.EqualTo(0.5f));
            Assert.That(rules.SpawnMinRadius, Is.EqualTo(0f));
            Assert.That(rules.SpawnMaxRadius, Is.EqualTo(0f));
            Assert.That(rules.SpawnMinHeight, Is.EqualTo(0f));
            Assert.That(rules.SpawnMaxHeight, Is.EqualTo(0f));
            Assert.That(rules.MaxCount, Is.EqualTo(1));
            Assert.That(rules.SpawnInterval, Is.EqualTo(0.1f));
            Assert.That(rules.ApproachSpeed, Is.EqualTo(3f));
        }

        [Test]
        public void 負の高さの範囲も受け付ける()
        {
            var rules = Create(spawnMinHeight: -2f, spawnMaxHeight: -1f);

            Assert.That(rules.SpawnMinHeight, Is.EqualTo(-2f));
            Assert.That(rules.SpawnMaxHeight, Is.EqualTo(-1f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void 刺された判定の距離が不正ならbiteDistanceで拒否する(float value)
        {
            AssertRejected(() => Create(biteDistance: value), "biteDistance");
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void 刺されるまでの秒数が不正ならbiteDurationで拒否する(float value)
        {
            AssertRejected(() => Create(biteDuration: value), "biteDuration");
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void 出現間隔が不正ならspawnIntervalで拒否する(float value)
        {
            AssertRejected(() => Create(spawnInterval: value), "spawnInterval");
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void 接近の速さが不正ならapproachSpeedで拒否する(float value)
        {
            AssertRejected(() => Create(approachSpeed: value), "approachSpeed");
        }

        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void 内側の水平距離が不正ならspawnMinRadiusで拒否する(float value)
        {
            AssertRejected(() => Create(spawnMinRadius: value), "spawnMinRadius");
        }

        [Test]
        public void 外側の水平距離が内側より小さいならspawnMaxRadiusで拒否する()
        {
            AssertRejected(() => Create(spawnMinRadius: 1.0f, spawnMaxRadius: 0.9f), "spawnMaxRadius");
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void 外側の水平距離が有限値でないならspawnMaxRadiusで拒否する(float value)
        {
            AssertRejected(() => Create(spawnMaxRadius: value), "spawnMaxRadius");
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void 高さの下限が有限値でないならspawnMinHeightで拒否する(float value)
        {
            AssertRejected(() => Create(spawnMinHeight: value), "spawnMinHeight");
        }

        [Test]
        public void 高さの下限が上限より大きいならspawnMaxHeightで拒否する()
        {
            AssertRejected(() => Create(spawnMinHeight: 0.5f, spawnMaxHeight: -0.5f), "spawnMaxHeight");
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void 高さの上限が有限値でないならspawnMaxHeightで拒否する(float value)
        {
            AssertRejected(() => Create(spawnMaxHeight: value), "spawnMaxHeight");
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void 同時数の上限が1未満ならmaxCountで拒否する(int value)
        {
            AssertRejected(() => Create(maxCount: value), "maxCount");
        }

        [Test]
        public void 複数の値が不正なら引数の並び順で最初の不正な値を知らせる()
        {
            AssertRejected(() => Create(biteDistance: 0f, maxCount: 0, approachSpeed: -1f), "biteDistance");
            AssertRejected(() => Create(maxCount: 0, approachSpeed: -1f), "maxCount");
            AssertRejected(() => Create(spawnMinRadius: -1f, spawnMaxRadius: -2f), "spawnMinRadius");
        }
    }
}
