using System;
using System.Numerics;
using MosquitoSpray.Core.Rounds;
using NUnit.Framework;

namespace MosquitoSpray.Tests.EditMode.Rounds
{
    public sealed class StrayJudgeTests
    {
        // 定位置は原点以外にして、定位置を差し引き忘れる誤りを検出する
        static readonly Vector3 Home = new Vector3(0.5f, 1.2f, -0.3f);

        // 仮の初期値（60秒、1.0秒、警告を出す 0.5m、警告を消す 0.4m）
        static RoundRules DefaultRules() => new RoundRules(60f, 1.0f, 0.5f, 0.4f);

        // 定位置から水平に X 方向へ distance だけ離れた位置で判定する
        static bool Judge(float distance, bool warning, RoundRules rules = null)
        {
            return StrayJudge.ShouldWarn(Home, Home + new Vector3(distance, 0f, 0f), warning, rules ?? DefaultRules());
        }

        // ---- 警告なし（5.1, 5.2, 8.3） ----

        [Test]
        public void 警告なしで出す距離より近いと警告しない()
        {
            Assert.IsFalse(Judge(0.49f, false));
        }

        [Test]
        public void 警告なしで出す距離ちょうどなら警告する()
        {
            Assert.IsTrue(Judge(0.5f, false));
        }

        [Test]
        public void 警告なしで出す距離より少し手前でも許容誤差の内なら警告する()
        {
            Assert.IsTrue(Judge(0.499995f, false));
        }

        [Test]
        public void 警告なしで出す距離より遠いと警告する()
        {
            Assert.IsTrue(Judge(0.8f, false));
        }

        // ---- 警告中（5.3, 5.4, 8.3） ----

        [Test]
        public void 警告中で消す距離より遠いと続ける()
        {
            Assert.IsTrue(Judge(0.45f, true));
        }

        [Test]
        public void 警告中で消す距離ちょうどなら続ける()
        {
            Assert.IsTrue(Judge(0.4f, true));
        }

        [Test]
        public void 警告中で消す距離より少し手前でも許容誤差の内なら続ける()
        {
            Assert.IsTrue(Judge(0.399995f, true));
        }

        [Test]
        public void 警告中で消す距離より近いと解除する()
        {
            Assert.IsFalse(Judge(0.39f, true));
        }

        // ---- ヒステリシス（5.3, 5.2） ----

        [Test]
        public void 同じ距離でも直前の警告の有無で答えが変わる()
        {
            Assert.IsFalse(Judge(0.45f, false));
            Assert.IsTrue(Judge(0.45f, true));
        }

        // ---- 水平距離（5.5） ----

        [TestCase(false)]
        [TestCase(true)]
        public void 真上と真下に1メートル離れても水平距離は0なので警告しない(bool warning)
        {
            Assert.IsFalse(StrayJudge.ShouldWarn(Home, Home + new Vector3(0f, 1.0f, 0f), warning, DefaultRules()));
            Assert.IsFalse(StrayJudge.ShouldWarn(Home, Home + new Vector3(0f, -1.0f, 0f), warning, DefaultRules()));
        }

        [Test]
        public void 水平0_3メートルで高さが2メートル違っても警告なしなら警告しない()
        {
            Assert.IsFalse(StrayJudge.ShouldWarn(Home, Home + new Vector3(0.3f, 2.0f, 0f), false, DefaultRules()));
            Assert.IsFalse(StrayJudge.ShouldWarn(Home, Home + new Vector3(0.3f, -2.0f, 0f), false, DefaultRules()));
        }

        [Test]
        public void Xだけ_Zだけ_斜めのずれを同じ水平距離として扱う()
        {
            var rules = DefaultRules();
            Assert.IsTrue(StrayJudge.ShouldWarn(Home, Home + new Vector3(0.5f, 0f, 0f), false, rules));
            Assert.IsTrue(StrayJudge.ShouldWarn(Home, Home + new Vector3(0f, 0f, 0.5f), false, rules));
            Assert.IsTrue(StrayJudge.ShouldWarn(Home, Home + new Vector3(0.3f, 0f, 0.4f), false, rules));
            Assert.IsTrue(StrayJudge.ShouldWarn(Home, Home + new Vector3(-0.3f, 0f, -0.4f), false, rules));
        }

        // ---- 消す距離 = 出す距離（8.4） ----

        [Test]
        public void 消す距離と出す距離が同じ設定値で警告なしは1メートルちょうどで警告する()
        {
            var rules = new RoundRules(10f, 0f, 1.0f, 1.0f);
            Assert.IsTrue(Judge(1.0f, false, rules));
        }

        [Test]
        public void 消す距離と出す距離が同じ設定値で警告中は1メートルで続け0_99メートルで解除する()
        {
            var rules = new RoundRules(10f, 0f, 1.0f, 1.0f);
            Assert.IsTrue(Judge(1.0f, true, rules));
            Assert.IsFalse(Judge(0.99f, true, rules));
        }

        // ---- 設定値が無い ----

        [Test]
        public void 設定値が無いと引数名rulesで拒否される()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => StrayJudge.ShouldWarn(Home, Home, false, null));
            Assert.AreEqual("rules", ex.ParamName);
        }
    }
}
