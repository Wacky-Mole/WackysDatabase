using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.IO;
using System.Security.Cryptography;


using wackydatabase.Datas;
using wackydatabase.Util;
using wackydatabase.GetData;


namespace wackydatabase.PatchClasses
{
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    public static class Console_Patch
    {
        private static void Postfix()
        {
            WMRecipeCust.WLog.LogDebug("Patching Updated Console Commands");

            if (!WMRecipeCust.modEnabled.Value)
                return;
            if (SceneManager.GetActiveScene().name != "main") return; // can't do anything from main


            Terminal.ConsoleCommand WackyShowcommands =
                new("wackydb", "Display Help ",
                    args =>
                    {
                        string output = $"wackydb_reload\r\n"
                        // + $"wackydb_reset \r\n"
                        + $"wackydb_dump [item/recipe/piece] [ObjectName]\r\n"
                        + $"wackydb_dump_all (dumps all info already loaded - may not work with clones very well)\r\n"
                        + $"wackydb_save_recipe  [RecipeName](recipe output)\r\n"
                        + $"wackydb_save_piece [PieceName](piece output) \r\n"
                        + $"wackydb_save_item [ItemName](item Output)\r\n"
                        + $"wackydb_help\r\n"
                        + $"wackydb_clone  [item/recipe/piece] [Prefab to clone] [Unique name for the clone] \r\n"
                        + $"4th paramater for recipes - you can already have item WackySword loaded in game, but now want a recipe. WackySword Uses SwordIron  - wackydb_clone recipe WackySword RWackySword SwordIron - otherwise manually edit\r\n"
                        + $"wackydb_clone_recipeitem  [Prefab to clone] [Unique name for the clone](Recipe name will be Rname) (clones item and recipe at same time)\r\n"
                        + $"wackydb_vfx (outputs future Vfx gameobjects available)\r\n"
                        + $"wackydb_material (outputs Materials available)\r\n"

                        ;

                        args.Context?.AddString(output);
                    });

            Terminal.ConsoleCommand WackyShowcommandshelp =
                 new("wackydb_help", "Display Help ",
                     args =>
                     {
                         string output = $"wackydb_reload\r\n"
                        // + $"wackydb_reset \r\n"
                        + $"wackydb_dump [item/recipe/piece] [ObjectName]\r\n"
                        + $"wackydb_dump_all (dumps all info already loaded - may not work with clones very well)\r\n"
                        + $"wackydb_save_recipe  [RecipeName](recipe output)\r\n"
                        + $"wackydb_save_piece [PieceName](piece output) \r\n"
                        + $"wackydb_save_item [ItemName](item Output)\r\n"
                        + $"wackydb_help\r\n"
                        + $"wackydb_clone  [item/recipe/piece] [Prefab to clone] [Unique name for the clone] \r\n"
                        + $"4th paramater for recipes - you can already have item WackySword loaded in game, but now want a recipe. WackySword Uses SwordIron  - wackydb_clone recipe WackySword RWackySword SwordIron - otherwise manually edit\r\n"
                        + $"wackydb_clone_recipeitem  [Prefab to clone] [Unique name for the clone](Recipe name will be Rname) (clones item and recipe at same time)\r\n"
                        + $"wackydb_vfx (outputs future Vfx gameobjects available)\r\n"
                        + $"wackydb_material (outputs Materials available)\r\n"

                      ;

                         args.Context?.AddString(output);
                     });
            /*
            Terminal.ConsoleCommand WackyReset =
                 new("wackydb_reset", "reload the whole config files", // I should probably delete this one?
                     args =>
                     {
                         context.Config.Reload();
                         context.Config.Save();
                         args.Context?.AddString("Configs reloaded");
                     });
            */
            Terminal.ConsoleCommand WackyReload =
                 new("wackydb_reload", "reload the whole config files",
                     args =>
                     {
                         // GetRecipeDataFromFiles(); called in loadallrecipes
                         if (ObjectDB.instance && wackydatabase.WMRecipeCust.issettoSinglePlayer)
                         {

                             SetData.Reload josh = new SetData.Reload();
                             //WMRecipeCust.CurrentReload = josh;
                             // this needs to be fix

                             josh.LoadAllRecipeData(true);

                             
                             
                             args.Context?.AddString($"WackyDatabase reloaded recipes/items/pieces from files");
                             wackydatabase.WMRecipeCust.Dbgl("WackyDatabase reloaded recipes/items/pieces from files");
                         }
                         else
                         {
                             args.Context?.AddString($"WackyDatabase did NOT reload recipes/items/pieces from files"); // maybe?
                             wackydatabase.WMRecipeCust.Dbgl("WackyDatabase did NOT reload recipes/items/pieces from files");
                         }

                     });

            Terminal.ConsoleCommand WackyDump =
                 new("wackydb_dump", "dump the item or recipe into the logs",
                     args =>
                     {
                         if (args.Length - 1 < 2)
                         {
                             args.Context?.AddString("Not enough arguments");

                         }
                         else
                         {
                             string recipe = args[1];
                             string comtype = args[2];
                             if (recipe == "item" || recipe == "Item")
                             {
                                 WItemData_json recipeData = GetData.GetData.GetItemDataByName(comtype);
                                 if (recipeData == null)
                                     return;
                                 WMRecipeCust.Dbgl(JsonUtility.ToJson(recipeData));

                             }
                             else if (recipe == "piece" || recipe == "Piece")
                             {
                                 PieceData_json data = GetData.GetData.GetPieceRecipeByName(comtype);
                                 if (data == null)
                                     return;
                                 WMRecipeCust.Dbgl(JsonUtility.ToJson(data));
                             }
                             else
                             {
                                 RecipeData_json recipeData = GetData.GetData.GetRecipeDataByName(comtype);
                                 if (recipeData == null)
                                     return;
                                 WMRecipeCust.Dbgl(JsonUtility.ToJson(recipeData));
                             }
                             args.Context?.AddString($"WackyDatabase dumped {comtype}");
                         }
                     });

            Terminal.ConsoleCommand WackyDumpAll =
                 new("wackydb_dump_all", "dump all",
                     args =>
                     {
                         string TheStringMaster = "";
                         string temp = "";
                         if (WMRecipeCust.issettoSinglePlayer)
                         {
                             foreach (var data in wackydatabase.WMRecipeCust.ItemDatas)
                             {
                                 if (data != null)
                                 {
                                     WItemData_json output1 = GetData.GetData.GetItemDataByName(data.name);
                                     if (output1 == null)
                                         continue;
                                     output1.clone = data.clone;
                                     output1.cloneMaterial = data?.cloneMaterial;
                                     output1.clonePrefabName = data?.clonePrefabName;
                                     temp = JsonUtility.ToJson(output1);
                                     TheStringMaster = TheStringMaster + temp + System.Environment.NewLine;
                                     WMRecipeCust.Dbgl(temp);
                                 }
                             }
                             foreach (var data2 in WMRecipeCust.PieceDatas)
                             {
                                 if (data2 != null)
                                 {
                                     PieceData_json output2 = GetData.GetData.GetPieceRecipeByName(data2.name, false);
                                     if (output2 == null)
                                         continue;
                                     output2.clone = data2.clone;
                                     output2.cloneMaterial = data2.cloneMaterial;
                                     output2.clonePrefabName = data2?.clonePrefabName;
                                     output2.piecehammer = data2.piecehammer;
                                     temp = JsonUtility.ToJson(output2);
                                     TheStringMaster = TheStringMaster + temp + System.Environment.NewLine;
                                     WMRecipeCust.Dbgl(temp);
                                 }
                             }
                             foreach (var data3 in WMRecipeCust.recipeDatas)
                             {
                                 if (data3 != null)
                                 {
                                     RecipeData_json output3 = GetData.GetData.GetRecipeDataByName(data3.name);
                                     if (output3 == null)
                                         continue;
                                     output3.clone = data3.clone;
                                     //output3.cloneColor = data3.cloneColor;
                                     output3.clonePrefabName = data3.clonePrefabName;
                                     temp = JsonUtility.ToJson(output3);
                                     TheStringMaster = TheStringMaster + temp + System.Environment.NewLine;
                                     WMRecipeCust.Dbgl(temp);
                                 }
                             }
                             File.WriteAllText(Path.Combine(WMRecipeCust.assetPathconfig, "DumpAll.txt"), TheStringMaster);
                             args.Context?.AddString($"WackyDatabase dumped all, created file DumpAll.txt");
                         }
                         else
                         {
                             args.Context?.AddString($"In Multiplayer, so no all dump");
                         }

                     });

            Terminal.ConsoleCommand WackyitemSave =
                new("wackydb_save_item", "Save an Item ",
                    args =>
                    {
                        string file = args[1];
                        WItemData_json recipData = GetData.GetData.GetItemDataByName(file);
                        if (recipData == null)
                            return;
                        WMRecipeCust.CheckModFolder();
                        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathItems, "Item_" + recipData.name + ".json"), JsonUtility.ToJson(recipData, true));
                        args.Context?.AddString($"saved item data to Item_{file}.json");

                    });
            Terminal.ConsoleCommand WackyPieceSave =
                new("wackydb_save_piece", "Save a piece ",
                    args =>
                    {
                        string file = args[1];
                        PieceData_json recipData = GetData.GetData.GetPieceRecipeByName(file);
                        if (recipData == null)
                            return;
                        WMRecipeCust.CheckModFolder();
                        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathPieces, "Piece_" + recipData.name + ".json"), JsonUtility.ToJson(recipData, true));
                        args.Context?.AddString($"saved data to Piece_{file}.json");

                    });
            Terminal.ConsoleCommand WackyRecipeSave =
