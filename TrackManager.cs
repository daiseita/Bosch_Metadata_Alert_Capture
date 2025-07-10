using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml;

namespace BoschMetadataAlertCapture
{
    public class TrackManager
    {
        private class TrackPoint
        {
            public double X { get; set; }
            public double Y { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private readonly ConcurrentDictionary<string, List<TrackPoint>> _objectTrajectories = new();
        private readonly bool _enableTrackLogging;
        private readonly int _retentionSeconds;

        public TrackManager()
        {
            _enableTrackLogging = bool.TryParse(ConfigurationManager.AppSettings["EnableTrackLogging"], out var enabled) && enabled;
            _retentionSeconds = int.TryParse(ConfigurationManager.AppSettings["TrackRetentionSeconds"], out var seconds) ? seconds : 60;
        }

        public bool IsEnabled => _enableTrackLogging;

        public void UpdateObjectTracks(XmlDocument xmlDoc)
        {
            if (!_enableTrackLogging)
                return;

            var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("tt", "http://www.onvif.org/ver10/schema");

            var objectNodes = xmlDoc.SelectNodes("//tt:Object", nsmgr);
            if (objectNodes == null) return;

            foreach (XmlElement objNode in objectNodes)
            {
                string objectId = objNode.GetAttribute("ObjectId");
                if (string.IsNullOrEmpty(objectId)) continue;

                var cogNode = objNode.SelectSingleNode("tt:Appearance/tt:Shape/tt:CenterOfGravity", nsmgr) as XmlElement;
                if (cogNode == null) continue;

                if (double.TryParse(cogNode.GetAttribute("x"), out double x) &&
                    double.TryParse(cogNode.GetAttribute("y"), out double y))
                {
                    var trackPoint = new TrackPoint { X = x, Y = y, Timestamp = DateTime.Now };

                    _objectTrajectories.AddOrUpdate(objectId,
                        _ => new List<TrackPoint> { trackPoint },
                        (_, list) =>
                        {
                            list.Add(trackPoint);
                            return list;
                        });

                    RemoveExpiredTracks(objectId);

                    AppendTrackListToObject(objNode, _objectTrajectories[objectId], xmlDoc);
                }
            }
        }

        private void RemoveExpiredTracks(string objectId)
        {
            if (_objectTrajectories.TryGetValue(objectId, out var trackList))
            {
                var cutoff = DateTime.Now.AddSeconds(-_retentionSeconds);
                trackList.RemoveAll(p => p.Timestamp < cutoff);
            }
            if (_objectTrajectories[objectId].Count == 0) { _objectTrajectories.TryRemove(objectId, out _); }
        }

        private void AppendTrackListToObject(XmlElement objNode, List<TrackPoint> trackPoints, XmlDocument xmlDoc)
        {
            var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("tt", "http://www.onvif.org/ver10/schema");

            var oldTrack = objNode.SelectSingleNode("tt:Track", nsmgr);
            if (oldTrack != null)
                objNode.RemoveChild(oldTrack);

            var trackElement = xmlDoc.CreateElement("tt", "Track", "http://www.onvif.org/ver10/schema");            
            foreach (var point in trackPoints)
            {
                var pointElement = xmlDoc.CreateElement("tt", "Point", "http://www.onvif.org/ver10/schema");
                pointElement.SetAttribute("X", point.X.ToString("F4"));
                pointElement.SetAttribute("Y", point.Y.ToString("F4"));
                pointElement.SetAttribute("Timestamp", point.Timestamp.ToString("o"));
                trackElement.AppendChild(pointElement);
            }

            objNode.AppendChild(trackElement);
        }
    }
}
