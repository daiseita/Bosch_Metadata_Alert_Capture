using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BoschMetadataAlertCapture
{
    /// <summary>
    /// RTSP metadata 接收器，使用 FFmpeg 連線並儲存符合條件的事件
    /// </summary>
    public class CameraMetadataReceiver
    {
        public string RtspUrl { get; private set; }
        public string CameraName { get; private set; }

        public MetadataFilter Filter { get; set; }
        private EventTimeoutManager TimeoutManager { get; set; }

        public CameraMetadataReceiver(string cameraName, string rtspUrl)
        {
            CameraName = cameraName;
            RtspUrl = rtspUrl;
            Filter = new MetadataFilter();
            TimeoutManager = new EventTimeoutManager();
        }

        public async Task StartReceivingAsync()
        {
            if (!await CheckMetadataTrackAsync())
            {
                Console.WriteLine($"[{CameraName}] ❌ 未發現 metadata data track，跳過。");
                return;
            }

            Directory.CreateDirectory("event");

            var psi = new ProcessStartInfo
            {
                FileName = @"C:\ffmpeg\bin\ffmpeg.exe",
                Arguments = $"-rtsp_transport tcp -i \"{RtspUrl}\" -map 0:1 -c copy -f data -",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            Console.WriteLine($"[{CameraName}] 📡 開始接收 metadata 事件...");

            var process = Process.Start(psi);
            if (process != null)
            {
                var reader = process.StandardOutput;
                var charBuffer = new char[4096];
                int bytesRead;
                string textBuffer = "";

                while ((bytesRead = await reader.ReadAsync(charBuffer, 0, charBuffer.Length)) > 0)
                {
                    textBuffer += new string(charBuffer, 0, bytesRead);

                    int endIndex;
                    while ((endIndex = textBuffer.IndexOf("</tt:MetadataStream>", StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        var metadataXml = textBuffer.Substring(0, endIndex + "</tt:MetadataStream>".Length);
                        textBuffer = textBuffer.Substring(endIndex + "</tt:MetadataStream>".Length);

                        if (Filter.ShouldSave(metadataXml))
                        {
                            if (TimeoutManager.IsTimeoutEnabled && !TimeoutManager.CheckAndUpdateTimeout(metadataXml))
                                continue;

                            string filename = $"event/{CameraName}_{DateTime.Now:yyyyMMdd_HHmmssfff}.xml";
                            File.WriteAllText(filename, metadataXml);
                            Console.WriteLine($"[{CameraName}] 🚨 偵測事件，已儲存 {filename}");
                        }
                    }
                }

                string errorOutput = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                Console.WriteLine($"[{CameraName}] 📤 FFmpeg 結束，錯誤輸出：{errorOutput}");
            }
        }

        public async Task<bool> CheckMetadataTrackAsync()
        {
            var psi = new ProcessStartInfo
            {
                FileName = @"C:\ffmpeg\bin\ffprobe.exe",
                Arguments = $"-v error -show_streams -of json \"{RtspUrl}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            if (process != null)
            {
                string result = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                return result.Contains("data");
            }
            return false;
        }
    }
}
