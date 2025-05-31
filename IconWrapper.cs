using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AovClass
{
    public class IconWrapper
    {
        private byte[] bytes;
        public List<IconElement> iconElements;
        public Dictionary<int, int> iconIndexDict;

        public IconWrapper(byte[] bytes)
        {
            this.bytes = (byte[])bytes.Clone();
            iconElements = new List<IconElement>();
            iconIndexDict = new Dictionary<int, int>();
            int start;
            if (bytes[0] == 'M' && bytes[1] == 'S' && bytes[2] == 'E' && bytes[3] == 'S')
                start = BitConverter.ToInt32(bytes, 132);
            else
                start = 0;
            if (start == bytes.Length)
                return;
            int count;
            while (start < bytes.Length)
            {
                count = BitConverter.ToInt32(bytes, start) + 4;
                IconElement ic = new IconElement(bytes[start..(start + count)]);
                iconIndexDict[ic.iconId] = iconElements.Count;
                iconElements.Add(ic);
                start += count;
            }
        }

        public void SetIcon(int sourceId, byte[] iconBytes)
        {
            if (!iconIndexDict.ContainsKey(sourceId))
            {
                throw new Exception("not found id " + sourceId);
            }
            iconElements[iconIndexDict[sourceId]] = new IconElement(iconBytes);
            iconElements[iconIndexDict[sourceId]].SetIconIndex(sourceId % 100);
            iconElements[iconIndexDict[sourceId]].SetHeroId(sourceId / 100);
            iconElements[iconIndexDict[sourceId]].SetIconId(sourceId);
            iconElements[iconIndexDict[sourceId]].SetIconCode("30" + (sourceId / 100) + (sourceId % 100));
        }

        public void CopyIcon(int sourceId, int targetId)
        {
            CopyIcon(sourceId, targetId, true);
        }

        public void CopyIcon(int sourceId, int targetId, bool swap)
        {
            if (!iconIndexDict.ContainsKey(sourceId) || !iconIndexDict.ContainsKey(targetId))
            {
                throw new Exception("not found id " + sourceId + " or " + targetId);
            }
            if (sourceId == targetId)
                return;
            string oldHeroCode = iconElements[iconIndexDict[sourceId]].heronamecode;
            byte[] bytes = iconElements[iconIndexDict[targetId]].GetBytes();
            iconElements[iconIndexDict[sourceId]] = new IconElement(bytes);
            iconElements[iconIndexDict[sourceId]].SetIconIndex(sourceId % 100);
            iconElements[iconIndexDict[sourceId]].SetHeroId(sourceId / 100);
            if (sourceId % 100 == 0)
            {
                if (swap)
                {
                    iconElements[iconIndexDict[targetId]].SetIconId(sourceId);
                    iconElements[iconIndexDict[targetId]].SetIconIndex(targetId % 100);
                    iconElements[iconIndexDict[targetId]].SetIconCode("30" + (sourceId / 100) + (sourceId % 100));
                    if (sourceId / 100 != targetId / 100)
                    {
                        iconElements[iconIndexDict[targetId]].SetHeroNameCode(oldHeroCode);
                    }
                }
                else
                {
                    iconElements[iconIndexDict[sourceId]].SetIconId(sourceId);
                    iconElements[iconIndexDict[sourceId]].SetIconCode("30" + (sourceId / 100) + (sourceId % 100));
                    if (sourceId / 100 != targetId / 100)
                    {
                        iconElements[iconIndexDict[sourceId]].SetHeroNameCode(oldHeroCode);
                    }
                }
            }
            else
            {
                iconElements[iconIndexDict[sourceId]].SetIconId(sourceId);
                iconElements[iconIndexDict[sourceId]].SetIconCode("30" + (sourceId / 100) + (sourceId % 100));
            }
        }

        public IconElement? GetIcon(int iconId)
        {
            try
            {
                return iconElements[iconIndexDict[iconId]];
            }
            catch
            {
                return null;
            }
        }

        public byte[] GetBytes()
        {
            //byte[] childBytes = [];
            //foreach (IconElement e in iconElements)
            //{
            //    childBytes = ArrayExtension.MergeArray(childBytes, e.GetBytes());
            //    if (e.iconId == 10700)
            //        File.WriteAllBytes("E:/testresult.bytes", e.GetBytes());
            //}
            int start;
            if (bytes[0] == 'M' && bytes[1] == 'S' && bytes[2] == 'E' && bytes[3] == 'S')
            {
                start = BitConverter.ToInt32(bytes, 132);
                bytes = bytes.ReplaceSubArray(12, 16, BitConverter.GetBytes(iconElements.Count));
            }
            else
                start = 0;
            bytes = bytes[..start];
            foreach (IconElement e in iconElements)
            {
                bytes = ArrayExtension.MergeArray(bytes, e.GetBytes());
            }
            return bytes;
        }
    }

    public class IconElement
    {
        private byte[] bytes;
        public int iconId;
        public int iconIndex;
        public int heroId;
        public string heronamecode;
        public string skinnamecode;
        public string iconCode;

        public IconElement(byte[] bytes)
        {
            this.bytes = (byte[])bytes.Clone();

            iconId = BitConverter.ToInt32(bytes, 4);
            iconIndex = BitConverter.ToInt32(bytes, 36);
            heroId = BitConverter.ToInt32(bytes, 8);
            heronamecode = Encoding.UTF8.GetString(bytes[16..35]);
            skinnamecode = Encoding.UTF8.GetString(bytes[44..63]);
            iconCode = Encoding.UTF8.GetString(bytes[68..(67 + BitConverter.ToInt32(bytes, 64))]);
        }

        public void SetHeroNameCode(string code)
        {
            bytes = bytes.ReplaceSubArray(16, 35, Encoding.UTF8.GetBytes(code));
        }

        public void SetIconId(int iconId)
        {
            this.iconId = iconId;
            byte[] barr = BitConverter.GetBytes(iconId);
            for (int i = 0; i < barr.Length; i++)
            {
                bytes[4 + i] = barr[i];
            }
        }

        public void SetHeroId(int heroId)
        {
            this.heroId = heroId;
            byte[] barr = BitConverter.GetBytes(heroId);
            for (int i = 0; i < barr.Length; i++)
            {
                bytes[8 + i] = barr[i];
            }
        }

        public void SetIconCode(string iconCode)
        {
            this.iconCode = iconCode;
            int start = 68, end = 67 + BitConverter.ToInt32(bytes, 64);
            bytes = ArrayExtension.MergeArray(bytes[..start], Encoding.UTF8.GetBytes(iconCode), bytes[end..]);
            byte[] barr = BitConverter.GetBytes(bytes.Length - 4);
            for (int i = 0; i < barr.Length; i++)
            {
                bytes[i] = barr[i];
            }
            barr = BitConverter.GetBytes(iconCode.Length + 1);
            for (int i = 0; i < barr.Length; i++)
            {
                bytes[64 + i] = barr[i];
            }
        }

        public void SetIconIndex(int iconIndex)
        {
            this.iconIndex = iconIndex;
            byte[] barr = BitConverter.GetBytes(iconIndex);
            for (int i = 0; i < barr.Length; i++)
            {
                bytes[36 + i] = barr[i];
            }
        }

        public byte[] GetBytes()
        {
            return bytes;
        }
    }
}
