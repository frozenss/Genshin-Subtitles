using System;
using System.Collections.Generic;
using AppConfig = GI_Subtitles.Core.Config.Config;

namespace GI_Subtitles.Core.Overlay
{
    public sealed class ConfigRegionPairStore : IRegionPairStore
    {
        public const string PairsConfigKey = "RegionPairs";

        public IReadOnlyList<RegionPairRecord> ReadPairs()
        {
            List<RegionPairRecord> stored = AppConfig.Get<List<RegionPairRecord>>(PairsConfigKey, null);
            if (stored == null)
            {
                return Array.Empty<RegionPairRecord>();
            }

            return stored;
        }

        public LegacyRegionSlots ReadLegacy()
        {
            return new LegacyRegionSlots
            {
                Region = AppConfig.Get("Region", string.Empty),
                Region2 = AppConfig.Get("Region2", string.Empty),
                PadVertical = AppConfig.GetPad(0),
                PadHorizontal = AppConfig.GetPadHorizontal(0)
            };
        }

        public void WritePairs(IReadOnlyList<RegionPairRecord> pairs)
        {
            AppConfig.Set(PairsConfigKey, pairs);
        }
    }
}
