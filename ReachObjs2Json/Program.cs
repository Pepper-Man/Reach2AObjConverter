using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bungie;
using Bungie.Tags;
using System.Text.Json;

namespace Reach2AObjConverter
{
    internal class ReachObjs2Json
    {
        public class ObjectDefinition
        {
            public string tag { get; set; }

            // Decal-specific values
            public class DecalSettings
            {
                public string baseRef { get; set; }
                public string alphaRef { get; set; }
                public string bumpRef { get; set; }
                public float[] tintColour { get; set; }
                public long blendMode { get; set; }
                public float[] scaleXY { get; set; }
                public float[] radius { get; set; }
            }

            public List<DecalSettings> decalSettings { get; set; }
            
            public ObjectDefinition()
            {
                decalSettings = new List<DecalSettings>();
            }
        }

        public class ObjectPlacement
        {
            public int typeIndex { get; set; }
            public int nameIndex { get; set; }
            public uint flags { get; set; }
            public float[] position { get; set; }
            public float[] rotation { get; set; }
            public float scale { get; set; }
            public string variantName { get; set; }
            public int ownerTeam { get; set; }

            // Sound scenery specific values
            public int volumeType { get; set; }
            public float height { get; set; }
            public float[] coneBounds { get; set; }
            public float coneGain { get; set; }
            public float obstrDistance {  get; set; }
            public float dntPlyDistance { get; set; }
            public float atkDistance { get; set; }
            public float minDistance { get; set; }
            public float susBegDistance { get; set; }
            public float susEndDistance { get; set; }
            public float maxDistance { get; set; }
            public float sustainDb {  get; set; }

            // Decal specific values
            public float scaleX { get; set; }
            public float scaleY { get; set; }

            // Weapon specific values
            public long roundsLeft { get; set; }
            public long roundsLoaded { get; set; }
            public uint weapFlags { get; set; }
        }

        public class TriggerVolume
        {
            public class SectorPoint
            {
                public float[] position { get; set; }
                public float[] normal { get; set; }
            }

            public string name { get; set; }
            public int objNameIndex { get; set; }
            public long nodeIndex { get; set; }
            public string nodeName { get; set; }
            public int type { get; set; }
            public float[] forward { get; set; }
            public float[] up { get; set; }
            public float[] position { get; set; }
            public float[] extents { get; set; }
            public float zSink { get; set; }
            public List<SectorPoint> sectorPoints { get; set; }
            public int killTrigVol { get; set; }

            public TriggerVolume()
            {
                sectorPoints = new List<SectorPoint>();
            }
        }

        public class ResultsContainer
        {
            public List<ObjectDefinition> definitions { get; set; }
            public List<ObjectPlacement> placements { get; set; }
        }

        public class ObjectDataContainer
        {
            public List<ObjectDefinition> sceneryDefinitions { get; set; }
            public List<ObjectPlacement> sceneryPlacements { get; set; }
            public List<ObjectDefinition> vehicleDefinitions { get; set; }
            public List<ObjectPlacement> vehiclePlacements { get; set; }
            public List<ObjectDefinition> equipmentDefinitions { get; set; }
            public List<ObjectPlacement> equipmentPlacements { get; set; }
            public List<ObjectDefinition> soundscenDefinitions { get; set; }
            public List<ObjectPlacement> soundscenPlacements { get; set; }
            public List<TriggerVolume> triggerVolumes { get; set; }
            public List<ObjectDefinition> crateDefinitions { get; set; }
            public List<ObjectPlacement> cratePlacements { get; set; }
            public List<ObjectDefinition> decalDefinitions { get; set; }
            public List<ObjectPlacement> decalPlacements { get; set; }
            public List<ObjectDefinition> weaponDefinitions { get; set; }
            public List<ObjectPlacement> weaponPlacements { get; set; }
        }

        public class ScenarioSplitResContainer
        {
            public TagPath sceneryResource {  get; set; }
            public TagPath bipedResource { get; set; }
            public TagPath vehicleResource { get; set; }
            public TagPath equipmentResource { get; set; }
            public TagPath weaponResource { get; set; }
            public TagPath deviceResource { get; set; }
            public TagPath effscenResource { get; set; }
            public TagPath decalResource { get; set; }
            public TagPath trigvolResource { get; set; }
            public TagPath soundscenResource { get; set; }
            public TagPath decoratorResource { get; set; }
        }

        public class FilePathSanitiser
        {
            // Define the invalid path characters
            private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

            public static string SanitiseFilePath(string input)
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Input file path cannot be null or whitespace.");
                    return "";
                }

