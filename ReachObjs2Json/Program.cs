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

            try
            {
                tagFile.Load(tagPath);
                Console.WriteLine("Tagfile opened\nReading scenario object data:\n");

                // SCENERY //
                var scenData = GetObjectData(tagFile, "scenery");
                scenDefData = scenData.definitions;
                scenPlaceData = scenData.placements;

                // VEHICLES //
                var vehiData = GetObjectData(tagFile, "vehicles");
                vehiDefData = vehiData.definitions;
                vehiPlaceData = vehiData.placements;

                // EQUIPMENT //
                var eqipData = GetObjectData(tagFile, "equipment");
                eqipDefData = eqipData.definitions;
                eqipPlaceData = eqipData.placements;

                // SOUND SCENERY //
                var ssceData = GetObjectData(tagFile, "sound scenery");
                ssceDefData = ssceData.definitions;
                sscePlaceData = ssceData.placements;
                
                // TRIGGER VOLUMES //
                trigVolData = GetTrigVolData(tagFile);

                // CRATES //
                var crateData = GetObjectData(tagFile, "crates");
                crateDefData = crateData.definitions;
                cratePlaceData = crateData.placements;

                // DECALS //
                var decalData = GetObjectData(tagFile, "decals");
                decalDefData = decalData.definitions;
                decalPlaceData = decalData.placements;
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
                    decalPlacements = decalPlaceData
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
    
        public static ResultsContainer GetObjectData(TagFile tagFile, string objectType)
        {
            ResultsContainer results = new ResultsContainer();
            List<ObjectDefinition> objDefData = new List<ObjectDefinition>();
            List<ObjectPlacement> objPlaceData = new List<ObjectPlacement>();

            // Get total number of object definitions
            int objDefCount;
            if (objectType == "vehicles")
            {
                objDefCount = ((TagFieldBlock)tagFile.SelectField($"Block:vehicle palette")).Elements.Count();
            }
            else if (objectType == "crates")
            {
                objDefCount = ((TagFieldBlock)tagFile.SelectField($"Block:crate palette")).Elements.Count();
            }
            else if (objectType == "decals")
            {
                objDefCount = ((TagFieldBlock)tagFile.SelectField($"Block:decal palette")).Elements.Count();
            }
            else
            {
                objDefCount = ((TagFieldBlock)tagFile.SelectField($"Block:{objectType} palette")).Elements.Count();
            }
            
            // Get all object definition data
            for (int i = 0; i < objDefCount; i++)
            {
                ObjectDefinition objDef = new ObjectDefinition();
                Console.WriteLine($"{objectType} definition {i}:");
                TagPath path;
                if (objectType == "vehicles")
                {
                    path = ((TagFieldReference)tagFile.SelectField($"Block:vehicle palette[{i}]/Reference:name")).Path;
                }
                else if (objectType == "crates")
                {
                    path = ((TagFieldReference)tagFile.SelectField($"Block:crate palette[{i}]/Reference:name")).Path;
                }
                else if (objectType == "decals")
                {
                    path = ((TagFieldReference)tagFile.SelectField($"Block:decal palette[{i}]/Reference:reference")).Path;
                }
                else
                {
                    path = ((TagFieldReference)tagFile.SelectField($"Block:{objectType} palette[{i}]/Reference:name")).Path;
                }
                
                Console.WriteLine($"\tTag path: {path}\n");
                objDef.tag = path.RelativePath;

                objDefData.Add(objDef);
            }

            // Get total number of placed objects
            int objPlacedCount = ((TagFieldBlock)tagFile.SelectField($"Block:{objectType}")).Elements.Count();

            // Get all object data
            for (int i = 0; i < objPlacedCount; i++)
            {
                ObjectPlacement objPlacement = new ObjectPlacement();
                Console.WriteLine($"{objectType} placement {i}:");

                if (objectType != "decals")
                {
                    int type = ((TagFieldBlockIndex)tagFile.SelectField($"Block:{objectType}[{i}]/ShortBlockIndex:type")).Value;
                    Console.WriteLine($"\tType index: {type}");
                    objPlacement.typeIndex = type;

                    int name = ((TagFieldBlockIndex)tagFile.SelectField($"Block:{objectType}[{i}]/ShortBlockIndex:name")).Value;
                    Console.WriteLine($"\tName index: {name}");
                    objPlacement.nameIndex = name;

                    uint flags = ((TagFieldFlags)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/Flags:placement flags")).RawValue;
                    Console.WriteLine($"\tFlags: {type}");
                    objPlacement.flags = flags;

                    float scale = ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/Real:scale")).Data;
                    Console.WriteLine($"\tScale: {scale}");
                    objPlacement.scale = scale;

                    float[] pos = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/RealPoint3d:position")).Data;
                    Console.WriteLine($"\tPosition: {pos[0]}, {pos[1]}, {pos[2]}");
                    objPlacement.position = pos;

                    float[] rot = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/RealEulerAngles3d:rotation")).Data;
                    Console.WriteLine($"\tRotation: {rot[0]}, {rot[1]}, {rot[2]}");
                    objPlacement.rotation = rot;

                    if (objectType != "equipment" && objectType != "sound scenery")
                    {
                        string variant = ((TagFieldElementStringID)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:permutation data/StringID:variant name")).Data;
                        Console.WriteLine($"\tVariant name: {variant}");
                        objPlacement.variantName = variant;
                    }

                    int team = -1;
                    if (objectType == "sound scenery")
                    {
                        int volType = ((TagFieldEnum)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/LongEnum:volume type")).Value;
                        Console.WriteLine($"\tVolume type: {volType}");
                        objPlacement.volumeType = volType;

                        float height = ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Real:height")).Data;
                        Console.WriteLine($"\tHeight: {height}");
                        objPlacement.height = height;

                        float[] coneBounds = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/AngleBounds:override cone angle bounds")).Data;
                        Console.WriteLine($"\tCone bounds: {coneBounds[0]}, {coneBounds[1]}");
                        objPlacement.coneBounds = coneBounds;

                        float coneGain = ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Real:override outer cone gain")).Data;
                        Console.WriteLine($"\tCone gain: {coneGain}");
                        objPlacement.coneGain = coneGain;

                        float dntObstrDist = ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:don't obstruct distance")).Data;
                        Console.WriteLine($"\tDon't obstruct distance: {dntObstrDist}");
                        objPlacement.obstrDistance = dntObstrDist;

                        float dntPlayDist = ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:don't play distance")).Data;
                        Console.WriteLine($"\tDon't play distance: {dntPlayDist}");
                        objPlacement.dntPlyDistance = dntPlayDist;

                        float attackDist = ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:attack distance")).Data;
                        Console.WriteLine($"\tAttack distance: {attackDist}");
                        objPlacement.atkDistance = attackDist;

                        float minDist = ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:minimum distance")).Data;
                        Console.WriteLine($"\tMinimum distance: {minDist}");
                        objPlacement.minDistance = minDist;

                        float susBegDist = ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:sustain begin distance")).Data;
                        Console.WriteLine($"\tSustain begin distance: {susBegDist}");
                        objPlacement.susBegDistance = susBegDist;

                        float susEndDist = ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:sustain end distance")).Data;
                        Console.WriteLine($"\tSustain end distance: {susEndDist}");
                        objPlacement.susEndDistance = susEndDist;

                        float maxDist = ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:maximum distance")).Data;
                        Console.WriteLine($"\tMaximum distance: {maxDist}");
                        objPlacement.maxDistance = maxDist;

                        float susDb = ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:sustain db")).Data;
                        Console.WriteLine($"\tSustain Db: {susDb}");
                        objPlacement.sustainDb = susDb;
                    }
                    else
                    {
                        team = ((TagFieldEnum)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:multiplayer data/CharEnum:owner team")).Value;
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
    }
}
