using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AovClass
{
    internal class Extensions
    {
    }

    public static class ArrayExtension
    {
        public static bool SameElementWith<T>(this T[] arr1, T[] arr2)
        {
            return arr1.ToList().FindIndex(t1 => arr2.Contains(t1)) != -1;
        }

        public static int CountMatches<T>(this T[] tArr, T[] subArr)
        {
            int count = 0;
            int start = -1;
            while ((start = IndexOf(tArr, subArr, start + 1)) >= 0)
            {
                count++;
            }
            return count;
        }

        public static int IndexOf<T>(this T[] outerArray, T[] smallerArray)
        {
            return IndexOf(outerArray, smallerArray, 0);
        }

        public static int IndexOf<T>(this T[] outerArray, T[] smallerArray, int start)
        {
            for (int i = start; i < outerArray.Length - smallerArray.Length + 1; ++i)
            {
                bool found = true;
                for (int j = 0; j < smallerArray.Length; ++j)
                {
                    if (!outerArray[i + j].Equals(smallerArray[j]))
                    {
                        if (outerArray[i + j] is byte b1 && smallerArray[j] is byte b2)
                        {
                            if (char.ToLower((char) b1) != char.ToLower((char) b2))
                            {
                                found = false;
                                break;
                            }
                        }else{
                        //    && (typeof(T) == typeof(byte?) || typeof(T) == typeof(byte?)
                        //        && outerArray[i + j] != null && smallerArray[j] != null
                        //        && (char.ToLower((char) (outerArray[i + j] as byte?)) != char.ToLower((char)(smallerArray[j] as byte?)))
                        //    )
                            found = false;
                            break;
                        }
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return -1;
        }

        public static int LastIndexOf<T>(this T[] outerArray, T[] smallerArray, int end)
        {
            for (int i = end; i >= 0; --i)
            {
                bool found = true;
                for (int j = 0; j < smallerArray.Length; ++j)
                {
                    if (!outerArray[i + j].Equals(smallerArray[j]))
                    {
                        if (outerArray[i + j] is byte b1 && smallerArray[j] is byte b2)
                        {
                            if (char.ToLower((char)b1) != char.ToLower((char)b2))
                            {
                                found = false;
                                break;
                            }
                        }
                        else
                        {
                            //    && (typeof(T) == typeof(byte?) || typeof(T) == typeof(byte?)
                            //        && outerArray[i + j] != null && smallerArray[j] != null
                            //        && (char.ToLower((char) (outerArray[i + j] as byte?)) != char.ToLower((char)(smallerArray[j] as byte?)))
                            //    )
                            found = false;
                            break;
                        }
                    }
                }
                if (found)
                    return i;
            }
            return -1;
        }

        public static T[] ReplaceSubArray<T>(this T[] oriArray, int startIndex, int endIndex, T[] newSubArray)
        {
            return MergeArray(oriArray[0..startIndex], newSubArray, oriArray[endIndex..oriArray.Length]);
        }


        public static T[] ReplaceLast<T>(this T[] bytes, T[] targetBytes, T[] replaceBytes)
        {
            int start;
            if ((start = bytes.LastIndexOf(targetBytes, bytes.Length - 1)) >= 0)
            {
                bytes = bytes.ReplaceSubArray(start, start + targetBytes.Length, replaceBytes);
            }
            return bytes;
        }

        public static T[] ReplaceFirst<T>(this T[] bytes, T[] targetBytes, T[] replaceBytes)
        {
            int start;
            if ((start = bytes.IndexOf(targetBytes)) >= 0)
            {
                bytes = bytes.ReplaceSubArray(start, start + targetBytes.Length, replaceBytes);
            }
            return bytes;
        }

        public static T[] ReplaceAll<T>(this T[] bytes, T[] targetBytes, T[] replaceBytes)
        {
            if (targetBytes == replaceBytes)
                return bytes;
            int start = 0;
            while ((start = bytes.IndexOf(targetBytes, start)) >= 0)
            {
                bytes = bytes.ReplaceSubArray(start, start + targetBytes.Length, replaceBytes);
            }
            return bytes;
        }

        public static T[] MergeArray<T>(params T[][] tArr)
        {
            List<T> list = [];
            foreach (T[] t in tArr)
            {
                list.AddRange(t);
            }
            return [.. list];
        }
    }

    public static class CustomNodeExtension
    {
        public static void InsertChild(this XmlNode node, int pos, XmlNode newChild)
        {
            if (pos >= node.ChildNodes.Count)
            {
                return;
            }
            newChild = node.OwnerDocument.ImportNode(newChild, true);
            node.InsertBefore(newChild, node.ChildNodes[pos]);
        }

        public static XmlNode CreateCheckHeroIdTick(int heroId, string guid)
        {
            string xml = 
                $"    <Track trackName=\"CheckHeroIdTick{heroId}\" eventType=\"CheckHeroIdTick\" guid=\"{guid}\" enabled=\"true\" useRefParam=\"false\" refParamName=\"\" r=\"0.000\" g=\"0.000\" b=\"0.000\" execOnForceStopped=\"false\" execOnActionCompleted=\"false\" stopAfterLastEvent=\"true\">\r\n" +
                $"      <Event eventName=\"CheckHeroIdTick\" time=\"0.000\" isDuration=\"false\" guid=\"{guid}\">\r\n" +
                $"        <TemplateObject name=\"targetId\" id=\"1\" objectName=\"target\" isTemp=\"false\" refParamName=\"\" useRefParam=\"false\" />\r\n" +
                $"        <int name=\"heroId\" value=\"{heroId}\" refParamName=\"\" useRefParam=\"false\" />\r\n" +
                $"      </Event>\r\n    " +
                $"    </Track>";
            XmlDocument doc = new();
            doc.LoadXml(xml);
#pragma warning disable CS8603 // Possible null reference return.
            return doc.DocumentElement;
#pragma warning restore CS8603 // Possible null reference return.
        }

        /// <summary>
        /// Create CheckSkinIdTick Track as new XmlNode, Event has targetId and skinId
        /// </summary>
        /// <param name="skinId">SkinId need check</param>
        /// <param name="guid">Guid of track to Sync with condition</param>
        /// <param name="objectId">'id' of targetId TemplateObject</param>
        /// <param name="objectName">'objectName' of targetId TemplateObject</param>
        /// <returns>CheckSkinIdTick Node</returns>
        public static XmlNode CreateCheckSkinIdTick(int skinId, string guid, int objectId, string objectName)
        {
            string xml = 
                $"    <Track trackName=\"CheckSkinIdTick{skinId}\" eventType=\"CheckSkinIdTick\" guid=\"{guid}\" enabled=\"true\" useRefParam=\"false\" refParamName=\"\" r=\"0.000\" g=\"0.000\" b=\"0.000\" execOnForceStopped=\"false\" execOnActionCompleted=\"false\" stopAfterLastEvent=\"true\">\r\n" +
                $"      <Event eventName=\"CheckSkinIdTick\" time=\"0.000\" isDuration=\"false\" guid=\"{guid}\">\r\n" +
                $"        <TemplateObject name=\"targetId\" id=\"{objectId}\" objectName=\"{objectName}\" isTemp=\"false\" refParamName=\"\" useRefParam=\"false\" />\r\n" +
                $"        <int name=\"skinId\" value=\"{skinId}\" refParamName=\"\" useRefParam=\"false\" />\r\n" +
                $"      </Event>\r\n    " +
                $"    </Track>";
            XmlDocument doc = new();
            doc.LoadXml(xml);
#pragma warning disable CS8603 // Possible null reference return.
            return doc.DocumentElement;
#pragma warning restore CS8603 // Possible null reference return.
        }

        public static XmlNode CreateEventCondition(int id, string guid, bool status)
        {
            string _status = status ? "true" : "false";
            string xml = $"<Condition id=\"{id}\" guid=\"{guid}\" status=\"{_status}\" />";
            XmlDocument doc = new();
            doc.LoadXml(xml);
#pragma warning disable CS8603 // Possible null reference return.
            return doc.DocumentElement;
#pragma warning restore CS8603 // Possible null reference return.
        }

        public static string? GetAttribute(this XmlNode node, string attrName)
        {
            return node.Attributes?[attrName]?.InnerText;
        }

        public static void SetAttribute(this XmlNode node, string attrName, string newValue)
        {
            node.Attributes[attrName].InnerText = newValue;
        }

        public static List<XmlNode>? GetChildrenByName(this XmlNode node, string childName)
        {
            List<XmlNode>? result = [];
            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                XmlNode? nodeChild = node.ChildNodes.Item(i);
                if (nodeChild.Name == childName)
                {
                    result.Add(nodeChild);
                }
            }
            return result;
        }

        public static XmlNode? GetChildByAttribute(this XmlNode node, string attrName, string attrValue)
        {
            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                XmlNode? nodeChild = node.ChildNodes.Item(i);
                if (nodeChild.Attributes[attrName] != null && nodeChild.Attributes[attrName].InnerText == attrValue)
                {
                    return nodeChild;
                }
            }
            return null;
        }

        public static List<XmlNode> GetChildrenByAttribute(this XmlNode node, string attrName, string attrValue)
        {
            List<XmlNode> result = [];
            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                XmlNode? nodeChild = node.ChildNodes.Item(i);
                if (nodeChild.Attributes[attrName] != null && nodeChild.Attributes[attrName].InnerText == attrValue)
                {
                    result.Add(nodeChild);
                }
            }
            return result;
        }
    }
}
