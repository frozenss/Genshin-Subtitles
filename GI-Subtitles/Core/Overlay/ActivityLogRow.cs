using System;

namespace GI_Subtitles.Core.Overlay
{
    public sealed class ActivityLogRow
    {
        public ActivityLogRow(
            DateTime utcTimestamp,
            OperatorJob job,
            int? pairOrdinal,
            string resultResourceKey,
            object[] resultFormatArguments)
        {
            UtcTimestamp = utcTimestamp;
            Job = job;
            PairOrdinal = pairOrdinal;
            ResultResourceKey = resultResourceKey;
            ResultFormatArguments = resultFormatArguments;
        }

        public DateTime UtcTimestamp { get; }

        public OperatorJob Job { get; }

        public int? PairOrdinal { get; }

        public string ResultResourceKey { get; }

        public object[] ResultFormatArguments { get; }
    }
}
