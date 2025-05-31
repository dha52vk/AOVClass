using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AovClass
{
    public class LabelWrapper
    {
        private byte[] bytes;
        public List<LabelElement> labelElements;
        public Dictionary<int, int> labelIndexMap;

        public LabelWrapper(byte[] bytes)
        {
            this.bytes = (byte[])bytes.Clone();
            labelElements = new List<LabelElement>();
            labelIndexMap = new Dictionary<int, int>();
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
                LabelElement l = new LabelElement(bytes[start..(start + count)]);
                labelIndexMap[l.labelId] = labelElements.Count;
                labelElements.Add(l);
                start += count;
            }
        }

        public void SetLabel(int sourceId, byte[] labelBytes)
        {
            if (!labelIndexMap.ContainsKey(sourceId))
            {
                throw new Exception("not found label for id " + sourceId);
            }
            labelElements[labelIndexMap[sourceId]] = new LabelElement(labelBytes);
            labelElements[labelIndexMap[sourceId]].SetLabelIndex(sourceId % 100);
            labelElements[labelIndexMap[sourceId]].SetHeroId(sourceId / 100);
            labelElements[labelIndexMap[sourceId]].SetLabelId(sourceId);
        }

        public int CopyLabel(int sourceId, int targetId)
        {
            if (!labelIndexMap.ContainsKey(sourceId))
            {
                return 1;
            }
            else if (!labelIndexMap.ContainsKey(targetId))
            {
                return 2;
            }

            byte[] bytes = labelElements[labelIndexMap[targetId]].GetBytes();
            labelElements[labelIndexMap[sourceId]] = new LabelElement(bytes);
            labelElements[labelIndexMap[sourceId]].SetLabelIndex(sourceId % 100);
            labelElements[labelIndexMap[sourceId]].SetHeroId(sourceId / 100);
            labelElements[labelIndexMap[sourceId]].SetLabelId(sourceId);
            return 0;
        }

        public byte[] GetBytes()
        {
            byte[] childBytes = new byte[0];
            foreach (LabelElement e in labelElements)
            {
                childBytes = ArrayExtension.MergeArray(childBytes, e.GetBytes());
            }
            int start;
            if (bytes[0] == 'M' && bytes[1] == 'S' && bytes[2] == 'E' && bytes[3] == 'S')
            {
                start = BitConverter.ToInt32(bytes, 132);
                bytes = bytes.ReplaceSubArray(12, 16, BitConverter.GetBytes(labelElements.Count));
            }
            else
                start = 0;
            return ArrayExtension.MergeArray(bytes[0..start], childBytes);
        }
    }

    public class LabelElement
    {
        private byte[] bytes;
        public int labelId;
        public int labelIndex;
        public int heroId;

        public LabelElement(byte[] bytes)
        {
            this.bytes = (byte[])bytes.Clone();

            labelId = BitConverter.ToInt32(bytes, 4);
            labelIndex = BitConverter.ToInt32(bytes, 36);
            heroId = BitConverter.ToInt32(bytes, 8);
        }

        public void SetLabelId(int labelId)
        {
            this.labelId = labelId;
            byte[] barr = BitConverter.GetBytes(labelId);
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

        public void SetLabelIndex(int labelIndex)
        {
            this.labelIndex = labelIndex;
            byte[] barr = BitConverter.GetBytes(labelIndex);
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
