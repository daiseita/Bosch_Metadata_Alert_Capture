using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace BoschMetadataAlertCapture
{
    public enum OutputType
    {
        Xml,
        Json
    }

    public class CameraMetadataReceiver
    {
        public string RtspUrl { get; private set; }
        public string CameraName { get; private set; }

        public MetadataFilter Filter { get; set; }
        private EventTimeoutManager TimeoutManager { get; set; }

        public OutputType OutputType { get; set; }
        public bool SaveSnapshotOnEvent { get; set; } = false;

        private TrackManager TrackManager { get; set; }

        public CameraMetadataReceiver(string cameraName, string rtspUrl, OutputType outputType = OutputType.Xml)
        {
            CameraName = cameraName;
            RtspUrl = rtspUrl;
            Filter = new MetadataFilter();
            TimeoutManager = new EventTimeoutManager();
            OutputType = outputType;
            TrackManager = new TrackManager();
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

            using (var process = Process.Start(psi))
            {
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
                            textBuffer = textBuffer[(endIndex + "</tt:MetadataStream>".Length)..];

                            if (Filter.ShouldSave(metadataXml))
                            {
                                if (TimeoutManager.IsTimeoutEnabled && !TimeoutManager.CheckAndUpdateTimeout(metadataXml))
                                    continue;

                                await SaveMetadataAsync(metadataXml);
                            }
                        }
                    }

                    string errorOutput = await process.StandardError.ReadToEndAsync();
                    process.WaitForExit();
                    Console.WriteLine($"[{CameraName}] 📤 FFmpeg 結束，錯誤輸出：{errorOutput}");
                }
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

            using (var process = Process.Start(psi))
            {
                if (process != null)
                {
                    string result = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit();

                    return result.Contains("data");
                }
            }
            return false;
        }

        private async Task SaveMetadataAsync(string metadataXml)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
            string baseFilename = $"event/{CameraName}_{timestamp}";
            string metadataFilename;

            var xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(metadataXml);
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"[{CameraName}] ❌ Metadata 解析錯誤：{ex.Message}");
                return;
            }

            // 更新軌跡
            TrackManager.UpdateObjectTracks(xmlDoc);                       

            if (OutputType == OutputType.Xml)
            {
                metadataFilename = $"{baseFilename}.xml";
                xmlDoc.Save(metadataFilename);
            }
            else
            {
                string jsonText = JsonSerializer.Serialize(XmlToDictionary(xmlDoc.DocumentElement), new JsonSerializerOptions { WriteIndented = true });
                metadataFilename = $"{baseFilename}.json";
                await File.WriteAllTextAsync(metadataFilename, jsonText);
            }

            Console.WriteLine($"[{CameraName}] 🚨 偵測事件，已儲存 {metadataFilename}");

            if (SaveSnapshotOnEvent)
            {               
                string snapshotFilename = $"{baseFilename}.jpg";
                CaptureSnapshot(snapshotFilename);
            }
        }

        private void CaptureSnapshot(string filePath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = @"C:\ffmpeg\bin\ffmpeg.exe",
                    Arguments = $"-rtsp_transport tcp -i \"{RtspUrl}\" -frames:v 1 -q:v 2 \"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    process?.WaitForExit();
                }

                if (File.Exists(filePath))
                    Console.WriteLine($"[{CameraName}] 📸 快照儲存完成：{filePath}");
                else
                    Console.WriteLine($"[{CameraName}] ❌ 快照儲存失敗！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{CameraName}] ❌ 快照錯誤：{ex.Message}");
            }
        }

        private Dictionary<string, object> XmlToDictionary(XmlElement element)
        {
            var dict = new Dictionary<string, object>();

            foreach (XmlAttribute attr in element.Attributes)
                dict[$"@{attr.Name}"] = attr.Value;

            foreach (XmlNode child in element.ChildNodes)
            {
                if (child is XmlElement childElement)
                {
                    var childDict = XmlToDictionary(childElement);

                    if (dict.ContainsKey(childElement.Name))
                    {
                        var existing = dict[childElement.Name];
                        if (existing is List<object> list)
                            list.Add(childDict);
                        else
                            dict[childElement.Name] = new List<object> { existing, childDict };
                    }
                    else
                    {
                        dict[childElement.Name] = childDict;
                    }
                }
                else if (child is XmlText textNode)
                {
                    dict["#text"] = textNode.Value;
                }
            }

            return dict;
        }
    }
}
