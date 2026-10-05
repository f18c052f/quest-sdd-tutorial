using System;
using MosquitoSpray.Core.Mosquitoes;

namespace MosquitoSpray.Tests.EditMode.Mosquitoes
{
    /// <summary>
    /// テスト用の <see cref="IRandomSource"/>。渡した値を順に返す。
    /// 値を使い切ったら先頭へ戻って繰り返す（巡回する）。範囲外や NaN も、渡せばそのまま返す。
    /// </summary>
    public sealed class FixedRandomSource : IRandomSource
    {
        readonly float[] _values;
        int _index;

        /// <param name="values">返す値。1つ以上</param>
        public FixedRandomSource(params float[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (values.Length == 0) throw new ArgumentException("値を1つ以上渡してください。", nameof(values));
            _values = (float[])values.Clone();
        }

        /// <summary>これまでに <see cref="NextFloat"/> が呼ばれた回数。</summary>
        public int CallCount { get; private set; }

        public float NextFloat()
        {
            var value = _values[_index];
            _index = (_index + 1) % _values.Length;
            CallCount++;
            return value;
        }
    }
}
