using BoschMetadataAlertCapture;
using System.Threading;

namespace BoschMetadataAlertCapture.Tests
{
    [TestClass]
    public class EventTimeoutManagerTests
    {
        [TestMethod]
        public async Task CheckAndUpdateTimeout_WhenCalledWithinInterval_ShouldReturnFalse()
        {
            // Arrange
            var manager = new EventTimeoutManager(); // 預設間隔為 2000 毫秒
            var metadataXml = "<tt:MetadataStream><tt:Object ObjectId=\"123\"></tt:Object></tt:MetadataStream>";

            // Act
            bool firstCall = manager.CheckAndUpdateTimeout(metadataXml);
            bool secondCall = manager.CheckAndUpdateTimeout(metadataXml); // 立即進行第二次呼叫

            // Assert
            Assert.IsTrue(firstCall, "第一次呼叫應永遠通過。");
            Assert.IsFalse(secondCall, "在超時間隔內，第二次呼叫應被阻擋。");
        }

        [TestMethod]
        public async Task CheckAndUpdateTimeout_WhenCalledAfterInterval_ShouldReturnTrue()
        {
            // Arrange
            var manager = new EventTimeoutManager(); // 預設間隔為 2000 毫秒
            var metadataXml = "<tt:MetadataStream><tt:Object ObjectId=\"456\"></tt:Object></tt:MetadataStream>";
            var timeoutInterval = manager.TimeoutInterval; // 取得設定的超時時間

            // Act
            bool firstCall = manager.CheckAndUpdateTimeout(metadataXml);
            await Task.Delay(timeoutInterval + 100); // 等待超過超時間隔
            bool secondCall = manager.CheckAndUpdateTimeout(metadataXml);

            // Assert
            Assert.IsTrue(firstCall, "第一次呼叫應永遠通過。");
            Assert.IsTrue(secondCall, "在超時間隔後，第二次呼叫應再次通過。");
        }

        [TestMethod]
        public void CheckAndUpdateTimeout_WithDifferentObjectIds_ShouldBeHandledSeparately()
        {
            // Arrange
            var manager = new EventTimeoutManager();
            var metadataXml1 = "<tt:MetadataStream><tt:Object ObjectId=\"789\"></tt:Object></tt:MetadataStream>";
            var metadataXml2 = "<tt:MetadataStream><tt:Object ObjectId=\"999\"></tt:Object></tt:MetadataStream>";

            // Act
            bool firstCallObj1 = manager.CheckAndUpdateTimeout(metadataXml1);
            bool firstCallObj2 = manager.CheckAndUpdateTimeout(metadataXml2);

            // Assert
            Assert.IsTrue(firstCallObj1, "不同 ObjectId 的第一次呼叫應通過。");
            Assert.IsTrue(firstCallObj2, "不同 ObjectId 的第一次呼叫應通過，不受其他 ObjectId 影響。");
        }
    }
}