using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Corinth;
using Corinth.Game;
using Corinth.Tags;

namespace JsonTo2AScenario
{
    internal class JsonTo2AScenario
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
                public string vectorRef { get; set; }
                public float[] tintColour { get; set; }
                public float tintIntensity { get; set; }
                public float modulation { get; set; }
                public long blendMode { get; set; }
                public long albedoMode { get; set; }
                public long bumpMode { get; set; }
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
            public float obstrDistance { get; set; }
            public float dntPlyDistance { get; set; }
            public float atkDistance { get; set; }
            public float minDistance { get; set; }
            public float susBegDistance { get; set; }
            public float susEndDistance { get; set; }
            public float maxDistance { get; set; }
            public float sustainDb { get; set; }

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
                SetObjectDefData(tagFile, "scenery", container.sceneryDefinitions, "scenery");
                Console.WriteLine("\n\n--- SCENERY PLACEMENTS ---");
                SetObjectPlaceData(tagFile, "scenery", container.sceneryPlacements, container.sceneryDefinitions);

                // VEHICLES //
                Console.WriteLine("\n\n--- VEHICLE DEFINITIONS ---");
                SetObjectDefData(tagFile, "vehicle", container.vehicleDefinitions, "vehicle");
                Console.WriteLine("\n\n--- VEHICLE PLACEMENTS ---");
                SetObjectPlaceData(tagFile, "vehicles", container.vehiclePlacements, container.vehicleDefinitions);

                // EQUIPMENT //
                Console.WriteLine("\n\n--- EQUIPMENT DEFINITIONS ---");
                SetObjectDefData(tagFile, "equipment", container.equipmentDefinitions, "equipment");
                Console.WriteLine("\n\n--- EQUIPMENT PLACEMENTS ---");
                SetObjectPlaceData(tagFile, "equipment", container.equipmentPlacements, container.equipmentDefinitions);

                // SOUND SCENERY //
                Console.WriteLine("\n\n--- SOUND SCENERY DEFINITIONS ---");
                SetObjectDefData(tagFile, "sound scenery", container.soundscenDefinitions, "sound_scenery");
                Console.WriteLine("\n\n--- SOUND SCENERY PLACEMENTS ---");
                SetObjectPlaceData(tagFile, "sound scenery", container.soundscenPlacements, container.soundscenDefinitions);
                
                // TRIGGER VOLUMES //
                Console.WriteLine("\n\n--- TRIGGER VOLUMES ---");
                SetTrigVolData(tagFile, container.triggerVolumes);

                // CRATES //
                Console.WriteLine("\n\n--- CRATE DEFINITIONS ---");
                SetObjectDefData(tagFile, "crate", container.crateDefinitions, "crate");
                Console.WriteLine("\n\n--- CRATE PLACEMENTS ---");
                SetObjectPlaceData(tagFile, "crates", container.cratePlacements, container.crateDefinitions);

                // DECALS //
                Console.WriteLine("\n\n--- DECAL DEFINITIONS ---");
                SetObjectDefData(tagFile, "decal", container.decalDefinitions, "decal_system");
                Console.WriteLine("\n\n--- DECAL PLACEMENTS ---");
                SetObjectPlaceData(tagFile, "decals", container.decalPlacements, container.decalDefinitions);

                // WEAPONS //
                Console.WriteLine("\n\n--- WEAPON DEFINITIONS ---");
                SetObjectDefData(tagFile, "weapon", container.weaponDefinitions, "weapon");
                Console.WriteLine("\n\n--- WEAPON PLACEMENTS ---");
                SetObjectPlaceData(tagFile, "weapons", container.weaponPlacements, container.weaponDefinitions);
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

        public static void SetObjectDefData(TagFile tagFile, string objectType, List<ObjectDefinition> objDefinitions, string tagExt)
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

