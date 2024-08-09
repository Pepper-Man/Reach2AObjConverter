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

        public class ObjectDataContainer
        {
            public List<ObjectDefinition> sceneryDefinitions { get; set; }
            public List<ObjectPlacement> sceneryPlacements { get; set; }
            public List<ObjectDefinition> vehicleDefinitions { get; set; }
            public List<ObjectPlacement> vehiclePlacements { get; set; }
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
                    Console.WriteLine($"Scenery definition {i}: \n\tTag: {def.tag}");
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

                    i++;
                }

                // VEHICLES //

                // Reach vehicle tag paths need to be translated to H2A vehicle tag paths
                Dictionary<string, string> vehicleMapping = new Dictionary<string, string>()
                {
                    { "objects\\vehicles\\covenant\\banshee\\banshee", "objects\\vehicles\\h2a_banshee\\h2a_banshee_mp" },
                    { "objects\\vehicles\\human\\turrets\\machinegun\\machinegun", "objects\\vehicles\\h2a_fixed_turret\\h2a_fixed_turret"},
                    { "objects\\vehicles\\covenant\\ghost\\ghost", "objects\\vehicles\\h2a_ghost\\h2a_ghost"},
                    { "objects\\vehicles\\human\\warthog\\warthog", "objects\\vehicles\\h2a_warthog\\h2a_warthog" },
                    { "objects\\vehicles\\covenant\\turrets\\plasma_turret\\plasma_turret_mounted", "objects\\vehicles\\h2a_covenant_turret\\h2a_covenant_turret" },
                    { "objects\\vehicles\\human\\falcon\\falcon", "objects\\vehicles\\h2a_hornet\\h2a_hornet" },
                    { "objects\\vehicles\\human\\mongoose\\mongoose", "objects\\vehicles\\h2a_mongoose\\h2a_mongoose" },
                    { "objects\\vehicles\\human\\scorpion\\scorpion", "objects\\vehicles\\h2a_scorpion\\h2a_scorpion" },
                    { "objects\\vehicles\\covenant\\wraith\\wraith", "objects\\vehicles\\h2a_wraith\\h2a_wraith" }
                };

                // Output deserialized data
                Console.WriteLine("\n\n--- VEHICLE DEFINITIONS ---");
                ((TagFieldBlock)tagFile.SelectField("Block:vehicle palette")).RemoveAllElements();
                ((TagFieldBlock)tagFile.SelectField("Block:vehicles")).RemoveAllElements();
                i = 0;
                foreach (var def in container.vehicleDefinitions)
                {
                    Console.WriteLine($"Vehicle definition {i}: \n\tTag: {def.tag}");
                    ((TagFieldBlock)tagFile.SelectField("Block:vehicle palette")).AddElement();

                    // Set vehicle tag path
                    try
                    {
                        ((TagFieldReference)tagFile.SelectField($"Block:vehicle palette[{i}]/Reference:name")).Path = TagPath.FromPathAndExtension(vehicleMapping[def.tag], "vehicle");
                    }
                    catch (KeyNotFoundException)
                    {
                        Console.WriteLine($"\nKey not found in vehicle mapping dict for {def.tag}, using Reach path");
                        ((TagFieldReference)tagFile.SelectField($"Block:vehicle palette[{i}]/Reference:name")).Path = TagPath.FromPathAndExtension(def.tag, "vehicle");
                    }

                    i++;
                }

                Console.WriteLine("\n\n--- VEHICLE PLACEMENTS ---");
                i = 0;
                foreach (var vehi in container.vehiclePlacements)
                {
                    Console.WriteLine($"Vehicle Placement {i}: \n\tType: {vehi.typeIndex} \n\tName: {vehi.nameIndex} \n\tFlags: {vehi.flags} \n\tPosition: {vehi.position[0]}, {vehi.position[1]}, {vehi.position[2]} \n\tRotation: {vehi.rotation[0]}, {vehi.rotation[1]}, {vehi.rotation[2]} \n\tScale: {vehi.scale} \n\tVariant: {vehi.variantName} \n\tTeam: {vehi.ownerTeam}");
                    ((TagFieldBlock)tagFile.SelectField("Block:vehicles")).AddElement();

                    // Set type
                    ((TagFieldBlockIndex)tagFile.SelectField($"Block:vehicles[{i}]/ShortBlockIndex:type")).Value = vehi.typeIndex;

                    // Set name
                    ((TagFieldBlockIndex)tagFile.SelectField($"Block:vehicles[{i}]/ShortBlockIndex:name")).Value = vehi.nameIndex;

                    // Set flags
                    ((TagFieldFlags)tagFile.SelectField($"Block:vehicles[{i}]/Struct:object data/Flags:placement flags")).RawValue = vehi.flags;

                    // Set position
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:vehicles[{i}]/Struct:object data/RealPoint3d:position")).Data = vehi.position;

                    // Set rotation
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:vehicles[{i}]/Struct:object data/RealEulerAngles3d:rotation")).Data = vehi.rotation;

                    // Set scale
                    ((TagFieldElementSingle)tagFile.SelectField($"Block:vehicles[{i}]/Struct:object data/Real:scale")).Data = vehi.scale;

                    // Set variant
                    ((TagFieldElementStringID)tagFile.SelectField($"Block:vehicles[{i}]/Struct:permutation data/StringID:variant name")).Data = vehi.variantName;

                    // Set team
                    ((TagFieldEnum)tagFile.SelectField($"Block:vehicles[{i}]/Struct:multiplayer data/CharEnum:owner team")).Value = vehi.ownerTeam;

                    // Set tag type
                    ((TagFieldEnum)tagFile.SelectField($"Block:vehicles[{i}]/Struct:object data/Struct:object id/CharEnum:type")).Value = 1;

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
