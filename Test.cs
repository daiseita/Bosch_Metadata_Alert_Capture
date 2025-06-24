using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BoschMetadataAlertCapture
{
    internal class Test
    {

        public static async Task Test1Async()
        {
            string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
            string rtspUrl = "rtsp://service:!QAZ2wsx@192.168.168.56/rtsp_tunnel?p=0&h26x=4&vcd=2";

            Directory.CreateDirectory("event");

            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-rtsp_transport tcp -i \"{rtspUrl}\" -map 0:1 -c copy -f data -",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            Console.WriteLine("啟動 FFmpeg 擷取 metadata stream 中...");
            var process = Process.Start(psi);

            if (process != null)
            {
                StreamReader reader = process.StandardOutput;
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

                        if (metadataXml.Contains("<tt:VideoAnalytics>", StringComparison.OrdinalIgnoreCase))
                        {
                            string filename = $"event/event_{DateTime.Now:yyyyMMdd_HHmmssfff}.xml";
                            File.WriteAllText(filename, metadataXml);
                            Console.WriteLine($"> 偵測到告警事件，儲存 {filename}");
                        }
                    }
                }

                string errorOutput = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                Console.WriteLine("FFmpeg 結束，錯誤輸出：");
                Console.WriteLine(errorOutput);
            }
            else
            {
                Console.WriteLine("無法啟動 FFmpeg。");
            }
        }
    }
}