            // Reach weapon tag paths need to be translated to H2A weapon tag paths
            Dictionary<string, string> weaponMapping = new Dictionary<string, string>()
            {
                { "objects\\weapons\\melee\\energy_sword\\energy_sword", "objects\\weapons\\h2a_energy_sword\\h2a_energy_sword" },
                { "objects\\weapons\\melee\\gravity_hammer\\gravity_hammer", "objects\\weapons\\h2a_energy_sword_infected\\h2a_energy_sword_infected"},
                { "objects\\weapons\\multiplayer\\assault_bomb\\assault_bomb", "objects\\weapons\\multiplayer\\h2a_assault_bomb\\h2a_assault_bomb"},
                { "objects\\weapons\\multiplayer\\flag\\flag", "objects\\weapons\\multiplayer\\flag\\flag" },
                { "objects\\weapons\\multiplayer\\skull\\skull", "objects\\weapons\\h2a_oddball\\h2a_oddball" },
                { "objects\\weapons\\pistol\\magnum\\magnum", "objects\\weapons\\h2a_magnum\\h2a_magnum" },
                { "objects\\weapons\\pistol\\needler\\needler", "objects\\weapons\\h2a_needler\\h2a_needler" },
                { "objects\\weapons\\pistol\\plasma_pistol\\plasma_pistol", "objects\\weapons\\h2a_plasma_pistol\\h2a_plasma_pistol" },
                { "objects\\weapons\\rifle\\assault_rifle\\assault_rifle", "objects\\weapons\\h2a_assault_rifle\\h2a_assault_rifle" },
                { "objects\\weapons\\rifle\\concussion_rifle\\concussion_rifle", "objects\\weapons\\h2a_brute_shot\\h2a_brute_shot" },
                { "objects\\weapons\\rifle\\dmr\\dmr", "objects\\weapons\\h2a_battle_rifle\\h2a_br" },
                { "objects\\weapons\\rifle\\focus_rifle\\focus_rifle", "objects\\weapons\\h2a_particle_beam_rifle\\h2a_beam_rifle" },
                { "objects\\weapons\\rifle\\grenade_launcher\\grenade_launcher", "objects\\weapons\\h2a_brute_shot\\h2a_brute_shot" },
                { "objects\\weapons\\rifle\\needle_rifle\\needle_rifle", "objects\\weapons\\h2a_covenant_carbine\\h2a_covenant_carbine" },
                { "objects\\weapons\\rifle\\plasma_repeater\\plasma_repeater", "objects\\weapons\\h2a_brute_plasma_rifle\\h2a_brute_plasma" },
                { "objects\\weapons\\rifle\\plasma_rifle\\plasma_rifle", "objects\\weapons\\h2a_plasma_rifle\\h2a_plasma_rifle" },
                { "objects\\weapons\\rifle\\shotgun\\shotgun", "objects\\weapons\\h2a_shotgun\\h2a_shotgun" },
                { "objects\\weapons\\rifle\\sniper_rifle\\sniper_rifle", "objects\\weapons\\h2a_sniper_rifle\\h2a_sniper_rifle" },
                { "objects\\weapons\\rifle\\spike_rifle\\spike_rifle", "objects\\weapons\\h2a_brute_plasma_rifle\\h2a_brute_plasma" },
                { "objects\\weapons\\support_high\\flak_cannon\\flak_cannon", "objects\\weapons\\h2a_fuel_rod_gun\\h2a_fuel_rod_gun" },
                { "objects\\weapons\\support_high\\plasma_launcher\\plasma_launcher", "objects\\weapons\\h2a_fuel_rod_gun\\h2a_fuel_rod_gun" },
                { "objects\\weapons\\support_high\\rocket_launcher\\rocket_launcher", "objects\\weapons\\h2a_rocket_launcher\\h2a_rocket_launcher" },
                { "objects\\weapons\\support_high\\spartan_laser\\spartan_laser", "objects\\weapons\\h2a_rocket_launcher\\h2a_rocket_launcher" }
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
                        ((TagFieldReference)tagFile.SelectField($"Block:vehicle palette[{i}]/Reference:name")).Path = TagPath.FromPathAndExtension(vehicleMapping[def.tag], tagExt);
                    }
                    catch (KeyNotFoundException)
                    {
                        Console.WriteLine($"\nKey not found in vehicle mapping dict for {def.tag}, using Reach path");
                        ((TagFieldReference)tagFile.SelectField($"Block:vehicle palette[{i}]/Reference:name")).Path = TagPath.FromPathAndExtension(def.tag, tagExt);
                    }
                }
                else if (objectType == "equipment")
                {
                    try
                    {
                        ((TagFieldReference)tagFile.SelectField($"Block:equipment palette[{i}]/Reference:name")).Path = TagPath.FromPathAndExtension(equipmentMapping[def.tag], tagExt);
                    }
                    catch (KeyNotFoundException)
                    {
                        Console.WriteLine($"\nKey not found in equipment mapping dict for {def.tag}, using Reach path");
                        ((TagFieldReference)tagFile.SelectField($"Block:equipment palette[{i}]/Reference:name")).Path = TagPath.FromPathAndExtension(def.tag, tagExt);
                    }
                }
                else if (objectType == "weapon")
                {
                    try
                    {
                        ((TagFieldReference)tagFile.SelectField($"Block:weapon palette[{i}]/Reference:name")).Path = TagPath.FromPathAndExtension(weaponMapping[def.tag], tagExt);
                    }
                    catch (KeyNotFoundException)
                    {
                        Console.WriteLine($"\nKey not found in weapon mapping dict for {def.tag}, using Reach path");
                        ((TagFieldReference)tagFile.SelectField($"Block:weapon palette[{i}]/Reference:name")).Path = TagPath.FromPathAndExtension(def.tag, tagExt);
                    }
                }
                else if (objectType == "decal")
                {
                    TagPath path = TagPath.FromPathAndExtension(def.tag, tagExt);
                    ((TagFieldReference)tagFile.SelectField($"Block:{objectType} palette[{i}]/Reference:reference")).Path = path;
                    SetDecalSystemData(def, path);
                }
                else
                {
                    // Set object tag path
                    ((TagFieldReference)tagFile.SelectField($"Block:{objectType} palette[{i}]/Reference:name")).Path = TagPath.FromPathAndExtension(def.tag, tagExt);
                }
                
                i++;
            }
        }
    
        public static void SetObjectPlaceData(TagFile tagFile, string objectType, List<ObjectPlacement> objPlacements, List<ObjectDefinition> objDefinitions)
        {
            // Convert object type string to enum value
            Dictionary<string, int> objTypeMapping = new Dictionary<string, int>()
            {
                { "scenery", 6 },
                { "vehicles", 1 },
                { "equipment", 3 },
                { "sound scenery", 10 },
                { "crates", 11 },
                { "weapons", 2 }
            };

            int i = 0;
            ((TagFieldBlock)tagFile.SelectField($"Block:{objectType}")).RemoveAllElements();
            foreach (var obj in objPlacements)
            {
                Console.WriteLine($"{objectType} Placement {i}:");
                ((TagFieldBlock)tagFile.SelectField($"Block:{objectType}")).AddElement();

                if (objectType != "decals")
                {
                    // Set type
                    Console.WriteLine($"\tType: {obj.typeIndex}");
                    ((TagFieldBlockIndex)tagFile.SelectField($"Block:{objectType}[{i}]/ShortBlockIndex:type")).Value = obj.typeIndex;

                    // Set name
                    Console.WriteLine($"\tName: {obj.nameIndex}");
                    ((TagFieldBlockIndex)tagFile.SelectField($"Block:{objectType}[{i}]/ShortBlockIndex:name")).Value = obj.nameIndex;

                    // Set flags
                    Console.WriteLine($"\tFlags: {obj.flags}");
                    ((TagFieldFlags)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/Flags:placement flags")).RawValue = obj.flags;

                    // Set position
                    Console.WriteLine($"\tPosition: {obj.position[0]}, {obj.position[1]}, {obj.position[2]}");
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/RealPoint3d:position")).Data = obj.position;

                    // Set rotation
                    Console.WriteLine($"\tRotation: {obj.rotation[0]}, {obj.rotation[1]}, {obj.rotation[2]}");
                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/RealEulerAngles3d:rotation")).Data = obj.rotation;

                    // Set scale
                    Console.WriteLine($"\tScale: {obj.scale}");
                    ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/Real:scale")).Data = obj.scale;

                    if (objectType != "equipment" && objectType != "sound scenery")
                    {
                        // Set variant
                        Console.WriteLine($"\tVariant: {obj.variantName}");
                        ((TagFieldElementStringID)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:permutation data/StringID:variant name")).Data = obj.variantName;
                    }

                    if (objectType != "sound scenery")
                    {
                        // Set team
                        Console.WriteLine($"\tTeam: {obj.ownerTeam}");
                        ((TagFieldEnum)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:multiplayer data/CharEnum:owner team")).Value = obj.ownerTeam;
                    }

                    // Set tag type
                    Console.WriteLine($"\tTag: {objTypeMapping[objectType]}");
                    ((TagFieldEnum)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:object data/Struct:object id/CharEnum:type")).Value = objTypeMapping[objectType];

                    // Sound scenery specific settings
                    if (objectType == "sound scenery")
                    {
                        // Set volume type
                        Console.WriteLine($"\tVolume type: {obj.volumeType}");
                        ((TagFieldEnum)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/LongEnum:volume type")).Value = obj.volumeType;

                        // Set volume height
                        Console.WriteLine($"\tVolume height: {obj.height}");
                        ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Real:height")).Data = obj.height;

                        // Set cone angle bounds
                        Console.WriteLine($"\tCone angle bounds: {obj.coneBounds[0]}, {obj.coneBounds[1]}");
                        ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/AngleBounds:override cone angle bounds")).Data = obj.coneBounds;

                        // Set cone gain
                        Console.WriteLine($"\tCone gain: {obj.coneGain}");
                        ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Real:override outer cone gain")).Data = obj.coneGain;

                        // Set don't obstruct distance
                        Console.WriteLine($"\tDon't obstruct distance: {obj.obstrDistance}");
                        ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:don't obstruct distance")).Data = obj.obstrDistance;

                        // Set don't play distance
                        Console.WriteLine($"\tDon't play distance: {obj.dntPlyDistance}");
                        ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:don't play distance")).Data = obj.dntPlyDistance;

                        // Set attack distance
                        Console.WriteLine($"\tAttack distance: {obj.atkDistance}");
                        ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:attack distance")).Data = obj.atkDistance;

                        // Set minimum distance
                        Console.WriteLine($"\tMinimum distance: {obj.minDistance}");
                        ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:minimum distance")).Data = obj.minDistance;

                        // Set sustain begin distance
                        Console.WriteLine($"\tSustain begin distance: {obj.susBegDistance}");
                        ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:sustain begin distance")).Data = obj.susBegDistance;

                        // Set sustain end distance
                        Console.WriteLine($"\tSustain end distance: {obj.susEndDistance}");
                        ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:sustain end distance")).Data = obj.susEndDistance;

                        // Set maximum distance
                        Console.WriteLine($"\tMaximum distance: {obj.maxDistance}");
                        ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:maximum distance")).Data = obj.maxDistance;

                        // Set sustain Db
                        Console.WriteLine($"\tSustain Db: {obj.sustainDb}");
                        ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:sound_scenery/Struct:override distance parameters/Real:sustain db")).Data = obj.sustainDb;
                    }
                    // Weapon specific settings
                    else if (objectType == "weapons")
                    {
                        // Set rounds left
                        Console.WriteLine($"\tRounds left: {obj.roundsLeft}");
                        ((TagFieldElementInteger)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:weapon data/ShortInteger:rounds left")).Data = obj.roundsLeft;

                        // Set rounds loaded
                        Console.WriteLine($"\tRounds loaded: {obj.roundsLoaded}");
                        ((TagFieldElementInteger)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:weapon data/ShortInteger:rounds loaded")).Data = obj.roundsLoaded;

                        // Set weapon-specific flags
                        Console.WriteLine($"\tWeapon flags: {obj.weapFlags}");
                        ((TagFieldFlags)tagFile.SelectField($"Block:{objectType}[{i}]/Struct:weapon data/Flags:flags")).RawValue = obj.weapFlags;
                    }
                }
                else
                {
                    // Decal-specific data
                    ((TagFieldBlockIndex)tagFile.SelectField($"Block:{objectType}[{i}]/ShortBlockIndex:decal palette index")).Value = obj.typeIndex;
                    Console.WriteLine($"\tType index: {obj.typeIndex}");

                    ((TagFieldFlags)tagFile.SelectField($"Block:{objectType}[{i}]/ByteFlags:flags")).RawValue = 1;
                    Console.WriteLine($"\tPlanar flag: True");

                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{objectType}[{i}]/RealPoint3d:position")).Data = obj.position;
                    Console.WriteLine($"\tPosition: {obj.position[0]}, {obj.position[1]}, {obj.position[2]}");

                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:{objectType}[{i}]/RealQuaternion:rotation")).Data = obj.rotation;
                    Console.WriteLine($"\tRotation: {obj.rotation[0]}, {obj.rotation[1]}, {obj.rotation[2]}");
                    
                    // Get default scaling data from decal system tag
                    TagPath decalSystem = ((TagFieldReference)tagFile.SelectField($"Block:decal palette[{obj.typeIndex}]/Reference:reference")).Path;

                    //((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Real:scale y")).Data = obj.scaleY;
                    //Console.WriteLine($"\tScale Y: {obj.scaleY}");

                    try
                    {
                        using (TagFile decSysTag = new TagFile(decalSystem))
                        {
                            float scaleX = ((TagFieldElementArraySingle)decSysTag.SelectField($"RealPoint2d:decal scale override")).Data[0];
                            float scaleY = ((TagFieldElementArraySingle)decSysTag.SelectField($"RealPoint2d:decal scale override")).Data[1];

                            if (scaleX != 1.0f || scaleY != 1.0f)
                            {
                                ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Real:scale x")).Data = scaleX;
                                Console.WriteLine($"\tScale X: {scaleX}");

                                ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Real:scale y")).Data = scaleY;
                                Console.WriteLine($"\tScale Y: {scaleY}");
                            }
                            else
                            {
                                ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Real:scale x")).Data = obj.scaleX;
                                Console.WriteLine($"\tScale X: {obj.scaleX}");

                                ((TagFieldElementSingle)tagFile.SelectField($"Block:{objectType}[{i}]/Real:scale y")).Data = obj.scaleY;
                                Console.WriteLine($"\tScale Y: {obj.scaleY}");
                            }

                            Console.WriteLine($"Reach scale X:{obj.scaleX} Y:{obj.scaleY}");

                            int decalsCount = ((TagFieldBlock)decSysTag.SelectField($"Block:decals")).Elements.Count;
                            if ((obj.scaleX == 1.0f || obj.scaleX == 0.0f) && (obj.scaleY == 1.0f || obj.scaleY == 0.0f))
                            {
                                Console.WriteLine("Setting decal system radius back to reach value");
                                // Decal placement scaling is 0 or 1, thus we need to revert decal system radius back to JSON value
                                float[] radiusValues = objDefinitions[obj.typeIndex].decalSettings[0].radius;
                                for (int x = 0; x < decalsCount; x++)
                                {
                                    ((TagFieldElementArraySingle)decSysTag.SelectField($"Block:decals[{x}]/RealBounds:radius")).Data = radiusValues;
                                    decSysTag.Save();
                                }
                            }
                            else
                            {
                                // Otherwise set decal system radius to 1
                                Console.WriteLine("Setting decal system scale to 1");
                                for (int x = 0; x < decalsCount; x++)
                                {
                                    ((TagFieldElementArraySingle)decSysTag.SelectField($"Block:decals[{x}]/RealBounds:radius")).Data = new[] { 1.0f, 1.0f };
                                    decSysTag.Save();
                                }
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Error when accessing decal system to get default scale, or it does not exist");
                    }

                    
                }

                i++;
            }
        }
    
        public static void SetTrigVolData(TagFile tagFile, List<TriggerVolume> trigVols)
        {
            int i = 0;
            ((TagFieldBlock)tagFile.SelectField($"Block:trigger volumes")).RemoveAllElements();
            foreach (TriggerVolume trigVol in trigVols)
            {
                Console.WriteLine($"Trigger volume {i}:");
                ((TagFieldBlock)tagFile.SelectField($"Block:trigger volumes")).AddElement();

                ((TagFieldElementStringID)tagFile.SelectField($"Block:trigger volumes[{i}]/StringID:name")).Data = trigVol.name;
                Console.WriteLine($"\tName: {trigVol.name}");

                ((TagFieldBlockIndex)tagFile.SelectField($"Block:trigger volumes[{i}]/ShortBlockIndex:object name")).Value = trigVol.objNameIndex;
                Console.WriteLine($"\tObject name index: {trigVol.objNameIndex}");

                ((TagFieldElementInteger)tagFile.SelectField($"Block:trigger volumes[{i}]/ShortInteger:runtime node index")).Data = trigVol.nodeIndex;
                Console.WriteLine($"\tRuntime node index: {trigVol.nodeIndex}");

                ((TagFieldElementStringID)tagFile.SelectField($"Block:trigger volumes[{i}]/StringID:node name")).Data = trigVol.nodeName;
                Console.WriteLine($"\tNode name: {trigVol.nodeName}");

                ((TagFieldEnum)tagFile.SelectField($"Block:trigger volumes[{i}]/ShortEnum:type")).Value = trigVol.type;
                Console.WriteLine($"\tType: {trigVol.type}");

                ((TagFieldElementArraySingle)tagFile.SelectField($"Block:trigger volumes[{i}]/RealVector3d:forward")).Data = trigVol.forward;
                Console.WriteLine($"\tForward: {trigVol.forward[0]}, {trigVol.forward[1]}, {trigVol.forward[2]}");

                ((TagFieldElementArraySingle)tagFile.SelectField($"Block:trigger volumes[{i}]/RealVector3d:up")).Data = trigVol.up;
                Console.WriteLine($"\tUp: {trigVol.up[0]}, {trigVol.up[1]}, {trigVol.up[2]}");

                ((TagFieldElementArraySingle)tagFile.SelectField($"Block:trigger volumes[{i}]/RealPoint3d:position")).Data = trigVol.position;
                Console.WriteLine($"\tPosition: {trigVol.position[0]}, {trigVol.position[1]}, {trigVol.position[2]}");

                ((TagFieldElementArraySingle)tagFile.SelectField($"Block:trigger volumes[{i}]/RealPoint3d:extents")).Data = trigVol.extents;
                Console.WriteLine($"\tExtents: {trigVol.extents[0]}, {trigVol.extents[1]}, {trigVol.extents[2]}");

                ((TagFieldElementSingle)tagFile.SelectField($"Block:trigger volumes[{i}]/Real:z sink")).Data = trigVol.zSink;
                Console.WriteLine($"\tZ sink: {trigVol.zSink}");

                int j = 0;
                foreach (TriggerVolume.SectorPoint sectorPoint in trigVol.sectorPoints)
                {
                    Console.WriteLine($"\tSector point {j}:");
                    ((TagFieldBlock)tagFile.SelectField($"Block:trigger volumes[{i}]/Block:sector points")).AddElement();

                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:trigger volumes[{i}]/Block:sector points[{j}]/RealPoint3d:position")).Data = sectorPoint.position;
                    Console.WriteLine($"\t\tPosition: {sectorPoint.position[0]}, {sectorPoint.position[1]}, {sectorPoint.position[2]}");

                    ((TagFieldElementArraySingle)tagFile.SelectField($"Block:trigger volumes[{i}]/Block:sector points[{j}]/RealEulerAngles2d:normal")).Data = sectorPoint.normal;
                    Console.WriteLine($"\t\tNormal: {sectorPoint.normal[0]}, {sectorPoint.normal[1]}");

                    j++;
                }

                i++;
            }
        }
    
        public static void SetDecalSystemData(ObjectDefinition decalSys, TagPath decalSysPath)
        {
            var decalFile = new TagFile();
            if (File.Exists(decalSysPath.Filename))
            {

                try
                {
                    decalFile.Load(decalSysPath);
                    Console.WriteLine($"\tTagfile \"{decalSysPath.RelativePath}\" opened\n\tWriting decal material data:");

                    int i = 0;
                    foreach (ObjectDefinition.DecalSettings decalShader in decalSys.decalSettings)
                    {
                        ((TagFieldBlock)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters")).RemoveAllElements();

                        // Determine which shader to use based on the presence of bitmaps
                        string shaderPath = GetShaderPath();
                        Console.WriteLine($"\t\tUsing {shaderPath} material shader");

                        // Set the material shader path
                        TagPath matShader = TagPath.FromPathAndExtension($"shaders\\material_shaders\\pepper\\{shaderPath}", "material_shader");
                        ((TagFieldReference)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Reference:material shader")).Path = matShader;

                        // Set the base map
                        AddMaterialParameter("color_map", decalShader.baseRef);

                        // Set the alpha map if available
                        if (decalShader.alphaRef != null)
                        {
                            AddMaterialParameter("alpha_map", decalShader.alphaRef);
                        }

                        // Set the bump map if available
                        if (decalShader.bumpRef != null && decalShader.blendMode != 2)
                        {
                            AddMaterialParameter("normal_map", decalShader.bumpRef);
                        }

                        // Set vector map if available
                        if (decalShader.vectorRef != null && (decalShader.albedoMode == 8 || decalShader.albedoMode == 9))
                        {
                            AddMaterialParameter("vector_map", decalShader.vectorRef);
                        }

                        // This is quite nasty but is pretty much the only way to determine which mat shader to use
                        string GetShaderPath()
                        {
                            bool IsBase() => decalShader.baseRef != null;
                            bool IsAlpha() => decalShader.alphaRef != null;
                            bool IsBump() => decalShader.bumpRef != null;
                            bool IsVector() => decalShader.vectorRef != null;
                            bool IsNotAlpha() => decalShader.alphaRef == null;
                            bool IsNotBump() => decalShader.bumpRef == null;
                            bool IsNotVector() => decalShader.vectorRef == null;
                            bool IsBlendModeNotMultiply() => decalShader.blendMode != 2;
                            bool IsSpecialAlbedoMode() => decalShader.albedoMode == 8 || decalShader.albedoMode == 9;

                            if (IsBase() && IsNotAlpha() && IsNotBump() && IsNotVector())
                            {
                                return "decal_base";
                            }
                            else if (IsBase() && IsAlpha() && IsNotBump() && IsNotVector())
                            {
                                return "decal_base_alpha";
                            }
                            else if (IsBase() && IsBump() && IsNotAlpha() && decalShader.bumpMode > 0)
                            {
                                return IsBlendModeNotMultiply() ? "decal_base_normal" : "decal_base";
                            }
                            else if (IsBase() && IsBump() && IsAlpha() && decalShader.bumpMode > 0)
                            {
                                return IsBlendModeNotMultiply() ? "decal_base_alpha_normal" : "decal_base_alpha";
                            }
                            else if (IsBase() && IsVector() && IsSpecialAlbedoMode() && decalShader.bumpMode <= 0)
                            {
                                return "decal_vector_alpha";
                            }

                            Console.WriteLine("\t\tFailed to determine material shader for decal system");
                            return "invalid";
                        }

                        // This function handles all the managedblam side of things for adding bitmap references
                        void AddMaterialParameter(string parameterName, string bitmapPath)
                        {
                            if (bitmapPath == null) return;

                            TagPath map = TagPath.FromPathAndExtension(bitmapPath, "bitmap");
                            ((TagFieldBlock)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters")).AddElement();
                            int paramIndex = ((TagFieldBlock)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters")).Elements.Count - 1;

                            ((TagFieldElementStringID)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters[{paramIndex}]/StringID:parameter name")).Data = parameterName;
                            ((TagFieldReference)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters[{paramIndex}]/Reference:bitmap")).Path = map;
                        }

                        // Set blend mode
                        ((TagFieldEnum)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/CharEnum:alpha blend mode")).Value = (int)decalShader.blendMode;
                        Console.WriteLine($"\t\tBlend mode: {decalShader.blendMode}");

                        // Set radius to 1 if square to combat Reach scale getting combined with radius
                        int decalCount = ((TagFieldBlock)decalFile.SelectField($"Block:decals")).Elements.Count;
                        for (int x = 0; x < decalCount; x++)
                        {
                            TagPath diffusePath = ((TagFieldReference)decalFile.SelectField($"Block:decals[{x}]/Struct:actual material?/Block:material parameters[0]/Reference:bitmap")).Path;
                            using (TagFile diffuseTag = new TagFile(diffusePath))
                            {
                                long width = ((TagFieldElementInteger)diffuseTag.SelectField($"Block:bitmaps[0]/ShortInteger:width")).Data;
                                long height = ((TagFieldElementInteger)diffuseTag.SelectField($"Block:bitmaps[0]/ShortInteger:height")).Data;
                                /*
                                if (height == width)
                                {
                                    // Bitmap is square, apply 1.0 radius
                                    ((TagFieldElementArraySingle)decalFile.SelectField($"Block:decals[{x}]/RealBounds:radius")).Data = new[] { 1.0f, 1.0f };
                                }
                                */
                            }

                            ((TagFieldElementArraySingle)decalFile.SelectField($"RealPoint2d:decal scale override[{x}]")).Data = decalShader.scaleXY;
                        }

                        // Set colour tinting data, if said data is present
                        if (decalShader.tintColour != null)
                        {
                            int paramIndex = ((TagFieldBlock)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters")).Elements.Count - 1;
                            if (!decalShader.tintColour.SequenceEqual(new float[] { 1.0f, 1.0f, 1.0f }))
                            {
                                // Colour
                                ((TagFieldBlock)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters")).AddElement();
                                paramIndex++;
                                ((TagFieldElementStringID)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters[{paramIndex}]/StringID:parameter name")).Data = "tint_color";
                                ((TagFieldEnum)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters[{paramIndex}]/LongEnum:parameter type")).Value = 4;

                                GameColor colour = GameColor.FromRgb(decalShader.tintColour[0], decalShader.tintColour[1], decalShader.tintColour[2]);
                                ((TagFieldElementArraySingle)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters[{paramIndex}]/RealArgbColor:color")).Data = new float[] { 1.0f, colour.Red, colour.Green, colour.Blue };
                                ((TagFieldBlock)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters[{paramIndex}]/Block:function parameters")).AddElement();
                                ((TagFieldCustomFunctionEditor)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters[{paramIndex}]/Block:function parameters[0]/Custom:function")).Value.SetColor(0, colour);
                                Console.WriteLine($"\t\tTint colour: {colour.Red}, {colour.Green}, {colour.Blue}");
                            }
                            // Tint intensity
                            ((TagFieldBlock)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters")).AddElement();
                            paramIndex++;
                            ((TagFieldElementStringID)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters[{paramIndex}]/StringID:parameter name")).Data = "tint_intensity";
                            ((TagFieldEnum)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters[{paramIndex}]/LongEnum:parameter type")).Value = 1;
                            ((TagFieldElementSingle)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters[{paramIndex}]/Real:real")).Data = decalShader.tintIntensity;

                            // Tint modulation
                            ((TagFieldBlock)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters")).AddElement();
                            paramIndex++;
                            ((TagFieldElementStringID)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters[{paramIndex}]/StringID:parameter name")).Data = "tint_modulation_factor";
                            ((TagFieldEnum)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters[{paramIndex}]/LongEnum:parameter type")).Value = 1;
                            ((TagFieldElementSingle)decalFile.SelectField($"Block:decals[{i}]/Struct:actual material?/Block:material parameters[{paramIndex}]/Real:real")).Data = decalShader.modulation;
                        }
                        
                        i++;
                    }

                }
                catch
                {
                    Console.WriteLine($"\tManagedBlam error when writing to \"{decalSysPath.RelativePath}.decal_system\"");
                }
                finally
                {
                    decalFile.Save();
                    decalFile.Dispose();
                    Console.WriteLine($"\tTagfile \"{decalSysPath.RelativePath}.decal_system\" closed\n");
                }
            }
            else
            {
                Console.WriteLine($"\tTag \"{decalSysPath.RelativePathWithExtension}\" does\n\tnot exist, skipping material setup for it.\n");
            }
        }
    }
}
