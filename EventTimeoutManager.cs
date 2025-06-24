using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Text.RegularExpressions;

namespace BoschMetadataAlertCapture
{
    /// <summary>
    /// 管理 ObjectId 的事件 timeout 間隔，避免短時間重複儲存
    /// </summary>
    public class EventTimeoutManager
    {
        private ConcurrentDictionary<string, DateTime> ObjectEventTimes = new ConcurrentDictionary<string, DateTime>();

        public bool IsTimeoutEnabled => GetEventTimeoutEnabled();
        public int TimeoutInterval => GetEventTimeoutInterval();

        public bool CheckAndUpdateTimeout(string metadataXml)
        {
            var match = Regex.Match(metadataXml, "<tt:Object[^>]*ObjectId=\"(\\d+)\"", RegexOptions.IgnoreCase);
            if (!match.Success)
                return true; // 沒 ObjectId 就直接處理

            string objectId = match.Groups[1].Value;
            DateTime now = DateTime.Now;

            if (ObjectEventTimes.TryGetValue(objectId, out DateTime lastEventTime))
            {
                if ((now - lastEventTime).TotalMilliseconds < TimeoutInterval)
                    return false; // 間隔內，略過
            }

            ObjectEventTimes[objectId] = now;
            return true;
        }

        private static bool GetEventTimeoutEnabled()
        {
            var configValue = ConfigurationManager.AppSettings["StartEventTimeout"];
            return bool.TryParse(configValue, out bool result) && result;
        }

        private static int GetEventTimeoutInterval()
        {
            var configValue = ConfigurationManager.AppSettings["EventTimeoutInterval"];
            return int.TryParse(configValue, out int result) ? result : 2000;
        }
    }
}
