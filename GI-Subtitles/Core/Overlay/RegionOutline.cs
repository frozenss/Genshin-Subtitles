namespace GI_Subtitles.Core.Overlay
{
    public sealed class RegionOutline
    {
        public RegionOutline(int pairOrdinal, OverlayRect rect, bool isDisplay)
        {
            PairOrdinal = pairOrdinal;
            Rect = rect ?? OverlayRect.Invalid;
            IsDisplay = isDisplay;
        }

        public int PairOrdinal { get; }

        public OverlayRect Rect { get; }

        public bool IsDisplay { get; }
    }
}
