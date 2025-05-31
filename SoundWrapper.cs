using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AovClass
{
    public class SoundWrapper
    {
        private byte[] bytes;
        public List<SoundElement> soundElements;

        public SoundWrapper(byte[] bytes)
        {
            this.bytes = (byte[])bytes.Clone();
            soundElements = new List<SoundElement>();
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
                SoundElement s = new SoundElement(bytes[start..(start + count)]);
                soundElements.Add(s);
                start += count;
            }
        }

        public void copySound(int baseId, int targetId)
        {
            copySound(baseId, targetId, true);
        }

        public void copySound(int baseId, int targetId, bool removeOldSound)
        {
            List<SoundElement> targetSounds = new List<SoundElement>();
            for (int i = 0; i < soundElements.Count; i++)
            {
                if (soundElements[i].skinId == baseId)
                {
                    if (removeOldSound)
                    {
                        soundElements.RemoveAt(i);
                        i--;
                    }
                }
                else if (soundElements[i].skinId == targetId)
                {
                    SoundElement sound = new SoundElement(soundElements[i].getBytes());
                    sound.setSkinId(baseId, removeOldSound);
                    targetSounds.Add(sound);
                }
            }
            soundElements.AddRange(targetSounds);
        }

        public void setSound(int baseId, List<SoundElement> targetSounds)
        {
            for (int i = 0; i < soundElements.Count; i++)
            {
                if (soundElements[i].skinId == baseId)
                {
                    soundElements.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < targetSounds.Count; i++)
            {
                targetSounds[i].setSkinId(baseId);
            }
            soundElements.AddRange(targetSounds);
        }

        public byte[] getBytes()
        {
            byte[] childBytes = new byte[0];
            foreach (SoundElement e in soundElements)
            {
                childBytes = ArrayExtension.MergeArray(childBytes, e.getBytes());
            }
            int start;
            if (bytes[0] == 'M' && bytes[1] == 'S' && bytes[2] == 'E' && bytes[3] == 'S')
            {
                start = BitConverter.ToInt32(bytes, 132);
                bytes = bytes.ReplaceSubArray(12, 16, BitConverter.GetBytes(soundElements.Count));
            }
            else
                start = 0;
            return ArrayExtension.MergeArray(bytes[0..start], childBytes);
        }
    }

    public class SoundElement
    {
        private byte[] bytes;
        public readonly string soundId;
        public int skinId;

        public SoundElement(byte[] bytes)
        {
            this.bytes = (byte[])bytes.Clone();
            int i = BitConverter.ToInt32(bytes, 4);
            if (i > 99999)
            {
                if (i < 10000000)
                {
                    soundId = (i + "").Substring(5);
                    skinId = int.Parse((i + "").Substring(0, 5));
                }
                else
                {
                    soundId = (i + "").Substring(7);
                    skinId = int.Parse((i + "").Substring(0, 7));
                }
            }
            else
            {
                soundId = i + "";
                skinId = 0;
            }
        }

        public void setSkinId(int skinId)
        {
            setSkinId(skinId, true);
        }

        public void setSkinId(int skinId, bool changeSoundId)
        {
            if (this.skinId == 0)
                return;
            bytes = bytes.ReplaceAll(BitConverter.GetBytes(this.skinId), BitConverter.GetBytes(skinId));
            if (changeSoundId)
                bytes = bytes.ReplaceAll(BitConverter.GetBytes(int.Parse(this.skinId + soundId)),
                        BitConverter.GetBytes(int.Parse(skinId + soundId)));
            this.skinId = skinId;
        }

        public byte[] getBytes()
        {
            return bytes;
        }
    }
}
