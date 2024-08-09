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
        public class SceneryDefinition
        {
            public string tag { get; set; }
        }

        public class SceneryPlacement
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


        public class ObjectDataContainer
        {
            public List<SceneryDefinition> sceneryDefinitions { get; set; }
            public List<SceneryPlacement> sceneryPlacements { get; set; }
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
            List<SceneryDefinition> scenDefData = new List<SceneryDefinition>();
            List<SceneryPlacement> scenPlaceData = new List<SceneryPlacement>();

            try
            {
                tagFile.Load(tagPath);
                Console.WriteLine("Tagfile opened\nReading scenario object data:\n");

                // SCENERY //
                // Get total number of scenery definitions
                int sceneryDefCount = ((TagFieldBlock)tagFile.SelectField("Block:scenery palette")).Elements.Count();

                // Get all scenery definition data
                for (int i = 0; i < sceneryDefCount; i++)
                {
                    SceneryDefinition sceneryDef = new SceneryDefinition();
                    Console.WriteLine($"Scenery definition {i}:");
                    TagPath path = ((TagFieldReference)tagFile.SelectField($"Block:scenery palette[{i}]/Reference:name")).Path;
                    Console.WriteLine($"\tTag path: {path}\n");
                    sceneryDef.tag = path.RelativePath;

                    scenDefData.Add(sceneryDef);
                }

                // Get total number of scenery objects
                int sceneryObjCount = ((TagFieldBlock)tagFile.SelectField("Block:scenery")).Elements.Count();

                // Get all scenery object data
                for (int i = 0; i < sceneryObjCount; i++)
                {
                    SceneryPlacement sceneryObj = new SceneryPlacement();
                    Console.WriteLine($"Scenery placement {i}:");

                    int type = ((TagFieldBlockIndex)tagFile.SelectField($"Block:scenery[{i}]/ShortBlockIndex:type")).Value;
                    Console.WriteLine($"\tType index: {type}");
                    sceneryObj.typeIndex = type;

                    int name = ((TagFieldBlockIndex)tagFile.SelectField($"Block:scenery[{i}]/ShortBlockIndex:name")).Value;
                    Console.WriteLine($"\tName index: {name}");
                    sceneryObj.nameIndex = name;

                    uint flags = ((TagFieldFlags)tagFile.SelectField($"Block:scenery[{i}]/Struct:object data/Flags:placement flags")).RawValue;
                    Console.WriteLine($"\tFlags: {type}");
                    sceneryObj.flags = flags;

                    float[] pos = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:scenery[{i}]/Struct:object data/RealPoint3d:position")).Data;
                    Console.WriteLine($"\tPosition: {pos[0]}, {pos[1]}, {pos[2]}");
                    sceneryObj.position = pos;

                    float[] rot = ((TagFieldElementArraySingle)tagFile.SelectField($"Block:scenery[{i}]/Struct:object data/RealEulerAngles3d:rotation")).Data;
                    Console.WriteLine($"\tRotation: {rot[0]}, {rot[1]}, {rot[2]}");
                    sceneryObj.rotation = rot;

                    float scale = ((TagFieldElementSingle)tagFile.SelectField($"Block:scenery[{i}]/Struct:object data/Real:scale")).Data;
                    Console.WriteLine($"\tScale: {scale}");
                    sceneryObj.scale = scale;

                    string variant = ((TagFieldElementStringID)tagFile.SelectField($"Block:scenery[{i}]/Struct:permutation data/StringID:variant name")).Data;
                    Console.WriteLine($"\tVariant name: {variant}");
                    sceneryObj.variantName = variant;

                    int team = ((TagFieldEnum)tagFile.SelectField($"Block:scenery[{i}]/Struct:multiplayer data/CharEnum:owner team")).Value;
                    Console.WriteLine($"\tOwner team: {team}");
                    sceneryObj.ownerTeam = team;

                    scenPlaceData.Add(sceneryObj);
                }

                // CRATES or smth //
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
                    sceneryPlacements = scenPlaceData
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
    }
}