new("wackydb_save_recipe", "Save a recipe ",
    args =>
    {
        string file = args[1];
        RecipeData_json recipData = GetData.GetData.GetRecipeDataByName(file);
        if (recipData == null)
            return;
        WMRecipeCust.CheckModFolder();
        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathRecipes, "Recipe_" + recipData.name + ".json"), JsonUtility.ToJson(recipData, true));
        args.Context?.AddString($"saved data to Recipe_{file}.json");

    });

            Terminal.ConsoleCommand WackyMaterials =
new("wackydb_material", "Create txt file of materials",
    args =>
    {
        string theString = Functions.GetAllMaterialsFile();
        WMRecipeCust.CheckModFolder();
        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathconfig, "Materials.txt"), theString);
        args.Context?.AddString($"saved data to Materials.txt");

    });

            Terminal.ConsoleCommand Wackyvfx =
new("wackydb_vfx", "Create txt file of VFX",
    args =>
    {
        string theString2 = Functions.GetAllVFXFile();
        WMRecipeCust.CheckModFolder();
        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathconfig, "vfx.txt"), theString2);
        args.Context?.AddString($"saved data to VFX.txt");

    });

            /* syntax for cloning
             * wackydb_clone <item/recipe/piece> <prefab to clone> <nameofclone>(has to be unquie otherwise we would have to check) 
             * 
             */
            Terminal.ConsoleCommand WackyClone =
                new("wackydb_clone", "Clone an item or piecce with different status, names, effects ect... ",
                    args =>
                    {
                        if (args.Length - 1 < 3)
                        {
                            args.Context?.AddString("<color=lime>Not enough arguments</color>");

                        }
                        else
                        {
                            string commandtype = args[1];
                            string prefab = args[2];
                            string newname = args[3];
                            string file = args[3];
                            if (newname == "SwordTest")
                            {
                                args.Context?.AddString($"<color=red>{newname} is already a ingame name. -Bad </color>");
                                return;
                            }

                            if (commandtype == "recipe" || commandtype == "Recipe")
                            {
                                WMRecipeCust.CheckModFolder();
                                if (args.Length - 1 < 4)
                                {
                                    RecipeData_json clone = GetData.GetData.GetRecipeDataByName(prefab);// actually it could be a different prefab if cloned item
                                    if (clone == null)
                                        return;
                                    clone.name = newname;
                                    clone.clone = true;
                                    clone.clonePrefabName = prefab;
                                    File.WriteAllText(Path.Combine(WMRecipeCust.assetPathRecipes, "Recipe_" + clone.name + ".json"), JsonUtility.ToJson(clone, true));
                                    file = "Recipe" + clone.name;
                                }
                                else
                                {
                                    string prefabitem = args[4];
                                    RecipeData_json clone = GetData.GetData.GetRecipeDataByName(prefabitem);//  prefab of cloned item
                                    if (clone == null)
                                        return;
                                    clone.name = newname;
                                    clone.clone = true;
                                    clone.clonePrefabName = prefab; // cloned item
                                    File.WriteAllText(Path.Combine(WMRecipeCust.assetPathRecipes, "Recipe_" + clone.name + ".json"), JsonUtility.ToJson(clone, true));
                                    file = "Cloned Item " + clone.name + " Clone Recipe from " + prefabitem;

                                } // added optional arugment for cloned items


                            }
                            if (commandtype == "item" || commandtype == "Item")
                            {
                                WItemData_json clone = GetData.GetData.GetItemDataByName(prefab);
                                if (clone == null)
                                    return;
                                clone.name = newname;
                                clone.clone = true;
                                clone.clonePrefabName = prefab;
                                clone.m_name = newname;


                                if (clone == null)
                                    return;
                                WMRecipeCust.CheckModFolder();
                                File.WriteAllText(Path.Combine(WMRecipeCust.assetPathItems, "Item_" + clone.name + ".json"), JsonUtility.ToJson(clone, true));
                                file = "Item_" + clone.name;



                            }
                            if (commandtype == "piece" || commandtype == "Piece")
                            {
                                PieceData_json clone = GetData.GetData.GetPieceRecipeByName(prefab);
                                if (clone == null)
                                    return;
                                clone.name = newname;
                                clone.clone = true;
                                clone.clonePrefabName = prefab;



                                if (clone == null)
                                    return;
                                WMRecipeCust.CheckModFolder();
                                File.WriteAllText(Path.Combine(WMRecipeCust.assetPathPieces, "Piece_" + clone.name + ".json"), JsonUtility.ToJson(clone, true));
                                file = "Piece_" + clone.name;

                            }
                            args.Context?.AddString($"saved cloned data to {file}.json");
                        }
                    });
            Terminal.ConsoleCommand WackyCloneRecipe =
                new("wackydb_clone_recipeitem", "Clone recipe and item with the orginal prefab ",
                    args =>
                    {

                        WMRecipeCust.CheckModFolder();
                        if (args.Length - 1 < 2)
                        {
                            args.Context?.AddString("<color=lime>Not enough arguments</color>");
                        }
                        else
                        {
                            string prefab = args[1];
                            string newname = args[2];
                            string file = args[2];
                            WItemData_json itemclone = GetData.GetData.GetItemDataByName(prefab);
                            if (itemclone == null)
                                return;
                            itemclone.name = newname;
                            itemclone.clone = true;
                            itemclone.clonePrefabName = prefab;
                            itemclone.m_name = newname;
                            File.WriteAllText(Path.Combine(WMRecipeCust.assetPathItems, "Item_" + itemclone.name + ".json"), JsonUtility.ToJson(itemclone, true));

                            RecipeData_json clone = GetData.GetData.GetRecipeDataByName(prefab);//  prefab of cloned item
                            if (clone == null)
                                return;
                            clone.name = "R" + newname;
                            clone.clone = true;
                            clone.clonePrefabName = itemclone.name; // cloned item
                            File.WriteAllText(Path.Combine(WMRecipeCust.assetPathRecipes, "Recipe_" + clone.name + ".json"), JsonUtility.ToJson(clone, true));

                            file = "Cloned Item saved as Item_" + itemclone.name + ".json, cloned Recipe saved as Recipe_" + clone.name + ".json which is from the Orginal Recipe " + prefab;
                            args.Context?.AddString($"{file}");
                        }

                    });


            Terminal.ConsoleCommand Wackyadmin =    //dont look :)
                 new("customizationGuessing", "Gives you reload powers if you can guess the password",  // just for fun. Doesn't really give you admin powers
               args =>
               {
                   if (!WMRecipeCust.issettoSinglePlayer && WMRecipeCust.kickcount < 3)// backdoor for funizes only availble when on multiplayer mode.. hahaaa
                   {
                       string passguess = "";
                       try
                       {
                           passguess = args[1];
                       }
                       catch
                       {
                           WMRecipeCust.WLog.LogWarning("Congrats on finding the backdoor... You have 3 chances to guess the password or you will be called out that your a dirty cheater in chat and probably being kicked by Azu or an admin");
                           return;
                       }

                       WMRecipeCust.WLog.LogWarning($"guess {WMRecipeCust.kickcount + 1}");

                       string file = passguess;
                       string hash = Functions.ComputeSha256Hash(file);
                       string secrethash = "f289b4717485d90d9dee6ce2a9992e4fcfa4317a9439c148053d52c637b0691b"; // real hash is entered
                       if (hash == secrethash)
                       {
                           WMRecipeCust.WLog.LogWarning("Congrats you cheater,  Enjoy nothin");

                       }
                       else
                       {
                           WMRecipeCust.kickcount++;
                           if (WMRecipeCust.kickcount >= 3)
                           {
                               //List<string> stringList = ZNet.instance.GetPlayerList().Select(player => player.m_name).ToList();
                               string name = Player.m_localPlayer.name; // someday make this so it shouts to other players
                               Chat.m_instance.AddString("[WackysDatabase]",
                        $"<color=\"red\">Cheater Cheater, pants on fire. {name} tried to get admin access and failed. Laugh at this person or kick them.</color>",
                             Talker.Type.Normal);
                               WMRecipeCust.WLog.LogWarning("Cheater Cheater, pants on fire");
                           }

                       }

                   }

               });



        }
    }
}
