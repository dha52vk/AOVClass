using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AovClass.Models
{
    public class SkinLevelWrapper
    {
        public readonly List<SkinLabel> SkinLabelLevels = [];
        public readonly List<SpecialSkinLevel> SpecialSkinLevels = [];
        public readonly List<int> SkinsHasOrgan = [];

        public SkinLevelWrapper()
        {

        }

        public int? GetSkinLevel(Skin skin)
        {
            SpecialSkinLevel? specialSearch = SpecialSkinLevels.Find(s => s.id == skin.Id);
            SkinLabel? skinLabel = SkinLabelLevels.Find(l => l.label == skin.Label);
            if (specialSearch != null)
            {
                return specialSearch.skinLevel;
            }
            else if (skinLabel != null)
            {
                return skinLabel.skinLevel;
            }
            else
            {
                return 0;
            }
        }
    }

    public enum DefaultLevel
    {
        Default=0,
        A=1,
        S=2,
        S_Plus=3,
        SS=4,
        SSS=5
    }
}
