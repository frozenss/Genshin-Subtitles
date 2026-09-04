using System.Collections.Generic;

namespace GI_Subtitles.Core.Overlay
{
    public interface IRegionPairStore
    {
        IReadOnlyList<RegionPairRecord> ReadPairs();

        LegacyRegionSlots ReadLegacy();

        void WritePairs(IReadOnlyList<RegionPairRecord> pairs);
    }

    public sealed class RegionPairRecord
    {
        public OverlayRect Capture { get; set; }

        public OverlayRect Display { get; set; }
    }

    public sealed class LegacyRegionSlots
    {
        public string Region { get; set; }

        public string Region2 { get; set; }

        public int PadVertical { get; set; }

        public int PadHorizontal { get; set; }
    }
}
