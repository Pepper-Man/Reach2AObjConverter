using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Corinth;
using Corinth.Tags;

namespace JsonTo2AScenario
{
    internal class JsonTo2AScenario
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

            public static string SanitisePath(string input, string type)
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

                // Check file is in H2AMPEK
                if (type == "tag")
                {
                    if (!sanitisedPath.Contains("H2AMPEK\\tags"))
                    {
                        Console.WriteLine("Input file is not in the HREK tags folder.");
                        return "";
                    }
                }

                // Check correct extension
                if (type == "tag")
                {
                    if (Path.GetExtension(sanitisedPath) != ".scenario")
                    {
                        Console.WriteLine("Input file is not a .scenario tag.");
                        return "";
                    }
                }
                else if (type == "json")
                {
                    if (Path.GetExtension(sanitisedPath) != ".json")
                    {
                        Console.WriteLine("Input file is not a .json file.");
                        return "";
                    }
                }

                return sanitisedPath;
            }
        }

        static void Main(string[] args)
        {
            // Get H2AMP tag input from user
            string userTagPath;
            /*
            while (true)
            {
                Console.WriteLine("Enter full path to H2AMP .scenario_structure_lighting_info tag:\n");
                string userInput = Console.ReadLine();
                userTagPath = FilePathSanitiser.SanitisePath(userInput, "tag");
                if (userTagPath != "")
                {
                    Console.WriteLine("Valid path entered");
                    break;
                }
            }
            */

            userTagPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\H2AMPEK\\tags\\levels\\pepper\\cex_headlong\\cex_headlong.scenario";
            // Get H2AMPEK path
            int h2ampekIndex = userTagPath.IndexOf("H2AMPEK");
            string h2ampek = userTagPath.Substring(0, h2ampekIndex + 7);

            // Get relative tag path
            string relativePath = Path.ChangeExtension(userTagPath.Substring(h2ampekIndex + 13), null);

            // Get JSON path from user
            string jsonPath;
            /*
            while (true)
            {
                Console.WriteLine("Enter full path to .json file:\n");
                string userInput = Console.ReadLine();
                jsonPath = FilePathSanitiser.SanitisePath(userInput, "json");
                if (jsonPath != "")
                {
                    Console.WriteLine("Valid path entered");
                    break;
                }
            }
            */
            jsonPath = "I:\\Reach2AObjConverter\\ReachObjs2Json\\bin\\x64\\Debug\\cex_headlong_objectdata.json";

            // Read JSON from file
            string json = File.ReadAllText(jsonPath);

            // Deserialize JSON to object
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            ObjectDataContainer container = JsonSerializer.Deserialize<ObjectDataContainer>(json, options);

            // ManagedBlam Initialisation
            void callback(ManagedBlamCrashInfo LambdaExpression) { }
            ManagedBlamStartupParameters startupParams = new ManagedBlamStartupParameters
            {
                InitializationLevel = InitializationType.TagsOnly
            };
            ManagedBlamSystem.Start(h2ampek, callback, startupParams);
            var tagFile = new TagFile();
            var tagPath = TagPath.FromPathAndExtension(relativePath, "scenario");

            try
            {
                tagFile.Load(tagPath);
                Console.WriteLine("Tagfile opened\nWriting scenario object data:\n");

                // SCENERY //
                // Output deserialized data
                Console.WriteLine("--- SCENERY DEFINITIONS ---");
                ((TagFieldBlock)tagFile.SelectField("Block:scenery palette")).RemoveAllElements();
                ((TagFieldBlock)tagFile.SelectField("Block:scenery")).RemoveAllElements();
                int i = 0;
                foreach (var def in container.sceneryDefinitions)
                {
                    Console.WriteLine($"Light Definition {i}: \n\tTag: {def.tag}");
                    ((TagFieldBlock)tagFile.SelectField("Block:scenery palette")).AddElement();

                    // Set scenery tag path
                    ((TagFieldReference)tagFile.SelectField($"Block:scenery palette[{i}]/Reference:name")).Path = TagPath.FromPathAndExtension(def.tag, "scenery");

                    i++;
                }

                Console.WriteLine("\n\n--- SCENERY PLACEMENTS ---");
                i = 0;
                foreach (var scen in container.sceneryPlacements)
                {
                    Console.WriteLine($"Scenery Placement {i}: \n\tType: {scen.typeIndex} \n\tName: {scen.nameIndex} \n\tFlags: {scen.flags} \n\tPosition: {scen.position[0]}, {scen.position[1]}, {scen.position[2]} \n\tRotation: {scen.rotation[0]}, {scen.rotation[1]}, {scen.rotation[2]} \n\tScale: {scen.scale} \n\tVariant: {scen.variantName} \n\tTeam: {scen.ownerTeam}");
                    ((TagFieldBlock)tagFile.SelectField("Block:scenery")).AddElement();

                    // Set type
                    ((TagFieldBlockIndex)tagFile.SelectField($"Block:scenery[{i}]/ShortBlockIndex:type")).Value = scen.typeIndex;

                    // Set name
                    ((TagFieldBlockIndex)tagFile.SelectField($"Block:scenery[{i}]/ShortBlockIndex:name")).Value = scen.nameIndex;

                    // Set flags
                    ((TagFieldFlags)tagFile.SelectField($"Block:scenery[{i}]/Struct:object data/Flags:placement flags")).RawValue = scen.flags;

                    // Set position
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:scenery[{i}]/Struct:object data/RealPoint3d:position")).Data = scen.position;

                    // Set rotation
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:scenery[{i}]/Struct:object data/RealEulerAngles3d:rotation")).Data = scen.rotation;

                    // Set scale
                    ((TagFieldElementSingle)tagFile.SelectField($"Block:scenery[{i}]/Struct:object data/Real:scale")).Data = scen.scale;

                    // Set variant
                    ((TagFieldElementStringID)tagFile.SelectField($"Block:scenery[{i}]/Struct:permutation data/StringID:variant name")).Data = scen.variantName;

                    // Set team
                    ((TagFieldEnum)tagFile.SelectField($"Block:scenery[{i}]/Struct:multiplayer data/CharEnum:owner team")).Value = scen.ownerTeam;

                    // Set tag type
                    ((TagFieldEnum)tagFile.SelectField($"Block:scenery[{i}]/Struct:object data/Struct:object id/CharEnum:type")).Value = 6;

                    // Set source
                    i++;
                }
            }
            catch
            {
                Console.WriteLine("Unknown managedblam error");
            }
            finally
            {
                tagFile.Save();
                tagFile.Dispose();
                ManagedBlamSystem.Stop();
                Console.WriteLine("\nSuccessfully written tag data! Press enter to exit.\n");
                Console.ReadLine();
            }
        }
    }
}
