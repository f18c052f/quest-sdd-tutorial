namespace MosquitoSpray.Core.Rounds
{
    /// <summary>ラウンドの状態。</summary>
    public enum RoundPhase
    {
        /// <summary>開始の指示を待っている。</summary>
        Waiting,

        /// <summary>プレイ中。</summary>
        Playing,

        /// <summary>制限時間が尽きて結果を出している。</summary>
        Result,
    }
}
