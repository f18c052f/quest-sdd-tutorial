using System;
using MosquitoSpray.Core.Rounds;
using NUnit.Framework;

namespace MosquitoSpray.Tests.EditMode.Rounds
{
    public class RoundRulesTests
    {
        // 仮の初期値。引数を名前で上書きして、1つだけ不正な値にする
        static RoundRules Create(
            float timeLimit = 60f,
            float restartDelay = 1.0f,
            float strayWarnDistance = 0.5f,
            float strayClearDistance = 0.4f)
        {
            return new RoundRules(timeLimit, restartDelay, strayWarnDistance, strayClearDistance);
        }

        static void AssertRejected(TestDelegate create, string paramName)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(create);

            Assert.That(ex.ParamName, Is.EqualTo(paramName));
        }

        [Test]
        public void 仮の初期値で作ると4つのプロパティが渡した値と等しい()
        {
            var rules = Create();

            Assert.That(rules.TimeLimit, Is.EqualTo(60f));
            Assert.That(rules.RestartDelay, Is.EqualTo(1.0f));
            Assert.That(rules.StrayWarnDistance, Is.EqualTo(0.5f));
            Assert.That(rules.StrayClearDistance, Is.EqualTo(0.4f));
        }

        [Test]
        public void 受付待ちが0で消す距離と出す距離が等しい組も受け付ける()
        {
            var rules = Create(timeLimit: 10f, restartDelay: 0f, strayWarnDistance: 1.0f, strayClearDistance: 1.0f);

            Assert.That(rules.TimeLimit, Is.EqualTo(10f));
            Assert.That(rules.RestartDelay, Is.EqualTo(0f));
            Assert.That(rules.StrayWarnDistance, Is.EqualTo(1.0f));
            Assert.That(rules.StrayClearDistance, Is.EqualTo(1.0f));
        }

        [Test]
        public void 仮の初期値以外の別の組でも作れる()
        {
            var rules = Create(timeLimit: 2.5f, restartDelay: 0.25f, strayWarnDistance: 0.75f, strayClearDistance: 0.25f);

            Assert.That(rules.TimeLimit, Is.EqualTo(2.5f));
            Assert.That(rules.RestartDelay, Is.EqualTo(0.25f));
            Assert.That(rules.StrayWarnDistance, Is.EqualTo(0.75f));
            Assert.That(rules.StrayClearDistance, Is.EqualTo(0.25f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void 制限時間が不正ならtimeLimitで拒否する(float value)
        {
            AssertRejected(() => Create(timeLimit: value), "timeLimit");
        }

        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void 受付待ちが不正ならrestartDelayで拒否する(float value)
        {
            AssertRejected(() => Create(restartDelay: value), "restartDelay");
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void 警告を出す距離が不正ならstrayWarnDistanceで拒否する(float value)
        {
            AssertRejected(() => Create(strayWarnDistance: value), "strayWarnDistance");
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void 警告を消す距離が不正ならstrayClearDistanceで拒否する(float value)
        {
            AssertRejected(() => Create(strayClearDistance: value), "strayClearDistance");
        }

        [Test]
        public void 消す距離が出す距離より大きいならstrayClearDistanceで拒否する()
        {
            AssertRejected(() => Create(strayWarnDistance: 0.5f, strayClearDistance: 0.6f), "strayClearDistance");
        }

        [Test]
        public void 複数の値が不正なら引数の並び順で最初の不正な値を知らせる()
        {
            AssertRejected(() => Create(timeLimit: 0f, restartDelay: -1f, strayWarnDistance: 0f, strayClearDistance: 0f), "timeLimit");
            AssertRejected(() => Create(restartDelay: -1f, strayWarnDistance: 0f, strayClearDistance: 0f), "restartDelay");
            AssertRejected(() => Create(strayWarnDistance: 0f, strayClearDistance: 0f), "strayWarnDistance");
        }
    }
}
