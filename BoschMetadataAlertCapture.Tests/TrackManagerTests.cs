using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;
using System.Xml;
using BoschMetadataAlertCapture;

namespace BoschMetadataAlertCapture.Tests
{
    [TestClass]
    public class TrackManagerTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            // 模擬 app.config 參數
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings.Remove("EnableTrackLogging");
            config.AppSettings.Settings.Remove("TrackRetentionSeconds");
            config.AppSettings.Settings.Add("EnableTrackLogging", "true");
            config.AppSettings.Settings.Add("TrackRetentionSeconds", "1"); // 短秒數方便測試
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        [TestMethod]
        public void UpdateObjectTracks_ShouldAddTrackPoints()
        {
            // Arrange
            var manager = new TrackManager();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <tt:MetadataStream xmlns:tt='http://www.onvif.org/ver10/schema'>
                  <tt:VideoAnalytics>
                    <tt:Frame>
                      <tt:Object ObjectId='obj1'>
                        <tt:Appearance>
                          <tt:Shape>
                            <tt:CenterOfGravity x='0.5' y='0.5' />
                          </tt:Shape>
                        </tt:Appearance>
                      </tt:Object>
                    </tt:Frame>
                  </tt:VideoAnalytics>
                </tt:MetadataStream>");

            // Act
            manager.UpdateObjectTracks(xmlDoc);

            // Assert
            var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("tt", "http://www.onvif.org/ver10/schema");
            var trackNode = xmlDoc.SelectSingleNode("//tt:Object[@ObjectId='obj1']/tt:Track", nsmgr);
            Assert.IsNotNull(trackNode, "應該要有 Track 節點");
            Assert.AreEqual(1, trackNode.ChildNodes.Count, "Track 節點內應該有 1 個 Point");
        }

        [TestMethod]
        public void RemoveExpiredTracks_ShouldClearOldTracks()
        {
            // Arrange
            var manager = new TrackManager();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <tt:MetadataStream xmlns:tt='http://www.onvif.org/ver10/schema'>
                  <tt:VideoAnalytics>
                    <tt:Frame>
                      <tt:Object ObjectId='obj2'>
                        <tt:Appearance>
                          <tt:Shape>
                            <tt:CenterOfGravity x='0.3' y='0.4' />
                          </tt:Shape>
                        </tt:Appearance>
                      </tt:Object>
                    </tt:Frame>
                  </tt:VideoAnalytics>
                </tt:MetadataStream>");

            // Act
            manager.UpdateObjectTracks(xmlDoc);

            // 讓資料過期
            System.Threading.Thread.Sleep(1500);

            // 再觸發一次更新（應該把過期資料清掉）
            manager.UpdateObjectTracks(xmlDoc);

            // Assert
            var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("tt", "http://www.onvif.org/ver10/schema");
            var trackNode = xmlDoc.SelectSingleNode("//tt:Object[@ObjectId='obj2']/tt:Track", nsmgr);
            Assert.IsNotNull(trackNode, "應該還是有 Track 節點");

            // 確認裡面只有 1 個 Point (剛剛過期的應該被清掉)
            Assert.AreEqual(1, trackNode.ChildNodes.Count, "過期 Point 應該被移除，只剩 1 筆新資料");
        }

        [TestMethod]
        public void TrackManager_Disabled_ShouldDoNothing()
        {
            // Arrange
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["EnableTrackLogging"].Value = "false";
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            var manager = new TrackManager();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(@"
                <tt:MetadataStream xmlns:tt='http://www.onvif.org/ver10/schema'>
                  <tt:VideoAnalytics>
                    <tt:Frame>
                      <tt:Object ObjectId='obj3'>
                        <tt:Appearance>
                          <tt:Shape>
                            <tt:CenterOfGravity x='0.3' y='0.4' />
                          </tt:Shape>
                        </tt:Appearance>
                      </tt:Object>
                    </tt:Frame>
                  </tt:VideoAnalytics>
                </tt:MetadataStream>");

            // Act
            manager.UpdateObjectTracks(xmlDoc);

            // Assert
            var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("tt", "http://www.onvif.org/ver10/schema");
            var trackNode = xmlDoc.SelectSingleNode("//tt:Object[@ObjectId='obj3']/tt:Track", nsmgr);
            Assert.IsNull(trackNode, "關閉狀態下不應該產生 Track 節點");
        }
    }
}
