using GI_Subtitles.Services.OCR;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GI_Test
{
    [TestClass]
    public class RecognitionRegionFallbackTests
    {
        [TestMethod]
        public void ConsecutivePrimaryFailuresEnableOneSecondaryProbe()
        {
            var fallback = new RecognitionRegionFallback(3);

            fallback.RecordResult(false, false);
            fallback.RecordResult(false, false);
            Assert.IsFalse(fallback.UseSecondaryRegion);

            fallback.RecordResult(false, false);
            Assert.IsTrue(fallback.UseSecondaryRegion);
        }

        [TestMethod]
        public void SuccessfulPrimaryResultClearsAccumulatedFailures()
        {
            var fallback = new RecognitionRegionFallback(3);

            fallback.RecordResult(false, false);
            fallback.RecordResult(false, false);
            fallback.RecordResult(false, true);
            fallback.RecordResult(false, false);

            Assert.IsFalse(fallback.UseSecondaryRegion);
        }

        [TestMethod]
        public void SecondaryProbeAlwaysReturnsToPrimaryRegion()
        {
            var fallback = new RecognitionRegionFallback(1);

            fallback.RecordResult(false, false);
            Assert.IsTrue(fallback.UseSecondaryRegion);

            fallback.RecordResult(true, true);
            Assert.IsFalse(fallback.UseSecondaryRegion);
        }
    }
}