                // Trim whitespace and quotes
                string sanitisedPath = input.Trim().Trim('"');

                // Check for invalid characters
                if (sanitisedPath.IndexOfAny(InvalidPathChars) >= 0)
                {
                    Console.WriteLine("Input file path contains invalid characters.");
                    return "";
                }

                // Get the absolute path to ensure it's well-formed
                try
                {
                    sanitisedPath = Path.GetFullPath(sanitisedPath);
                }
                catch
                {
                    Console.WriteLine("Input file path is not valid.");
                    return "";
                }

                // Check file exists
                if (!File.Exists(sanitisedPath))
                {
                    Console.WriteLine("Input file does not exist.");
                    return "";
                }

                // Check file is in HREK
                if (!sanitisedPath.Contains("HREK\\tags"))
                {
                    Console.WriteLine("Input file is not in the HREK tags folder.");
                    return "";
                }

                // Check correct tag type
                if (Path.GetExtension(sanitisedPath) != ".scenario")
                {
                    Console.WriteLine("Input file is not a .scenario tag.");
                    return "";
                }

                return sanitisedPath;
            }
        }

        static void Main(string[] args)
        {
            string userTagPath;
            /*
            while (true)
            {
                Console.WriteLine("Enter full path to Reach .scenario tag:\n");
                string userInput = Console.ReadLine();
                userTagPath = FilePathSanitiser.SanitiseFilePath(userInput);
                if (userTagPath != "")
                {
                    Console.WriteLine("Valid path entered");
                    break;
                }
            }
            */

            userTagPath = "I:\\SteamLibrary\\steamapps\\common\\HREK\\tags\\levels\\dlc\\cex_headlong\\cex_headlong.scenario";

            // Get HREK path
            int hrekIndex = userTagPath.IndexOf("HREK");
            string hrek = userTagPath.Substring(0, hrekIndex + 4);

            // Get relative tag path
            string relativePath = Path.ChangeExtension(userTagPath.Substring(hrekIndex + 10), null);


            // ManagedBlam Initialisation
            void callback(ManagedBlamCrashInfo LambdaExpression) { }
            ManagedBlamStartupParameters startupParams = new ManagedBlamStartupParameters
            {
                InitializationLevel = InitializationType.TagsOnly
            };
            ManagedBlamSystem.Start(hrek, callback, startupParams);

            var tagFile = new TagFile();
            var tagPath = TagPath.FromPathAndExtension(relativePath, "scenario");

            // List initialisation
            List<ObjectDefinition> scenDefData = new List<ObjectDefinition>();
            List<ObjectPlacement> scenPlaceData = new List<ObjectPlacement>();
            List<ObjectDefinition> vehiDefData = new List<ObjectDefinition>();
            List<ObjectPlacement> vehiPlaceData = new List<ObjectPlacement>();
            List<ObjectDefinition> eqipDefData = new List<ObjectDefinition>();
            List<ObjectPlacement> eqipPlaceData = new List<ObjectPlacement>();
            List<ObjectDefinition> ssceDefData = new List<ObjectDefinition>();
            List<ObjectPlacement> sscePlaceData = new List<ObjectPlacement>();
            List<TriggerVolume> trigVolData = new List<TriggerVolume>();
            List<ObjectDefinition> crateDefData = new List<ObjectDefinition>();
            List<ObjectPlacement> cratePlaceData = new List<ObjectPlacement>();
            List<ObjectDefinition> decalDefData = new List<ObjectDefinition>();
            List<ObjectPlacement> decalPlaceData = new List<ObjectPlacement>();
            List<ObjectDefinition> weapDefData = new List<ObjectDefinition>();
            List<ObjectPlacement> weapPlaceData = new List<ObjectPlacement>();

            try
            {
                tagFile.Load(tagPath);
                Console.WriteLine("Tagfile opened\nReading scenario object data:\n");

                // Get tag resources
                ScenarioSplitResContainer scenarioResTags = new ScenarioSplitResContainer
                {
                    sceneryResource = ((TagFieldReference)tagFile.SelectField($"Block:scenario resources[0]/Block:new split resources[0]/Reference:scenery resource")).Path,
                    bipedResource = ((TagFieldReference)tagFile.SelectField($"Block:scenario resources[0]/Block:new split resources[0]/Reference:biped resource")).Path,
                    vehicleResource = ((TagFieldReference)tagFile.SelectField($"Block:scenario resources[0]/Block:new split resources[0]/Reference:vehicle resource")).Path,
                    equipmentResource = ((TagFieldReference)tagFile.SelectField($"Block:scenario resources[0]/Block:new split resources[0]/Reference:equipment resource")).Path,
                    weaponResource = ((TagFieldReference)tagFile.SelectField($"Block:scenario resources[0]/Block:new split resources[0]/Reference:weapon resource")).Path,
                    deviceResource = ((TagFieldReference)tagFile.SelectField($"Block:scenario resources[0]/Block:new split resources[0]/Reference:device resource")).Path,
                    effscenResource = ((TagFieldReference)tagFile.SelectField($"Block:scenario resources[0]/Block:new split resources[0]/Reference:effect scenery")).Path,
                    decalResource = ((TagFieldReference)tagFile.SelectField($"Block:scenario resources[0]/Block:new split resources[0]/Reference:decal resource")).Path,
                    trigvolResource = ((TagFieldReference)tagFile.SelectField($"Block:scenario resources[0]/Block:new split resources[0]/Reference:trigger volume resource")).Path,
                    soundscenResource = ((TagFieldReference)tagFile.SelectField($"Block:scenario resources[0]/Block:new split resources[0]/Reference:sound scenery resource")).Path,
                    decoratorResource = ((TagFieldReference)tagFile.SelectField($"Block:scenario resources[0]/Block:new split resources[0]/Reference:decorator resource")).Path
                };

                ResultsContainer GetResultsContainer(TagPath resource, string tagType)
                {
                    if (resource == null)
                    {
                        return GetObjectData(tagFile, tagType, "scenario");
                    }
                    else
                    {
                        using (TagFile resourceTag = new TagFile(resource))
                        {
                            return GetObjectData(resourceTag, tagType, "resource");
                        }
                    }
                }

                // SCENERY //
                ResultsContainer scenData = GetResultsContainer(scenarioResTags.sceneryResource, "scenerys");
                scenDefData = scenData.definitions;
                scenPlaceData = scenData.placements;

                // VEHICLES //
                ResultsContainer vehiData = GetResultsContainer(scenarioResTags.vehicleResource, "vehicles");
                vehiDefData = vehiData.definitions;
                vehiPlaceData = vehiData.placements;
                
                // EQUIPMENT //
                ResultsContainer eqipData = GetResultsContainer(scenarioResTags.equipmentResource, "equipments");
                eqipDefData = eqipData.definitions;
                eqipPlaceData = eqipData.placements;

                // SOUND SCENERY //
                ResultsContainer ssceData = GetResultsContainer(scenarioResTags.soundscenResource, "sound_scenerys");
                ssceDefData = ssceData.definitions;
                sscePlaceData = ssceData.placements;

                // TRIGGER VOLUMES //
                List<TriggerVolume> trigvolData;
                if (scenarioResTags.trigvolResource == null)
                {
                    trigVolData = GetTrigVolData(tagFile);
                }
                else
                {
                    TagFile trigvolTag = new TagFile(scenarioResTags.trigvolResource);
                    trigvolData = GetTrigVolData(trigvolTag);
                    trigvolTag.Dispose();
                }

                // CRATES (Crate data is stored in the scenery resource tag, if it exists) //
                ResultsContainer crateData = GetResultsContainer(scenarioResTags.sceneryResource, "crates");
                crateDefData = crateData.definitions;
                cratePlaceData = crateData.placements;

                // DECALS //
                ResultsContainer decalData = GetResultsContainer(scenarioResTags.decalResource, "decals");
                decalDefData = decalData.definitions;
                decalPlaceData = decalData.placements;

                // WEAPONS //
                ResultsContainer weapData = GetResultsContainer(scenarioResTags.weaponResource, "weapons");
                weapDefData = weapData.definitions;
                weapPlaceData = weapData.placements;
            }
            catch
            {
                Console.WriteLine("Unknown managedblam error");
            }
            finally
            {
                // Gracefully close tag file
                tagFile.Dispose();
                Console.WriteLine("\nTagfile closed\n\n");

                ObjectDataContainer objectDataContainer = new ObjectDataContainer
                {
                    sceneryDefinitions = scenDefData,
                    sceneryPlacements = scenPlaceData,
                    vehicleDefinitions = vehiDefData,
                    vehiclePlacements = vehiPlaceData,
                    equipmentDefinitions = eqipDefData,
                    equipmentPlacements = eqipPlaceData,
                    soundscenDefinitions = ssceDefData,
                    soundscenPlacements = sscePlaceData,
                    triggerVolumes = trigVolData,
                    crateDefinitions = crateDefData,
                    cratePlacements = cratePlaceData,
                    decalDefinitions = decalDefData,
                    decalPlacements = decalPlaceData,
                    weaponDefinitions = weapDefData,
                    weaponPlacements = weapPlaceData
                };

                // Serialize to JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                string json = JsonSerializer.Serialize(objectDataContainer, options);

                // Write JSON to file
                string filePath = Path.Combine(AppContext.BaseDirectory, $"{Path.GetFileName(relativePath)}_objectdata.json");
                File.WriteAllText(filePath, json);

                Console.WriteLine($"JSON data written to {filePath}");
            }

            Console.WriteLine("\nPress enter to exit");
            Console.ReadLine();
            ManagedBlamSystem.Stop();
        }
    
        public static ResultsContainer GetObjectData(TagFile tagFile, string objectType, string tagType)
        {
            ResultsContainer results = new ResultsContainer();
            List<ObjectDefinition> objDefData = new List<ObjectDefinition>();
            List<ObjectPlacement> objPlaceData = new List<ObjectPlacement>();
            Console.WriteLine($"\n--- {objectType.ToUpper()} ---\n");

            // Dictionary to handle the annoying differences in block names between scenario and resource tags
            Dictionary<Tuple<string, string>, string> blockNameMapping = new Dictionary<Tuple<string, string>, string>
            {
                { Tuple.Create("scenerys", "scenario"), "scenery" },
                { Tuple.Create("scenerys", "resource"), "scenerys" },
                { Tuple.Create("vehicles", "scenario"), "vehicles" },
                { Tuple.Create("vehicles", "resource"), "vehicles" },
                { Tuple.Create("equipments", "scenario"), "equipment" },
                { Tuple.Create("equipments", "resource"), "equipments" },
                { Tuple.Create("sound_scenerys", "scenario"), "sound scenery" },
                { Tuple.Create("sound_scenerys", "resource"), "sound_scenerys" },
                { Tuple.Create("sound_scenerys_palette", "resource"), "sound_scenery" },
                { Tuple.Create("sound_scenerys_palette", "scenario"), "sound scenery" },
                { Tuple.Create("crates", "scenario"), "crates" },
                { Tuple.Create("crates", "resource"), "crates" },
                { Tuple.Create("decals", "scenario"), "decals" },
                { Tuple.Create("decals", "resource"), "decals" },
                { Tuple.Create("decals_palette", "scenario"), "decal " },
                { Tuple.Create("decals_palette", "resource"), "" },
                { Tuple.Create("weapons", "scenario"), "weapons" },
                { Tuple.Create("weapons", "resource"), "weapons" }
            };

            // Get total number of object definitions
            int objDefCount;
            if (objectType == "decals")
            {
                objDefCount = ((TagFieldBlock)tagFile.SelectField($"Block:{blockNameMapping[Tuple.Create("decals_palette", tagType)]}palette")).Elements.Count();
            }
            else if (objectType == "sound_scenerys")
            {
                objDefCount = ((TagFieldBlock)tagFile.SelectField($"Block:{blockNameMapping[Tuple.Create("sound_scenerys_palette", tagType)]} palette")).Elements.Count();
            }
            else
            {
                objDefCount = ((TagFieldBlock)tagFile.SelectField($"Block:{objectType.Substring(0, objectType.Length - 1)} palette")).Elements.Count();
            }
            
            // Get all object definition data
            for (int i = 0; i < objDefCount; i++)
            {
                ObjectDefinition objDef = new ObjectDefinition();
                Console.WriteLine($"{objectType} definition {i}:");
                TagPath path;

                if (objectType == "decals")
                {
                    path = ((TagFieldReference)tagFile.SelectField($"Block:{blockNameMapping[Tuple.Create("decals_palette", tagType)]}palette[{i}]/Reference:reference")).Path;
                    Console.WriteLine($"\tTag path: {path}\n");
                    objDef.tag = path.RelativePath;
                    objDef = GetDecalShaderData(objDef, path);
                }
                else if (objectType == "sound_scenerys")
                {
                    path = ((TagFieldReference)tagFile.SelectField($"Block:{blockNameMapping[Tuple.Create("sound_scenerys_palette", tagType)]} palette[{i}]/Reference:name")).Path;
                    Console.WriteLine($"\tTag path: {path}\n");
                    objDef.tag = path.RelativePath;
                }
                else
                {
                    path = ((TagFieldReference)tagFile.SelectField($"Block:{objectType.Substring(0, objectType.Length - 1)} palette[{i}]/Reference:name")).Path;
                    Console.WriteLine($"\tTag path: {path}\n");
                    objDef.tag = path.RelativePath;
                }
                
                objDefData.Add(objDef);
            }

            string blockString = blockNameMapping[Tuple.Create(objectType, tagType)];

            // Get total number of placed objects
            int objPlacedCount = ((TagFieldBlock)tagFile.SelectField($"Block:{blockString}")).Elements.Count();

            // Get all object data
            for (int i = 0; i < objPlacedCount; i++)
            {
                ObjectPlacement objPlacement = new ObjectPlacement();
                Console.WriteLine($"{blockNameMapping[Tuple.Create(objectType, "scenario")]} placement {i}:");
                
                if (objectType != "decals")
                {
                    int type = ((TagFieldBlockIndex)tagFile.SelectField($"Block:{blockString}[{i}]/ShortBlockIndex:type")).Value;
                    Console.WriteLine($"\tType index: {type}");
                    objPlacement.typeIndex = type;

                    int name = ((TagFieldBlockIndex)tagFile.SelectField($"Block:{blockString}[{i}]/ShortBlockIndex:name")).Value;
                    Console.WriteLine($"\tName index: {name}");
                    objPlacement.nameIndex = name;

                    uint flags = ((TagFieldFlags)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:object data/Flags:placement flags")).RawValue;
                    Console.WriteLine($"\tFlags: {type}");
                    objPlacement.flags = flags;

                    float scale = ((TagFieldElementSingle)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:object data/Real:scale")).Data;
                    Console.WriteLine($"\tScale: {scale}");
                    objPlacement.scale = scale;

                    float[] pos = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:object data/RealPoint3d:position")).Data;
                    Console.WriteLine($"\tPosition: {pos[0]}, {pos[1]}, {pos[2]}");
                    objPlacement.position = pos;

                    float[] rot = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:object data/RealEulerAngles3d:rotation")).Data;
                    Console.WriteLine($"\tRotation: {rot[0]}, {rot[1]}, {rot[2]}");
                    objPlacement.rotation = rot;

                    if (blockString != "equipments" && blockString != "equipment" && blockString != "sound scenery" && blockString != "sound_scenerys")
                    {
                        string variant = ((TagFieldElementStringID)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:permutation data/StringID:variant name")).Data;
                        Console.WriteLine($"\tVariant name: {variant}");
                        objPlacement.variantName = variant;
                    }

                    int team = 8; // Don't set default as -1, will crash MB if it gets written to tag. 8 = neutral team
                    if (objectType == "sound_scenerys" || objectType == "sound scenery")
                    {
                        int volType = ((TagFieldEnum)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:sound_scenery/LongEnum:volume type")).Value;
                        Console.WriteLine($"\tVolume type: {volType}");
                        objPlacement.volumeType = volType;

                        float height = ((TagFieldElementSingle)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:sound_scenery/Real:height")).Data;
                        Console.WriteLine($"\tHeight: {height}");
                        objPlacement.height = height;

                        float[] coneBounds = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:sound_scenery/AngleBounds:override cone angle bounds")).Data;
                        Console.WriteLine($"\tCone bounds: {coneBounds[0]}, {coneBounds[1]}");
                        objPlacement.coneBounds = coneBounds;

                        float coneGain = ((TagFieldElementSingle)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:sound_scenery/Real:override outer cone gain")).Data;
                        Console.WriteLine($"\tCone gain: {coneGain}");
                        objPlacement.coneGain = coneGain;

                        float dntObstrDist = ((TagFieldElementSingle)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:don't obstruct distance")).Data;
                        Console.WriteLine($"\tDon't obstruct distance: {dntObstrDist}");
                        objPlacement.obstrDistance = dntObstrDist;

                        float dntPlayDist = ((TagFieldElementSingle)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:don't play distance")).Data;
                        Console.WriteLine($"\tDon't play distance: {dntPlayDist}");
                        objPlacement.dntPlyDistance = dntPlayDist;

                        float attackDist = ((TagFieldElementSingle)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:attack distance")).Data;
                        Console.WriteLine($"\tAttack distance: {attackDist}");
                        objPlacement.atkDistance = attackDist;

                        float minDist = ((TagFieldElementSingle)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:minimum distance")).Data;
                        Console.WriteLine($"\tMinimum distance: {minDist}");
                        objPlacement.minDistance = minDist;

                        float susBegDist = ((TagFieldElementSingle)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:sustain begin distance")).Data;
                        Console.WriteLine($"\tSustain begin distance: {susBegDist}");
                        objPlacement.susBegDistance = susBegDist;

                        float susEndDist = ((TagFieldElementSingle)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:sustain end distance")).Data;
                        Console.WriteLine($"\tSustain end distance: {susEndDist}");
                        objPlacement.susEndDistance = susEndDist;

                        float maxDist = ((TagFieldElementSingle)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:maximum distance")).Data;
                        Console.WriteLine($"\tMaximum distance: {maxDist}");
                        objPlacement.maxDistance = maxDist;

                        float susDb = ((TagFieldElementSingle)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:sustain db")).Data;
                        Console.WriteLine($"\tSustain Db: {susDb}");
                        objPlacement.sustainDb = susDb;
                    }
                    else if (objectType == "weapons")
                    {
                        long rndLeft = ((TagFieldElementInteger)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:weapon data/ShortInteger:rounds left")).Data;
                        Console.WriteLine($"\tRounds left: {rndLeft}");
                        objPlacement.roundsLeft = rndLeft;

                        long rndLoad = ((TagFieldElementInteger)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:weapon data/ShortInteger:rounds loaded")).Data;
                        Console.WriteLine($"\tRounds loaded: {rndLoad}");
                        objPlacement.roundsLoaded = rndLoad;

                        uint weapFlags = ((TagFieldFlags)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:weapon data/Flags:flags")).RawValue;
                        Console.WriteLine($"\tWeapon flags: {weapFlags}");
                        objPlacement.weapFlags = weapFlags;

                        team = ((TagFieldEnum)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:multiplayer data/CharEnum:owner team")).Value;
                    }
                    else
                    {
                        team = ((TagFieldEnum)tagFile.SelectField($"Block:{blockString}[{i}]/Struct:multiplayer data/CharEnum:owner team")).Value;
                    }
                    Console.WriteLine($"\tOwner team: {team}\n");
                    objPlacement.ownerTeam = team;
                }
                else
                {
                    // Decal-specific data
                    int type = ((TagFieldBlockIndex)tagFile.SelectField($"Block:{objectType}[{i}]/ShortBlockIndex:decal palette index")).Value;
                    Console.WriteLine($"\tType index: {type}");
                    objPlacement.typeIndex = type;

                    float[] pos = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{objectType}[{i}]/RealPoint3d:position")).Data;
                    Console.WriteLine($"\tPosition: {pos[0]}, {pos[1]}, {pos[2]}");
                    objPlacement.position = pos;

                    float[] rot = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{objectType}[{i}]/RealQuaternion:rotation")).Data;
                    Console.WriteLine($"\tRotation: {rot[0]}, {rot[1]}, {rot[2]}");
                    objPlacement.rotation = rot;

                    float scaleX = ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Real:scale x")).Data;
                    Console.WriteLine($"\tScale X: {scaleX}");
                    objPlacement.scaleX = scaleX;

                    float scaleY = ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Real:scale y")).Data;
                    Console.WriteLine($"\tScale Y: {scaleY}");
                    objPlacement.scaleY = scaleY;
                }

                objPlaceData.Add(objPlacement);
            }

            results.definitions = objDefData;
            results.placements = objPlaceData;
            return results;
        }
    
        public static List<TriggerVolume> GetTrigVolData(TagFile tagFile)
        {
            Console.WriteLine("\n--- TRIGGER VOLUMES ---\n");
            List<TriggerVolume> triggerVolumes = new List<TriggerVolume>();

            // Get total number of trigger volumes
            int volCount = ((TagFieldBlock)tagFile.SelectField($"Block:trigger volumes")).Elements.Count();

            // Get all object trigger volume data
            for (int i = 0; i < volCount; i++)
            {
                TriggerVolume trigVol = new TriggerVolume();
                Console.WriteLine($"\nTrigger volume {i}:");

                trigVol.name = ((TagFieldElementStringID)tagFile.SelectField($"Block:trigger volumes[{i}]/StringID:name")).Data;
                Console.WriteLine($"\tName: {trigVol.name}");

                trigVol.objNameIndex = ((TagFieldBlockIndex)tagFile.SelectField($"Block:trigger volumes[{i}]/ShortBlockIndex:object name")).Value;
                Console.WriteLine($"\tObject name index: {trigVol.objNameIndex}");

                trigVol.nodeIndex = ((TagFieldElementInteger)tagFile.SelectField($"Block:trigger volumes[{i}]/ShortInteger:runtime node index")).Data;
                Console.WriteLine($"\tRuntime node index: {trigVol.nodeIndex}");

                trigVol.nodeName = ((TagFieldElementStringID)tagFile.SelectField($"Block:trigger volumes[{i}]/StringID:node name")).Data;
                Console.WriteLine($"\tNode name: {trigVol.nodeName}");

                trigVol.type = ((TagFieldEnum)tagFile.SelectField($"Block:trigger volumes[{i}]/ShortEnum:type")).Value;
                Console.WriteLine($"\tType: {trigVol.type}");

                trigVol.forward = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:trigger volumes[{i}]/RealVector3d:forward")).Data;
                Console.WriteLine($"\tForward: {trigVol.forward[0]}, {trigVol.forward[1]}, {trigVol.forward[2]}");

                trigVol.up = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:trigger volumes[{i}]/RealVector3d:up")).Data;
                Console.WriteLine($"\tUp: {trigVol.up[0]}, {trigVol.up[1]}, {trigVol.up[2]}");

                trigVol.position = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:trigger volumes[{i}]/RealPoint3d:position")).Data;
                Console.WriteLine($"\tPosition: {trigVol.position[0]}, {trigVol.position[1]}, {trigVol.position[2]}");

                trigVol.extents = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:trigger volumes[{i}]/RealPoint3d:extents")).Data;
                Console.WriteLine($"\tExtents: {trigVol.extents[0]}, {trigVol.extents[1]}, {trigVol.extents[2]}");

                trigVol.zSink = ((TagFieldElementSingle)tagFile.SelectField($"Block:trigger volumes[{i}]/Real:z sink")).Data;
                Console.WriteLine($"\tZ sink: {trigVol.zSink}");

                // Get total number of sector points, then read data
                int sectorCount = ((TagFieldBlock)tagFile.SelectField($"Block:trigger volumes[{i}]/Block:sector points")).Elements.Count();
                for (int j = 0; j < sectorCount; j++)
                {
                    TriggerVolume.SectorPoint sectorPoint = new TriggerVolume.SectorPoint();
                    Console.WriteLine($"\tSector point {j}:");

                    sectorPoint.position = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:trigger volumes[{i}]/Block:sector points[{j}]/RealPoint3d:position")).Data;
                    Console.WriteLine($"\t\tPosition: {sectorPoint.position[0]}, {sectorPoint.position[1]}, {sectorPoint.position[2]}");

                    sectorPoint.normal = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:trigger volumes[{i}]/Block:sector points[{j}]/RealEulerAngles2d:normal")).Data;
                    Console.WriteLine($"\t\tNormal: {sectorPoint.normal[0]}, {sectorPoint.normal[1]}");

                    trigVol.sectorPoints.Add(sectorPoint);
                }

                trigVol.killTrigVol = ((TagFieldBlockIndex)tagFile.SelectField($"Block:trigger volumes[{i}]/ShortBlockIndex:kill trigger volume")).Value;
                Console.WriteLine($"\tKill trigger volume index: {trigVol.killTrigVol}");

                triggerVolumes.Add(trigVol);
            }

            return triggerVolumes;
        }
    
        public static ObjectDefinition GetDecalShaderData(ObjectDefinition decalData, TagPath decalPath)
        {
            var decalFile = new TagFile();
            List<ObjectDefinition.DecalSettings> allDecalSettings = new List<ObjectDefinition.DecalSettings>();

            try
            {
                decalFile.Load(decalPath);
                Console.WriteLine($"\tDecal tag \"{decalPath.RelativePath}.decal_system\" opened\n\tReading shader data:\n");

                // Get total number of decals in decal system
                int decalCount = ((TagFieldBlock)decalFile.SelectField($"Block:decals")).Elements.Count();

                for (int i = 0; i < decalCount; i++)
                {
                    // Need to determine if selected albedo option can have an alpha map
                    Dictionary<long, string> shaderHasAlpha = new Dictionary<long, string>()
                    {
                        { 0, "no" },
                        { 1, "no"},
                        { 2, "yes"},
                        { 3, "yes" },
                        { 4, "no" },
                        { 5, "no" },
                        { 6, "yes" },
                        { 7, "yes" },
                        { 8, "no" },
                        { 9, "no" },
                        { 10, "yes" }
                    };


                    Console.WriteLine($"\tDecal {i}:");
                    ObjectDefinition.DecalSettings decalSettings = new ObjectDefinition.DecalSettings();
                    // Get total number of parameteres in decal shader
                    int paramCount = ((TagFieldBlock)decalFile.SelectField($"Block:decals[{i}]/Struct:actual shader?/Block:parameters")).Elements.Count();

                    // Get albedo setting
                    long albedoType = ((TagFieldElementInteger)decalFile.SelectField($"Block:decals[{i}]/Struct:actual shader?/Block:options[0]/ShortInteger:short")).Data;

                    // Get blend mode
                    long blendMode = ((TagFieldElementInteger)decalFile.SelectField($"Block:decals[{i}]/Struct:actual shader?/Block:options[1]/ShortInteger:short")).Data;
                    decalSettings.blendMode = blendMode;

                    // Get tinting setting
                    long tintType = ((TagFieldElementInteger)decalFile.SelectField($"Block:decals[{i}]/Struct:actual shader?/Block:options[5]/ShortInteger:short")).Data;

                    // Determine if shader is using bump mapping
                    bool shaderHasBump = false;
                    if (((TagFieldElementInteger)decalFile.SelectField($"Block:decals[{i}]/Struct:actual shader?/Block:options[4]/ShortInteger:short")).Data != 0)
                    {
                        shaderHasBump = true;
                    }

                    for (int j = 0; j < paramCount; j++)
                    {
                        string paramName = ((TagFieldElementStringID)decalFile.SelectField($"Block:decals[{i}]/Struct:actual shader?/Block:parameters[{j}]/StringID:parameter name")).Data;

                        if (paramName == "base_map")
                        {
                            // Base map
                            string baseMap = ((TagFieldReference)decalFile.SelectField($"Block:decals[{i}]/Struct:actual shader?/Block:parameters[{j}]/Reference:bitmap")).Path.RelativePath;
                            Console.WriteLine($"\t\tBase map: {baseMap}");
                            decalSettings.baseRef = baseMap;
                        }
                        else if (paramName == "alpha_map" && shaderHasAlpha[albedoType] == "yes")
                        {
                            // Alpha map, only get data if shader is currently set to use alpha map
                            string alphaMap = ((TagFieldReference)decalFile.SelectField($"Block:decals[{i}]/Struct:actual shader?/Block:parameters[{j}]/Reference:bitmap")).Path.RelativePath;
                            Console.WriteLine($"\t\tAlpha map: {alphaMap}");
                            decalSettings.alphaRef = alphaMap;
                        }
                        else if (paramName == "tint_color" && tintType != 0)
                        {
                            // Tint colour, only get data if shader is currently using tinting
                            var colourData = ((TagFieldCustomFunctionEditor)decalFile.SelectField($"Block:decals[{i}]/Struct:actual shader?/Block:parameters[{j}]/Block:animated parameters[0]/Custom:animation function")).Value.GetColor(0);
                            float[] colours = { colourData.Red, colourData.Green, colourData.Blue };
                            Console.WriteLine($"\t\tTint colour: {colours[0]}, {colours[1]}, {colours[2]}");
                            decalSettings.tintColour= colours;
                        }
                        else if (paramName == "bump_map" && shaderHasBump)
                        {
                            // Bump map, only get data if shader is currently set to use bump mapping
                            string bumpMap = ((TagFieldReference)decalFile.SelectField($"Block:decals[{i}]/Struct:actual shader?/Block:parameters[{j}]/Reference:bitmap")).Path.RelativePath;
                            Console.WriteLine($"\t\tBump map: {bumpMap}");
                            decalSettings.bumpRef = bumpMap;
                        }
                        else
                        {
                            // Dunno
                            Console.WriteLine("\tUnused paramter, ignore");
                            continue;
                        }
                    }

                    // Need to determine decal system scaling if diffuse bitmap is not square
                    if (decalSettings.baseRef != null)
                    {
                        TagPath diffusePath = TagPath.FromPathAndExtension(decalSettings.baseRef, "bitmap");

                        using (TagFile diffuseTag = new TagFile(diffusePath))
                        {
                            long width = ((TagFieldElementInteger)diffuseTag.SelectField($"Block:bitmaps[0]/ShortInteger:width")).Data;
                            long height = ((TagFieldElementInteger)diffuseTag.SelectField($"Block:bitmaps[0]/ShortInteger:height")).Data;
                            // Assume bitmap is square until proven otherwise
                            float scaleX = 1;
                            float scaleY = 1;

                            if (height > width)
                            {
                                // Bitmap is thin and long
                                scaleX = (float)width / height;
                            }
                            else if (width > height)
                            {
                                // Bitmap is wide and short
                                scaleY = (float)height / width;
                            }

                            decalSettings.scaleXY = new float[] { scaleX, scaleY };
                        }
                    }

                    // Get radius
                    decalSettings.radius = ((TagFieldElementArraySingle)decalFile.SelectField($"Block:decals[{i}]/RealBounds:radius")).Data;

                    allDecalSettings.Add(decalSettings);
                }
            }
            catch
            {
                Console.WriteLine($"\tManagedBlam error when reading \"{decalPath.RelativePath}.decal_system\"");
            }
            finally
            {
                decalFile.Dispose();
                decalData.decalSettings = allDecalSettings;
                Console.WriteLine($"\n\tTagfile \"{decalPath.RelativePath}.decal_system\" closed\n\n");
            }

            return decalData;
        }
    }
}
