using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AovClass
{
    class SkillMarkWrapper
    {
        private byte[] bytes;
        public List<MarkElement> markElements;
        private List<int> listHeroId;

        public SkillMarkWrapper(byte[] bytes)
        {
            this.bytes = (byte[]) bytes.Clone();
            markElements = new List<MarkElement>();
            listHeroId = new List<int>();
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
                MarkElement m = new MarkElement(bytes[start..(start + count)]);
                if (m.markEffects.Count != 0)
                    listHeroId.Add(m.GetHeroId());
                markElements.Add(m);
                start += count;
            }
        }

        public bool ContainsHeroId(int heroId)
        {
            return listHeroId.Contains(heroId);
        }

        public void ReplaceMarkEffect(int heroId, string regex, string replace)
        {
            for (int i = 0; i < markElements.Count; i++)
            {
                if (markElements[i].GetHeroId() == heroId)
                {
                    markElements[i].ReplaceMarkEffect(regex, replace);
                }
            }
        }

        public byte[] GetBytes()
        {
            byte[] childBytes = new byte[0];
            foreach (MarkElement m in markElements)
            {
                childBytes = ArrayExtension.MergeArray(childBytes, m.GetBytes());
            }
            int start;
            if (bytes[0] == 'M' && bytes[1] == 'S' && bytes[2] == 'E' && bytes[3] == 'S')
            {
                start = BitConverter.ToInt32(bytes, 132);
                bytes = bytes.ReplaceSubArray(12, 16, BitConverter.GetBytes(markElements.Count));
            }
            else
                start = 0;
            return ArrayExtension.MergeArray(bytes[0..start], childBytes);
        }
    }

    class MarkElement
    {
        private byte[] bytes;
        public int markId;
        public string markPath;
        public List<string> markEffects;
        public List<int> markEffectStarts;

        public MarkElement(byte[] bytes)
        {
            this.bytes = (byte[]) bytes.Clone();
            markId = BitConverter.ToInt32(bytes, 4);
            int start, count;
            start = 12;
            count = BitConverter.ToInt32(bytes, start) + 4;
            start = start + count;
            count = BitConverter.ToInt32(bytes, start) + 4;
            start = start + count;
            count = BitConverter.ToInt32(bytes, start) + 4;
            markPath = Encoding.UTF8.GetString(bytes[(start + 4)..(start + count - 1)]);
            start = start + count + 42;
            markEffects = new List<string>();
            markEffectStarts = new List<int>();
            while (start < bytes.Length && BitConverter.ToInt32(bytes, start) > 0
                    && BitConverter.ToInt32(bytes, start) < bytes.Length - start)
            {
                count = BitConverter.ToInt32(bytes, start) + 4;
                string effect = Encoding.UTF8.GetString(bytes[(start + 4)..(start + count - 1)]);
                if (!effect.Equals(""))
                {
                    markEffects.Add(effect);
                    markEffectStarts.Add(start);
                }
                start = start + count;
            }
        }

        public void ReplaceMarkEffect(string regex, string replace)
        {
            for (int i = 0; i < markEffects.Count; i++)
            {
                SetMarkEffect(i, Regex.Replace(markEffects[i], regex, replace, RegexOptions.IgnoreCase));
            }
        }

        public void SetMarkEffect(int index, string newMark)
        {
            if (index < 0 || index >= markEffects.Count)
            {
                return;
            }
            int deltaLength = newMark.Length - markEffects[index].Length;
            bytes = bytes.ReplaceSubArray(markEffectStarts[index] + 4,
                    markEffectStarts[index] + BitConverter.ToInt32(bytes, markEffectStarts[index]) + 3,
                    Encoding.UTF8.GetBytes(newMark));
            markEffects[index] = newMark;
            byte[] barr = BitConverter.GetBytes(BitConverter.ToInt32(bytes, markEffectStarts[index]) + deltaLength);
            for (int i = 0; i < barr.Length; i++)
            {
                bytes[markEffectStarts[index] + i] = barr[i];
            }
            for (int i = index + 1; i < markEffectStarts.Count; i++)
            {
                markEffectStarts[i] = markEffectStarts[i] + deltaLength;
            }
            barr = BitConverter.GetBytes(bytes.Length - 4);
            for (int i = 0; i < barr.Length; i++)
            {
                bytes[i] = barr[i];
            }
        }

        public int GetHeroId()
        {
            if (markId < 10000)
                return -1;
            else if (markId < 100000)
                return markId / 100;
            else if (markId < 1000000)
                return int.Parse((markId + "").Substring(1, 4));
            else
                return -1;
        }

        public byte[] GetBytes()
        {
            return bytes;
        }
    }
}
