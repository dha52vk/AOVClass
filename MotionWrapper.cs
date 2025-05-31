using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AovClass
{
    class MotionWrapper
    {
        private byte[] bytes;
        public List<MotionElement> motionElements;

        public MotionWrapper(byte[] bytes)
        {
            this.bytes = (byte[])bytes.Clone();
            motionElements = new List<MotionElement>();
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
                MotionElement s = new MotionElement(bytes[start..(start + count)]);
                motionElements.Add(s);
                start += count;
            }
        }

        public void showMotionCodes(int heroId)
        {
            showMotionCodes(heroId, 0);
        }

        public void showMotionCodes(int heroId, int space)
        {
            string s = "";
            for (int i = 0; i < space; i++)
            {
                s += " ";
            }
            foreach (MotionElement m in motionElements)
            {
                if (m.getHeroId() == heroId)
                {
                    Trace.WriteLine(s + m.motionCodes);
                }
            }
        }

        public void copyMotion(int heroId, string baseMotionCode, string newMotionCode)
        {
            copyMotion(heroId, new string[] { baseMotionCode }, newMotionCode);
        }

        public void copyMotion(int heroId, string[] baseMotionCode, string newMotionCode)
        {
            List<int>? baseIndexs = null;
            int newIndex = -1;
            for (int i = 0; i < motionElements.Count; i++)
            {
                if (motionElements[i].getHeroId() == heroId)
                {
                    if (motionElements[i].motionCodes.Contains(newMotionCode))
                    {
                        newIndex = i;
                    }
                    else if (motionElements[i].motionCodes.ToArray().SameElementWith(baseMotionCode))
                    {
                        if (baseIndexs == null)
                            baseIndexs = new List<int>();
                        baseIndexs.Add(i);
                    }
                }
            }
            if (baseIndexs != null && newIndex != -1)
            {
                foreach (int baseIndex in baseIndexs)
                {
                    int oldIndex = motionElements[baseIndex].getIndex();
                    motionElements[baseIndex] = new MotionElement(motionElements[newIndex].getBytes());
                    motionElements[baseIndex].setIndex(oldIndex);
                }
            }
            else
            {
                throw new Exception("not found code " + baseMotionCode + " or " + newMotionCode);
            }
        }

        public byte[] getBytes()
        {
            byte[] childBytes = new byte[0];
            for (int i = 0; i < motionElements.Count; i++)
            {
                // motionElements[i).setIndex(i+1);
                childBytes = ArrayExtension.MergeArray(childBytes, motionElements[i].getBytes());
            }
            int start;
            if (bytes[0] == 'M' && bytes[1] == 'S' && bytes[2] == 'E' && bytes[3] == 'S')
            {
                start = BitConverter.ToInt32(bytes, 132);
                bytes = bytes.ReplaceSubArray(12, 16, BitConverter.GetBytes(motionElements.Count));
            }
            else
                start = 0;
            return ArrayExtension.MergeArray(bytes[0..start], childBytes);
        }
    }

    class MotionElement
    {
        private byte[] bytes;
        public List<string> motionCodes;

        public MotionElement(byte[] bytes)
        {
            this.bytes = (byte[])bytes.Clone();
            motionCodes = new List<string>();

            int index = 9;
            while (index < bytes.Length - 5)
            {
                int Length = BitConverter.ToInt32(bytes, index);
                if (Length == 0)
                    throw new Exception("Length error");
                if (Length == 1)
                {
                    index += 5;
                    continue;
                }
                motionCodes.Add(Encoding.UTF8.GetString(bytes[(index + 4)..(index + Length + 3)]));
                index += Length + 4;
            }
        }

        public int getHeroId()
        {
            return BitConverter.ToInt32(bytes, bytes.Length - 5);
        }

        public void setIndex(int index)
        {
            bytes = bytes.ReplaceSubArray(4, 8, BitConverter.GetBytes(index));
        }

        public int getIndex()
        {
            return BitConverter.ToInt32(bytes, 4);
        }

        public byte[] getBytes()
        {
            return bytes;
        }
    }
}
