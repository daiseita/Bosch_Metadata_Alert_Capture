
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BoschMetadataAlertCapture
{
    class Program
    {
        static async Task Main(string[] args)
        {           

            var manager = new CameraMetadataManager();

            // ➜ 這裡加入多隻攝影機
            manager.AddAndRun("Cam56", "rtsp://service:!QAZ2wsx@192.168.168.56/rtsp_tunnel?p=0&h26x=4&vcd=2", new[] { "" }, false , false);
            manager.AddAndRun("Cam52", "rtsp://service:!QAZ2wsx@192.168.168.52/rtsp_tunnel?p=0&h26x=4&vcd=2" , new [] { "" }, false, false);


            await manager.WaitForAllAsync();
        }
    }
}
