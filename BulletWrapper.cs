using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AovClass
{
    class ListBulletElement
    {
        private byte[] bytes;
        public List<BulletElement> bulletElements;
        private Dictionary<int, List<BulletElement>> bulletMapWithId;
        private List<int> heroIdList;

        public ListBulletElement(byte[] bytes)
        {
            this.bytes = (byte[]) bytes.Clone();
            bulletElements = new List<BulletElement>();
            bulletMapWithId = new Dictionary<int, List<BulletElement>>();
            heroIdList = new List<int>();
            int start;
            if (bytes[0] == 'M' && bytes[1] == 'S' && bytes[2] == 'E' && bytes[3] == 'S')
            {
                start = BitConverter.ToInt32(bytes, 132);
            }
            else
                start = 0;
            if (start == bytes.Length)
                return;
            int count;
            while (start < bytes.Length)
            {
                count = BitConverter.ToInt32(bytes, start) + 4;
                BulletElement b = new BulletElement(bytes[start..(start + count)]);
                if (!bulletMapWithId.ContainsKey(b.getHeroId()))
                {
                    bulletMapWithId[b.getHeroId()] = new List<BulletElement>();
                }
                bulletMapWithId[b.getHeroId()].Add(b);
                heroIdList.Add(b.getHeroId());
                bulletElements.Add(b);
                start += count;
            }
        }

        public List<BulletElement> GetBulletElement(int heroId)
        {
            return bulletMapWithId[heroId];
        }

        public bool ContainsHeroId(int heroId)
        {
            return heroIdList.Contains(heroId);
        }

        public void ReplaceBulletEffect(int heroId, string regex, string replace)
        {
            for (int i = 0; i < bulletElements.Count; i++)
            {
                if (bulletElements[i].getHeroId() == heroId)
                {
                    bulletElements[i].setEffectName((value)=> Regex.Replace(value, regex, replace, RegexOptions.IgnoreCase));
                }
            }
        }

        public byte[] GetBytes()
        {
            byte[] childBytes = new byte[0];
            foreach (BulletElement b in bulletElements)
            {
                childBytes = ArrayExtension.MergeArray(childBytes, b.getBytes());
            }
            int start;
            if (bytes[0] == 'M' && bytes[1] == 'S' && bytes[2] == 'E' && bytes[3] == 'S')
            {
                start = BitConverter.ToInt32(bytes, 132);
                bytes = bytes.ReplaceSubArray(12, 16, BitConverter.GetBytes(bulletElements.Count));
            }
            else
                start = 0;
            return ArrayExtension.MergeArray(bytes[0..start], childBytes);
        }
    }

    class BulletElement
    {
        private byte[] bytes;
        private int effectStart;
        public int bulletId;
        public byte[] bulletName;
        public string effectName;

        public BulletElement(byte[] bytes)
        {
            this.bytes = (byte[]) bytes.Clone();
            bulletId = BitConverter.ToInt32(bytes, 4);
            int start, count;
            start = 9;
            count = BitConverter.ToInt32(bytes, start) + 4;
            bulletName = bytes[(start + 4)..(start + count - 1)];
            start = start + count + 41;
            effectStart = start;
            count = BitConverter.ToInt32(bytes, start) + 4;
            effectName = Encoding.UTF8.GetString(bytes[(start + 4)..(start + count - 1)]);
        }

        public void setEffectName(Func<string,string> valueLambda)
        {
            string effect = Encoding.UTF8.GetString(bytes[(effectStart + 4)..(effectStart + BitConverter.ToInt32(bytes, effectStart) + 3)]);
            effect = valueLambda(effect);
            bytes = bytes.ReplaceSubArray(effectStart + 4,
                    effectStart + BitConverter.ToInt32(bytes, effectStart) + 3, Encoding.UTF8.GetBytes(effect));
            int[] indexChanges = new int[] { 0, effectStart };
            foreach (int changeAt in indexChanges)
            {
                byte[] barr = BitConverter.GetBytes(bytes.Length - changeAt - 4);
                for (int i = 0; i < barr.Length; i++)
                {
                    bytes[changeAt + i] = barr[i];
                }
            }
        }

        public string getBulletName()
        {
            return Encoding.UTF8.GetString(bulletName);
        }

        public int getHeroId()
        {
            string id = "";
            string name = Encoding.UTF8.GetString(bulletName);
            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] >= '0' && name[i] <= '9')
                {
                    id += name[i];
                }
                else
                {
                    break;
                }
            }
            if (id.Length == 4 && id[0] == '3')
            {
                id = id.Substring(1);
            }
            if (id.Equals(""))
                id = "-1";
            return int.Parse(id);
        }

        public byte[] getBytes()
        {
            return bytes;
        }
    }
}
