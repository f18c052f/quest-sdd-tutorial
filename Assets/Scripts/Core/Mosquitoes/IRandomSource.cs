namespace MosquitoSpray.Core.Mosquitoes
{
    /// <summary>出現位置を決める乱数の供給元。本番の実装は Adapter に置き、テストでは値を固定する。</summary>
    public interface IRandomSource
    {
        /// <summary>0 以上 1 未満の値を返す。</summary>
        float NextFloat();
    }
}
