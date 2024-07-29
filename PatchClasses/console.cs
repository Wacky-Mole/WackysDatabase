﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using wackydatabase.Datas;
using wackydatabase.GetData;
using wackydatabase.Read;
using wackydatabase.Util;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace wackydatabase.PatchClasses
{

    public class QuoteSurroundingEventEmitter : ChainedEventEmitter
    {
        private int _itemIndex;

        public QuoteSurroundingEventEmitter(IEventEmitter nextEmitter) : base(nextEmitter) { }

        public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            if (eventInfo.Source.StaticType == typeof(object) && _itemIndex++ % 2 == 1)
            {
                eventInfo.Style = ScalarStyle.SingleQuoted;
            }
            base.Emit(eventInfo, emitter);
        }
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    public static class Console_Patch
    {

        /*
         * 
         * 		new ConsoleCommand("spawn", "[amount] [level] [p/e/i] - spawn something. (End word with a star (*) to create each object containing that word.) Add a 'p' after to try to pick up the spawned items, adding 'e' will try to use/equip, 'i' will only spawn and pickup if you don't have one in your inventory.", delegate(ConsoleEventArgs args)
		{
			if (args.Length <= 1 || !ZNetScene.instance)
			{
				return false;
			}
			string text4 = args[1];
			int count = args.TryParameterInt(2);
			int level2 = args.TryParameterInt(3);
			bool pickup = args.HasArgumentAnywhere("p", 2);
			bool use = args.HasArgumentAnywhere("e", 2);
			bool onlyIfMissing = args.HasArgumentAnywhere("i", 2);
			DateTime now = DateTime.Now;
			if (text4.Length >= 2 && text4[text4.Length - 1] == '*')
			{
				text4 = text4.Substring(0, text4.Length - 1).ToLower();
				foreach (string prefabName in ZNetScene.instance.GetPrefabNames())
				{
					string text5 = prefabName.ToLower();
					if (text5.Contains(text4) && (text4.Contains("fx") || !text5.Contains("fx")))
					{
						spawn(prefabName);
					}
				}
			}
			else
			{
				spawn(text4);
			}
			ZLog.Log("Spawn time :" + (DateTime.Now - now).TotalMilliseconds + " ms");
			Gogan.LogEvent("Cheat", "Spawn", text4, count);
			return true;
			void spawn(string name)
        */
        private static void Postfix()
        {
            WMRecipeCust.WLog.LogDebug("Patching Updated Console Commands");

            if (!WMRecipeCust.modEnabled.Value)
                return;




           Terminal.ConsoleCommand WackyClearCache =
            new("wackydb_clearcache", "ClearCache",
             args =>
             {
                 WMRecipeCust.WLog.LogWarning(" deleteing Cache");
                 WMRecipeCust.DeleteCache();

                 //WMRecipeCust.Dbgl("Checking Cache Folder and Loading Any Item/Mock Clones");
                 // ReadFiles clones = new ReadFiles();
                 //clones.GetCacheClonesOnly();
                 MaterialDataManager.Instance.materials.Clear();
                 TextureDataManager.textureCache.Clear();

                 args.Context?.AddString(" deleteing Cache");

             });

            Terminal.ConsoleCommand WackysnapCache =
        new("wackydb_snapshot", "snapshot",
         args =>
         {
             foreach (var item in WMRecipeCust.SnapshotPiecestoDo)
             {
                 Functions.SnapshotPiece(item);
             }
           //  WMRecipeCust.SnapshotPiecestoDo.Clear();


         });



            if (SceneManager.GetActiveScene().name != "main") return; // can't do anything from main 


            Terminal.ConsoleCommand WackyShowcommands =
                new("wackydb", "Display Help ",
                    args =>
                    {
                        string output = $"wackydb_reload -reloads on client or server\r\n" 
                        // + $"wackydb_reset \r\n"
                        + $"wackydb_save_recipe  [RecipeName](recipe output)\r\n"
                        + $"wackydb_save_piece [PieceName](piece output) \r\n"
                        + $"wackydb_save_item [ItemName](item Output)\r\n"
                        + $"wackydb_save_creature [CreatureName](Creature Output)\r\n"
                        + $"wackydb_save_pickable [PickableName](Pickable Output)\r\n"
                        + $"wackydb_save_material [MaterialName] Save Material Info - Usually has _mat\r\n"
                        + $"wackydb_se [SEEffectName] Save SEeffect\r\n"
                        + $"wackydb_help\r\n"
                        + $"wackydb_clone  [item/recipe/piece/creatures/material/se/pickables] [Prefab to clone] [Unique name for the clone] \r\n"
                        + $"4th paramater for recipes - you can already have item WackySword loaded in game, but now want a recipe. WackySword Uses SwordIron  - wackydb_clone recipe WackySword RWackySword SwordIron - otherwise manually edit\r\n"
                        + $"wackydb_clone_recipeitem  [Prefab to clone] [Unique name for the clone](Recipe name will be Rname) (clones item and recipe at same time)\r\n"
                        + $"wackydb_vfx (outputs vfx gameobjects available)\r\n"
                        + $"wackydb_fx (outputs fx gameobjects available)\r\n"
                        + $"wackydb_sfx (outputs Sfx gameobjects available)\r\n"
                        + $"wackydb_material (outputs Materials available)\r\n"
                        + $"wackydb_sendtheload -experimental\r\n"
                        + $"wackydb_get_piecehammers -Get All Hammer in Game\r\n"
                        + $"wackydb_all_se -Save All SE Effects to Bulk Folder\r\n"
                        + $"wackydb_all_items -Save ALL Items to Bulk Folder\r\n"
                        + $"wackydb_all_recipes -Save all Recipes to Bulk Folder\r\n"
                        + $"wackydb_all_pieces [Hammer][Optional-Category]-Save all Pieces to Bulk Folder: default hammer, optionally by category\r\n"
                        + $"wackydb_all_creatures -Save All Creatures\r\n"
                        + $"wackydb_all_pickables -Save All Pickables and Tree Bases\r\n"
                        + $"wackydb_describe [ObjectName] Describe an Objects Visual Data\r\n"
                        + $"wackydb_clearcache Clears cache\r\n"
                        + $"wackydb_snapshot Reloads snapshot after wackydb_reload\r\n"

                        ;

                        args.Context?.AddString(output);
                    });

            Terminal.ConsoleCommand WackyShowcommandshelp =
                 new("wackydb_help", "Display Help ",
                     args =>
                     {
                         string output = $"wackydb_reload\r\n"
                        // + $"wackydb_reset \r\n"
                        + $"wackydb_save_recipe  [RecipeName](recipe output)\r\n"
                        + $"wackydb_save_piece [PieceName](piece output) \r\n"
                        + $"wackydb_save_item [ItemName](item Output)\r\n"
                        + $"wackydb_save_creature [CreatureName](Creature Output)\r\n"
                        + $"wackydb_save_pickable [PickableName](Pickable Output)\r\n"
                        + $"wackydb_save_material [MaterialName] Save Material Info - Usually has _mat\r\n"
                        + $"wackydb_se [SEEffectName] Save SEeffect\r\n"
                        + $"wackydb_help\r\n"
                        + $"wackydb_clone  [item/recipe/piece/creatures/material/se/pickables] [Prefab to clone] [Unique name for the clone] \r\n"
                        + $"4th paramater for recipes - you can already have item WackySword loaded in game, but now want a recipe. WackySword Uses SwordIron  - wackydb_clone recipe WackySword RWackySword SwordIron - otherwise manually edit\r\n"
                        + $"wackydb_clone_recipeitem  [Prefab to clone] [Unique name for the clone](Recipe name will be Rname) (clones item and recipe at same time)\r\n"
                        + $"wackydb_vfx (outputs vfx gameobjects available)\r\n"
                        + $"wackydb_fx (outputs fx gameobjects available)\r\n"
                        + $"wackydb_sfx (outputs Sfx gameobjects available)\r\n"
                        + $"wackydb_material (outputs Materials available)\r\n"
                        + $"wackydb_sendtheload -experimental\r\n"
                        + $"wackydb_get_piecehammers -Get All Hammer in Game\r\n"
                        + $"wackydb_all_se -Save All SE Effects to Bulk Folder\r\n"
                        + $"wackydb_all_items -Save ALL Items to Bulk Folder\r\n"
                        + $"wackydb_all_recipes -Save all Recipes to Bulk Folder\r\n"
                        + $"wackydb_all_pieces [Hammer][Optional-Category]-Save all Pieces to Bulk Folder: default hammer, optionally by category\r\n"
                        + $"wackydb_all_creatures -Save All Creatures\r\n"
                        + $"wackydb_all_pickables -Save All Pickables and Tree Bases\r\n"
                        + $"wackydb_describe [ObjectName] Describe an Objects Visual Data\r\n"
                        + $"wackydb_clearcache Clears cache\r\n"
                        + $"wackydb_snapshot Reloads snapshot after wackydb_reload\r\n"

                      ;

                         args.Context?.AddString(output);
                     });

            Terminal.ConsoleCommand WackyReload =
                     new("wackydb_reload", "reload the whole config files",
                         args =>
                         {
                             // GetRecipeDataFromFiles(); called in loadallrecipes
                             if (ObjectDB.instance && WMRecipeCust.issettoSinglePlayer)
                             {

                                 ReadFiles readnow = new ReadFiles();                               
                                 WMRecipeCust.context.StartCoroutine(readnow.GetDataFromFiles(true)); 
                                 WMRecipeCust.readFiles = readnow;

                                 SetData.Reload josh = new SetData.Reload();
                                 WMRecipeCust.CurrentReload = josh;

                                 WMRecipeCust.context.StartCoroutine(josh.LoadAllRecipeData(true, true));

                                 //josh.LoadAllRecipeData(true); // console singleplayer reload


                                 //args.Context?.AddString($"WackyDatabase reloaded recipes/items/pieces from files");
                                 //wackydatabase.WMRecipeCust.Dbgl("WackyDatabase reloaded recipes/items/pieces from files");
                             }
                             else if(WMRecipeCust.Admin)
                             {
                                 ZPackage pkg = new ZPackage();
                                 pkg.Write("true");
                                 ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "WackyDBAdminReload", new object[] { pkg });
                                 //ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "WackyDBAdminReload", true);

                                 wackydatabase.WMRecipeCust.Dbgl("Admin: Attempting to tell Server to reload");
                                 args.Context?.AddString($"Admin: Attempting to tell Server to reload");
                             }
                             else
                             {
                                 args.Context?.AddString($"WackyDatabase did NOT reload recipes/items/pieces from files"); // maybe?
                                 wackydatabase.WMRecipeCust.Dbgl("WackyDatabase did NOT reload recipes/items/pieces from files");
                             }

                         });

            Terminal.ConsoleCommand WackyReloadFast =
                new("wackydb_reload_fast", "reload the whole config files fast",
                 args =>
                 {
                     // GetRecipeDataFromFiles(); called in loadallrecipes
                     if (ObjectDB.instance && WMRecipeCust.issettoSinglePlayer)
                     {

                         ReadFiles readnow = new ReadFiles();
                         WMRecipeCust.context.StartCoroutine(readnow.GetDataFromFiles());
                         WMRecipeCust.readFiles = readnow;

                         SetData.Reload josh = new SetData.Reload();
                         WMRecipeCust.CurrentReload = josh;

                         WMRecipeCust.context.StartCoroutine(josh.LoadAllRecipeData(true));



                         args.Context?.AddString($"WackyDatabase reloaded recipes/items/pieces from files");
                     }

                 });

            Terminal.ConsoleCommand SendBigData =
                    new("wackydb_sendtheload", "Sends images/icons/obj to clients, highly experimental",
                        args =>
                        {
                            if (WMRecipeCust.Admin)
                            {

                                ZPackage pkg = new ZPackage();
                                pkg.Write("true");
                                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "WackyDBAdminBigData", new object[] { pkg });
                                //ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "WackyDBAdminReload", true);

                                wackydatabase.WMRecipeCust.Dbgl("Admin: Attempting to tell Server to Send the Motherload");
                                args.Context?.AddString($"Admin: Attempting to tell Server to Send the Motherload");
                            }


                        });


                            Terminal.ConsoleCommand WackyitemSave =
                new("wackydb_save_item", "Save an Item ",
                    args =>
                    {
                        string file = args[1];
                        GetDataYML ItemCheck = new GetDataYML();

                        WItemData recipData = ItemCheck.GetItemDataByName(file, ObjectDB.instance);
                        if (recipData == null)
                            return;
                        WMRecipeCust.CheckModFolder();
                        var serializer = new SerializerBuilder().WithNewLine("\n")
                            .Build();
                        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathItems, "Item_" + recipData.name + ".yml"), serializer.Serialize(recipData));
                        args.Context?.AddString($"saved item data to Item_{file}.yml");

                    }, isCheat: false, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, () => (!ZNetScene.instance) ? new List<string>() : ZNetScene.instance.GetPrefabNames());
            Terminal.ConsoleCommand WackyPieceSave =
                new("wackydb_save_piece", "Save a piece ",
                    args =>
                    {
                        string file = args[1];
                        GetDataYML PieceCheck = new GetDataYML();
                        PieceData recipData = PieceCheck.GetPieceRecipeByName(file, ObjectDB.instance);
                        if (recipData == null)
                            return;
                        WMRecipeCust.CheckModFolder();
                        var serializer = new SerializerBuilder().WithNewLine("\n")
                            .Build();
                        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathPieces, "Piece_" + recipData.name + ".yml"), serializer.Serialize(recipData));
                        args.Context?.AddString($"saved data to Piece_{file}.yml");

                    }, isCheat: false, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, () => (!ZNetScene.instance) ? new List<string>() : ZNetScene.instance.GetPrefabNames());
            Terminal.ConsoleCommand WackyRecipeSave =
                new("wackydb_save_recipe", "Save a recipe ",
                    args =>
                    {
                        string file = args[1];
                        GetDataYML RecipeCheck = new GetDataYML();
                        RecipeData recipData = RecipeCheck.GetRecipeDataByName(file, ObjectDB.instance);
                        if (recipData == null)
                            return;
                        WMRecipeCust.CheckModFolder();
                        var serializer = new SerializerBuilder().WithNewLine("\n")
                            .Build();
                        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathRecipes, "Recipe_" + recipData.name + ".yml"), serializer.Serialize(recipData));
                        args.Context?.AddString($"saved data to Recipe_{file}.yml");

                    }, isCheat: false, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, () => (!ZNetScene.instance) ? new List<string>() : ZNetScene.instance.GetPrefabNames());


            Terminal.ConsoleCommand WackyCreatureSave =
                new("wackydb_save_creature", "Save a Creature ",
                args =>
                {
                    string file = args[1];
                    GetDataYML CreatureCheck = new GetDataYML();
                    CreatureData creatureData = CreatureCheck.GetCreature(file);
                    if (creatureData == null)
                        return;
                    WMRecipeCust.CheckModFolder();
                    var serializer = new SerializerBuilder().WithNewLine("\n")
                        .Build();
                    File.WriteAllText(Path.Combine(WMRecipeCust.assetPathCreatures, "Creature_" + creatureData.name + ".yml"), serializer.Serialize(creatureData));
                    args.Context?.AddString($"saved data to Creature_{file}.yml");

                }, isCheat: false, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, () => (!ZNetScene.instance) ? new List<string>() : ZNetScene.instance.GetPrefabNames());
            
            Terminal.ConsoleCommand WackyPickableSave =
                new("wackydb_save_pickable", "Save a Pickable or Treebase ",
                args =>
                {
                    string file = args[1];
                    GetDataYML PickableCheck = new GetDataYML();
                    Pickable[] array = Resources.FindObjectsOfTypeAll<Pickable>();
                    PickableData pics = PickableCheck.GetPickable(file, array);
                    TreeBase[] array2 = Resources.FindObjectsOfTypeAll<TreeBase>();
                    TreeBaseData pics2 = PickableCheck.GetTreeBase(file, array2);
                    string tag = "Pickable";
                    if (pics == null)
                    {                   
                        tag = "Treebase_";
                        if (pics2 == null)
                            return;
                    }
                    WMRecipeCust.CheckModFolder();
                    var serializer = new SerializerBuilder().WithNewLine("\n")
                        .Build();
                    if (tag == "Pickable")
                    {
                        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathPickables, "Pickable_" + pics.name + ".yml"), serializer.Serialize(pics));
                    }
                    else
                    {
                        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathPickables, tag + pics2.name + ".yml"), serializer.Serialize(pics2));
                    }

                    args.Context?.AddString($"Saved data to Folder Pickables {tag}_{file}.yml");

                }, isCheat: false, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, () => (!ZNetScene.instance) ? new List<string>() : ZNetScene.instance.GetPrefabNames());

            Terminal.ConsoleCommand WackyRecipeItem =
            new("wackydb_save_recipeitem", "Save the recipe and item at the same time, this will create a recipe if not found",
                args =>
                {

                    WMRecipeCust.CheckModFolder();
                    if (args.Length - 1 < 1)
                    {
                        args.Context?.AddString("<color=red>Not enough arguments</color>");
                    }
                    else
                    {
                        var serializer = new SerializerBuilder().WithNewLine("\n")
                        .Build();
                        GetDataYML RecipeCheck = new GetDataYML();
                        string prefab = args[1];
                        //string newname = args[2];
                        // string file = args[2];
                        WItemData item = RecipeCheck.GetItemDataByName(prefab, ObjectDB.instance);
                        if (item == null)
                            return;

                        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathItems, "Item_" + item.name + ".yml"), serializer.Serialize(item));
                        string file = "";

                        RecipeData recipe = RecipeCheck.GetRecipeDataByName(prefab, ObjectDB.instance);//  prefab of cloned item
                        if (recipe != null)
                        { 
                          //  clone.name = "R" + newname;
                         //   clone.clonePrefabName = itemclone.name; // cloned item
                            File.WriteAllText(Path.Combine(WMRecipeCust.assetPathRecipes, "Recipe_" + recipe.name + ".yml"), serializer.Serialize(recipe));
                            file = "Item saved as Item_" + item.name + ".yml,  Recipe saved as Recipe_" + recipe.name + ".yml";
                        }else
                        {
                            recipe = RecipeCheck.GetRecipeDataByName("Club", ObjectDB.instance);// Club clone
                            if (recipe == null)
                                return;
                            recipe.name = "R" + prefab;
                            recipe.clonePrefabName = prefab; // cloned item
                            file = "Item saved as Item_" + item.name + ".yml,  Recipe saved as a clone from 'Club' Recipe_" + recipe.name + ".yml";
                            File.WriteAllText(Path.Combine(WMRecipeCust.assetPathRecipes, "Recipe_" + recipe.name + ".yml"), serializer.Serialize(recipe));
                        }


                        args.Context?.AddString($"{file}");
                    }

                }, isCheat: false, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, () => (!ZNetScene.instance) ? new List<string>() : ZNetScene.instance.GetPrefabNames());

            Terminal.ConsoleCommand WackyMaterials =
                new("wackydb_material", "Create txt file of materials",
                    args =>
                    {
                        string theString = Functions.GetAllMaterialsFile();
                        WMRecipeCust.CheckModFolder();
                        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathconfig, "Materials.txt"), theString);
                        args.Context?.AddString($"saved data to Materials.txt");
                        WMRecipeCust.WLog.LogInfo($"saved data to Materials.txt");

                    });

            Terminal.ConsoleCommand WackyPieceStations =
                new("wackydb_get_piecehammers", "Create txt file of PieceHammers/PieceStations",
                    args =>
                    {
                        string theString = "";
                        //WMRecipeCust.CheckModFolder();
                        foreach (var hammer in WMRecipeCust.MaybePieceStations)
                        {
                            theString = theString + hammer.ToString() + " \r\n";
                        }
                        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathconfig, "Hammers.txt"), theString);
                        args.Context?.AddString($"saved data to Hammers.txt");
                        WMRecipeCust.WLog.LogInfo($"saved data to Hammers.txt");

                    });

            Terminal.ConsoleCommand Wackyvfx =
                new("wackydb_vfx", "Create txt file of VFX",
                    args =>
                    {
                        string theString2 = Functions.GetAllVFXFile();
                        WMRecipeCust.CheckModFolder();
                        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathconfig, "vfx.txt"), theString2);
                        args.Context?.AddString($"saved data to VFX.txt");
                        WMRecipeCust.WLog.LogInfo($"saved data to VFX.txt");

                    });

             Terminal.ConsoleCommand Wackysfx =
                new("wackydb_sfx", "Create txt file of SFX",
                    args =>
                    {
                        string theString3 = Functions.GetAllSFXFile();
                        WMRecipeCust.CheckModFolder();
                        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathconfig, "sfx.txt"), theString3);
                        args.Context?.AddString($"saved data to sfx.txt");
                        WMRecipeCust.WLog.LogInfo($"saved data to sfx.txt");

                    });


            Terminal.ConsoleCommand Wackyfx =
               new("wackydb_fx", "Create txt file of FX - iffy",
                   args =>
                   {
                       string theString4 = Functions.GetAllFXFile();
                       WMRecipeCust.CheckModFolder();
                       File.WriteAllText(Path.Combine(WMRecipeCust.assetPathconfig, "FX.txt"), theString4);
                       args.Context?.AddString($"saved data to FX.txt");
                       WMRecipeCust.WLog.LogInfo($"saved data to FX.txt");

                   });

            Terminal.ConsoleCommand WackySE =
                new("wackydb_all_se", "Get all SE effects in game and create your own",
                    args =>
                    {
                        var tod = ObjectDB.instance;
                        var max = tod.m_StatusEffects.Count();
                        GetDataYML SEcheck = new GetDataYML();
                        int count = 0;

                        if (!Directory.Exists(WMRecipeCust.assetPathBulkYML))
                        {
                            WMRecipeCust.Dbgl("Creating wackyDatabase-BulkYML Folder in Config");
                            Directory.CreateDirectory(WMRecipeCust.assetPathBulkYML);
                        }
                        if (!Directory.Exists(WMRecipeCust.assetPathBulkYMLEffects))
                        {
                            Directory.CreateDirectory(WMRecipeCust.assetPathBulkYMLEffects);
                        }

                        var serializer = new SerializerBuilder().WithNewLine("\n")
                                        .Build();
                       // var deserialized = new DeserializerBuilder()
                          //      .Build();

                        while (count != max)
                        {
                           var temp = SEcheck.GetStatusEByNum(count, tod);
                            count++;
                            if (temp == null)
                                continue;
                            var part1 = serializer.Serialize(temp);
                            // deserialize yml into dictionary
                            /*
                            var deserialized = new DeserializerBuilder()
                            .Build()
                            .Deserialize<Dictionary<string, string>>(part1);
                            deserialized.Remove("m_icon");

                            var finalYml = new SerializerBuilder()
                            .WithEventEmitter(nextEmitter => new QuoteSurroundingEventEmitter(nextEmitter))
                            .Build()
                            .Serialize(deserialized);
                            */


                            File.WriteAllText(Path.Combine(WMRecipeCust.assetPathBulkYMLEffects, "SE_" +temp.Name+".yml"), part1);
                            
                        }
                        args.Context?.AddString($"saved all Status Effects to wackyDatabase-BulkYML Effects");
                        WMRecipeCust.WLog.LogInfo($"saved all Status Effects to wackyDatabase-BulkYML Effects");

                    });
                    Terminal.ConsoleCommand WackyAllItems =
                    new("wackydb_all_items", "Get all Items in game",
                        args =>
                        {
                            if (!Directory.Exists(WMRecipeCust.assetPathBulkYML))
                            {
                                WMRecipeCust.Dbgl("Creating wackyDatabase-BulkYML Folder in Config");
                                Directory.CreateDirectory(WMRecipeCust.assetPathBulkYML);
                            }
                            if (!Directory.Exists(WMRecipeCust.assetPathBulkYMLItems))
                            {
                                Directory.CreateDirectory(WMRecipeCust.assetPathBulkYMLItems);
                            }
                            var tod = ObjectDB.instance;
                            var max = tod.m_items.Count();
                            GetDataYML ItemCheck = new GetDataYML();
                            int count = 0;

                            var serializer = new SerializerBuilder().WithNewLine("\n")
                                            .Build();
                            // var deserialized = new DeserializerBuilder()
                            //      .Build();

                            while (count != max)
                            {
                                var temp = ItemCheck.GetItemDataByCount(count, tod);
                                count++;
                                if (temp == null)
                                    continue;
                                var part1 = serializer.Serialize(temp);


                                File.WriteAllText(Path.Combine(WMRecipeCust.assetPathBulkYMLItems, "Item_" + temp.name + ".yml"), part1);

                            }
                            args.Context?.AddString($"saved all Items in WackyBulk Items");
                            WMRecipeCust.WLog.LogInfo($"saved all Items in WackyBulk Items");

                        });
                    Terminal.ConsoleCommand WackyAllCreatures =
                    new("wackydb_all_creatures", "Get all Creatures in game",
                        args =>
                        {
                            if (!Directory.Exists(WMRecipeCust.assetPathBulkYML))
                            {
                                WMRecipeCust.Dbgl("Creating wackyDatabase-BulkYML Folder in Config");
                                Directory.CreateDirectory(WMRecipeCust.assetPathBulkYML);
                            }
                            if (!Directory.Exists(WMRecipeCust.assetPathBulkYMLCreatures))
                            {
                                Directory.CreateDirectory(WMRecipeCust.assetPathBulkYMLCreatures);
                            }
                            GetDataYML ItemCheck = new GetDataYML();
                            ItemCheck.GetAllCreature();


                            args.Context?.AddString($"saved all Creatures in WackyBulk ");
                            WMRecipeCust.WLog.LogInfo($"saved all Creatures in WackyBulk ");

                        });

                        Terminal.ConsoleCommand WackyAllRecipe =
                            new("wackydb_all_recipes", "Get all Recipes in game",
                            args =>
                            {
                            if (!Directory.Exists(WMRecipeCust.assetPathBulkYML))
                            {
                                WMRecipeCust.Dbgl("Creating wackyDatabase-BulkYML Folder in Config");
                                Directory.CreateDirectory(WMRecipeCust.assetPathBulkYML);
                            }
                            if (!Directory.Exists(WMRecipeCust.assetPathBulkYMLRecipes))
                            {
                                Directory.CreateDirectory(WMRecipeCust.assetPathBulkYMLRecipes);
                            }
                                var tod = ObjectDB.instance;
                            var max = tod.m_recipes.Count();
                           // WMRecipeCust.WLog.LogWarning($"max is {max} with {tod.m_recipes[0].name}being first");
                                GetDataYML RecipeCheck = new GetDataYML();
                            int count = 0;

                            var serializer = new SerializerBuilder().WithNewLine("\n")
                                            .Build();
                            // var deserialized = new DeserializerBuilder()
                            //      .Build();

                            while (count != max)
                            {
                                var temp = RecipeCheck.GetRecipeDataByNum(count, tod);
                                count++;
                                if (temp == null)
                                    continue;
                                var part1 = serializer.Serialize(temp);


                                File.WriteAllText(Path.Combine(WMRecipeCust.assetPathBulkYMLRecipes, "Recipe_" + temp.name + ".yml"), part1);

                            }

                            args.Context?.AddString($"saved all Recipes in WackyBulk Recipes Folder");
                            WMRecipeCust.WLog.LogInfo("saved all Recipes in WackyBulk Recipes Folder");

                        });
                        Terminal.ConsoleCommand WackyAllPickables =
                        new("wackydb_all_pickables", "Get all Pickables/TreeBase in game",
                        args =>
                        {
                            if (!Directory.Exists(WMRecipeCust.assetPathBulkYML))
                            {
                                WMRecipeCust.Dbgl("Creating wackyDatabase-BulkYML Folder in Config");
                                Directory.CreateDirectory(WMRecipeCust.assetPathBulkYML);
                            }
                            if (!Directory.Exists(WMRecipeCust.assetPathBulkYMLPickables))
                            {
                                Directory.CreateDirectory(WMRecipeCust.assetPathBulkYMLPickables);
                            }

                            GetDataYML PickCheck = new GetDataYML();
                            var temp = PickCheck.GetAllPickables();


                            args.Context?.AddString($"saved all Pickables/TreeBases in WackyBulk Pickables Folder");
                            WMRecipeCust.WLog.LogInfo("saved all Pickables/TreeBases in WackyBulk Pickables Folder");

                        });
    
                         Terminal.ConsoleCommand WackyAllPiece =
                            new("wackydb_all_pieces", "Get all Pieces in game by hammer and optionally by category",
                            args =>
                            {
                                if (args.Length-1 <1)
                                {
                                    args.Context?.AddString("<color=red>Enter a piece hammer</color> default hammer is Hammer");

                                }
                                else
                                {
                                    string cat = null;
                                    string hammer = args[1];
                                    if (args.Length-1 ==2)
                                         cat = args[2];

                                    if (!Directory.Exists(WMRecipeCust.assetPathBulkYML))
                                    {
                                        WMRecipeCust.Dbgl("Creating wackyDatabase-BulkYML Folder in Config");
                                        Directory.CreateDirectory(WMRecipeCust.assetPathBulkYML);
                                    }

                                    if (!Directory.Exists(WMRecipeCust.assetPathBulkYMLPieces))
                                    {
                                        Directory.CreateDirectory(WMRecipeCust.assetPathBulkYMLPieces);
                                    }
                                    var tod = ObjectDB.instance;
                                    ItemDrop HamerItemdrop = null;
                                    ItemDrop itemD = null;

                                    try
                                    {
                                        HamerItemdrop = tod.GetItemPrefab(hammer).GetComponent<ItemDrop>();
                                    }
                                    catch { WMRecipeCust.WLog.LogWarning($"{hammer} not found"); return; }


                                    int max = HamerItemdrop.m_itemData.m_shared.m_buildPieces.m_pieces.Count();
                                    WMRecipeCust.WLog.LogWarning($"Count is {max} for m__pieces");

                                    if (cat != null)
                                    {
                                        List<Piece> PieceList = null;
                                        try
                                        {
                                            Piece.PieceCategory james = (Piece.PieceCategory)Enum.Parse(typeof(Piece.PieceCategory), cat);
                                            WMRecipeCust.WLog.LogWarning($"Piece Hammer CAt");
                                            max = HamerItemdrop.m_itemData.m_shared.m_buildPieces.GetAvailablePiecesInCategory(james);
                                            HamerItemdrop.m_itemData.m_shared.m_buildPieces.m_selectedCategory = james;
                                            PieceList = HamerItemdrop.m_itemData.m_shared.m_buildPieces.GetPiecesInSelectedCategory();
                                        }
                                        catch { WMRecipeCust.WLog.LogWarning($"{cat} category was not parsed correclty"); cat = null; }

                                        if (PieceList != null)
                                        {
                                            GetDataYML PieceCheck = new GetDataYML();
                                            var serializer = new SerializerBuilder().WithNewLine("\n")
                                                                    .Build();
                                            foreach (var pie in PieceList)
                                            {

                                                var temp = PieceCheck.GetPiece(HamerItemdrop, hammer, pie.gameObject, tod);

                                                if (temp == null)
                                                    continue;

                                                var part1 = serializer.Serialize(temp);
                                                File.WriteAllText(Path.Combine(WMRecipeCust.assetPathBulkYMLPieces, "Piece_" + temp.name + ".yml"), part1);
                                            }


                                            args.Context?.AddString($"saved all Pieces from hammer {hammer} with category {cat} in WackyBulk Pieces");
                                            WMRecipeCust.WLog.LogInfo($"saved all Pieces from hammer {hammer} with category {cat} in WackyBulk Pieces");
                                        }
                                        else
                                        {
                                            args.Context?.AddString($"Failure for category saving of pieces {hammer} in WackyBulk");
                                            WMRecipeCust.WLog.LogWarning($"Failure for category saving of pieces {hammer} in WackyBulk");
                                        }
                                    }
                                    else
                                    {


                                        GetDataYML PieceCheck = new GetDataYML();
                                        int count = 0;

                                        var serializer = new SerializerBuilder().WithNewLine("\n")
                                                                    .Build();
                                        // var deserialized = new DeserializerBuilder()
                                        //      .Build();

                                        while (count != max)
                                        {
                                            var temp = PieceCheck.GetPieceRecipeByNum(count, hammer, HamerItemdrop, tod, null);
                                            count++;
                                            if (temp == null)
                                                continue;
                                            var part1 = serializer.Serialize(temp);


                                            File.WriteAllText(Path.Combine(WMRecipeCust.assetPathBulkYMLPieces, "Piece_" + temp.name + ".yml"), part1);

                                        }
                                        args.Context?.AddString($"saved all Pieces from hammer {hammer} in WackyBulk");
                                        WMRecipeCust.WLog.LogInfo($"saved all Pieces from hammer {hammer} in WackyBulk");
                                    }// end else
                                }

                            });


            Terminal.ConsoleCommand WackySEOne =
                new("wackydb_se", "Get one SE effect by name",
                    args =>
                    {

                        string name = args[1];
                        var tod = ObjectDB.instance;
                        GetDataYML SEcheck = new GetDataYML();

                        var serializer = new SerializerBuilder()
                                        .WithNewLine("\n")
                                        .Build();

                        var temp = SEcheck.GetStatusEByName(name, tod);
                          //args.Context?.AddString($"No SE effect by that name")
                     

                        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathEffects, "SE_" + temp.Name + ".yml"), serializer.Serialize(temp));
                                                    
                        args.Context?.AddString($"saved SE effect {name} to SE_{name}.yml in Effects folder");
                        WMRecipeCust.WLog.LogInfo($"saved SE effect {name} to SE_{name}.yml in Effects folder");


                    });

            Terminal.ConsoleCommand WackySEcreate =
                new("wackydb_se_create", "Create a clone se_effect to use",
                    args =>
                    {

                       // string name = args[1];
                        var tod = ObjectDB.instance;
                        GetDataYML SEcheck = new GetDataYML();

                        var serializer = new SerializerBuilder()
                                        .WithNewLine("\n")
                                        .Build();

                        
                        var temp = SEcheck.GetStatusEByName("SetEffect_TrollArmor", tod);
                        //StatusData temp = new StatusData();
                        var rand = new System.Random();
                        int num = rand.Next(1, 1000);
                        temp.Name = "SetEffect_New"+num;



                        File.WriteAllText(Path.Combine(WMRecipeCust.assetPathEffects, "SE_" + temp.Name + ".yml"), serializer.Serialize(temp));

                        args.Context?.AddString($"Created a clone from SetEffect_TrollArmor, saved as a file SE_{temp.Name}.yml in Effects folder, PLEASE ReNAME");
                        WMRecipeCust.WLog.LogInfo($"Created a clone from SetEffect_TrollArmor, saved as a file SE_{temp.Name}.yml in Effects folder, PLEASE ReNAME");


                    });

            Terminal.ConsoleCommand WackyClone =
                new("wackydb_clone", "Clone an Item/Piece/Recipe/Material/creature/SE with different stats, names, effects ect... ",
                    args =>
                    {
                        if (args.Length - 1 < 3)
                        {
                            args.Context?.AddString("<color=red>Not enough arguments</color>");

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
                            var serializer = new SerializerBuilder()
                            .WithNewLine("\n")
                            .Build();                         
                            GetDataYML RecipeCheck = new GetDataYML();

                            if (commandtype == "recipe" || commandtype == "Recipe")
                            {
                                WMRecipeCust.CheckModFolder();
                                if (args.Length - 1 < 4)
                                {
                                    RecipeData clone = RecipeCheck.GetRecipeDataByName(prefab, ObjectDB.instance);// actually it could be a different prefab if cloned item
                                    if (clone == null)
                                        return;
                                    clone.name = newname;
                                    clone.clonePrefabName = prefab;
                                    File.WriteAllText(Path.Combine(WMRecipeCust.assetPathRecipes, "Recipe_" + clone.name + ".yml"), serializer.Serialize(clone));
                                    file = "Recipe" + clone.name;
                                }
                                else
                                {
                                    string prefabitem = args[4];
                                    RecipeData clone = RecipeCheck.GetRecipeDataByName(prefabitem, ObjectDB.instance);//  prefab of cloned item
                                    if (clone == null)
                                        return;
                                    clone.name = newname;
                                    clone.clonePrefabName = prefab; // cloned item
                                    File.WriteAllText(Path.Combine(WMRecipeCust.assetPathRecipes, "Recipe_" + clone.name + ".yml"), serializer.Serialize(clone));
                                    file = "Cloned Item " + clone.name + " Clone Recipe from " + prefabitem;

                                } // added optional arugment for cloned items


                            }
                            if (commandtype == "item" || commandtype == "Item")
                            {
                                WItemData clone = RecipeCheck.GetItemDataByName(prefab, ObjectDB.instance);
                                if (clone == null)
                                    return;
                                clone.name = newname;
                                clone.clonePrefabName = prefab;
                                clone.m_name = newname;

                                WMRecipeCust.CheckModFolder();
                                File.WriteAllText(Path.Combine(WMRecipeCust.assetPathItems, "Item_" + clone.name + ".yml"), serializer.Serialize(clone));
                                file = "Item_" + clone.name;



                            }
                            if (commandtype == "piece" || commandtype == "Piece")
                            {
                                PieceData clone = RecipeCheck.GetPieceRecipeByName(prefab, ObjectDB.instance);
                                if (clone == null)
                                    return;
                                clone.name = newname;
                                clone.clonePrefabName = prefab;

                                WMRecipeCust.CheckModFolder();
                                File.WriteAllText(Path.Combine(WMRecipeCust.assetPathPieces, "Piece_" + clone.name + ".yml"), serializer.Serialize(clone));
                                file = "Piece_" + clone.name;

                            }
                            if (commandtype == "pickable" || commandtype == "Pickable" || commandtype == "Pickables" || commandtype == "pickables")
                            {
                                Pickable[] pickAbles = Resources.FindObjectsOfTypeAll<Pickable>();
                                PickableData clone = RecipeCheck.GetPickable(prefab, pickAbles);
                                if (clone == null)
                                    return;
                                clone.name = newname;
                                clone.cloneOfWhatPickable = prefab;

                                WMRecipeCust.CheckModFolder();
                                File.WriteAllText(Path.Combine(WMRecipeCust.assetPathPickables, "Pickable_" + clone.name + ".yml"), serializer.Serialize(clone));
                                file = "Pickable_" + clone.name;
                            }
                            if (commandtype == "treebase" || commandtype == "Treebase")
                            {
                                TreeBase[] treeBases = Resources.FindObjectsOfTypeAll<TreeBase>();
                                TreeBaseData clone = RecipeCheck.GetTreeBase(prefab, treeBases);
                                if (clone == null)
                                    return;
                                clone.name = newname;
                                clone.cloneOfWhatTree = prefab;

                                WMRecipeCust.CheckModFolder();
                                File.WriteAllText(Path.Combine(WMRecipeCust.assetPathPieces, "Treebase_" + clone.name + ".yml"), serializer.Serialize(clone));
                                file = "Treebase_" + clone.name;
                            }

                            if (commandtype == "creature" || commandtype == "Creature" || commandtype == "mob")
                            {
                                CreatureData clone = RecipeCheck.GetCreature(prefab);
                                if (clone == null)
                                    return;
                                clone.name = newname;
                                clone.clone_creature = prefab;

                                WMRecipeCust.CheckModFolder();
                                File.WriteAllText(Path.Combine(WMRecipeCust.assetPathCreatures, "Creature_" + clone.name + ".yml"), serializer.Serialize(clone));
                                file = "Creature_" + clone.name;

                            }
                            if (commandtype == "se" || commandtype == "SE" || commandtype == "Se")
                            {
                                try
                                {
                                    var tod = ObjectDB.instance;
                                    GetDataYML SEcheck = new GetDataYML();

                                    var temp = SEcheck.GetStatusEByName(prefab, tod);
                                    
                                    if (temp == null)
                                    {
                                       // args.Context?.AddString(temp.Name + " Was not found");
                                        WMRecipeCust.WLog.LogInfo(temp.Name + " Was not found");

                                    } 

                                    temp.Name = newname;
                                    temp.ClonedSE = prefab;

                                    File.WriteAllText(Path.Combine(WMRecipeCust.assetPathEffects, "SE_" + temp.Name + ".yml"), serializer.Serialize(temp));

                                    args.Context?.AddString($"Created a clone from " + prefab + " , saved as a file SE_" + temp.Name + ".yml  in Effects folder");
                                    WMRecipeCust.WLog.LogInfo($"Created a clone from " + prefab + " , saved as a file SE_" + temp.Name + ".yml in Effects folder");
                                } catch { WMRecipeCust.WLog.LogWarning("wackydb_clone SE is unhappy, feed it better cookies"); file = "not saved"; }
                                

                            }

                            if (commandtype == "material" || commandtype == "Material" || commandtype == "mat")
                            {
                                string name = prefab;

                                if (WMRecipeCust.originalMaterials.ContainsKey(name))
                                {
                                    var material = WMRecipeCust.originalMaterials[name];
                                    var loader = new YamlLoader();

                                    MaterialInstance mi = new MaterialInstance()
                                    {
                                        original = name,
                                        name = newname,
                                        overwrite = false
                                    };

                                    int propertyCount = material.shader.GetPropertyCount();

                                    for (int k = 0; k < propertyCount; k++)
                                    {
                                        ShaderPropertyType type = material.shader.GetPropertyType(k);
                                        string propertyName = material.shader.GetPropertyName(k);

                                        switch (type)
                                        {
                                            case ShaderPropertyType.Color:
                                                mi.changes.colors.Add(propertyName, material.GetColor(propertyName));
                                                break;
                                            case ShaderPropertyType.Float:
                                            case ShaderPropertyType.Range:
                                            case ShaderPropertyType.Vector:
                                                mi.changes.floats.Add(propertyName, material.GetFloat(propertyName));
                                                break;
                                            case ShaderPropertyType.Texture:
                                                Texture t = material.GetTexture(propertyName);

                                                try
                                                {
                                                    if (t != null)
                                                    {
                                                        mi.changes.textures.Add(propertyName, (Texture2D) t);
                                                        TextureDataManager.SaveTexture(t.name, t);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Debug.LogError($"[{WMRecipeCust.ModName}]: Unable to write texture for property: {propertyName} - {ex.Message}");
                                                }

                                                break;
                                        }
                                    }

                                    loader.Write(Path.Combine(WMRecipeCust.assetPathMaterials, newname + ".yml"), mi);
                                    args.Context?.AddString($"Saved in the Material folder, as {newname}.yml");

                                }
                            }
                        args.Context?.AddString($"saved cloned data to {file}.yml");
                        }
                    }, isCheat: false, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, () => (!ZNetScene.instance) ? new List<string>() : ZNetScene.instance.GetPrefabNames());
            Terminal.ConsoleCommand WackyCloneRecipe =
                new("wackydb_clone_recipeitem", "Clone recipe and item with the orginal prefab ",
                    args =>
                    {

                        WMRecipeCust.CheckModFolder();
                        if (args.Length - 1 < 2)
                        {
                            args.Context?.AddString("<color=red>Not enough arguments</color>");
                        }
                        else
                        {
                            var serializer = new SerializerBuilder()
                            .WithNewLine("\n")
                            .Build();
                            GetDataYML RecipeCheck = new GetDataYML();
                            string prefab = args[1];
                            string newname = args[2];
                            string file = args[2];
                            WItemData itemclone = RecipeCheck.GetItemDataByName(prefab, ObjectDB.instance);
                            if (itemclone == null)
                                return;
                            itemclone.name = newname;
                            itemclone.clonePrefabName = prefab;
                            itemclone.m_name = newname;
                            File.WriteAllText(Path.Combine(WMRecipeCust.assetPathItems, "Item_" + itemclone.name + ".yml"), serializer.Serialize(itemclone));

                            RecipeData clone = RecipeCheck.GetRecipeDataByName(prefab, ObjectDB.instance);//  prefab of cloned item
                            if (clone == null)
                                return;
                            clone.name = "R" + newname;
                            clone.clonePrefabName = itemclone.name; // cloned item
                            File.WriteAllText(Path.Combine(WMRecipeCust.assetPathRecipes, "Recipe_" + clone.name + ".yml"), serializer.Serialize(clone));

                            file = "Cloned Item saved as Item_" + itemclone.name + ".yml, cloned Recipe saved as Recipe_" + clone.name + ".yml which is from the Orginal Recipe " + prefab;
                            args.Context?.AddString($"{file}");
                        }

                    }, isCheat: false, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, () => (!ZNetScene.instance) ? new List<string>() : ZNetScene.instance.GetPrefabNames());


            Terminal.ConsoleCommand Wackyadmin =    //dont look :)
                 new("wackydb_backdoor", "Gives you reload powers if you can guess the password",  // just for fun. Doesn't really give you admin powers
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
                    
               }, isCheat: false, isNetwork: false, onlyServer: false, isSecret: true, allowInDevBuild: false );


            Terminal.ConsoleCommand WackyDescribe = new("wackydb_describe", "Export visual description information for an item", args =>
            {
                if (args.Length - 1 < 1)
                {
                    args.Context?.AddString("<color=red>Not enough arguments </color>");

                }
                else
                {
                    string name = args[1];
                    WMRecipeCust.WLog.LogInfo(name);
                    try
                    {
                        VisualController.Export(PrefabAssistant.Describe(name));
                    } catch { WMRecipeCust.WLog.LogWarning("Error, Didnt save");
                        args.Context?.AddString($"Error, Didnt save");
                    }
                    args.Context?.AddString($"Saved in config folder Describe_{name}.yml");
                }

            }, isCheat: false, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, () => (!ZNetScene.instance) ? new List<string>() : ZNetScene.instance.GetPrefabNames());


            Terminal.ConsoleCommand WackySaveMaterial = new("wackydb_save_material", "Export default material settings for a material", args =>
            {
                if (args.Length - 1 < 1)
                {
                    args.Context?.AddString("<color=red>Not enough arguments</color>");
                    return;
                }
                else
                {
                    string name = args[1];
                    string clonedName = PrefabAssistant.SaveMaterial(name);

                    if (clonedName != null)
                    {
                        args.Context?.AddString($"Saved in the Material folder, as {clonedName}.yml");
                    }
                    else
                    {
                        args.Context?.AddString($"Material '{name}' not found.");
                    }
                }
            }, isCheat: false, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false);
        }
    }
}
