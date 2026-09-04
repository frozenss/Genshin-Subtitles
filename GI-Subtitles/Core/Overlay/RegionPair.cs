namespace GI_Subtitles.Core.Overlay
{
    public sealed class RegionPair
    {
        public RegionPair(OverlayRect capture, OverlayRect display)
        {
            Capture = capture ?? OverlayRect.Invalid;
            Display = display ?? OverlayRect.Invalid;
        }

        public OverlayRect Capture { get; }

        public OverlayRect Display { get; }
    }
}
