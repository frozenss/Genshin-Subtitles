using System;
using System.Collections.Generic;
using GI_Subtitles.Core.Overlay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GI_Test
{
    /// <summary>
    /// The result column's segmented projection: every line is a localized
    /// bracketed result tag plus the content after it, composed from separate
    /// resources so the tag never has to be parsed back out of localized
    /// text. The segments are the single source the window shows and copies.
    /// </summary>
    [TestClass]
    public class TestActivityLogResultComposer
    {
        // Mirrors the zh-CN resource values; the resolver stands in for the
        // window's resource lookup so the composer stays testable.
        private static readonly Dictionary<string, string> Texts = new Dictionary<string, string>
        {
            { ActivityLogResultComposer.ResourceKeyTagOcr, "[OCR]" },
            { ActivityLogResultComposer.ResourceKeyTagOriginal, "[原文]" },
            { ActivityLogResultComposer.ResourceKeyTagTranslation, "[译文]" },
            { ActivityLogResultComposer.ResourceKeyQuoted, "「{0}」" },
            { ActivityLogResultComposer.ResourceKeyMatchMiss, " 匹配 miss" },
            { ActivityLogResultComposer.ResourceKeyDetectionMiss, "检测 miss" },
            { "Test_ActionFinished", "完成 {0}" }
        };

        [TestMethod]
        public void NormalPipelineRow_ComposesTaggedOcrSourceTranslationLines()
        {
            ActivityLogResultProjection projection = ActivityLogResultComposer.Compose(
                Row(ocrText: "hello", original: "你好", translation: "hello world"),
                Resolve);

            Assert.AreEqual(
                "[OCR]「hello」" + Environment.NewLine
                + "[原文]「你好」" + Environment.NewLine
                + "[译文]「hello world」",
                projection.PlainText);
            Assert.AreEqual(3, projection.Lines.Count);
            Assert.AreEqual(ActivityLogResultTag.Ocr, projection.Lines[0].Tag);
            Assert.AreEqual("[OCR]", projection.Lines[0].TagText);
            Assert.AreEqual("「hello」", projection.Lines[0].ContentText);
            Assert.AreEqual(ActivityLogResultTag.Original, projection.Lines[1].Tag);
            Assert.AreEqual(ActivityLogResultTag.Translation, projection.Lines[2].Tag);
        }

        [TestMethod]
        public void MatchMissRow_BorrowsTheSourceTag()
        {
            ActivityLogResultProjection projection = ActivityLogResultComposer.Compose(
                Row(ocrText: "hello", matchMiss: true),
                Resolve);

            Assert.AreEqual(
                "[OCR]「hello」" + Environment.NewLine + "[原文] 匹配 miss",
                projection.PlainText);
            Assert.AreEqual(2, projection.Lines.Count);
            Assert.AreEqual(ActivityLogResultTag.Original, projection.Lines[1].Tag);
            Assert.AreEqual("[原文]", projection.Lines[1].TagText);
            Assert.AreEqual(" 匹配 miss", projection.Lines[1].ContentText);
        }

        [TestMethod]
        public void DetectionMissRow_CarriesNoTag()
        {
            ActivityLogResultProjection projection = ActivityLogResultComposer.Compose(
                Row(detectionMiss: true),
                Resolve);

            Assert.AreEqual("检测 miss", projection.PlainText);
            Assert.AreEqual(1, projection.Lines.Count);
            Assert.AreEqual(ActivityLogResultTag.None, projection.Lines[0].Tag);
            Assert.AreEqual(string.Empty, projection.Lines[0].TagText);
            Assert.AreEqual("检测 miss", projection.Lines[0].ContentText);
        }

        [TestMethod]
        public void DualOutputTranslation_IsOneTaggedLineWithStackedContent()
        {
            string dual = "第一语言" + Environment.NewLine + "second language";
            ActivityLogResultProjection projection = ActivityLogResultComposer.Compose(
                Row(ocrText: "x", original: "y", translation: dual),
                Resolve);

            Assert.AreEqual(3, projection.Lines.Count);
            ActivityLogResultLine translationLine = projection.Lines[2];
            Assert.AreEqual(ActivityLogResultTag.Translation, translationLine.Tag);
            Assert.AreEqual("[译文]", translationLine.TagText);
            Assert.AreEqual("「" + dual + "」", translationLine.ContentText);
            Assert.AreEqual(
                "[OCR]「x」" + Environment.NewLine
                + "[原文]「y」" + Environment.NewLine
                + "[译文]「" + dual + "」",
                projection.PlainText);
        }

        [TestMethod]
        public void ActionRow_ResolvesItsOwnResourceKeyUntagged()
        {
            ActivityLogResultProjection projection = ActivityLogResultComposer.Compose(
                Row(resultResourceKey: "Test_ActionFinished", resultFormatArguments: new object[] { "日本語パック" }),
                Resolve);

            Assert.AreEqual("完成 日本語パック", projection.PlainText);
            Assert.AreEqual(1, projection.Lines.Count);
            Assert.AreEqual(ActivityLogResultTag.None, projection.Lines[0].Tag);
            Assert.AreEqual(string.Empty, projection.Lines[0].TagText);
        }

        private static string Resolve(string key, object[] args)
        {
            string format;
            if (string.IsNullOrEmpty(key) || !Texts.TryGetValue(key, out format))
            {
                return string.Empty;
            }

            if (args == null || args.Length == 0)
            {
                return format;
            }

            return string.Format(format, args);
        }

        private static ActivityLogRow Row(
            string ocrText = null,
            string original = null,
            string translation = null,
            bool detectionMiss = false,
            bool matchMiss = false,
            string resultResourceKey = null,
            object[] resultFormatArguments = null)
        {
            return new ActivityLogRow(
                new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc),
                new[] { OperatorJob.Capture, OperatorJob.Ocr, OperatorJob.Match },
                ActivityLogScope.Pair,
                1,
                true,
                resultResourceKey,
                resultFormatArguments,
                ocrText,
                original,
                translation,
                detectionMiss,
                matchMiss,
                false);
        }
    }
}
