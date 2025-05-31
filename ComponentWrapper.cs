using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AovClass
{
    class ComponentWrapper
    {
        private byte[] bytes;
        public List<CharComponent> charComponents;

        public ComponentWrapper(byte[] bytes)
        {
            this.bytes = (byte[]) bytes.Clone();
            charComponents = new List<CharComponent>();
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
                CharComponent s = new CharComponent(bytes[start..(start + count)]);
                charComponents.Add(s);
                start += count;
            }
        }

        public void RemoveSkinComponent(int skinId)
        {
            for (int i = 0; i < charComponents.Count; i++)
            {
                if (charComponents[i].ContainsId(skinId))
                {
                    charComponents.RemoveAt(i);
                    i--;
                }
            }
        }

        public byte[] getBytes()
        {
            byte[] childBytes = new byte[0];
            foreach (CharComponent e in charComponents)
            {
                childBytes = ArrayExtension.MergeArray(childBytes, e.GetBytes());
            }
            int start;
            if (bytes[0] == 'M' && bytes[1] == 'S' && bytes[2] == 'E' && bytes[3] == 'S')
            {
                start = BitConverter.ToInt32(bytes, 132);
                bytes = bytes.ReplaceSubArray(12, 16, BitConverter.GetBytes(charComponents.Count));
            }
            else
                start = 0;
            return ArrayExtension.MergeArray(bytes[0..start], childBytes);
        }
    }

    class CharComponent
    {
        private byte[] bytes;
        public readonly int componentId;
        public List<int> skinIdList;

        public CharComponent(byte[] bytes)
        {
            this.bytes = (byte[])bytes.Clone();
            skinIdList = new List<int>();
            componentId = BitConverter.ToInt32(bytes, 4);

            int start = 155;
            if (bytes.CountMatches(Encoding.UTF8.GetBytes("_##")) == 2)
            {
                start = 174;
            }
            int skinId;
            while ((skinId = BitConverter.ToInt32(bytes, start)) > 9999 && skinId < 100000)
            {
                skinIdList.Add(skinId);
                start += 29;
            }
        }

        public bool ContainsId(int skinId)
        {
            return skinIdList.Contains(skinId);
        }

        public byte[] GetBytes()
        {
            return bytes;
        }
    }
}
