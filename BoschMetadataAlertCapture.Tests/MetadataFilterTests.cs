using BoschMetadataAlertCapture; // 引用主專案的命名空間

namespace BoschMetadataAlertCapture.Tests
{
    [TestClass]
    public class MetadataFilterTests
    {
        [TestMethod]
        public void ShouldSave_WhenFilterMatchesAndIsRequired_ReturnsTrue()
        {
            // Arrange (安排)
            var filter = new MetadataFilter
            {
                XmlFilters = new[] { "<tt:Appearance>" },
                RequireAllFilters = false
            };
            // 來源 XML 包含 <tt:Appearance> 標籤
            var metadataXml = "<tt:MetadataStream><tt:VideoAnalytics><tt:Frame><tt:Object><tt:Appearance></tt:Appearance></tt:Object></tt:Frame></tt:VideoAnalytics></tt:MetadataStream>";

            // Act (執行)
            bool result = filter.ShouldSave(metadataXml);

            // Assert (斷言)
            Assert.IsTrue(result, "當 XML 包含指定的過濾標籤時，應回傳 true。");
        }

        [TestMethod]
        public void ShouldSave_WhenFilterDoesNotMatch_ReturnsFalse()
        {
            // Arrange
            var filter = new MetadataFilter
            {
                XmlFilters = new[] { "<non_existent_tag>" },
                RequireAllFilters = false
            };
            var metadataXml = "<tt:MetadataStream><tt:VideoAnalytics></tt:VideoAnalytics></tt:MetadataStream>";

            // Act
            bool result = filter.ShouldSave(metadataXml);

            // Assert
            Assert.IsFalse(result, "當 XML 不包含指定的過濾標籤時，應回傳 false。");
        }

        [TestMethod]
        public void ShouldSave_WhenRequireAllFiltersIsTrueAndAllMatch_ReturnsTrue()
        {
            // Arrange
            // RequireAllFilters 設為 true，代表所有條件都必須滿足
            var filter = new MetadataFilter
            {
                XmlFilters = new[] { "<tt:Object>", "<tt:Appearance>" },
                RequireAllFilters = true
            };
            var metadataXml = "<tt:MetadataStream><tt:VideoAnalytics><tt:Frame><tt:Object><tt:Appearance></tt:Appearance></tt:Object></tt:Frame></tt:VideoAnalytics></tt:MetadataStream>";

            // Act
            bool result = filter.ShouldSave(metadataXml);

            // Assert
            Assert.IsTrue(result, "當 RequireAllFilters 為 true 且所有標籤都符合時，應回傳 true。");
        }

        [TestMethod]
        public void ShouldSave_WhenRequireAllFiltersIsTrueAndOneDoesNotMatch_ReturnsFalse()
        {
            // Arrange
            // RequireAllFilters 設為 true，但其中一個條件不滿足
            var filter = new MetadataFilter
            {
                XmlFilters = new[] { "<tt:Object>", "<non_existent_tag>" },
                RequireAllFilters = true
            };
            var metadataXml = "<tt:MetadataStream><tt:VideoAnalytics><tt:Object></tt:Object></tt:VideoAnalytics></tt:MetadataStream>";

            // Act
            bool result = filter.ShouldSave(metadataXml);

            // Assert
            Assert.IsFalse(result, "當 RequireAllFilters 為 true 且有一個標籤不符合時，應回傳 false。");
        }
    }
}