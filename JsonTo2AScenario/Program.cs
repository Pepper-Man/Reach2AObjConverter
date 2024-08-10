using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
            public List<ObjectDefinition> equipmentDefinitions { get; set; }
            public List<ObjectPlacement> equipmentPlacements { get; set; }
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
                Console.WriteLine("--- SCENERY DEFINITIONS ---");
                SetObjectDefData(tagFile, "scenery", container.sceneryDefinitions);
                Console.WriteLine("\n\n--- SCENERY PLACEMENTS ---");
                SetObjectPlaceData(tagFile, "scenery", container.sceneryPlacements);

                // VEHICLES //
                Console.WriteLine("\n\n--- VEHICLE DEFINITIONS ---");
                SetObjectDefData(tagFile, "vehicle", container.vehicleDefinitions);
                Console.WriteLine("\n\n--- VEHICLE PLACEMENTS ---");
                SetObjectPlaceData(tagFile, "vehicles", container.vehiclePlacements);

                // EQUIPMENT //
                Console.WriteLine("\n\n--- EQUIPMENT DEFINITIONS ---");
                SetObjectDefData(tagFile, "equipment", container.equipmentDefinitions);
                Console.WriteLine("\n\n--- EQUIPMENT PLACEMENTS ---");
                SetObjectPlaceData(tagFile, "equipment", container.equipmentPlacements);
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

        public static void SetObjectDefData(TagFile tagFile, string objectType, List<ObjectDefinition> objDefinitions)
        {
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

            // Reach equipment tag paths need to be translated to H2A equipment tag paths
            Dictionary<string, string> equipmentMapping = new Dictionary<string, string>()
            {
                { "objects\\multi\\powerups\\powerup_blue\\powerup_blue", "objects\\multi\\powerups\\h2a_activecamo_blue\\h2a_activecamo" },
                { "objects\\multi\\powerups\\powerup_red\\powerup_red", "objects\\multi\\powerups\\h2a_overshield_yellow\\h2a_overshield"},
                { "objects\\weapons\\grenade\\frag_grenade\\frag_grenade", "objects\\weapons\\h2a_frag_grenade\\h2a_frag_grenade"},
                { "objects\\weapons\\grenade\\plasma_grenade\\plasma_grenade", "objects\\weapons\\h2a_plasma_grenade\\h2a_plasma_grenade" },
                { "objects\\equipment\\bubbleshield_module\\bubbleshield_module", "objects\\multi\\powerups\\ord_damage\\ord_damage" }
            };

            ((TagFieldBlock)tagFile.SelectField($"Block:{objectType} palette")).RemoveAllElements();  
            int i = 0;
            foreach (var def in objDefinitions)
            {
                Console.WriteLine($"{objectType} definition {i}: \n\tTag: {def.tag}");
                ((TagFieldBlock)tagFile.SelectField($"Block:{objectType} palette")).AddElement();

                if (objectType == "vehicle")
                {
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
                }
                else if (objectType == "equipment")
                {
                    try
                    {
                        ((TagFieldReference)tagFile.SelectField($"Block:equipment palette[{i}]/Reference:name")).Path = TagPath.FromPathAndExtension(equipmentMapping[def.tag], "equipment");
                    }
                    catch (KeyNotFoundException)
                    {
                        Console.WriteLine($"\nKey not found in equipment mapping dict for {def.tag}, using Reach path");
                        ((TagFieldReference)tagFile.SelectField($"Block:equipment palette[{i}]/Reference:name")).Path = TagPath.FromPathAndExtension(def.tag, "equipment");
                    }
                }
                else
                {
                    // Set object tag path
                    ((TagFieldReference)tagFile.SelectField($"Block:{objectType} palette[{i}]/Reference:name")).Path = TagPath.FromPathAndExtension(def.tag, objectType);
                }
                
                i++;
            }
        }
    
        public static void SetObjectPlaceData(TagFile tagFile, string objectType, List<ObjectPlacement> objPlacements)
        {
            // Convert object type string to enum value
            Dictionary<string, int> objTypeMapping = new Dictionary<string, int>()
            {
                { "scenery", 6 },
                { "vehicles", 1},
                { "equipment", 3}
            };

            int i = 0;
            ((TagFieldBlock)tagFile.SelectField($"Block:{objectType}")).RemoveAllElements();
            foreach (var obj in objPlacements)
            {
                Console.WriteLine($"{objectType} Placement {i}: \n\tType: {obj.typeIndex} \n\tName: {obj.nameIndex} \n\tFlags: {obj.flags} \n\tPosition: {obj.position[0]}, {obj.position[1]}, {obj.position[2]} \n\tRotation: {obj.rotation[0]}, {obj.rotation[1]}, {obj.rotation[2]} \n\tScale: {obj.scale} \n\tVariant: {obj.variantName} \n\tTeam: {obj.ownerTeam}");
                ((TagFieldBlock)tagFile.SelectField($"Block:{objectType}")).AddElement();

                // Set type
                ((TagFieldBlockIndex)tagFile.SelectField($"Block:{objectType}[{i}]/ShortBlockIndex:type")).Value = obj.typeIndex;

                // Set name
                ((TagFieldBlockIndex)tagFile.SelectField($"Block:{objectType}[{i}]/ShortBlockIndex:name")).Value = obj.nameIndex;

                // Set flags
                ((TagFieldFlags)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/Flags:placement flags")).RawValue = obj.flags;

                // Set position
                ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/RealPoint3d:position")).Data = obj.position;

                // Set rotation
                ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/RealEulerAngles3d:rotation")).Data = obj.rotation;

                // Set scale
                ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/Real:scale")).Data = obj.scale;

                if (objectType != "equipment")
                {
                    // Set variant
                    ((TagFieldElementStringID)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:permutation data/StringID:variant name")).Data = obj.variantName;
                }
                
                // Set team
                ((TagFieldEnum)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:multiplayer data/CharEnum:owner team")).Value = obj.ownerTeam;

                // Set tag type
                ((TagFieldEnum)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/Struct:object id/CharEnum:type")).Value = objTypeMapping[objectType];

                i++;
            }
        }
    }
}
