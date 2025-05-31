using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AovClass
{
    public class ModSources
    {
        public required string ChannelName= "IzumiTv";
        public required string YtbLink= "https://www.youtube.com/@MiyamuraModAOV";
        public required string ResourcesPath;
        public required string SpecialModPath;
        public required string AovVersion;
        public string InfosParentPath { get => Path.Combine(ResourcesPath, "Prefab_Characters/"); }
        public string ActionsParentPath { get => Path.Combine(ResourcesPath, "Ages/Prefab_Characters/Prefab_Hero/"); }
        public string DatabinPath { get => Path.Combine(ResourcesPath, "Databin/Client/"); }
        public string AssetRefsPath { get => Path.Combine(ResourcesPath, "AssetRefs/"); }
        public string LanguageCode = "VN";
        public string LanguageFolder { get => $"{LanguageCode}_Garena_{LanguageCode}"; }
        public required string SaveModPath;
        public List<string> TrackTypeNotRemoveCheckSkinId =[];
        public Dictionary<string, List<string>> ParametersFound = [];
        public Dictionary<int, List<string>> ParticleNotMod = [];
        public List<int> SkinNotSwapIcon = [];
        //public List<int> SkinNotExtraBack = [];
        public Dictionary<int, int> SpecialSoundId = [];
        public Dictionary<int, Dictionary<string, byte[]>> SpecialSoundElements = [];
        public Dictionary<int, byte[]> SpecialLabelElements = [];
        public Dictionary<int, byte[]> SpecialIconElements = [];
        public Dictionary<int, byte[]> SpecialInfoElements = [];

        public ModSources()
        {

        }
    }
}
