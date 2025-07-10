using BoschMetadataAlertCapture;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace BoschMetadataAlertCapture
{
    public class CameraMetadataManager
    {
        private List<Task> runningTasks = new List<Task>();
        /// <summary>
        /// 建立監看攝影機
        /// </summary>
        /// <param name="cameraName" value="12"></param>
        /// <param name="rtspUrl"></param>
        /// <param name="xmlFilters">過濾xml標籤</param>
        /// <param name="requireAllFilters">是否需要完全符合/僅符合一項</param>
        /// <remarks>
        /// xmlFilters : 過濾xml標籤
        /// requireAllFilters : 是否需要完全符合/僅符合一項        
        /// </remarks>               
        public void AddAndRun(string cameraName, string rtspUrl, string[] xmlFilters =null, bool requireAllFilters  =false ,bool saveSnapshotOnEvent = false)
        {            
            var receiver = new CameraMetadataReceiver(cameraName, rtspUrl) { 
            
                Filter = new MetadataFilter() { XmlFilters = xmlFilters ?? Array.Empty<string>(), RequireAllFilters = requireAllFilters },
                SaveSnapshotOnEvent = saveSnapshotOnEvent                   
            };
            var task = receiver.StartReceivingAsync();
            runningTasks.Add(task);
        }

        public async Task WaitForAllAsync()
        {
            await Task.WhenAll(runningTasks);
        }
    }
}
