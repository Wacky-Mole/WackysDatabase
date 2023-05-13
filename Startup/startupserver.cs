using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using wackydatabase.Datas;
using wackydatabase.Util;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Core;
using System.Security.Policy;
using HarmonyLib;
using wackydatabase.GetData;
using static Interpolate;
using System.Reflection;
using wackydatabase.OBJimporter;

namespace wackydatabase.Startup
{

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZrouteMethodsAdminReloadPushOnServer
    {
        internal static void Prefix()
        {
            if (!ZNet.instance.IsServer()) return; // for servers only

            WMRecipeCust.WLog.LogInfo("Server Ready to receive AdminReload");
           // ZRoutedRpc.instance.Register($"{WMRecipeCust.ModName} AdminReload",new Action<long, bool>(WMRecipeCust.AdminReload));

            ZRoutedRpc.instance.Register("WackyDBAdminReload", new Action<long, ZPackage>(WMRecipeCust.AdminReload));
            ZRoutedRpc.instance.Register("WackyDBAdminBigData", new Action<long, ZPackage>(HandleData.SendData));

            if (WMRecipeCust.Firstrun)
            {
                ObjModelLoader.LoadObjs(); // This means will never get sync data, but that's okay?
                if (ZNet.instance.IsServer() & ZNet.instance.IsDedicated() && !WMRecipeCust.ServerDedLoad.Value)
                    WMRecipeCust.Firstrun = false;
            }

        }
    }


    /*
    [HarmonyPatch]
    public static class Checkme
    {
        private static MethodInfo TargetMethod()
        {
            var paul = AccessTools.Method(typeof(ZSteamMatchmaking), "<OnServerResponded>g__TryConvertTagsStringToDictionary|37_0");

            return paul;

        }

        private static void Prefix(string tagsString)
        {
            //print tagstring
            WMRecipeCust.WLog.LogWarning("ZsteamString " + tagsString);
        }
    }
    */



    public class Startupserver
    {


         public IEnumerable<string> CheckForJsons()
        {
            WMRecipeCust.CheckModFolder();


            var allfiles = Directory.GetFiles(WMRecipeCust.assetPathconfig, "*.json", SearchOption.AllDirectories);//.Where(f => Path.(f) != WMRecipeCust.oldjsons).ToArray(); 
            //var filesToExclude = Directory.GetFiles(WMRecipeCust.assetPathOldJsons, "");
            //var wantedfiles = allfiles.Except(filesToExclude);

            foreach (string file in allfiles)
            {

                WMRecipeCust.jsonsFound = true;
                break;
            }

            return allfiles;
        }

        internal void SaveYMLBasedONJsons(IEnumerable<string> wantedfiles)
        {

            var serializer = new SerializerBuilder()
            .Build();

            var deslizer = new DeserializerBuilder().Build();

            if (!Directory.Exists(WMRecipeCust.assetPathOldJsons))
            {
                WMRecipeCust.Dbgl("Creating OldJsonFolder");
                Directory.CreateDirectory(WMRecipeCust.assetPathOldJsons);
            }

            GetDataYML ObjectCheck = new GetDataYML();


            foreach (string file in wantedfiles)
            {

                if (file.Contains("Item") || file.Contains("item")) // items are being rather mean with the damage classes
                {
                    try
                    {

                        var output = deslizer.Deserialize<WItemData_json>(File.ReadAllText(file));

                        WItemData temp1 = ObjectCheck.GetItemDataByName(output.name, ObjectDB.instance);
                        if (temp1 == null)
                            continue;
                        if (output.clone)
                            temp1.clonePrefabName = output.clonePrefabName;

                        // File.WriteAllText(Path.Combine(WMRecipeCust.assetPathItems, "Item_" + temp1.name + ".yml"), serializer.Serialize(temp1));

                        var copyfile = file;
                        File.Copy(file, Path.Combine(WMRecipeCust.assetPathOldJsons, "Item_" + output.name + ".json"));
                        copyfile = Path.ChangeExtension(file, ".yml");
                        File.WriteAllText(copyfile, serializer.Serialize(temp1));
                        File.Delete(file);

                    }
                    catch { WMRecipeCust.WLog.LogWarning("Something went wrong in file " + file); }

                }
                else if (file.Contains("Piece") || file.Contains("piece"))
                {
                    try
                    {
                        var output = deslizer.Deserialize<PieceData_json>(File.ReadAllText(file));

                        PieceData temp1 = ObjectCheck.GetPieceRecipeByName(output.name, ObjectDB.instance);
                        if (temp1 == null)
                            continue;
                        if (output.clone)
                            temp1.clonePrefabName = output.clonePrefabName;
                        if (output.disabled)
                            temp1.disabled = true;
                        if(output.adminonly)
                            temp1.adminonly = true;


                        var copyfile = file;
                        File.Copy(file, Path.Combine(WMRecipeCust.assetPathOldJsons, "Piece_" + output.name + ".json"));
                        copyfile = Path.ChangeExtension(file, ".yml");
                        File.WriteAllText(copyfile, serializer.Serialize(temp1));
                        File.Delete(file);

                    }
                    catch { WMRecipeCust.WLog.LogWarning("Something went wrong in file " + file); }
                }
                else if (file.Contains("Recipe") || file.Contains("recipe"))
                {
                    try
                    {
                        var output = deslizer.Deserialize<RecipeData_json>(File.ReadAllText(file));

                        RecipeData temp1 = ObjectCheck.GetRecipeDataByName(output.name, ObjectDB.instance);
                        if (temp1 == null)
                            continue;
                        if (output.clone)
                            temp1.clonePrefabName = output.clonePrefabName;
                        if (output.disabled)
                            temp1.disabled = true;

                        var copyfile = file;
                        File.Copy(file, Path.Combine(WMRecipeCust.assetPathOldJsons, "Recipe_" + output.name + ".json"));
                        copyfile = Path.ChangeExtension(file, ".yml");
                        File.WriteAllText(copyfile, serializer.Serialize(temp1));
                        File.Delete(file);
                        
                        
                        
                 

                        

                    }
                    catch { WMRecipeCust.WLog.LogWarning("Something went wrong in file " + file); }
                }
            }
        }

