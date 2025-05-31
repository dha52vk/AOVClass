using Aov_Mod_GUI.Models;
using AovClass.Models;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace AovClass
{
    public class ModController
    {
        public required List<Hero> heroList;
        public readonly ModSources ModSources;
        public readonly SkinLevelWrapper SkinLevels;
        private readonly StringComparison CultureIgnoreCase = StringComparison.CurrentCultureIgnoreCase;
        public Action<string> UpdateProgress { private get; set; }

        private string ModPackName = "";
        private readonly PackageElement[] packageCredits = [
            new PackageElement() { _Name = "YtbChannel", _JtType = "JTPri", _Type="TypeSystem.String", _Value= "IzumiTv" },
            new PackageElement() { _Name = "YtbLink", _JtType = "JTPri", _Type="TypeSystem.String", _Value= "https://www.youtube.com/@MiyamuraModAOV" }
        ];

        public ModController(ModSources modSources, SkinLevelWrapper skinLevels)
        {
            ModSources = modSources;
            SkinLevels = skinLevels;
            UpdateProgress = (value) => { };
        }

        public ModController(ModSources modSources, SkinLevelWrapper skinLevels, Action<string> updateProgressFunc)
        {
            ModSources = modSources;
            SkinLevels = skinLevels;
            UpdateProgress = updateProgressFunc;
        }

        public void ModSkin(List<ModInfo> mods, string ZipPackName)
        {
            UpdateProgress("");
            ModPackName = ZipPackName;
            ModIcon(mods);
            ModLabel(mods);
            ModInfos(mods);
            ModOrgan(mods);
            ModAssetRef(mods);
            ModAction(mods);
            ModLiteBullet(mods);
            ModSkillMark(mods);
            ModSound(mods);
            ModBack(mods);
            ModHaste(mods);
            File.Copy(Path.Combine(ModSources.ResourcesPath, "version.txt"),
                Path.Combine(ModSources.SaveModPath, MakeSimpleString(ModPackName), "Resources", ModSources.AovVersion, "version.txt"));
        }

        public void ModMultiSkin(List<ModMultiInfo> mods, string ZipPackName)
        {
            UpdateProgress("");
            ModPackName = ZipPackName;
            List<ModInfo> modList = [];
            foreach (var mod in mods)
            {
                foreach(var pair in mod.SkinChanges)
                {
                    modList.Add(new([pair.Key], pair.Value, mod.ModSettings));
                }
            }
            ModIcon(modList);
            ModLabel(modList);
            ModInfos(modList);
            ModOrgan(modList);
            //ModAssetRef(modList);
            ModMultiAction(mods);
            //ModLiteBullet(modList);
            //ModSkillMark(modList);
            ModSound(modList);
            ModBack(modList, true);
            ModHaste(modList, true);
            File.Copy(Path.Combine(ModSources.ResourcesPath, "version.txt"),
                Path.Combine(ModSources.SaveModPath, MakeSimpleString(ModPackName), "Resources", ModSources.AovVersion, "version.txt"));
        }

        public void ModInfos(List<ModInfo> _mods)
        {
            ModInfos(_mods, null);
        }

        public void ModInfos(List<ModInfo> _mods, string? saveParent)
        {
            List<ModInfo> mods = _mods
                .Where(m => m.ModSettings.ModIcon && SkinLevels.GetSkinLevel(m.NewSkin) >= (int)DefaultLevel.A)
                .ToList();
            if (_mods.Count == 0)
                return;
            string saveParentPath = saveParent ?? Path.Combine(ModSources.SaveModPath, MakeSimpleString(ModPackName), "Resources", ModSources.AovVersion);
            string saveCharComponentPath = Path.Combine(saveParentPath, "Databin/Client/Character/ResCharacterComponent.bytes");
            byte[]? charBytes = AovTranslation.Decompress(File.ReadAllBytes(
                    File.Exists(saveCharComponentPath) ? saveCharComponentPath :
                    Path.Combine(ModSources.DatabinPath, "Character/ResCharacterComponent.bytes")
                ));
            ComponentWrapper charComs = new(charBytes);

            UpdateProgress(" Dang mod ngoai hinh pack " + ModPackName);
            for (int l = 0; l < mods.Count; l++)
            {
                ModInfo modInfo = mods[l];
                UpdateProgress("    + Modding info " + (l + 1) + "/" + mods.Count + ": " + modInfo.NewSkin);
                PackageElement? element = null, trapElement = null;
                int newSkinId = (int)(modInfo.NewSkin.IsComponentSkin ? modInfo.NewSkin.Id /= 100 : modInfo.NewSkin.Id);
                int heroId = newSkinId;
                while (heroId > 999) { heroId /= 10; }

                DirectoryInfo tempDir = Directory.CreateTempSubdirectory();
                string saveInfoPath = Path.Combine(saveParentPath, "Prefab_Characters/Actor_" + heroId + "_Infos.pkg.bytes");
                ZipFile.ExtractToDirectory(File.Exists(saveInfoPath) ? saveInfoPath :
                    Path.Combine(ModSources.InfosParentPath, "Actor_" + heroId + "_Infos.pkg.bytes"), tempDir.FullName);
                string heroCodeName = Path.GetFileName(Directory.GetDirectories(Path.Combine(tempDir.FullName, "Prefab_Hero"))[0]) ?? "";
                string trapInputPath = Path.Combine(tempDir.FullName, "Prefab_Hero",
                    heroCodeName, heroCodeName + "_trap_actorinfo.bytes");
                if (File.Exists(trapInputPath))
                {
                    byte[]? trapInfoBytes = AovTranslation.Decompress(File.ReadAllBytes(trapInputPath));
                    if (trapInfoBytes != null)
                    {
                        trapElement = PackageSerializer.Deserialize(trapInfoBytes);
                    }
                }
                string infoPath = Path.Combine(tempDir.FullName, "Prefab_Hero/" + heroCodeName + "/", heroCodeName + "_actorinfo.bytes");
                byte[] inputBytes = File.ReadAllBytes(infoPath);
                byte[]? infoBytes = AovTranslation.Decompress(inputBytes);
                infoBytes ??= inputBytes;
                element = PackageSerializer.Deserialize(infoBytes);
                foreach (var credit in packageCredits)
                {
                    if (element.Children.Find(e => e.Name == credit.Name) == null)
                    {
                        element.InsertChild(1, credit);
                    }
                }
                foreach (var skin in modInfo.OldSkins)
                {
                    int oldSkinIndex = -1, newSkinIndex = -1;
                    int oldId = skin.Id;
                    if (oldId == modInfo.NewSkin.Id)
                        continue;
                    int comId = int.Parse((oldId + "")[3..]) - 1;
                    charComs.RemoveSkinComponent(heroId * 100 + comId);
                    List<string> nameExcept = ["ActorName", "useMecanim", "useNewMecanim", "oriSkinUseNewMecanim"];
                    if (oldId == heroId * 10 + 1)
                    {
                        int removeAt = 1 + packageCredits.Length;
                        while (element.Children[removeAt]._Name != "SkinPrefab")
                        {
                            if (nameExcept.Contains(element.Children[removeAt]._Name))
                            {
                                removeAt++;
                            }
                            else
                            {
                                element.Children.RemoveAt(removeAt);
                            }
                        }
                    }
                    PackageElement? SkinPrefabs = element.Children.Find(s => s._Name == "SkinPrefab");
                    if (SkinPrefabs != null)
                    {
                        for (int i = 0; i < SkinPrefabs.Children.Count; i++)
                        {
                            PackageElement? PrefabLOD = SkinPrefabs.Children[i].Children.Find(s => s._Name == "ArtSkinPrefabLOD");
                            if (PrefabLOD != null)
                            {
                                if (!int.TryParse(Path.GetFileName(PrefabLOD.Children[0]._Value).Split("_")[0], out int prefabId))
                                {
                                    continue;
                                }
                                if (prefabId == modInfo.NewSkin.Id)
                                {
                                    newSkinIndex = i;
                                }
                                else if (prefabId == oldId)
                                {
                                    oldSkinIndex = i;
                                }
                            }
                        }
                    }
                    if (newSkinIndex != -1)
                    {
                        PackageElement newSkinElement = (PackageElement)SkinPrefabs.Children[newSkinIndex].Clone();
                        if (modInfo.NewSkin.IsComponentSkin)
                        {
                            for (int i = 0; i < newSkinElement.Children.Count; i++)
                            {
                                PackageElement e = newSkinElement.Children[i];
                                int componentId = int.Parse((modInfo.NewSkin.Id + "")[^2..]);
                                for (int j = 0; j < e.Children.Count; j++)
                                {
                                    PackageElement child = e.Children[j];
                                    string[] split1 = child._Value.Split("/");
                                    string[] split2 = split1[^1].Split("_");
                                    if (split1.Length < 3 || split2.Length < 3)
                                        continue;
                                    List<string> paths = new(split1[0..3])
                                    {
                                        "Component"
                                    };
                                    List<string> paths2 = new(split2[0..2])
                                    {
                                        "RT_" + componentId,
                                        split2[^1]
                                    };
                                    paths.Add(string.Join("_", paths2));
                                    child._Value = string.Join("/", paths);
                                }
                            }
                        }
                        if (oldId == heroId * 10 + 1)
                        {
                            foreach (PackageElement child in newSkinElement.Children)
                            {
                                child.Name = Encoding.UTF8.GetBytes(child._Name
                                    .Replace("ArtSkinPrefabLOD", "ArtPrefabLOD")
                                    .Replace("ArtSkinPrefabLODEx", "ArtPrefabLODEx")
                                    .Replace("ArtSkinLobbyShowLOD", "ArtLobbyShowLOD")
                                    .Replace("useNewMecanim", "oriSkinUseNewMecanim")
                                    .Replace("useMecanim", "oriSkinUseMecanim"));
                                element.InsertChild(1, child);
                            }
                        }
                        else
                        {
                            SkinPrefabs.Children[oldSkinIndex] = newSkinElement;
                        }

                        if (trapElement != null)
                        {
                            PackageElement? trapSkinPrefabs = trapElement.Children.Find(s => s._Name == "SkinPrefab");
                            PackageElement newTrapElement = (PackageElement)trapSkinPrefabs.Children[newSkinIndex].Clone();
                            if (oldSkinIndex == -1)
                            {
                                foreach (PackageElement child in newSkinElement.Children)
                                {
                                    child.Name = Encoding.UTF8.GetBytes(child._Name
                                        .Replace("ArtSkinPrefabLOD", "ArtPrefabLOD")
                                        .Replace("ArtSkinPrefabLODEx", "ArtPrefabLODEx")
                                        .Replace("ArtSkinLobbyShowLOD", "ArtLobbyShowLOD"));
                                    element.AddChild(child);
                                }
                            }
                            else
                            {
                                trapSkinPrefabs.Children[oldSkinIndex] = newTrapElement;
                            }
                        }
                    }
                }
                //File.WriteAllBytes("E:/test.bytes", (PackageSerializer.Serialize(element)));
                File.WriteAllBytes(infoPath, AovTranslation.Compress(PackageSerializer.Serialize(element)));
                if (trapElement != null)
                {
                    File.WriteAllBytes(trapInputPath, AovTranslation.Compress(PackageSerializer.Serialize(trapElement)));
                }
                if (charComs != null)
                {
                    Directory.CreateDirectory(Directory.GetParent(saveCharComponentPath).FullName);
                    File.WriteAllBytes(saveCharComponentPath, charComs.getBytes());
                }
                Directory.CreateDirectory(Directory.GetParent(saveInfoPath).FullName);
                ZipDirectories(Directory.GetDirectories(tempDir.FullName), saveInfoPath);
                Directory.Delete(tempDir.FullName, true);
            }
        }

        public void ModOrgan(List<ModInfo> _modList)
        {
            ModOrgan(_modList, null);
        }

        public void ModOrgan(List<ModInfo> _modList, string? saveModPath)
        {
            List<ModInfo> modList = _modList
                .Where((modInfo) => modInfo.ModSettings.ModOrgan && SkinLevels.SkinsHasOrgan.Contains(modInfo.NewSkin.Id))
                .ToList();
            if (modList.Count == 0)
                return;
            string saveParentPath = Path.Combine(ModSources.SaveModPath, MakeSimpleString(ModPackName),
                "Resources", ModSources.AovVersion);
            UpdateProgress(" Dang mod hieu ung ve than pack " + ModPackName);

            string inputPath = ModSources.DatabinPath + "Actor/organSkin.bytes";
            string outputPath = !string.IsNullOrEmpty(saveModPath) ? saveModPath :
                    Path.Combine(saveParentPath, "Databin/Client/Actor/organSkin.bytes");
            if (File.Exists(outputPath))
                inputPath = outputPath;

            byte[]? outputBytes = AovTranslation.Decompress(File.ReadAllBytes(inputPath));

            for (int l = 0; l < modList.Count; l++)
            {
                ModInfo modInfo = modList[l];
                string id = modInfo.NewSkin.Id + "";
                UpdateProgress("    + Modding organs " + (l + 1) + "/" + modList.Count + ": " + modInfo.NewSkin);

                string originId = modInfo.OldSkins[0].Id + "";

                string heroId = id[..3];
                string skinId = id[3..];
                int newId = int.Parse(heroId) * 100 + int.Parse(skinId) - 1;
                int targetId = int.Parse(heroId) * 100 + int.Parse(originId[3..]) - 1;
                outputBytes = outputBytes.ReplaceAll(BitConverter.GetBytes(newId), BitConverter.GetBytes(targetId));
            }
            Directory.CreateDirectory(Directory.GetParent(outputPath).FullName);
            File.WriteAllBytes(outputPath, AovTranslation.Compress(outputBytes));
        }

        public void ModAssetRef(List<ModInfo> _modList)
        {
            ModAssetRef(_modList, null);
        }

        public void ModAssetRef(List<ModInfo> _modList, string? saveParent)
        {
            List<ModInfo> modList = _modList
                .Where(modInfo => modInfo.ModSettings.ModAction && SkinLevels.GetSkinLevel(modInfo.NewSkin) >= (int)DefaultLevel.S)
                .ToList();
            UpdateProgress(" Dang fix khung pack " + ModPackName);
            for (int l = 0; l < modList.Count; l++)
            {
                ModInfo modInfo = modList[l];
                // if (!modInfo.modSettings.modAction || modInfo.NewSkin.getSkinLevel() < 2) {
                // continue;
                // }
                UpdateProgress("    + Fix khung " + (l + 1) + "/" + modList.Count + ": " + modInfo.NewSkin);

                string id = modInfo.NewSkin.IsComponentSkin
                        ? modInfo.NewSkin.Id / 100 + ""
                        : modInfo.NewSkin.Id + "";
                string heroId = id[..3];
                string skinId = id[3..];
                int skin = int.Parse(skinId) - 1;
                int idMod = int.Parse(heroId) * 100 + skin;

                string saveParentPath = saveParent ?? Path.Combine(ModSources.SaveModPath, MakeSimpleString(ModPackName),
                    "Resources", ModSources.AovVersion);
                string inputPath = ModSources.AssetRefsPath + "Hero/" + heroId + "_AssetRef.bytes";
                string outputPath = Path.Combine(saveParentPath, "AssetRefs/Hero/" + heroId + "_AssetRef.bytes");
                if (File.Exists(outputPath))
                    inputPath = outputPath;
                byte[]? outputBytes = AovTranslation.Decompress(File.ReadAllBytes(inputPath));
                PackageElement assetRef = PackageSerializer.Deserialize(outputBytes);
                foreach (PackageElement creditElement in packageCredits)
                {
                    assetRef.AddChild(creditElement);
                }
                assetRef.ModifyChildren((child) =>
                {
                    if (child._JtType != "JTPri")
                        return child;
                    string value = child._Value;
                    if (!value.Contains("prefab_skill_effects/hero_skill_effects/", CultureIgnoreCase)
                            || (ModSources.ParticleNotMod.ContainsKey(idMod) && ModSources.ParticleNotMod[idMod].FindIndex(
                                            (subeffect) => value.Contains(subeffect, CultureIgnoreCase)) != -1))
                    {
                        return child;
                    }
                    string[] split = value.Split("/");
                    string newValue;
                    if (modInfo.NewSkin.IsComponentSkin && modInfo.NewSkin.ComponentLevel > (int)DefaultLevel.A)
                    {
                        newValue = "Prefab_Skill_Effects/Component_Effects/" + idMod + "/"
                                + modInfo.NewSkin.ComponentEffectId + "/"
                                + split[^1];
                    }
                    else
                    {
                        if (!modInfo.NewSkin.IsAwakeSkin)
                        {
                            newValue = string.Join("/", split[0..3]) + "/" + idMod + "/"
                                    + split[^1];
                        }
                        else
                        {
                            newValue = "Prefab_Skill_Effects/Component_Effects/" + idMod + "/" + idMod + "_5/"
                                    + split[^1];
                        }
                    }
                    child._Value = newValue;
                    return child;
                });
                Directory.CreateDirectory(Directory.GetParent(outputPath).FullName);
                //File.WriteAllBytes("E:/106_AssetRef.bytes", (PackageSerializer.Serialize(assetRef)));
                File.WriteAllBytes(outputPath, AovTranslation.Compress(PackageSerializer.Serialize(assetRef)));
            }
        }

        public void ModAction(List<ModInfo> _modList)
        {
            ModAction(_modList, null);
        }

        public void ModAction(List<ModInfo> _modList, string? saveParent)
        {
            UpdateProgress(" Dang mod hieu ung pack " + ModPackName);
            string inputZipPath = ModSources.ActionsParentPath + "CommonActions.pkg.bytes";
            string saveParentPath = saveParent ?? Path.Combine(ModSources.SaveModPath, MakeSimpleString(ModPackName),
                "Resources", ModSources.AovVersion);
            if (File.Exists(Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero/CommonActions.pkg.bytes")))
                inputZipPath = Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero/CommonActions.pkg.bytes");

            DirectoryInfo cacheDir2 = Directory.CreateTempSubdirectory();
            Directory.CreateDirectory(cacheDir2.FullName);
            ZipFile.ExtractToDirectory(inputZipPath, cacheDir2.FullName);
            string filemodName = "commonresource/Dance.xml";
            string inputPath = Path.Combine(cacheDir2.FullName, filemodName);
            string outputPath = inputPath;

            byte[] inputBytes = File.ReadAllBytes(inputPath);
            byte[]? outputBytes = AovTranslation.Decompress(inputBytes);
            if (outputBytes == null)
                return;

            ActionsXml danceXml = new();
            danceXml.LoadFromText(Encoding.UTF8.GetString(outputBytes));

            List<XmlNode> animTrackList = [];
            List<ModInfo> modList = _modList
                .Where(modInfo => modInfo.ModSettings.ModAction && SkinLevels.GetSkinLevel(modInfo.NewSkin) >= (int)DefaultLevel.S)
                .ToList();
            for (int l = 0; l < modList.Count; l++)
            {
                ModInfo modInfo = modList[l];
                UpdateProgress("    + Modding actions " + (l + 1) + "/" + modList.Count + ": " + modInfo.NewSkin);

                string id = modInfo.NewSkin.IsComponentSkin
                        ? modInfo.NewSkin.Id / 100 + ""
                        : modInfo.NewSkin.Id + "";
                string heroId = id[..3];
                string skinId = id[3..];
                int skin = int.Parse(skinId) - 1;
                int idMod = int.Parse(heroId) * 100 + skin;

                inputZipPath = ModSources.ActionsParentPath + "Actor_" + heroId + "_Actions.pkg.bytes";

                DirectoryInfo cacheDir1 = Directory.CreateTempSubdirectory();
                Directory.CreateDirectory(cacheDir1.FullName);
                ZipFile.ExtractToDirectory(inputZipPath, cacheDir1.FullName);

                filemodName = Path.Combine(Directory.GetDirectories(cacheDir1.FullName)[0], "skill");
                List<string> fileModPaths = [];
                //fileModPaths.AddRange(Directory.GetFiles(filemodName));
                foreach (string folder in Directory.GetDirectories(Directory.GetDirectories(cacheDir1.FullName)[0]))
                {
                    fileModPaths.AddRange(Directory.GetFiles(folder));
                }
                Dictionary<string, ActionsXml> package = [];
                foreach (var inputPathXml in fileModPaths)
                {
                    if (inputPathXml.Contains("back", CultureIgnoreCase)
                        || inputPathXml.Contains("born", CultureIgnoreCase)
                            || (inputPathXml.Contains("death", CultureIgnoreCase) && !modInfo.NewSkin.HasDeathEffect))
                    {
                        continue;
                    }
                    outputBytes = AovTranslation.Decompress(File.ReadAllBytes(inputPathXml));
                    if (outputBytes == null ||
                        (modInfo.NewSkin.FilenameNotMod != null
                            && modInfo.NewSkin.FilenameNotMod.ToList()
                                .Find((f) => f.Equals(Path.GetFileName(inputPathXml), CultureIgnoreCase)) != null))
                        continue;
                    ActionsXml projectXml = new();
                    projectXml.LoadFromText(Encoding.UTF8.GetString(outputBytes));
                    projectXml.ConvertVirtual2N();
                    List<XmlNode>? tracks = projectXml.GetActionNodes();
                    List<XmlNode> playSoundTicks = [];

                    for (int i = 0; i < tracks.Count; i++)
                    {
                        XmlNode track = tracks[i];
                        if (track.GetChildrenByName("Event").Count < 1)
                            continue;

                        ModActionTrack(track, modInfo, playSoundTicks, animTrackList);

                        if ((modInfo.NewSkin.FilenameNotModCheckId != null
                            && modInfo.NewSkin.FilenameNotModCheckId.ToList().Find((f) => f.Equals(Path.GetFileName(inputPathXml), CultureIgnoreCase)) != null)
                            || ModSources.TrackTypeNotRemoveCheckSkinId.ToList().Find((type) => type.Equals(track.GetAttribute("eventType"))) != null)
                        {
                            continue;
                        }
                        List<KeyValuePair<XmlNode, bool>>? conditionTracks = projectXml.GetConditionTracks(track);
                        if (conditionTracks != null)
                        {
                            for (int k = 0; k < conditionTracks.Count && k >= 0; k++)
                            {
                                XmlNode conditionTrack = conditionTracks[k].Key;
                                bool conditionStatus = conditionTracks[k].Value;
                                if (conditionTrack.GetAttribute("eventType") == "CheckSkinIdTick")
                                {
                                    XmlNode eventNode = conditionTrack.GetChildrenByName("Event")[0];
                                    XmlNode? skinIdParam = eventNode.GetChildByAttribute("name", "skinId");
                                    XmlNode? bEqualParam = eventNode.GetChildByAttribute("name", "bEqual");
                                    bool CheckIdEqual = conditionStatus
                                        && (bEqualParam == null || bEqualParam.GetAttribute("value") == "true");
                                    if (skinIdParam != null && skinIdParam.GetAttribute("value") == idMod.ToString())
                                    {
                                        if (CheckIdEqual)
                                        {
                                            track.RemoveChild(track.ChildNodes[k]);
                                            conditionTracks.RemoveAt(k);
                                            k--;
                                            UpdateProgress("     * Enable " + track.GetAttribute("trackName").PadRight(31) + Path.GetFileName(inputPathXml).PadRight(10) + $"({track.GetAttribute("guid")})");
                                        }
                                        else
                                        {
                                            track.SetAttribute("enabled", "false");
                                            UpdateProgress("     * Disable " + track.GetAttribute("trackName").PadRight(30) + Path.GetFileName(inputPathXml).PadRight(10) + $"({track.GetAttribute("guid")})");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    foreach (XmlNode tick in playSoundTicks)
                    {
                        projectXml.AppendActionNode(tick);
                    }
                    projectXml.AddComment($"Mod By {ModSources.ChannelName}!!  Subscribe: {ModSources.YtbLink}");
                    package.Add(inputPathXml, projectXml);
                    //File.WriteAllBytes(inputPathXml, (Encoding.UTF8.GetBytes(projectXml.GetOuterXml())));
                }
                if (Directory.Exists(ModSources.SpecialModPath)
                        && File.Exists(Path.Combine(ModSources.SpecialModPath, id + ".izumi")))
                {
                    SpecialMod(package, File.ReadAllText(Path.Combine(ModSources.SpecialModPath, id + ".izumi")));
                }
                foreach (var pair in package)
                {
                    File.WriteAllBytes(pair.Key, AovTranslation.Compress(Encoding.UTF8.GetBytes(pair.Value.GetOuterXml())));
                }
                string savePath = Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero/Actor_"
                        + heroId + "_Actions.pkg.bytes");
                Directory.CreateDirectory(Directory.GetParent(savePath).FullName);
                ZipDirectories(Directory.GetDirectories(cacheDir1.FullName), savePath);
                Directory.Delete(cacheDir1.FullName, true);
            }
            foreach (XmlNode node in animTrackList)
            {
                danceXml.AppendActionNode(node);
            }

            File.WriteAllBytes(outputPath, AovTranslation.Compress(Encoding.UTF8.GetBytes(danceXml.GetOuterXml())));

            string savePath2 = Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero/CommonActions.pkg.bytes");
            Directory.CreateDirectory(Directory.GetParent(savePath2).FullName);
            ZipDirectories(Directory.GetDirectories(cacheDir2.FullName), savePath2);
            Directory.Delete(cacheDir2.FullName, true);
        }

        private void ModActionTrack(XmlNode track, ModInfo modInfo)
        {
            ModActionTrack(track, modInfo, null, null);
        }

        private void ModActionTrack(XmlNode track, ModInfo modInfo, List<XmlNode>? playSoundTicks, List<XmlNode>? animTrackList)
        {
            string id = modInfo.NewSkin.IsComponentSkin
                    ? modInfo.NewSkin.Id / 100 + ""
                    : modInfo.NewSkin.Id + "";
            string heroId = id[..3];
            string skinId = id[3..];
            int skin = int.Parse(skinId) - 1;
            int idMod = int.Parse(heroId) * 100 + skin;
            for (int j = 0; j < track.GetChildrenByName("Event")[0].ChildNodes.Count; j++)
            {
                XmlNode? param = track.GetChildrenByName("Event")[0].ChildNodes[j];
                if (param == null)
                    continue;
                if (param.Name == "String")
                {
                    string? value = param.GetAttribute("value");
                    if (track.GetAttribute("eventType") == "PlayHeroSoundTick")
                    {
                        if (param.GetAttribute("name") == "eventName")
                        {
                            playSoundTicks?.Add(track.CloneNode(true));
                            string newValue = "";
                            if (value.Contains("_skin", CultureIgnoreCase))
                            {
                                newValue = value;
                            }
                            else if (!modInfo.NewSkin.IsAwakeSkin)
                            {
                                newValue = value + "_Skin" + skin;
                            }
                            else
                            {
                                if (value.Contains("_VO") || value.Contains("voice", CultureIgnoreCase))
                                {
                                    newValue = value + "_Skin" + skin + "_AW" + modInfo.NewSkin.LevelVOXUnlock;
                                }
                                else
                                {
                                    newValue = value + "_Skin" + skin + "_AW" + modInfo.NewSkin.LevelSFXUnlock;
                                }
                            }
                            param.SetAttribute("value", newValue);
                        }
                    }
                    else if (modInfo.NewSkin.ChangeAnim && track.GetAttribute("eventType") == "PlayAnimDuration")
                    {
                        if (param.GetAttribute("name") == "clipName")
                        {
                            param.SetAttribute("value", idMod + "/" + param.GetAttribute("value"));
                            XmlNode animTrack = track.CloneNode(true);
                            while (animTrack.ChildNodes[0].Name != "Event")
                            {
                                animTrack.RemoveChild(animTrack.ChildNodes[0]);
                            }
                            XmlNode? eventNode = animTrack.ChildNodes[0];
                            if (eventNode != null)
                            {
                                for (int k = 0; k < eventNode.ChildNodes.Count; k++)
                                {
                                    if (eventNode.ChildNodes[k].GetAttribute("name") != "targetId"
                                        && eventNode.ChildNodes[k].GetAttribute("name") != "clipName")
                                    {
                                        eventNode.RemoveChild(eventNode.ChildNodes[k]);
                                        k--;
                                    }
                                }
                            }
                            animTrackList?.Add(animTrack);
                        }
                    }
                    else
                    {
                        string newValue = "";
                        if (!value.Contains("prefab_skill_effects/hero_skill_effects/", CultureIgnoreCase)
                                || (modInfo.NewSkin.ParticleNotMod != null &&
                                modInfo.NewSkin.ParticleNotMod.ToList().Find((v) => value.Contains(v, CultureIgnoreCase)) != null))
                        {
                            newValue = value;
                        }
                        else
                        {
                            string[] split = value.Split("/");
                            if (modInfo.NewSkin.IsComponentSkin
                                    && modInfo.NewSkin.ComponentLevel >= (int)DefaultLevel.S)
                            {
                                newValue = "Prefab_Skill_Effects/Component_Effects/" + idMod + "/"
                                        + modInfo.NewSkin.ComponentEffectId + "/"
                                        + split[^1];
                            }
                            else
                            {
                                if (!modInfo.NewSkin.IsAwakeSkin)
                                {
                                    newValue = string.Join("/", split[0..3]) + "/" + (idMod) + "/" + split[^1];
                                }
                                else
                                {
                                    newValue = "Prefab_Skill_Effects/Component_Effects/" + idMod + "/" + idMod + "_5/" + split[^1];
                                }
                            }
                        }
                        param.SetAttribute("value", newValue);
                    }
                }
                else if (param.Name == "bool")
                {
                    if (param.GetAttribute("name") == "bAllowEmptyEffect")
                    {
                        param.SetAttribute("value", "false");
                    }
                }
            }
        }

        public void ModMultiAction(List<ModMultiInfo> _modMultiList)
        {
            ModMultiAction(_modMultiList, null);
        }

        public void ModMultiAction(List<ModMultiInfo> _modMultiList, string? saveParent)
        {
            UpdateProgress(" Dang mod hieu ung pack " + ModPackName);
            string inputZipPath = ModSources.ActionsParentPath + "CommonActions.pkg.bytes";
            string saveParentPath = saveParent ?? Path.Combine(ModSources.SaveModPath, MakeSimpleString(ModPackName),
                "Resources", ModSources.AovVersion);
            if (File.Exists(Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero/CommonActions.pkg.bytes")))
                inputZipPath = Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero/CommonActions.pkg.bytes");

            DirectoryInfo cacheDir2 = Directory.CreateTempSubdirectory();
            Directory.CreateDirectory(cacheDir2.FullName);
            ZipFile.ExtractToDirectory(inputZipPath, cacheDir2.FullName);
            string filemodName = "commonresource/Dance.xml";
            string inputPath = Path.Combine(cacheDir2.FullName, filemodName);
            string outputPath = inputPath;

            byte[] inputBytes = File.ReadAllBytes(inputPath);
            byte[]? outputBytes = AovTranslation.Decompress(inputBytes);
            if (outputBytes == null)
                return;

            ActionsXml danceXml = new();
            danceXml.LoadFromText(Encoding.UTF8.GetString(outputBytes));

            List<XmlNode> animTrackList = [];
            List<ModMultiInfo> modList = _modMultiList
                .Where(modInfo => modInfo.ModSettings.ModAction)
                .ToList();
            for (int l = 0; l < modList.Count; l++)
            {
                ModMultiInfo modInfo = modList[l];
                UpdateProgress("    + Modding multi actions " + (l + 1) + "/" + modList.Count + ": " + modInfo);

                string heroId = modInfo.SkinChanges.ElementAt(0).Key.Id.ToString()[..3];
                inputZipPath = ModSources.ActionsParentPath + "Actor_" + heroId + "_Actions.pkg.bytes";

                DirectoryInfo cacheDir1 = Directory.CreateTempSubdirectory();
                Directory.CreateDirectory(cacheDir1.FullName);
                ZipFile.ExtractToDirectory(inputZipPath, cacheDir1.FullName);

                filemodName = Path.Combine(Directory.GetDirectories(cacheDir1.FullName)[0], "skill");
                List<string> fileModPaths = [];
                //fileModPaths.AddRange(Directory.GetFiles(filemodName));
                foreach (string folder in Directory.GetDirectories(Directory.GetDirectories(cacheDir1.FullName)[0]))
                {
                    fileModPaths.AddRange(Directory.GetFiles(folder));
                }
                Dictionary<string, ActionsXml> package = [];
                bool hasAnimChanges = modInfo.SkinChanges.Any(pair => pair.Value.ChangeAnim);
                foreach (var inputPathXml in fileModPaths)
                {
                    if (inputPathXml.Contains("back", CultureIgnoreCase)
                        || inputPathXml.Contains("born", CultureIgnoreCase))
                    {
                        continue;
                    }
                    outputBytes = AovTranslation.Decompress(File.ReadAllBytes(inputPathXml));
                    if (outputBytes == null)
                        continue;
                    ActionsXml projectXml = new();
                    projectXml.LoadFromText(Encoding.UTF8.GetString(outputBytes));
                    projectXml.ConvertVirtual2N();
                    List<XmlNode> stopTracks = projectXml.GetActionNodes()
                        .Where(track => track.GetAttribute("eventType") == "StopTrack").ToList();
                    Dictionary<string, List<XmlNode>> skinTrackList = [];
                    Dictionary<string, List<string>> skinGuidNotMod = [];
                    List<string> skinTrackGuids = [];
                    projectXml.GetActionNodes().ForEach(track =>
                    {
                        string? eventType = track.GetAttribute("eventType");
                        XmlNode? eventNode = track.GetChildrenByName("Event").ElementAtOrDefault(0);
                        if (eventType == "CheckSkinIdTick" && eventNode != null)
                        {
                            XmlNode? bEqual = eventNode.GetChildByAttribute("name", "bEqual");
                            string? skinId = eventNode.GetChildByAttribute("name", "skinId")?.GetAttribute("value");
                            if (skinId != null)
                            {
                                if (!skinTrackList.ContainsKey(skinId))
                                    skinTrackList[skinId] = [];
                                if (!skinGuidNotMod.ContainsKey(skinId))
                                    skinGuidNotMod[skinId] = [];
                                foreach (var pair in projectXml.GetTracksHasConditionGuid(track.GetAttribute("guid")))
                                {
                                    if (pair.Value && (bEqual == null || bEqual.GetAttribute("value") == "true")
                                        || (!pair.Value && bEqual.GetAttribute("value") == "false"))
                                    {
                                        XmlNode clone = pair.Key.CloneNode(true);
                                        for(int i = 0; i < clone.ChildNodes.Count; i++)
                                        {
                                            if (clone.ChildNodes[i].Name == "Condition" 
                                                && clone.ChildNodes[i].GetAttribute("guid") == track.GetAttribute("guid"))
                                            {
                                                clone.RemoveChild(clone.ChildNodes[i]);
                                                i--;
                                            }
                                        }
                                        skinTrackList[skinId].Add(clone);
                                        skinTrackGuids.Add(clone.GetAttribute("guid"));
                                    }
                                    else
                                    {
                                        skinGuidNotMod[skinId].Add(pair.Key.GetAttribute("guid"));
                                    }
                                }
                            }
                        }
                    });
                    List<XmlNode> commonTracks = projectXml.GetActionNodes().Where(track =>
                    {
                        if (skinTrackGuids.Contains(track.GetAttribute("guid")))
                            return false;
                        string? eventType = track.GetAttribute("eventType");
                        if (eventType == "PlayHeroSoundTick"
                            || (hasAnimChanges && eventType == "PlayAnimDuration"))
                            return true;
                        XmlNode? eventNode = track.GetChildrenByName("Event").ElementAtOrDefault(0);
                        foreach (XmlNode param in eventNode.ChildNodes)
                        {
                            string? value = param.GetAttribute("value");
                            if (param.Name == "String" && value != null && value.Contains("Prefab_Skill_Effects/Hero_Skill_Effects/", CultureIgnoreCase))
                            {
                                return true;
                            }
                        }
                        return false;
                    }).ToList();

                    foreach (var pair in modInfo.SkinChanges)
                    {
                        if (pair.Value.FilenameNotMod != null
                            && pair.Value.FilenameNotMod.ToList()
                                .Find((f) => f.Equals(Path.GetFileName(inputPathXml), CultureIgnoreCase)) != null)
                        {
                            continue;
                        }
                        string id = pair.Value.IsComponentSkin
                                ? pair.Value.Id / 100 + ""
                                : pair.Value.Id + "";
                        int idMod = int.Parse(id[..3]) * 100 + int.Parse(id[3..]) - 1;
                        string baseId = pair.Key.IsComponentSkin
                            ? pair.Key.Id / 100 + ""
                            : pair.Key.Id + "";
                        int baseSkin = int.Parse(baseId[3..]) - 1;
                        int baseIdMod = int.Parse(baseId[..3]) * 100 + baseSkin;
                        string guidStart = ModSources.ChannelName + "-" + idMod + "-";

                        List<XmlNode> trackList = new(commonTracks);
                        if ((!pair.Value.FilenameNotModCheckId?.Any(f => Path.GetFileName(inputPathXml).Equals(f, CultureIgnoreCase)) ?? true))
                        {
                            if (skinTrackList.TryGetValue(idMod.ToString(), out List<XmlNode>? skinTL))
                            {
                                trackList.AddRange(skinTL);
                            }
                            for (int i = 0; i < trackList.Count; i++)
                            {
                                if (skinGuidNotMod.Any(guid => guid.Equals(trackList[i].GetAttribute("guid"))))
                                {
                                    trackList.RemoveAt(i);
                                    i--;
                                }
                            }
                        }
                        string checkGuid = "Mod_by_" + ModSources.ChannelName + "_Skin" + baseIdMod;
                        XmlNode checkSkinIdTrack = CustomNodeExtension.CreateCheckSkinIdTick(baseIdMod, checkGuid, 0, "self");
                        projectXml.InsertActionNode(0, checkSkinIdTrack);
                        XmlNode conditionFalse = CustomNodeExtension.CreateEventCondition(0, checkGuid, false);
                        XmlNode conditionTrue = CustomNodeExtension.CreateEventCondition(0, checkGuid, true);
                        foreach (var _track in trackList)
                        {
                            XmlNode track = _track.CloneNode(true);
                            for (int i = 0; i < track.ChildNodes.Count; i++)
                            {
                                string? condGuid = track.ChildNodes[i].GetAttribute("guid");
                                if (condGuid != null
                                    && condGuid.Contains(ModSources.ChannelName, CultureIgnoreCase))
                                {
                                    track.RemoveChild(track.ChildNodes[i]);
                                    i--;
                                }
                            }
                            track.InsertChild(0,conditionTrue);
                            ModActionTrack(track, new([pair.Key],pair.Value, modInfo.ModSettings));
                            string? guid = track.GetAttribute("guid");
                            string? newGuid = guid?.Remove(0, guidStart.Length).Insert(0, guidStart);
                            var stopThisTracks = stopTracks
                                .Where(track => track.GetChildrenByName("Event")?
                                    .Any(param => param.Name == "TrackObject" && param.GetAttribute("guid") == guid) ?? false)
                                .Select(track =>
                                {
                                    XmlNode node = track.CloneNode(true);
                                    foreach (XmlNode param in node.GetChildrenByName("Event")?.ElementAtOrDefault(0)?.ChildNodes.Cast<XmlNode>() ?? [])
                                    {
                                        if (param.Name == "TrackObject" && param.GetAttribute("guid") == guid)
                                        {
                                            param.SetAttribute("guid", newGuid);
                                        }
                                    }
                                    return node;
                                });
                            track.SetAttribute("guid", newGuid);
                            projectXml.AppendActionNode(track);
                            foreach (var stopTrack in stopThisTracks)
                            {
                                projectXml.AppendActionNode(stopTrack);
                            }
                            _track.InsertChild(0, conditionFalse);
                        }

                        if (Directory.Exists(ModSources.SpecialModPath)
                                && File.Exists(Path.Combine(ModSources.SpecialModPath, id + ".izumi")))
                        {
                            ActionsXml xml = new();
                            xml.LoadVirtual(trackList);
                            SpecialMod(new() { [Path.GetFileName(inputPathXml)] = xml }, File.ReadAllText(Path.Combine(ModSources.SpecialModPath, id + ".izumi")));
                        }
                    }

                    projectXml.AddComment($"Mod By {ModSources.ChannelName}!!  Subscribe: {ModSources.YtbLink}");
                    package.Add(inputPathXml, projectXml);
                    //File.WriteAllBytes(inputPathXml, (Encoding.UTF8.GetBytes(projectXml.GetOuterXml())));
                }
                foreach (var pair in package)
                {
                    File.WriteAllBytes(pair.Key, AovTranslation.Compress(Encoding.UTF8.GetBytes(pair.Value.GetOuterXml())));
                }
                string savePath = Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero/Actor_"
                        + heroId + "_Actions.pkg.bytes");
                Directory.CreateDirectory(Directory.GetParent(savePath).FullName);
                ZipDirectories(Directory.GetDirectories(cacheDir1.FullName), savePath);
                Directory.Delete(cacheDir1.FullName, true);
            }
            foreach (XmlNode node in animTrackList)
            {
                danceXml.AppendActionNode(node);
            }

            File.WriteAllBytes(outputPath, AovTranslation.Compress(Encoding.UTF8.GetBytes(danceXml.GetOuterXml())));

            string savePath2 = Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero/CommonActions.pkg.bytes");
            Directory.CreateDirectory(Directory.GetParent(savePath2).FullName);
            ZipDirectories(Directory.GetDirectories(cacheDir2.FullName), savePath2);
            Directory.Delete(cacheDir2.FullName, true);
        }

        public void ModLiteBullet(List<ModInfo> _modList)
        {
            ModLiteBullet(_modList, null);
        }

        public void ModLiteBullet(List<ModInfo> _modList, string? saveParent)
        {
            string inputPath = ModSources.DatabinPath + "Skill/liteBulletCfg.bytes";

            string saveParentPath = saveParent ?? Path.Combine(ModSources.SaveModPath, MakeSimpleString(ModPackName),
                "Resources", ModSources.AovVersion);
            string outputPath = Path.Combine(saveParentPath, "Databin/Client/Skill/liteBulletCfg.bytes");
            if (File.Exists(outputPath))
                inputPath = outputPath;
            ListBulletElement listBullet = new(
                    AovTranslation.Decompress(File.ReadAllBytes(inputPath)));
            List<ModInfo> modList = _modList
                .Where(modInfo => modInfo.ModSettings.ModAction && SkinLevels.GetSkinLevel(modInfo.NewSkin) >= (int)DefaultLevel.S
                    && listBullet.ContainsHeroId(int.Parse((modInfo.NewSkin.Id + "")[..3])))
                .ToList();
            if (modList.Count == 0)
                return;
            UpdateProgress(" Dang mod danh thuong pack " + ModPackName);
            for (int l = 0; l < modList.Count; l++)
            {
                ModInfo modInfo = modList[l];
                // if (!modInfo.modSettings.modAction || modInfo.NewSkin.getSkinLevel() < 2) {
                // continue;
                // }
                UpdateProgress("    + Modding lite bullets " + (l + 1) + "/" + modList.Count + ": " + modInfo.NewSkin);

                string id = modInfo.NewSkin.IsComponentSkin
                        ? modInfo.NewSkin.Id / 100 + ""
                        : modInfo.NewSkin.Id + "";
                string heroId = id[..3];
                string skinId = id[3..];
                int skin = int.Parse(skinId) - 1;
                int idMod = int.Parse(heroId) * 100 + skin;

                string heroCodeName = "";
                using (ZipArchive zipArchive = ZipFile.OpenRead(ModSources.InfosParentPath + "Actor_" + heroId + "_Infos.pkg.bytes"))
                {
                    ZipArchiveEntry entryGetName;
                    int i = 0;
                    while ((entryGetName = zipArchive.Entries[i]).FullName.Split("/").Length < 2)
                        i++;
                    heroCodeName = entryGetName.FullName.Split("/")[1];
                }

                string newCode, oldCode = "prefab_skill_effects/hero_skill_effects/" + heroCodeName;

                if (modInfo.NewSkin.IsComponentSkin && modInfo.NewSkin.ComponentLevel > (int)DefaultLevel.A)
                {
                    newCode = "Prefab_Skill_Effects/Component_Effects/" + idMod + "/" + modInfo.NewSkin.ComponentEffectId;
                }
                else
                {
                    if (!modInfo.NewSkin.IsAwakeSkin)
                    {
                        newCode = "prefab_skill_effects/hero_skill_effects/" + heroCodeName + "/" + idMod;
                    }
                    else
                    {
                        newCode = "Prefab_Skill_Effects/Component_Effects/" + idMod + "/" + idMod + "_5";
                    }
                }
                listBullet.ReplaceBulletEffect(int.Parse(heroId), oldCode, newCode);
            }
            Directory.CreateDirectory(Directory.GetParent(outputPath).FullName);
            File.WriteAllBytes(outputPath, AovTranslation.Compress(listBullet.GetBytes()));
        }

        public void ModSkillMark(List<ModInfo> _modList)
        {
            ModSkillMark(_modList, null);
        }

        public void ModSkillMark(List<ModInfo> _modList, string? saveParent)
        {
            string inputPath = ModSources.DatabinPath + "Skill/skillmark.bytes";
            string outputPath;
            string saveParentPath = saveParent ?? Path.Combine(ModSources.SaveModPath, MakeSimpleString(ModPackName),
                "Resources", ModSources.AovVersion);
            outputPath = Path.Combine(saveParentPath, "Databin/Client/Skill/skillmark.bytes");
            if (File.Exists(outputPath))
                inputPath = outputPath;
            SkillMarkWrapper listMark = new(AovTranslation.Decompress(File.ReadAllBytes(inputPath)));
            List<ModInfo> modList = _modList
                .Where(modInfo => modInfo.ModSettings.ModAction && SkinLevels.GetSkinLevel(modInfo.NewSkin) >= (int)DefaultLevel.S
                    || listMark.ContainsHeroId(int.Parse(modInfo.NewSkin.Id.ToString()[0..3])))
                .ToList();
            if (modList.Count == 0)
                return;
            UpdateProgress(" Dang mod dau an pack " + ModPackName);
            for (int l = 0; l < modList.Count; l++)
            {
                ModInfo modInfo = modList[l];
                // if (!modInfo.modSettings.modAction || modInfo.NewSkin.getSkinLevel() < 2) {
                // continue;
                // }
                UpdateProgress("    + Modding skill marks " + (l + 1) + "/" + modList.Count + ": " + modInfo.NewSkin);

                string id = modInfo.NewSkin.IsComponentSkin
                        ? modInfo.NewSkin.Id / 100 + ""
                        : modInfo.NewSkin.Id + "";
                string heroId = id[..3];
                string skinId = id[3..];
                int skin = int.Parse(skinId) - 1;
                int idMod = int.Parse(heroId) * 100 + skin;

                string heroCodeName = "";
                using (ZipArchive zipArchive = ZipFile.OpenRead(ModSources.InfosParentPath + "Actor_" + heroId + "_Infos.pkg.bytes"))
                {
                    ZipArchiveEntry entryGetName;
                    int i = 0;
                    while ((entryGetName = zipArchive.Entries[i]).FullName.Split("/").Length < 2)
                        i++;
                    heroCodeName = entryGetName.FullName.Split("/")[1];
                }

                string newCode, oldCode = "(?i)prefab_skill_effects/hero_skill_effects/" + heroCodeName;
                if (modInfo.NewSkin.IsComponentSkin && modInfo.NewSkin.ComponentLevel > (int)DefaultLevel.A)
                {
                    newCode = "Prefab_Skill_Effects/Component_Effects/" + idMod + "/" + modInfo.NewSkin.ComponentEffectId;
                }
                else
                {
                    if (!modInfo.NewSkin.IsAwakeSkin)
                    {
                        newCode = "prefab_skill_effects/hero_skill_effects/" + heroCodeName + "/" + idMod;
                    }
                    else
                    {
                        newCode = "Prefab_Skill_Effects/Component_Effects/" + idMod + "/" + idMod + "_5";
                    }
                }
                listMark.ReplaceMarkEffect(int.Parse(heroId), oldCode, newCode);
            }
            Directory.CreateDirectory(Directory.GetParent(outputPath).FullName);
            File.WriteAllBytes(outputPath, AovTranslation.Compress(listMark.GetBytes()));
        }

        public void ModSound(List<ModInfo> _modList)
        {
            ModSound(_modList, null);
        }

        public void ModSound(List<ModInfo> _modList, string? saveParent)
        {
            List<ModInfo> modList = _modList
                .Where(modInfo => modInfo.ModSettings.ModSound && SkinLevels.GetSkinLevel(modInfo.NewSkin) >= (int)DefaultLevel.S_Plus)
                .ToList();
            if (modList.Count == 0)
                return;
            UpdateProgress(" Dang mod am thanh pack " + ModPackName);

            string[] inputPaths = [ ModSources.DatabinPath + "Sound/BattleBank.bytes",
                ModSources.DatabinPath + "Sound/ChatSound.bytes",
                ModSources.DatabinPath + "Sound/HeroSound.bytes",
                ModSources.DatabinPath + "Sound/LobbyBank.bytes",
                ModSources.DatabinPath + "Sound/LobbySound.bytes"
            ];
            string[] outputPaths = new string[inputPaths.Length];
            SoundWrapper[] soundListArr = new SoundWrapper[inputPaths.Length];
            for (int i = 0; i < inputPaths.Length; i++)
            {
                string saveParentPath = saveParent ?? Path.Combine(ModSources.SaveModPath, MakeSimpleString(ModPackName),
                    "Resources", ModSources.AovVersion);
                outputPaths[i] = Path.Combine(saveParentPath, "Databin/Client/Sound/"
                        + Path.GetFileName(inputPaths[i]));
                if (File.Exists(outputPaths[i]))
                    inputPaths[i] = outputPaths[i];
                soundListArr[i] = new(AovTranslation.Decompress(File.ReadAllBytes(inputPaths[i])));
            }
            for (int l = 0; l < modList.Count; l++)
            {
                ModInfo modInfo = modList[l];
                // if (!modInfo.modSettings.modSound || modInfo.NewSkin.getSkinLevel() < 3) {
                // continue;
                // }
                string id = modInfo.NewSkin.IsComponentSkin
                        ? modInfo.NewSkin.Id / 100 + ""
                        : modInfo.NewSkin.Id + "";
                UpdateProgress("    + Modding sound " + (l + 1) + "/" + modList.Count + ": " + modInfo.NewSkin);
                int heroId = int.Parse(id[..3]);
                int targetId = heroId * 100 + int.Parse(id[3..]) - 1;
                if (ModSources.SpecialSoundId.TryGetValue(targetId, out int value))
                {
                    targetId = value;
                    // UpdateProgress(targetId + "");
                }
                // for (int f = 0; f < modInfo.targetSkins.Count; f++) {
                int f = 0;
                int baseId = int.Parse(modInfo.OldSkins[0].Id.ToString()[..3]) * 100
                        + int.Parse(modInfo.OldSkins[f].Id.ToString()[3..]) - 1;
                for (int i = 0; i < soundListArr.Length; i++)
                {
                    SoundWrapper? targetSounds = null;
                    if (ModSources.SpecialSoundElements.ContainsKey(int.Parse(id)))
                    {
                        targetSounds = new(ModSources.SpecialSoundElements[int.Parse(id)][Path.GetFileName(inputPaths[i]).ToLower()]);
                    }
                    if (int.Parse(modInfo.OldSkins[0].Id.ToString()[..3]) == heroId
                            && baseId != heroId * 100
                            && (modInfo.OldSkins[f].Label == null
                                    || SkinLevels.GetSkinLevel(modInfo.OldSkins[f]) < (int)DefaultLevel.S_Plus)
                            && i == 0)
                    {
                        soundListArr[i].copySound(heroId * 100, targetId, false);
                    }
                    else
                    {
                        if (targetSounds == null)
                        {
                            soundListArr[i].copySound(baseId, targetId);
                        }
                        else
                        {
                            soundListArr[i].setSound(baseId, targetSounds.soundElements);
                        }
                    }
                }
                // }
            }
            for (int i = 0; i < outputPaths.Length; i++)
            {
                Directory.CreateDirectory(Directory.GetParent(outputPaths[i]).FullName);
                File.WriteAllBytes(outputPaths[i], AovTranslation.Compress(soundListArr[i].getBytes()));
                //File.WriteAllBytes("E:/sounddebug/"+Path.GetFileName(outputPaths[i]), (soundListArr[i].getBytes()));
            }
        }

        public void ModIcon(List<ModInfo> _modList)
        {
            ModIcon(_modList, null);
        }

        public void ModIcon(List<ModInfo> _modList, string? saveParent)
        {
            UpdateProgress(" Dang mod icon, ten va man xuat hien pack " + ModPackName);
            string saveParentPath = saveParent ?? Path.Combine(ModSources.SaveModPath, MakeSimpleString(ModPackName),
                "Resources", ModSources.AovVersion);

            string inputPath = ModSources.DatabinPath + "Actor/heroSkin.bytes";
            string outputPath = Path.Combine(saveParentPath, "Databin/Client/Actor/heroSkin.bytes");
            if (File.Exists(outputPath))
                inputPath = outputPath;

            IconWrapper listIconElement = new(AovTranslation.Decompress(File.ReadAllBytes(inputPath)));
            List<ModInfo> modList = _modList
                .Where(modInfo => modInfo.ModSettings.ModIcon)
                .ToList();
            for (int l = 0; l < modList.Count; l++)
            {
                ModInfo modInfo = modList[l];
                // if (!modInfo.modSettings.modIcon)
                // continue;
                UpdateProgress("    + Modding icons " + (l + 1) + "/" + modList.Count + ": " + modInfo);
                string id = modInfo.NewSkin.IsComponentSkin
                        ? modInfo.NewSkin.Id / 100 + ""
                        : modInfo.NewSkin.Id + "";
                int heroId = int.Parse(id[..3]);
                int targetId = heroId * 100 + int.Parse(id[3..]) - 1;
                byte[]? iconBytes = null;
                if (ModSources.SpecialIconElements.ContainsKey(int.Parse(id)))
                {
                    iconBytes = ModSources.SpecialIconElements[int.Parse(id)];
                }
                foreach (Skin skin in modInfo.OldSkins)
                {
                    int baseId = int.Parse(modInfo.OldSkins[0].Id.ToString()[..3]) * 100
                            + int.Parse(skin.Id.ToString()[3..]) - 1;
                    if (iconBytes == null)
                    {
                        listIconElement.CopyIcon(baseId, targetId, !ModSources.SkinNotSwapIcon.Contains(int.Parse(id)));
                    }
                    else
                    {
                        listIconElement.SetIcon(baseId, iconBytes);
                        listIconElement.SetIcon(targetId, iconBytes);
                    }
                }
            }
            Directory.CreateDirectory(Directory.GetParent(outputPath).FullName);
            File.WriteAllBytes(outputPath, AovTranslation.Compress(listIconElement.GetBytes()));
        }

        public void ModLabel(List<ModInfo> _modList)
        {
            ModLabel(_modList, null);
        }

        public void ModLabel(List<ModInfo> _modList, string? saveParent)
        {
            UpdateProgress(" Dang mod bac skin pack " + ModPackName);

            string inputPath = ModSources.DatabinPath + "Shop/HeroSkinShop.bytes";
            string saveParentPath = saveParent ?? Path.Combine(ModSources.SaveModPath, MakeSimpleString(ModPackName),
                "Resources", ModSources.AovVersion);
            string outputPath = Path.Combine(saveParentPath, "Databin/Client/Shop/HeroSkinShop.bytes");
            if (File.Exists(outputPath))
                inputPath = outputPath;

            LabelWrapper listLabelElement = new(AovTranslation.Decompress(File.ReadAllBytes(inputPath)));
            List<ModInfo> modList = _modList
                .Where(modInfo => modInfo.ModSettings.ModIcon)
                .ToList();
            for (int l = 0; l < modList.Count; l++)
            {
                ModInfo modInfo = modList[l];
                // if (!modInfo.modSettings.modIcon)
                // continue;
                UpdateProgress("    + Modding label " + (l + 1) + "/" + modList.Count + ": " + modInfo);
                string id = modInfo.NewSkin.IsComponentSkin
                        ? modInfo.NewSkin.Id / 100 + ""
                        : modInfo.NewSkin.Id + "";
                int heroId = int.Parse(id[..3]);
                int targetId = heroId * 100 + int.Parse(id[3..]) - 1;
                byte[]? iconBytes = null;
                if (ModSources.SpecialLabelElements.ContainsKey(int.Parse(id)))
                {
                    iconBytes = ModSources.SpecialLabelElements[int.Parse(id)];
                }
                foreach (Skin skin in modInfo.OldSkins)
                {
                    int baseId = int.Parse(modInfo.OldSkins[0].Id.ToString()[..3]) * 100
                            + int.Parse(skin.Id.ToString()[3..]) - 1;
                    if (iconBytes == null)
                    {
                        int result = listLabelElement.CopyLabel(baseId, targetId);
                        bool notfound = false;
                        while (result == 2)
                        {
                            notfound = true;
                            foreach (Hero hero in heroList)
                            {
                                foreach (Skin skin2 in hero.Skins)
                                {
                                    if (skin2.Label == modInfo.NewSkin.Label)
                                    {
                                        targetId = int.Parse(skin2.Id.ToString()[..3]) * 100
                                                + int.Parse(skin2.Id.ToString()[3..]) - 1;
                                        result = listLabelElement.CopyLabel(baseId, targetId);
                                        if (result != 2)
                                        {
                                            break;
                                        }
                                    }
                                }
                                if (result != 2)
                                {
                                    break;
                                }
                            }
                        }
                        if (notfound)
                        {
                            UpdateProgress("        *changed new label to " + targetId + "(" + modInfo.NewSkin.Label + ")");
                        }
                    }
                    else
                    {
                        listLabelElement.SetLabel(baseId, iconBytes);
                        listLabelElement.SetLabel(targetId, iconBytes);
                    }
                }
            }
            Directory.CreateDirectory(Directory.GetParent(outputPath).FullName);
            //Console.WriteLine(outputPath);
            //File.WriteAllBytes("E:/test.bytes", (listLabelElement.GetBytes()));
            File.WriteAllBytes(outputPath, AovTranslation.Compress(listLabelElement.GetBytes()));
        }

        public void ModBack(List<ModInfo> _modList)
        {
            ModBack(_modList, null,false);
        }

        public void ModBack(List<ModInfo> _modList, bool baseOnSkin)
        {
            ModBack(_modList, null, baseOnSkin);
        }

        public void ModBack(List<ModInfo> _modList, string? saveParent, bool baseOnSkin)
        {
            List<ModInfo> modList = _modList
                .Where((modInfo) => modInfo.ModSettings.ModBack && SkinLevels.GetSkinLevel(modInfo.NewSkin) >= (int)DefaultLevel.SS)
                .ToList();
            if (modList.Count == 0)
                return;
            UpdateProgress(" Dang mod hieu ung bien ve pack " + ModPackName);
            string inputZipPath = ModSources.ActionsParentPath + "CommonActions.pkg.bytes";
            string saveParentPath = saveParent ?? Path.Combine(ModSources.SaveModPath, MakeSimpleString(ModPackName),
                "Resources", ModSources.AovVersion);
            if (File.Exists(Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero/CommonActions.pkg.bytes")))
                inputZipPath = Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero/CommonActions.pkg.bytes");


            DirectoryInfo cachedir2 = Directory.CreateTempSubdirectory();
            Directory.CreateDirectory(cachedir2.FullName);
            ZipFile.ExtractToDirectory(inputZipPath, cachedir2.FullName);
            string filemodName = "commonresource/Back.xml";
            string inputPath = Path.Combine(cachedir2.FullName, filemodName);
            string outputPath = inputPath;

            byte[] inputBytes = File.ReadAllBytes(inputPath);
            byte[]? outputBytes = AovTranslation.Decompress(inputBytes);
            if (outputBytes == null)
                return;

            ActionsXml backXml = new();
            backXml.LoadFromText(Encoding.UTF8.GetString(outputBytes));
            List<XmlNode>? actionNodes = backXml.GetActionNodes();

            List<XmlNode> animNodes = [];
            List<XmlNode> baseTracks = actionNodes.Where((node) =>
                {
                    if (!node.GetAttribute("eventType").Equals("TriggerParticleTick", CultureIgnoreCase)
                        && !node.GetAttribute("eventType").Equals("TriggerParticle", CultureIgnoreCase)
                        && !node.GetAttribute("eventType").Equals("PlayAnimDuration", CultureIgnoreCase))
                        return false;
                    List<KeyValuePair<XmlNode, bool>>? conditionTracks = backXml.GetConditionTracks(node);
                    if (conditionTracks == null)
                    {
                        if (node.GetAttribute("eventType").Equals("PlayAnimDuration", CultureIgnoreCase))
                        {
                            animNodes.Add(node.CloneNode(true));
                            return false;
                        }
                        return true;
                    }
                    for (int k = 0; k < conditionTracks.Count && k >= 0; k++)
                    {
                        XmlNode conditionTrack = conditionTracks[k].Key;
                        bool conditionStatus = conditionTracks[k].Value;
                        if (conditionTrack.GetAttribute("eventType") == "CheckSkinIdTick")
                        {
                            XmlNode eventNode = conditionTrack.GetChildrenByName("Event")[0];
                            XmlNode? skinIdParam = eventNode.GetChildByAttribute("name", "skinId");
                            XmlNode? bEqualParam = eventNode.GetChildByAttribute("name", "bEqual");
                            //Console.WriteLine(eventNode.GetChildByAttribute("name", "skinId").GetAttribute("value"));
                            bool CheckIdEqual = conditionStatus
                                && (bEqualParam == null || bEqualParam.GetAttribute("value") == "true");
                            //Console.WriteLine(eventNode.GetChildByAttribute("name", "skinId").GetAttribute("value") + ": " + CheckIdEqual);
                            if (CheckIdEqual)
                                return false;
                        }
                    }
                    if (node.GetAttribute("eventType").Equals("PlayAnimDuration", CultureIgnoreCase))
                    {
                        animNodes.Add(node.CloneNode(true));
                        return false;
                    }
                    return true;
                }).ToList();

            List<XmlNode> CheckSkinIdTicks = actionNodes
                .Where((node) => node.GetAttribute("eventType").Equals("CheckSkinIdTick", CultureIgnoreCase))
                .ToList();

            List<XmlNode> conditionDefaults = [];
            for (int l = 0; l < modList.Count; l++)
            {
                ModInfo modInfo = modList[l];
                UpdateProgress("    + Modding back " + (l + 1) + "/" + modList.Count + ": " + modInfo.NewSkin);

                int hero = int.Parse(modInfo.NewSkin.Id.ToString()[0..3]);

                string heroCodeName = "";
                using (ZipArchive zipArchive = ZipFile.OpenRead(ModSources.InfosParentPath + "Actor_" + hero + "_Infos.pkg.bytes"))
                {
                    ZipArchiveEntry entryGetName;
                    int i = 0;
                    while ((entryGetName = zipArchive.Entries[i]).FullName.Split("/").Length < 2)
                        i++;
                    heroCodeName = entryGetName.FullName.Split("/")[1];
                }
                string id = modInfo.NewSkin.IsComponentSkin
                    ? modInfo.NewSkin.Id / 100 + ""
                    : modInfo.NewSkin.Id + "";
                int skin = int.Parse(id[3..]) - 1;
                int idMod = hero * 100 + skin;
                string baseId = modInfo.OldSkins[0].IsComponentSkin
                    ? modInfo.OldSkins[0].Id / 100 + ""
                    : modInfo.OldSkins[0].Id + "";
                int baseSkin = int.Parse(baseId[3..]) - 1;
                int baseIdMod = int.Parse(baseId[..3]) * 100 + baseSkin;
                string guid;
                XmlNode CheckIdTick;
                if (!baseOnSkin)
                {
                    guid = "Mod_by_" + ModSources.ChannelName + "_Hero" + hero;
                    CheckIdTick = CustomNodeExtension.CreateCheckHeroIdTick(hero, guid);
                }
                else
                {
                    guid = "Mod_by_" + ModSources.ChannelName + "_Skin" + baseIdMod;
                    CheckIdTick = CustomNodeExtension.CreateCheckSkinIdTick(baseIdMod, guid, 1, "target");
                }
                XmlNode conditionTrue = CustomNodeExtension.CreateEventCondition(l, guid, true);
                XmlNode conditionFalse = CustomNodeExtension.CreateEventCondition(l, guid, false);
                backXml.InsertActionNode(0, CheckIdTick);
                conditionDefaults.Add(conditionFalse);

                List<XmlNode> baseBackTracks = [];
                baseBackTracks.AddRange(baseTracks);
                foreach (XmlNode track in CheckSkinIdTicks)
                {
                    XmlNode eventNode = track.GetChildrenByName("Event")[0];
                    XmlNode? skinId = eventNode.GetChildByAttribute("name", "skinId");
                    XmlNode? bEqual = eventNode.GetChildByAttribute("name", "bEqual");
                    if (skinId?.GetAttribute("value") == idMod.ToString())
                    {
                        List<KeyValuePair<XmlNode, bool>> nodes = backXml.GetTracksHasConditionGuid(track.GetAttribute("guid"));
                        foreach (var keypair in nodes)
                        {
                            var node = keypair.Key.CloneNode(true);
                            if (((bEqual == null || bEqual.GetAttribute("value").Equals("true")) && keypair.Value)
                                || (bEqual.GetAttribute("value") == "false" && !keypair.Value))
                            {
                                List<XmlNode>? cons = node.GetChildrenByName("Condition");
                                if (cons != null)
                                {
                                    foreach (XmlNode con in cons)
                                    {
                                        node.RemoveChild(con);
                                    }
                                }
                                baseBackTracks.Add(node);
                            }
                            else
                            {
                                baseBackTracks = baseBackTracks
                                    .Where((node) => node.GetAttribute("guid") != node.GetAttribute("guid"))
                                    .ToList();
                            }
                        }
                    }
                }

                List<XmlNode> extraBackNodes = [];
                if (!modInfo.NewSkin.NotAddExtraBack)
                {
                    using ZipArchive zipArchive = ZipFile.OpenRead(ModSources.ActionsParentPath + "Actor_" + hero + "_Actions.pkg.bytes");
                    ZipArchiveEntry? entry = null;
                    int i = 0;
                    while (i < zipArchive.Entries.Count && !(entry = zipArchive.Entries[i]).FullName.EndsWith(idMod + "_Back.xml", CultureIgnoreCase))
                        i++;
                    if (i < zipArchive.Entries.Count && entry != null)
                    {
                        Stream stream = entry.Open();
                        using MemoryStream ms = new();
                        byte[] buffer = new byte[4096];
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                        }
                        byte[]? xmlBytes = AovTranslation.Decompress(ms.ToArray());
                        if (xmlBytes != null)
                        {
                            string xml = Encoding.UTF8.GetString(xmlBytes);
                            ActionsXml extraBackXml = new();
                            extraBackXml.LoadFromText(xml);
                            extraBackNodes.AddRange(extraBackXml.GetActionNodes());
                        }
                    }
                }

                baseBackTracks.AddRange(extraBackNodes);

                if (!string.IsNullOrEmpty(modInfo.NewSkin.SpecialBackAnim))
                {
                    foreach (XmlNode _node in animNodes)
                    {
                        XmlNode node = _node.CloneNode(true);
                        XmlNode? clipName = node.GetChildrenByName("Event")?[0].GetChildByAttribute("name", "clipName");
                        clipName?.SetAttribute("value", modInfo.NewSkin.SpecialBackAnim + "/" + clipName?.GetAttribute("value"));
                        baseBackTracks.Add(node);
                    }
                }

                foreach (XmlNode _track in baseBackTracks)
                {
                    XmlNode track = _track.CloneNode(true);
                    XmlNode eventNode = track.GetChildrenByName("Event")[0];
                    track.InsertChild(0, conditionTrue);
                    string eventType = track.GetAttribute("eventType") ?? "";
                    string value = "";
                    if (eventType.Contains("TriggerParticle"))
                    {
                        XmlNode? resourceName = eventNode.GetChildByAttribute("name", "resourceName");
                        XmlNode? parentResourceName = eventNode.GetChildByAttribute("name", "parentResourceName");
                        value = parentResourceName?.GetAttribute("value") ?? "";
                        if (resourceName != null)
                        {
                            if (resourceName.GetAttribute("useRefParam").Equals("false", CultureIgnoreCase))
                            {
                                value = resourceName.GetAttribute("value") ?? "";
                            }
                            else
                            {
                                resourceName.SetAttribute("useRefParam", "false");
                            }
                            resourceName.SetAttribute("refParamName", "");
                            string[] split = value.Split("/");
                            string newValue;
                            if (modInfo.NewSkin.IsComponentSkin
                                    && modInfo.NewSkin.ComponentLevel >= (int)DefaultLevel.SS)
                            {
                                newValue = "Prefab_Skill_Effects/Component_Effects/" + idMod + "/"
                                        + modInfo.NewSkin.ComponentEffectId + "/"
                                        + split[^1];
                            }
                            else if (!modInfo.NewSkin.IsAwakeSkin)
                            {
                                newValue = "prefab_skill_effects/hero_skill_effects/" + heroCodeName + "/" +
                                        idMod + "/" + split[^1];
                            }
                            else
                            {
                                newValue = "Prefab_Skill_Effects/Component_Effects/" + idMod + "/" + idMod + "_5/"
                                        + split[^1];
                            }
                            resourceName.SetAttribute("value", newValue);
                        }
                        List<string> ChildNameRemove =
                            ["ReplacementUsage", "ReplacementSubUsage", "bEnableOptCull", "bTrailProtect",
                            "bOnlySetAlpha", "bApplySpecialEffect"];
                        foreach (string childName in ChildNameRemove)
                        {
                            XmlNode? ReplacementUsage = eventNode.GetChildByAttribute("name", childName);
                            if (ReplacementUsage != null)
                                eventNode.RemoveChild(ReplacementUsage);
                        }
                    }
                    else if (eventType.Contains("PlayHeroSound"))
                    {
                        XmlNode? eventName = eventNode.GetChildByAttribute("name", "eventName");
                        value = eventName?.GetAttribute("value") ?? "";
                        if (!string.IsNullOrEmpty(value))
                        {
                            eventName.SetAttribute("value", value + "_Skin" + skin);
                        }
                    }
                    backXml.AppendActionNode(track);
                }
            }

            foreach (XmlNode track in baseTracks)
            {
                foreach (XmlNode condition in conditionDefaults)
                {
                    track.InsertChild(0, condition.CloneNode(true));
                }
            }

            backXml.AddComment("Mod By " + ModSources.ChannelName + "! Subscribe: " + ModSources.YtbLink + "  ");
            UpdateProgress("      - Back xml has " + backXml.GetActionNodes().Count + " track (max 200 track execute)");
            backXml.ResyncConditionIdWithGuid();
            File.WriteAllBytes(outputPath, AovTranslation.Compress(Encoding.UTF8.GetBytes(backXml.GetOuterXml())));
            //File.WriteAllBytes("E:/test.xml", (Encoding.UTF8.GetBytes(backXml.GetOuterXml())));
            Directory.CreateDirectory(Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero"));
            ZipDirectories(Directory.GetDirectories(cachedir2.FullName),
                    Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero/CommonActions.pkg.bytes"));
            Directory.Delete(cachedir2.FullName, true);
        }

        public void ModHaste(List<ModInfo> _modList)
        {
            ModHaste(_modList, null, false);
        }
        
        public void ModHaste(List<ModInfo> _modList, bool baseOnSkin)
        {
            ModHaste(_modList, null, baseOnSkin);
        }

        public void ModHaste(List<ModInfo> _modList, string? saveParent, bool baseOnSkin)
        {
            List<ModInfo> modList = _modList
                .Where((modInfo) => modInfo.ModSettings.ModBack && SkinLevels.GetSkinLevel(modInfo.NewSkin) >= (int)DefaultLevel.SS)
                .ToList();
            if (modList.Count == 0)
                return;
            UpdateProgress(" Dang mod hieu ung gia toc pack " + ModPackName);
            string inputZipPath = ModSources.ActionsParentPath + "CommonActions.pkg.bytes";
            string saveParentPath = saveParent ?? Path.Combine(ModSources.SaveModPath, MakeSimpleString(ModPackName),
                "Resources", ModSources.AovVersion);
            if (File.Exists(Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero/CommonActions.pkg.bytes")))
                inputZipPath = Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero/CommonActions.pkg.bytes");

            DirectoryInfo cachedir2 = Directory.CreateTempSubdirectory();
            Directory.CreateDirectory(cachedir2.FullName);
            ZipFile.ExtractToDirectory(inputZipPath, cachedir2.FullName);
            string[] filemodNames = ["commonresource/HasteE1.xml", "commonresource/HasteE1_leave.xml"];
            foreach (string filemodName in filemodNames)
            {
                string inputPath = Path.Combine(cachedir2.FullName, filemodName);
                string outputPath = inputPath;

                byte[] inputBytes = File.ReadAllBytes(inputPath);
                byte[]? outputBytes = AovTranslation.Decompress(inputBytes);
                if (outputBytes == null)
                    return;

                ActionsXml hasteXml = new();
                hasteXml.LoadFromText(Encoding.UTF8.GetString(outputBytes));
                hasteXml.ConvertVirtual2N();
                List<XmlNode>? actionNodes = hasteXml.GetActionNodes();

                List<XmlNode> animNodes = [];
                List<XmlNode> baseTracks = actionNodes.Where((node) =>
                    {
                        if (//!node.GetAttribute("eventType").Equals("TriggerParticleTick", CultureIgnoreCase) && 
                            !node.GetAttribute("eventType").Equals("TriggerParticle", CultureIgnoreCase))
                            return false;
                        List<KeyValuePair<XmlNode, bool>>? conditionTracks = hasteXml.GetConditionTracks(node);
                        if (conditionTracks == null)
                        {
                            return true;
                        }
                        for (int k = 0; k < conditionTracks.Count && k >= 0; k++)
                        {
                            XmlNode conditionTrack = conditionTracks[k].Key;
                            bool conditionStatus = conditionTracks[k].Value;
                            if (conditionTrack.GetAttribute("eventType") == "CheckSkinIdTick")
                            {
                                XmlNode eventNode = conditionTrack.GetChildrenByName("Event")[0];
                                XmlNode? skinIdParam = eventNode.GetChildByAttribute("name", "skinId");
                                XmlNode? bEqualParam = eventNode.GetChildByAttribute("name", "bEqual");
                                //Console.WriteLine(eventNode.GetChildByAttribute("name", "skinId").GetAttribute("value"));
                                bool CheckIdEqual = conditionStatus
                                    && (bEqualParam == null || bEqualParam.GetAttribute("value") == "true");
                                //Console.WriteLine(eventNode.GetChildByAttribute("name", "skinId").GetAttribute("value") + ": " + CheckIdEqual);
                                if (CheckIdEqual)
                                    return false;
                            }
                        }
                        return true;
                    }).ToList();
                List<XmlNode> CheckSkinIdTicks = actionNodes
                    .Where((node) => node.GetAttribute("eventType").Equals("CheckSkinIdTick", CultureIgnoreCase))
                    .ToList();

                List<XmlNode> conditionDefaults = [];
                for (int l = 0; l < modList.Count; l++)
                {
                    ModInfo modInfo = modList[l];
                    UpdateProgress($"    + Modding {Path.GetFileNameWithoutExtension(filemodName)} " + (l + 1) + "/" + modList.Count + ": " + modInfo.NewSkin);

                    int hero = int.Parse(modInfo.NewSkin.Id.ToString()[0..3]);

                    string heroCodeName = "";
                    using (ZipArchive zipArchive = ZipFile.OpenRead(ModSources.InfosParentPath + "Actor_" + hero + "_Infos.pkg.bytes"))
                    {
                        ZipArchiveEntry entryGetName;
                        int i = 0;
                        while ((entryGetName = zipArchive.Entries[i]).FullName.Split("/").Length < 2)
                            i++;
                        heroCodeName = entryGetName.FullName.Split("/")[1];
                    }
                    string id = modInfo.NewSkin.IsComponentSkin
                        ? modInfo.NewSkin.Id / 100 + ""
                        : modInfo.NewSkin.Id + "";
                    int skin = int.Parse(id[3..]) - 1;
                    int idMod = hero * 100 + skin;
                    string baseId = modInfo.OldSkins[0].IsComponentSkin
                        ? modInfo.OldSkins[0].Id / 100 + ""
                        : modInfo.OldSkins[0].Id + "";
                    int baseSkin = int.Parse(baseId[3..]) - 1;
                    int baseIdMod = int.Parse(baseId[..3]) * 100 + baseSkin;
                    string guid;
                    XmlNode CheckIdTick;
                    if (!baseOnSkin)
                    {
                        guid = "Mod_by_" + ModSources.ChannelName + "_Hero" + hero;
                        CheckIdTick = CustomNodeExtension.CreateCheckHeroIdTick(hero, guid);
                    }
                    else
                    {
                        guid = "Mod_by_" + ModSources.ChannelName + "_Skin" + baseIdMod;
                        CheckIdTick = CustomNodeExtension.CreateCheckSkinIdTick(baseIdMod, guid, 1, "target");
                    }
                    XmlNode conditionTrue = CustomNodeExtension.CreateEventCondition(l, guid, true);
                    XmlNode conditionFalse = CustomNodeExtension.CreateEventCondition(l, guid, false);
                    hasteXml.InsertActionNode(0, CheckIdTick);
                    conditionDefaults.Add(conditionFalse);

                    List<XmlNode> baseHasteTracks = [];
                    baseHasteTracks.AddRange(baseTracks);
                    foreach (XmlNode track in CheckSkinIdTicks)
                    {
                        XmlNode eventNode = track.GetChildrenByName("Event")[0];
                        XmlNode? skinId = eventNode.GetChildByAttribute("name", "skinId");
                        XmlNode? bEqual = eventNode.GetChildByAttribute("name", "bEqual");
                        if (skinId?.GetAttribute("value") == idMod.ToString())
                        {
                            List<KeyValuePair<XmlNode, bool>> nodes = hasteXml.GetTracksHasConditionGuid(track.GetAttribute("guid"));
                            foreach (var keypair in nodes)
                            {
                                var node = keypair.Key.CloneNode(true);
                                if (((bEqual == null || bEqual.GetAttribute("value").Equals("true")) && keypair.Value)
                                    || (bEqual.GetAttribute("value") == "false" && !keypair.Value))
                                {
                                    List<XmlNode>? cons = node.GetChildrenByName("Condition");
                                    if (cons != null)
                                    {
                                        foreach (XmlNode con in cons)
                                        {
                                            node.RemoveChild(con);
                                        }
                                    }
                                    baseHasteTracks.Add(node);
                                }
                                else
                                {
                                    baseHasteTracks = baseHasteTracks
                                        .Where((node) => node.GetAttribute("guid") != node.GetAttribute("guid"))
                                        .ToList();
                                }
                            }
                        }
                    }

                    foreach (XmlNode _track in baseHasteTracks)
                    {
                        XmlNode track = _track.CloneNode(true);
                        XmlNode eventNode = track.GetChildrenByName("Event")[0];
                        track.InsertChild(0, conditionTrue);
                        string eventType = track.GetAttribute("eventType") ?? "";
                        string value = "";
                        if (eventType.Contains("TriggerParticle"))
                        {
                            XmlNode? resourceName = eventNode.GetChildByAttribute("name", "resourceName");
                            if (string.IsNullOrEmpty(modInfo.NewSkin.HasteName))
                            {
                                value = resourceName?.GetAttribute("value") ?? "";
                            }
                            else
                            {
                                value = modInfo.NewSkin.HasteName;
                                XmlNode? bindPosOffset = eventNode.GetChildByAttribute("name", "bindPosOffset");
                                bindPosOffset?.SetAttribute("x", "0.000");
                                bindPosOffset?.SetAttribute("y", "0.000");
                                bindPosOffset?.SetAttribute("z", "0.000");
                            }
                            if (resourceName != null)
                            {
                                resourceName.SetAttribute("useRefParam", "false");
                                resourceName.SetAttribute("refParamName", "");
                                string[] split = value.Split("/");
                                string newValue;
                                if (modInfo.NewSkin.IsComponentSkin
                                        && modInfo.NewSkin.ComponentLevel >= (int)DefaultLevel.SS)
                                {
                                    newValue = "Prefab_Skill_Effects/Component_Effects/" + idMod + "/"
                                            + modInfo.NewSkin.ComponentEffectId + "/"
                                            + split[^1];
                                }
                                else if (!modInfo.NewSkin.IsAwakeSkin)
                                {
                                    newValue = "prefab_skill_effects/hero_skill_effects/" + heroCodeName + "/" +
                                            idMod + "/" + split[^1];
                                }
                                else
                                {
                                    newValue = "Prefab_Skill_Effects/Component_Effects/" + idMod + "/" + idMod + "_5/"
                                            + split[^1];
                                }
                                resourceName.SetAttribute("value", newValue);
                            }
                            List<string> ChildNameRemove =
                                ["ReplacementUsage", "ReplacementSubUsage", "bEnableOptCull", "bTrailProtect", "bOnlySetAlpha"];
                            foreach (string childName in ChildNameRemove)
                            {
                                XmlNode? ReplacementUsage = eventNode.GetChildByAttribute("name", childName);
                                if (ReplacementUsage != null)
                                    eventNode.RemoveChild(ReplacementUsage);
                            }
                        }
                        else if (eventType.Contains("PlayHeroSound"))
                        {
                            XmlNode? eventName = eventNode.GetChildByAttribute("name", "eventName");
                            value = eventName?.GetAttribute("value") ?? "";
                            if (!string.IsNullOrEmpty(value))
                            {
                                eventName.SetAttribute("value", value + "_Skin" + skin);
                            }
                        }
                        hasteXml.AppendActionNode(track);
                    }
                }

                foreach (XmlNode track in baseTracks)
                {
                    foreach (XmlNode condition in conditionDefaults)
                    {
                        track.InsertChild(0, condition.CloneNode(true));
                    }
                }

                hasteXml.AddComment("Mod By " + ModSources.ChannelName + "! Subscribe: " + ModSources.YtbLink + "  ");
                UpdateProgress($"      - {Path.GetFileNameWithoutExtension(filemodName)} xml has " + hasteXml.GetActionNodes().Count + " track (max 200 track execute)");
                hasteXml.ResyncConditionIdWithGuid();
                File.WriteAllBytes(outputPath, AovTranslation.Compress(Encoding.UTF8.GetBytes(hasteXml.GetOuterXml())));
                //File.WriteAllBytes($"E:/test{Path.GetFileNameWithoutExtension(filemodName)}.xml", (Encoding.UTF8.GetBytes(hasteXml.GetOuterXml())));
            }
            Directory.CreateDirectory(Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero"));
            ZipDirectories(Directory.GetDirectories(cachedir2.FullName),
                    Path.Combine(saveParentPath, "Ages/Prefab_Characters/Prefab_Hero/CommonActions.pkg.bytes"));
            Directory.Delete(cachedir2.FullName, true);
        }

        public void SpecialMod(Dictionary<string, ActionsXml> package, string specialCode)
        {
            string[] lines = specialCode.Split(["\n", "\r\n", "\r"], StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, Func<XmlNode, string[], object?>> commands = new()
            {
                ["where"] = (node, options) =>
                {
                    IEnumerable<XmlNode> children = node.ChildNodes.Cast<XmlNode>();
                    foreach (string option in options)
                    {
                        string[] splits = option.Split(' ');
                        string optionName = splits[0], optionValue = splits[1];
                        if (optionName == "-name")
                        {
                            children = children.Where(x => x.Name == optionValue);
                        }
                        else if (optionName == "-attr")
                        {
                            string[] split2 = optionValue.Split('=');
                            string attrName = split2[0], attrValue = split2[1];
                            children = children.Where(x => x.GetAttribute(attrName) == attrValue);
                        }
                    }
                    return children.FirstOrDefault();
                },
                ["set"] = (node, options) =>
                {
                    foreach (string option in options)
                    {
                        string[] splits = option.Split(' ');
                        string optionName = splits[0], optionValue = splits[1];
                        if (optionName == "-attr")
                        {
                            string[] split2 = optionValue.Split('=');
                            string attrName = split2[0], attrValue = split2[1];
                            node.SetAttribute(attrName, attrValue);
                        }
                    }
                    return null;
                },
                ["remove_this"] = (node, options) =>
                {
                    node.ParentNode?.RemoveChild(node);
                    return null;
                },
                ["remove_attr"] = (node, options) =>
                {
                    foreach (string option in options)
                    {
                        var attr = node.Attributes?[option];
                        if (attr != null)
                            node.Attributes?.Remove(attr);
                    }
                    return null;
                },
                ["append"] = (node, options) =>
                {
                    XmlDocument doc = new();
                    doc.LoadXml(options[0]);
                    XmlNode? child = doc.DocumentElement;
                    if (child != null && node.OwnerDocument != null)
                    {
                        node.AppendChild(node.OwnerDocument.ImportNode(child, true));
                    }
                    return null;
                },
                ["insert"] = (node, options) =>
                {
                    int index = -1;
                    foreach (string option in options)
                    {
                        string[] splits = option.Split(' ');
                        string optionName = splits[0], optionValue = splits[1];
                        if (optionName == "-i" || optionName == "-index")
                        {
                            index = int.Parse(optionValue);
                        }
                    }
                    XmlDocument doc = new();
                    doc.LoadXml(options[0]);
                    XmlNode? child = doc.DocumentElement;
                    if (child != null && node.OwnerDocument != null && index != -1)
                    {
                        node.InsertChild(index, node.OwnerDocument.ImportNode(child, true));
                    }
                    return null;
                }
            };
            ActionsXml? actions = null;
            Stack<string> indents = [];
            Stack<XmlNode> nodeStack = [];
            indents.Push("");
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim() == "")
                {
                    continue;
                }
                string line = lines[i].Replace("\t", "    ");
                string indent = line[..^line.TrimStart().Length];
                while (indents.Peek().Length >= indent.Length)
                {
                    indents.Pop();
                    if (nodeStack.Count > 0)
                        nodeStack.Pop();
                }
                if (indents.Peek().Length < indent.Length)
                {
                    indents.Push(indent);
                }
                line = line.Trim();
                string[] splits = (line[^1] == ':' ? line[..^1] : line).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string command;
                List<string> options = [];
                if (line.Contains('{'))
                {
                    splits = line.Split("{")[0].Trim().Split(' ');
                    if (line.Contains('}'))
                    {
                        options = [line[(line.IndexOf('{') + 1)..line.IndexOf('}')]];
                    }
                    else
                    {
                        options = [line[(line.IndexOf('{') + 1)..]];
                        while (!lines[i].Contains('}'))
                        {
                            options[0] += "\n" + lines[i];
                            i++;
                        }
                        options[0] += string.Concat("\n", lines[i].AsSpan(0, lines[i].IndexOf('}')));
                    }
                }
                command = splits[0];
                int checkOption = -1;
                options.AddRange([.. splits[1..].Select((split, i) => {
                    if (i>checkOption){
                        if (split.StartsWith('-')){
                            checkOption = i+1;
                            return split + " " + splits[i + 2];
                        }else{
                            return split;
                        }
                    }else{
                        return null;
                    }
                }).Where(s => s != null)]);
                //Trace.WriteLine($"{command}: {string.Join(", ", options)}");

                if (indents.Count == 1 && line[^1] == ':')
                {
                    string? key = package.Keys.Where(k => k.EndsWith(line[..^1], StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                    if (key != null)
                    {
                        actions = package[key];
                    }
                }
                if (actions != null && indents.Count > 1)
                {
                    XmlNode? node = null;
                    if (indents.Count == 2)
                    {
                        node = actions.GetActionNodes()?[0].ParentNode;
                    }
                    else
                    {
                        node = nodeStack.Peek();
                    }
                    if (node != null)
                    {
                        object? res = commands[command](node, [.. options]);
                        if (res is XmlNode found)
                        {
                            nodeStack.Push(found);
                        }
                    }
                }
            }
        }

        static void ZipDirectories(string[] directories, string zipFilePath)
        {
            using ICSharpCode.SharpZipLib.Zip.ZipOutputStream zipOutputStream = new(File.Create(zipFilePath));
            zipOutputStream.SetLevel(0); // Set compression level

            foreach (string folderPath in directories)
            {
                AddFolderToZip(folderPath, zipOutputStream, Path.GetFileName(folderPath));
            }

            //using Ionic.Zip.ZipFile zipFile = new();
            //zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.None;
            //foreach (string directory in directories)
            //{
            //    zipFile.AddDirectory(directory);
            //}
            //zipFile.Save(zipFilePath);


            //using FileStream zipToCreate = new(zipFilePath, FileMode.Create);
            //using ZipArchive archive = new(zipToCreate, ZipArchiveMode.Create);
            //foreach (string _directory in directories)
            //{
            //    string directory = _directory;
            //    while (directory.EndsWith('/') || directory.EndsWith('\\'))
            //    {
            //        directory = directory.Remove(directory.Length - 1);
            //    }
            //    DirectoryInfo dirInfo = new(directory);
            //    FileInfo[] files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);

            //    foreach (FileInfo file in files)
            //    {
            //        string entryName = file.FullName[(directory.Length + 1)..];
            //        archive.CreateEntryFromFile(file.FullName, entryName, CompressionLevel.NoCompression);
            //    }
            //}
        }

        static void AddFolderToZip(string folderPath, ICSharpCode.SharpZipLib.Zip.ZipOutputStream zipOutputStream, string entryName)
        {
            foreach (string file in Directory.GetFiles(folderPath))
            {
                ICSharpCode.SharpZipLib.Zip.ZipEntry entry = new(Path.Combine(entryName, Path.GetFileName(file)));
                zipOutputStream.PutNextEntry(entry);

                using FileStream fs = File.OpenRead(file);
                byte[] buffer = new byte[4096];
                int sourceBytes;
                do
                {
                    sourceBytes = fs.Read(buffer, 0, buffer.Length);
                    zipOutputStream.Write(buffer, 0, sourceBytes);
                } while (sourceBytes > 0);
            }

            foreach (string dir in Directory.GetDirectories(folderPath))
            {
                string dirName = Path.GetFileName(dir);
                AddFolderToZip(dir, zipOutputStream, Path.Combine(entryName, dirName));
            }
        }

        public static string MakeSimpleString(string s)
        {
            return new string([.. s.Where((s) => Char.IsLetterOrDigit(s))]);
        }
    }

    public class ModInfo(List<Skin> oldSkins, Skin newSkin, ModSettings modSettings)
    {
        public List<Skin> OldSkins = oldSkins;
        public Skin NewSkin = newSkin;
        public ModSettings ModSettings = modSettings;

        public override string ToString()
        {
            return $"{OldSkins.Count} skins to {NewSkin.Name}";
        }
    }

    public class ModMultiInfo(List<KeyValuePair<Skin, Skin>> skinChanges, ModSettings modSettings)
    {
        public List<KeyValuePair<Skin, Skin>> SkinChanges = skinChanges;
        public ModSettings ModSettings = modSettings;

        public override string ToString()
        {
            return string.Join(" | ", SkinChanges.Select(p => p.Key + "=>" + p.Value));
        }
    }

    public class ModSettings(bool modIcon, bool modInfo, bool modOrgan, bool modAction, bool modSound,
        bool modBack, bool modHaste, bool modMotion)
    {
        public bool ModIcon = modIcon;
        public bool ModInfo = modInfo;
        public bool ModOrgan = modOrgan;
        public bool ModAction = modAction;
        public bool ModSound = modSound;
        public bool ModBack = modBack;
        public bool ModHaste = modHaste;
        public bool ModMotion = modMotion;

        public static ModSettings AllEnable { get => new(true, true, true, true, true, true, true, true); }
    }
}
