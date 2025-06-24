using System;
using System.Configuration;
using System.Linq;

namespace BoschMetadataAlertCapture
{
    /// <summary>
    /// 用來檢查 XML 內容是否符合指定過濾標籤
    /// </summary>
    public class MetadataFilter
    {
        public string[] XmlFilters { get; set; } = Array.Empty<string>();
        public bool RequireAllFilters { get; set; } = false;

        private static string[] GlobalFilters => GetGlobalFilters();

        public bool ShouldSave(string metadataXml)
        {
            var allFilters = GlobalFilters.Concat(XmlFilters).Distinct().ToArray();

            if (allFilters.Length == 0)
                return metadataXml.Contains("<tt:VideoAnalytics>", StringComparison.OrdinalIgnoreCase);

            var matchCount = allFilters.Count(tag => metadataXml.Contains(tag, StringComparison.OrdinalIgnoreCase));

            return RequireAllFilters
                ? (matchCount == allFilters.Length)
                : (matchCount > 0);
        }

        private static string[] GetGlobalFilters()
        {
            var configValue = ConfigurationManager.AppSettings["MetadataXmlFilter"];
            if (string.IsNullOrWhiteSpace(configValue)) return Array.Empty<string>();
            return configValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }
    }
}