        internal void BeginConvertingJsons(IEnumerable<string> wantedfiles)
        {
            var serializer = new SerializerBuilder()
            .Build();

            var deslizer = new DeserializerBuilder().Build();

            if (!Directory.Exists(WMRecipeCust.assetPathOldJsons))
            {
                WMRecipeCust.Dbgl("Creating OldJsonFolder");
                Directory.CreateDirectory(WMRecipeCust.assetPathOldJsons);
            }

            foreach (string file in wantedfiles)
                {

                    if (file.Contains("Item") || file.Contains("item")) // items are being rather mean with the damage classes
                    {
                        try
                        {

                            var output = deslizer.Deserialize<WItemData>(File.ReadAllText(file));
                            var yaml = serializer.Serialize(output);
                            
                           // var output1 = deslizer.Deserialize<ArmorData>(File.ReadAllText(file)); // I will have to look at this code for armor again
                            //var yaml1 = serializer.Serialize(output1);

                            File.WriteAllText(Path.Combine(WMRecipeCust.assetPathItems, "Item_" + output.name + ".yml"), yaml);

                            File.Move(file, Path.Combine(WMRecipeCust.assetPathOldJsons, "Item_" + output.name + ".json"));

                            //File.WriteAllText(Path.Combine(WMRecipeCust.assetPathOldJsons, "Item_" + output.name + ".json"), file);
                            //File.Delete(file);

                        }
                        catch { WMRecipeCust.WLog.LogWarning("Something went wrong in file " + file); }

                    }
                    else if (file.Contains("Piece") || file.Contains("piece"))
                    {
                        try
                        {
                            var output = deslizer.Deserialize<PieceData>(File.ReadAllText(file));
                            var yaml = serializer.Serialize(output);

                            File.WriteAllText(Path.Combine(WMRecipeCust.assetPathPieces, "Piece_" + output.name + ".yml"), yaml);
                        File.Move(file, Path.Combine(WMRecipeCust.assetPathOldJsons, "Piece_" + output.name + ".json"));

                        //File.WriteAllText(Path.Combine(WMRecipeCust.assetPathOldJsons, "Piece_" + output.name + ".json"), file);
                        //  File.Delete(file);


                    }
                        catch { WMRecipeCust.WLog.LogWarning("Something went wrong in file " + file); }
                    }
                    else // recipes
                    {
                        try
                        {
                            var output = deslizer.Deserialize<RecipeData>(File.ReadAllText(file));
                            var yaml = serializer.Serialize(output);

                            File.WriteAllText(Path.Combine(WMRecipeCust.assetPathRecipes, "Recipe_" + output.name + ".yml"), yaml);
                        File.Move(file, Path.Combine(WMRecipeCust.assetPathOldJsons, "Recipe_" + output.name + ".json"));

                        //File.WriteAllText(Path.Combine(WMRecipeCust.assetPathOldJsons, "Recipe_" + output.name + ".json"), file);
                        //  File.Delete(file);

                    }
                        catch { WMRecipeCust.WLog.LogWarning("Something went wrong in file " + file); }

                    }
                }


                // Should  Write YML files with Datas
                /*
                 * 
                if (!Directory.Exists(assetPathOldJsons))
                {
                    Dbgl("Creating Jsons old folder");
                    Directory.CreateDirectory(assetPathOldJsons);
                }

                var targetList = WMRecipeCust.recipeDatas
                 .Select(x => new TargetType() { amount = x.amount })
                 .ToList();
                https://docs.microsoft.com/en-us/dotnet/standard/generics/covariance-and-contravariance?redirectedfrom=MSDN
                IEnumerable<Derived> d = new List<Derived>(); // new list
                IEnumerable<Base> b = d; // old list base
                Action<Base> b = (target) => { Console.WriteLine(target.GetType().Name); };
                Action<Derived> d = b;
                d(new Derived());
                List<recipeDatas>
                    https://stackoverflow.com/questions/2033912/c-sharp-variance-problem-assigning-listderived-as-listbase


                Write ALL YML Files then Delete
                Directory.Delete(WMRecipeCust.assetPathconfig);
                foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathconfig, "*.json", SearchOption.AllDirectories)){ // safer
                Directory.Delete(file, false);
                }

                // wait a second?
                Directory.CreateDirectory(WMRecipeCust.assetPathconfig);
                */

        }
    }
}
