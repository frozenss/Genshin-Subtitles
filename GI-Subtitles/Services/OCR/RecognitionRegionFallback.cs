namespace GI_Subtitles.Services.OCR
{
    /// <summary>
    /// Controls the optional secondary OCR region without allowing it to become sticky.
    /// </summary>
    public sealed class RecognitionRegionFallback
    {
        public const int DefaultFailureThreshold = 5;

        private readonly int _failureThreshold;
        private int _consecutivePrimaryFailures;

        public RecognitionRegionFallback(int failureThreshold = DefaultFailureThreshold)
        {
            _failureThreshold = failureThreshold > 0
                ? failureThreshold
                : DefaultFailureThreshold;
        }

        public bool UseSecondaryRegion { get; private set; }

        public void RecordResult(bool usedSecondaryRegion, bool hasUsableText)
        {
            if (usedSecondaryRegion)
            {
                // Region 2 is a one-shot fallback probe. Always return to the region
                // explicitly selected as the primary subtitle region afterwards.
                Reset();
                return;
            }

            if (hasUsableText)
            {
                _consecutivePrimaryFailures = 0;
                return;
            }

            _consecutivePrimaryFailures++;
            if (_consecutivePrimaryFailures >= _failureThreshold)
            {
                _consecutivePrimaryFailures = 0;
                UseSecondaryRegion = true;
            }
        }

        public void Reset()
        {
            _consecutivePrimaryFailures = 0;
            UseSecondaryRegion = false;
        }
    }
}
