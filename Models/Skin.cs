using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AovClass.Models
{
    public class Skin
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string Label { get; set; }
        public bool ChangeAnim { get; set; }
        public bool HasDeathEffect { get; set; }
        public bool IsAwakeSkin { get; set; }
        public bool IsComponentSkin { get; set; }
        public bool NotAddExtraBack { get; set; }
        public string? ComponentEffectId { get; set; }
        public int? ComponentLevel { get; set; }
        public int? LevelSFXUnlock { get; set; }
        public int? LevelVOXUnlock { get; set; }
        public string? SpecialBackAnim { get; set; }
        public string? HasteName { get; set; }
        public string? HasteNameRun { get; set; }
        public string? HasteNameEnd { get; set; }
        public List<string>? ParticleNotMod { get; set; }
        public List<string>? FilenameNotMod { get; set; }
        public List<string>? FilenameNotModCheckId { get; set; }

        public string? IconURL { get { return $"https://github.com/dha52vk/AOVResources/raw/main/Normal/{Id}.jpg"; } }
        public string? IconMiniURL { get { return $"https://github.com/dha52vk/AOVResources/raw/main/Mini/{Id}.jpg"; } }

        public Skin() { }

        public Skin(int skinId, string label)
        {
            Id = skinId;
            Label = label;
            Name = "";
        }

        public Skin(int skinId, string name, string label)
        {
            Id = skinId;
            Label = label;
            Name = name;
        }

        public override string? ToString()
        {
            return Name + "(" + Id + ")";
        }
    }

    public class SpecialSkinLevel(int id, int skinLevel)
    {
        public int id = id;
        public int skinLevel = skinLevel;
    }

    public class SkinLabel(string label, int skinLevel)
    {
        public string label = label;
        public int skinLevel = skinLevel;
    }
    //public enum SkinLabel
    //{
    //    [EnumMember(Value = "Default")]
    //    Default = 0,
    //    [EnumMember(Value = "A")]
    //    A = 1,
    //    [EnumMember(Value = "S")]
    //    S = 2,
    //    [EnumMember(Value = "S_Plus")]
    //    S_Plus = 3,
    //    [EnumMember(Value = "SS")]
    //    SS = 4,
    //    [EnumMember(Value = "SSS_HH")]
    //    SSS_HH = 5,

    //    [EnumMember(Value = "A_HH")]
    //    A_HH = 1,
    //    [EnumMember(Value = "S_HH")]
    //    S_HH = 2,
    //    [EnumMember(Value = "S_Plus_HH")]
    //    S_Plus_HH = 3,
    //    [EnumMember(Value = "SS_HH")]
    //    SS_HH = 4,
    //    [EnumMember(Value = "SS_Chroma")]
    //    SS_Chroma = 4,

    //    [EnumMember(Value = "FMVP")]
    //    FMVP = 3,
    //    [EnumMember(Value = "Y2024")]
    //    Y2024 = 3,
    //    [EnumMember(Value = "SS_Premium")]
    //    SS_Premium = 4,
    //    [EnumMember(Value = "One_Punch_Man")]
    //    One_Punch_Man = 5,
    //    [EnumMember(Value = "Dimension_Breaker")]
    //    Dimension_Breaker = 5
    //}
}
