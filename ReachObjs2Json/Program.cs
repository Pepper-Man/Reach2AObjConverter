using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bungie;
using Bungie.Tags;
using System.Text.Json;
using System.Runtime.InteropServices.ComTypes;

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

            try
            {
                tagFile.Load(tagPath);
                Console.WriteLine("Tagfile opened\nReading scenario object data:\n");

                // SCENERY //
                scenDefData = GetObjectData(tagFile, "scenery").definitions;
                scenPlaceData = GetObjectData(tagFile, "scenery").placements;

                // VEHICLES //
                vehiDefData = GetObjectData(tagFile, "vehicles").definitions;
                vehiPlaceData = GetObjectData(tagFile, "vehicles").placements;

                // EQUIPMENT //
                eqipDefData = GetObjectData(tagFile, "equipment").definitions;
                eqipPlaceData = GetObjectData(tagFile, "equipment").placements;
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
                    equipmentPlacements = eqipPlaceData
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

                int type = ((TagFieldBlockIndex)tagFile.SelectField($"Block:{objectType}[{i}]/ShortBlockIndex:type")).Value;
                Console.WriteLine($"\tType index: {type}");
                objPlacement.typeIndex = type;

                int name = ((TagFieldBlockIndex)tagFile.SelectField($"Block:{objectType}[{i}]/ShortBlockIndex:name")).Value;
                Console.WriteLine($"\tName index: {name}");
                objPlacement.nameIndex = name;

                uint flags = ((TagFieldFlags)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/Flags:placement flags")).RawValue;
                Console.WriteLine($"\tFlags: {type}");
                objPlacement.flags = flags;

                float[] pos = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/RealPoint3d:position")).Data;
                Console.WriteLine($"\tPosition: {pos[0]}, {pos[1]}, {pos[2]}");
                objPlacement.position = pos;

                float[] rot = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/RealEulerAngles3d:rotation")).Data;
                Console.WriteLine($"\tRotation: {rot[0]}, {rot[1]}, {rot[2]}");
                objPlacement.rotation = rot;

                float scale = ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/Real:scale")).Data;
                Console.WriteLine($"\tScale: {scale}");
                objPlacement.scale = scale;

                if (objectType != "equipment")
                {
                    string variant = ((TagFieldElementStringID)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:permutation data/StringID:variant name")).Data;
                    Console.WriteLine($"\tVariant name: {variant}");
                    objPlacement.variantName = variant;
                }
                
                int team = ((TagFieldEnum)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:multiplayer data/CharEnum:owner team")).Value;
                Console.WriteLine($"\tOwner team: {team}\n");
                objPlacement.ownerTeam = team;

                objPlaceData.Add(objPlacement);
            }

            results.definitions = objDefData;
            results.placements = objPlaceData;
            return results;
        }
    }
}
