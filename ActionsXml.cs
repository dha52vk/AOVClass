using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace AovClass
{
    public partial class ActionsXml
    {
        public bool isVirtualXml { get =>virtualNodes != null; }
        public readonly XmlDocument document = new();
        public XmlElement? xmlElement;
        private List<XmlNode>? virtualNodes = null;
        private List<XmlNode>? actionNodes { get => virtualNodes ?? xmlElement.GetChildrenByName("Action")[0].Cast<XmlNode>().ToList(); }

        public void LoadFromText(string content)
        {
            int start = 0;
            while (content[start] != '<')
                start++;
            StringReader reader = new(content[start..]);
            document.Load(reader);
            Reload();
        }

        public void LoadFromFile(string path)
        {
            StreamReader reader = new(path);
            document.Load(reader);
            Reload();
        }

        public void LoadVirtual(List<XmlNode> nodeList)
        {
            virtualNodes = new(nodeList);
        }

        private void Reload()
        {
            xmlElement = document.DocumentElement;
            //actionNodes = xmlElement.GetChildrenByName("Action")[0].Cast<XmlNode>().ToList();
        }

        /// <summary>
        /// Get Action Tracks of Project Xml as List XmlNode
        /// </summary>
        /// <returns> Action Track List</returns>
        public List<XmlNode>? GetActionNodes()
        {
            return actionNodes;
        }

        public List<XmlNode> GetTemplateObjectList()
        {
            XmlNode? templateObjList = xmlElement.GetChildrenByName("TemplateObjectList").ElementAtOrDefault(0);
            return templateObjList == null ? [] : templateObjList.ChildNodes.Cast<XmlNode>().ToList();
        }

        public void AppendActionNode(XmlNode node)
        {
            node = document.ImportNode(node, true);
            XmlNode action = xmlElement.GetChildrenByName("Action")[0];
            action.AppendChild(node);
        }

        public void InsertActionNode(int position, XmlNode node)
        {
            node = document.ImportNode(node, true);
            XmlNode action = xmlElement.GetChildrenByName("Action")[0];
            action.InsertBefore(node, action.ChildNodes[position]);
        }

        /// <summary>
        /// Convert all track CheckSkinIdVirtualTick to CheckSkinIdTick
        /// </summary>
        public void ConvertVirtual2N()
        {
            List<XmlNode> checkNodes = actionNodes.Where((node) => node.Attributes["eventType"].Value == "CheckSkinIdVirtualTick").ToList();
            for (int i = 0; i < checkNodes.Count; i++)
            {
                XmlNode node = checkNodes[i];
                node.Attributes["trackName"].Value = MyRegex().Replace(node.Attributes["trackName"].Value
, "CheckSkinIdTick");
                node.Attributes["eventType"].Value = "CheckSkinIdTick";
                XmlNode trackEvent = node.GetChildrenByName("Event")[0];
                trackEvent.Attributes["eventName"].Value = "CheckSkinIdTick";
                XmlNode? useNegative = trackEvent.GetChildByAttribute("name", "useNegateValue");
                if (useNegative != null)
                {
                    useNegative.Attributes["name"].Value = "bEqual";
                    useNegative.Attributes["value"].Value = useNegative.Attributes["value"].Value == "true" ? "false" : "true";
                }
            }
        }

        public void ResyncConditionIdWithGuid()
        {
            if (isVirtualXml)
                throw new Exception("This actionsXml is virtual");
            XmlNodeList conditionNodes = xmlElement.GetElementsByTagName("Condition");

            for (int i = 0; i < conditionNodes.Count; i++)
            {
                XmlNode? conditionNode = conditionNodes.Item(i);
                string guid = conditionNode.GetAttribute("guid") ?? "";
                int id = actionNodes.FindIndex(node => node.GetAttribute("guid") == guid);
                if (id != -1)
                    conditionNode.SetAttribute("id", id + "");
            }
        }

        public void ResyncStopTracksWithGuid()
        {
            if (isVirtualXml)
                throw new Exception("This actionsXml is virtual");
            List<XmlNode> stopTracks = actionNodes.Where((node) => node.GetAttribute("eventType").Equals("StopTrack")).ToList();

            for (int i = 0; i < stopTracks.Count; i++)
            {
                XmlNode? stopTrack = stopTracks[i];
                XmlNode? trackId = stopTrack.GetChildrenByName("Event")?[0]
                    .GetChildByAttribute("name", "trackId");
                string guid = trackId?.GetAttribute("guid")??"";
                int id = actionNodes.FindIndex(node => node.GetAttribute("guid") == guid);
                if (id != -1)
                    trackId?.SetAttribute("id", id.ToString());
            }
        }

        /// <summary>
        /// Get List Track that has Condition with guid
        /// </summary>
        /// <param name="guid">Condition guid</param>
        /// <returns>List Pair(XmlNode, bool) with key is Track has condition and key is status of condition</XmlNode></returns>
        /// <exception cref="Exception"></exception>
        public List<KeyValuePair<XmlNode, bool>> GetTracksHasConditionGuid(string guid)
        {
            if (isVirtualXml)
                throw new Exception("This actionsXml is virtual");
            XmlNodeList nodeList = document.GetElementsByTagName("Condition");
            List<KeyValuePair<XmlNode, bool>> tracks = [];
            for (int i = 0; i < nodeList.Count; i++)
            {
                XmlNode? condition = nodeList.Item(i);
                string conditionGuid = condition?.GetAttribute("guid") ?? "";
                if (guid == conditionGuid)
                {
                    string status = condition?.GetAttribute("status") ?? "";
                    if (condition.ParentNode != null)
                        tracks.Add(KeyValuePair.Create(condition.ParentNode, status =="true"));
                }
            }
            return tracks;
        }

        public List<KeyValuePair<XmlNode, bool>>? GetConditionTracks(XmlNode node)
        {
            if (isVirtualXml)
                throw new Exception("This actionsXml is virtual");
            ResyncConditionIdWithGuid();
            if (!actionNodes.Contains(node))
                return null;
            List<XmlNode>? conditions = node.GetChildrenByName("Condition");
            List<KeyValuePair<XmlNode, bool>> result = [];
            foreach (XmlNode condition in conditions)
            {
                int id = int.Parse(condition.GetAttribute("id"));
                if (id > -1)
                result.Add(KeyValuePair.Create(actionNodes[id], condition.GetAttribute("status") == "true"));
            }
            return result;
        }

        public string GetOuterXml()
        {
            if (isVirtualXml)
                throw new Exception("This actionsXml is virtual");
            ResyncConditionIdWithGuid();
            ResyncStopTracksWithGuid();
            using var memoryStream = new MemoryStream();
            document.Save(memoryStream);
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }

        public void AddComment(string comment)
        {
            if (isVirtualXml)
                throw new Exception("This actionsXml is virtual");
            XmlComment c = document.CreateComment(comment);
            xmlElement.ParentNode.AppendChild(c);
        }

        [GeneratedRegex("CheckSkinIdVirtualTick", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex MyRegex();
    }

    public static class ActionAttributes
    {
        public readonly static string EventType = "eventType";
        public readonly static string TrackName = "trackName";
        public readonly static string Name = "name";
        public readonly static string Value = "value";
        public readonly static string Status = "status";
        public readonly static string Enabled = "enabled";
        public readonly static string UseRefParam = "useRefParam";
    }
}
