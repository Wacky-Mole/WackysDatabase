using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using wackydatabase.Datas;
using static CharacterAnimEvent;
using static wackydatabase.Armor.ArmorHelpers;

namespace wackydatabase.SetData.SetOldData
{
    internal class OldReloadSet
    {
        public void OldReload()
        {

            ObjectDB Instant = ObjectDB.instance;
            // CLONE PASS FIRST - only for craftingStation
            foreach (var data3 in WMRecipeCust.PieceDatas)
            {
                if (data3 != null && data3.clone)
                {
                    try
                    {
                        CraftingStation checkifStation = null;
                        GameObject go = DataHelpers.FindPieceObjectName(data3.clonePrefabName);
                        string tempnam = null;
                        tempnam = go.GetComponent<CraftingStation>()?.m_name;
                        if (tempnam != null)
                        {
                            checkifStation = DataHelpers.GetCraftingStation(tempnam); // for forge and other items that change names between item and CraftingStation
                            if (checkifStation != null) // means the prefab being cloned is a craftingStation and needs to proceed
                            {
                                SetPieceRecipeData(data3, Instant);
                            }
                        }
                    }
                    catch { } // spams just catch any empty
                }
            }
            // END CLONE PASS
            // Real PASS NOW
            foreach (var data in WMRecipeCust.ItemDatas) // call items first
            {
                try
                {
                    SetItemData(data, Instant);
                }
                catch { WMRecipeCust.WLog.LogWarning($"SetItem Data for {data.name} failed"); }

            }
            Instant.UpdateItemHashes();
            foreach (var data in WMRecipeCust.PieceDatas)
            {
                try
                {
                    SetPieceRecipeData(data, Instant);
                }
                catch { WMRecipeCust.WLog.LogWarning($"SetPiece Data for {data.name} failed"); }
            }
            foreach (var data in WMRecipeCust.recipeDatas) // recipes last
            {
                try
                {
                    SetRecipeData(data, Instant);
                }
                catch { WMRecipeCust.WLog.LogWarning($"SetRecipe Data for {data.name} failed"); }
            }
            WMRecipeCust.Dbgl($"Item Hashes Ready to Update");
            try
            {
                ObjectDB.instance.UpdateItemHashes();
            }
            catch
            {
                WMRecipeCust.Dbgl($"failed to update Hashes- probably due to too many calls");
            }

            WMRecipeCust.Dbgl($" You did reload LOCAL Files");
        }


        private static void SetRecipeData(RecipeData_json data, ObjectDB Instant)
        {
            bool skip = false;
            foreach (string citem in WMRecipeCust.ClonedR)
            {
                //Dbgl($"Recipe clone check {citem} against {data.name}");
                if (citem == data.name)
                    skip = true;
            }
            string tempname = data.name;
            if (data.clone) // both skip and
            {
                data.name = data.clonePrefabName;
            }
            GameObject go = DataHelpers.CheckforSpecialObjects(data.name);// check for special cases
            if (go == null)
                go = Instant.GetItemPrefab(data.name);

            if (go == null)
            {
                foreach (Recipe recipes in Instant.m_recipes)
                {
                    if (!(recipes.m_item == null) && recipes.name == data.name)
                    {
                        WMRecipeCust.Dbgl($"An actual Recipe_ {data.name} has been found!-- Only modification allowed");
                        if (data.disabled)
                        {
                            WMRecipeCust.Dbgl($"Removing recipe for {data.name} from the game");
                            Instant.m_recipes.Remove(recipes);
                            return;
                        }
                        recipes.m_amount = data.amount;
                        recipes.m_minStationLevel = data.minStationLevel;
                        recipes.m_craftingStation = DataHelpers.GetCraftingStation(data.craftingStation);
                        List<Piece.Requirement> reqs = new List<Piece.Requirement>();
                        // Dbgl("Made it to RecipeData!");
                        foreach (string req in data.reqs)
                        {
                            string[] parts = req.Split(':');
                            reqs.Add(new Piece.Requirement() { m_resItem = Instant.GetItemPrefab(parts[0]).GetComponent<ItemDrop>(), m_amount = int.Parse(parts[1]), m_amountPerLevel = int.Parse(parts[2]), m_recover = parts[3].ToLower() == "true" });
                        }
                        //Dbgl("Amost done with RecipeData!");
                        recipes.m_resources = reqs.ToArray();
                        return;

                    }
                }
            }
            if (go == null)
            {
                WMRecipeCust.WLog.LogWarning("maybe null " + data.name);
                return;
            }

            if (go.GetComponent<ItemDrop>() == null)
            {
                WMRecipeCust.WLog.LogWarning($"Item recipe data for {data.name} not found!");
                return;
            } // it is a prefab and it is an item.
            if (data.clone && !skip)
            {
                if (!data.disabled)
                {
                    WMRecipeCust.Dbgl("Setting Cloned Recipe for " + tempname);
                    Recipe clonerecipe = ScriptableObject.CreateInstance<Recipe>();
                    WMRecipeCust.ClonedR.Add(tempname);

                    clonerecipe.m_item = go.GetComponent<ItemDrop>();
                    clonerecipe.m_craftingStation = DataHelpers.GetCraftingStation(data.craftingStation);
                    clonerecipe.m_repairStation = DataHelpers.GetCraftingStation(data.craftingStation);
                    clonerecipe.m_minStationLevel = data.minStationLevel;
                    clonerecipe.m_amount = data.amount;
                    clonerecipe.name = tempname; //maybe
                                                 // clonerecipe.name = $"<color =#4f34eb>{tempname}</color>";
                    List<Piece.Requirement> reqs = new List<Piece.Requirement>();

                    // Dbgl("Made it to RecipeData!");
                    foreach (string req in data.reqs)
                    {
                        if (!string.IsNullOrEmpty(req))
                        {
                            string[] array = req.Split(':'); // safer vewrsion
                            string itemname = array[0];
                            if (Instant.GetItemPrefab(itemname))
                            {
                                int amount = ((array.Length < 2) ? 1 : int.Parse(array[1]));
                                int amountPerLevel = ((array.Length < 3) ? 1 : int.Parse(array[2]));
                                bool recover = array.Length != 4 || bool.Parse(array[3]);
                                Piece.Requirement item = new Piece.Requirement
                                {
                                    m_amount = amount,
                                    m_recover = recover,
                                    m_resItem = ObjectDB.instance.GetItemPrefab(itemname).GetComponent<ItemDrop>(),
                                    m_amountPerLevel = amountPerLevel
                                };
                                reqs.Add(item);
                            }
                        }
                    }// foreach
                    int index = 0;
                    clonerecipe.m_resources = reqs.ToArray();
                    for (int i = Instant.m_recipes.Count - 1; i >= 0; i--)
                    {
                        if (Instant.m_recipes[i].m_item?.m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)
                        {
                            index = i++; // some extra resourses, but I think it's worth it
                            break;
                        }
                    }
                    if (Instant.m_recipes.Count == 0)
                    {
                        index = 0;
                    }
                    Instant.m_recipes.Insert(index, clonerecipe);
                    //Dbgl($"Recipe clone check {citem} against {data.name}");

                    return;
                }
                else
                {
                    WMRecipeCust.Dbgl("Cloned Recipe is disabled for " + data.clonePrefabName + " Will not unload if already loaded");
                    return;
                }

            }
            else if (skip) // if a previous clone
            {
                for (int i = Instant.m_recipes.Count - 1; i >= 0; i--)
                {
                    if (Instant.m_recipes[i].name == tempname)
                    {

                        WMRecipeCust.Dbgl("ReSetting Cloned Recipe for " + tempname);
                        Recipe clonerecipe = ObjectDB.instance.m_recipes[i];
                        clonerecipe.m_item = go.GetComponent<ItemDrop>();
                        clonerecipe.m_craftingStation = DataHelpers.GetCraftingStation(data.craftingStation);
                        clonerecipe.m_repairStation = DataHelpers.GetCraftingStation(data.craftingStation);
                        if (clonerecipe.m_craftingStation == null)
                            WMRecipeCust.Dbgl("clone craftingStation set to null");
                        clonerecipe.m_minStationLevel = data.minStationLevel;
                        clonerecipe.m_amount = data.amount;
                        clonerecipe.name = tempname; //maybe
                                                     //clonerecipe.m_enabled = true;

                        List<Piece.Requirement> reqs = new List<Piece.Requirement>();

                        // Dbgl("Made it to RecipeData!");  
                        foreach (string req in data.reqs)
                        {
                            if (!string.IsNullOrEmpty(req))
                            {
                                string[] array = req.Split(':'); // safer vewrsion
                                string itemname = array[0];
                                if (Instant.GetItemPrefab(itemname))
                                {
                                    int amount = ((array.Length < 2) ? 1 : int.Parse(array[1]));
                                    int amountPerLevel = ((array.Length < 3) ? 1 : int.Parse(array[2]));
                                    bool recover = array.Length != 4 || bool.Parse(array[3]);
                                    Piece.Requirement item = new Piece.Requirement
                                    {
                                        m_amount = amount,
                                        m_recover = recover,
                                        m_resItem = ObjectDB.instance.GetItemPrefab(itemname).GetComponent<ItemDrop>(),
                                        m_amountPerLevel = amountPerLevel
                                    };
                                    reqs.Add(item);
                                }
                            }
                        }// foreach
                        clonerecipe.m_resources = reqs.ToArray();
                        return;
                    }
                }

            }
            else // ingame item
            {
                for (int i = Instant.m_recipes.Count - 1; i >= 0; i--)
                {
                    if (Instant.m_recipes[i].m_item?.m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)
                    {
                        // if not clone normal edit
                        WMRecipeCust.Dbgl("Setting Recipe for " + data.name);
                        if (data.disabled)
                        {
                            WMRecipeCust.Dbgl($"Removing recipe for {data.name} from the game");
                            Instant.m_recipes.RemoveAt(i);
                            return;
                        }
                        Instant.m_recipes[i].m_amount = data.amount;
                        Instant.m_recipes[i].m_minStationLevel = data.minStationLevel;
                        Instant.m_recipes[i].m_craftingStation = DataHelpers.GetCraftingStation(data.craftingStation);
                        //ObjectDB.instance.m_recipes[i].m_repairStation = GetCraftingStation(data.craftingStation); dont mess with maybe? if null repairable by all?
                        List<Piece.Requirement> reqs = new List<Piece.Requirement>();
                        // Dbgl("Made it to RecipeData!");
                        foreach (string req in data.reqs)
                        {
                            string[] parts = req.Split(':');
                            reqs.Add(new Piece.Requirement() { m_resItem = Instant.GetItemPrefab(parts[0]).GetComponent<ItemDrop>(), m_amount = int.Parse(parts[1]), m_amountPerLevel = int.Parse(parts[2]), m_recover = parts[3].ToLower() == "true" });
                        }
                        //Dbgl("Amost done with RecipeData!");
                        Instant.m_recipes[i].m_resources = reqs.ToArray();
                        return;
                    } // end normal
                } // checking recipes
            }
        }

        private static void SetPieceRecipeData(PieceData_json data, ObjectDB Instant)
        {
            bool skip = false;
            foreach (var citem in WMRecipeCust.ClonedP)
            {
                if (citem == data.name)
                    skip = true;
            }
            string tempname = data.name;
            if (data.clone && !skip)
            {
                data.name = data.clonePrefabName;
            }
            Piece piece = null;
            GameObject go = DataHelpers.CheckforSpecialObjects(data.name);// check for special cases
            if (go == null)
            {
                go = DataHelpers.GetPieces(Instant).Find(g => Utils.GetPrefabName(g) == data.name); // vanilla search  replace with FindPieceObjectName(data.name) in the future
                if (go == null)
                {
                    go = DataHelpers.GetModdedPieces(data.name); // known modded Hammer search
                    if (go == null)
                    {
                        WMRecipeCust.Dbgl($"Piece {data.name} not found! 3 layer search");
                        return;
                    }
                    else // 2nd layer
                        WMRecipeCust.Dbgl($"Piece {data.name} from known hammer {WMRecipeCust.selectedPiecehammer}");
                }
            }
            piece = go.GetComponent<Piece>();
            if (piece == null) // final check
            {
                WMRecipeCust.Dbgl("Piece data not found!");
                return;
            }
            if (data.clone && !skip) // object is a clone do clonethings
            {
                WMRecipeCust.Dbgl($"Item CLONE DATA in SetPiece for {tempname} ");
                Transform RootT = WMRecipeCust.Root.transform; // Root set to inactive to perserve components. 
                GameObject newItem = WMRecipeCust.Instantiate(go, RootT, false);
                Piece NewItemComp = newItem.GetComponent<Piece>();

                WMRecipeCust.ClonedP.Add(tempname); // check against
                newItem.name = tempname; // resets the orginal name- needs to be unquie
                NewItemComp.name = tempname; // ingame name

                var hash = newItem.name.GetStableHashCode();
                ZNetScene znet = ZNetScene.instance;
                if (znet)
                {
                    string name = newItem.name;
                    if (znet.m_namedPrefabs.ContainsKey(hash))
                    {
                        WMRecipeCust.Dbgl($"Prefab {name} already in ZNetScene");
                    }
                    else
                    {
                        if (newItem.GetComponent<ZNetView>() != null)
                        {
                            znet.m_prefabs.Add(newItem);
                        }
                        else
                        {
                            znet.m_nonNetViewPrefabs.Add(newItem);
                        }
                        znet.m_namedPrefabs.Add(hash, newItem);
                        WMRecipeCust.Dbgl($"Added prefab {name}");
                    }
                }
                CraftingStation craft2 = DataHelpers.GetCraftingStation(data.craftingStation);
                newItem.GetComponent<Piece>().m_craftingStation = craft2; // sets crafing item place


                GameObject piecehammer = Instant.GetItemPrefab(data.piecehammer);
                skip = true;
                /*
                if (Chainloader.PluginInfos.ContainsKey("com.maxsch.valheim.HammerTime"))
                {
                    piecehammer = Instant.GetItemPrefab("Hammer");
                    piecehammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(newItem);
                    try
                            { NewItemComp.m_category = (Piece.PieceCategory)Enum.Parse(typeof(Piece.PieceCategory), data.piecehammerCategory); }
                            catch { Dbgl($"piecehammerCategory named {data.piecehammerCategory} did not set correctly "); }
                }
                */
                if (piecehammer == null)
                {
                    if (WMRecipeCust.selectedPiecehammer == null)
                    {
                        WMRecipeCust.Dbgl($"piecehammer named {data.piecehammer} will not be used because the Item prefab was not found and it is not a PieceTable, so setting the piece to Hammer in Misc");
                        piecehammer = Instant.GetItemPrefab("Hammer");

                        NewItemComp.m_category = Piece.PieceCategory.Misc; // set the category
                        piecehammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(newItem);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(data.piecehammerCategory))
                        {
                            try
                            { NewItemComp.m_category = (Piece.PieceCategory)Enum.Parse(typeof(Piece.PieceCategory), data.piecehammerCategory); }
                            catch { WMRecipeCust.Dbgl($"piecehammerCategory named {data.piecehammerCategory} did not set correctly "); }
                        }
                        WMRecipeCust.selectedPiecehammer.m_pieces.Add(newItem); // adding item to PiceTable
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(data.piecehammerCategory))
                    {
                        try
                        { NewItemComp.m_category = (Piece.PieceCategory)Enum.Parse(typeof(Piece.PieceCategory), data.piecehammerCategory); }
                        catch { WMRecipeCust.Dbgl($"piecehammerCategory named {data.piecehammerCategory} did not set correctly "); }
                    }
                    piecehammer?.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(newItem); // if piecehammer is the actual item and not the PieceTable
                }
                data.name = tempname; // putting back name
                go = DataHelpers.FindPieceObjectName(data.name); // this needs to call to newItem for modifcation otherwise it modifies orginial.
                if (go == null)// just verifying
                {
                    WMRecipeCust.Dbgl($"Item {data.name} not found in SetPiece! after clone");
                    return;
                }
                if (go.GetComponent<Piece>() == null)
                {
                    WMRecipeCust.Dbgl($"Item data for {data.name} not found! after clone");
                    return;
                }
                go.GetComponent<Piece>().m_name = tempname; // set pieces name
            } // end clone 1st pass


            CraftingStation craft = DataHelpers.GetCraftingStation(data.craftingStation); // people might use this for more than just clones?
            go.GetComponent<Piece>().m_craftingStation = craft;

            if (!skip)
            { // Cats // if just added cloned doesn't need to be category changed.
                Piece ItemComp = go.GetComponent<Piece>();

                GameObject piecehammer = Instant.GetItemPrefab(data.piecehammer); // need to check to make sure hammer didn't change, if it did then needs to disable piece in certain cat before moving to next
                // Can't check the hammer easily, so checking the PieceCategory, hopefully someone doesn't make two Misc
                if (data.piecehammerCategory != null && data.piecehammer != null) // check that category and hammer is actually set
                {
                    if (ItemComp.m_category != (Piece.PieceCategory)Enum.Parse(typeof(Piece.PieceCategory), data.piecehammerCategory))
                    { // now disable old 
                        // change to only allow default categories.
                        if ((int)Enum.Parse(typeof(Piece.PieceCategory), data.piecehammerCategory) > 4 && Chainloader.PluginInfos.ContainsKey("com.maxsch.valheim.HammerTime"))
                        {
                            WMRecipeCust.Dbgl($"Category is above 4 and HammerTime is install, so no change will happen");
                        }
                        else
                        {

                            WMRecipeCust.Dbgl($"Category change has been detected for {data.name}, disabling old piece and setting new piece location");

                            if (piecehammer == null)
                            {
                                if (WMRecipeCust.selectedPiecehammer == null)
                                {
                                    piecehammer = ObjectDB.instance.GetItemPrefab("Hammer"); // default add // default delete
                                    piecehammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Remove(go);
                                }
                                else WMRecipeCust.selectedPiecehammer.m_pieces.Remove(go); // found in modded hammers
                            }
                            else piecehammer?.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Remove(go); // if piecehammer is the actual item and not the PieceTable

                            /*
                            if (Chainloader.PluginInfos.ContainsKey("com.jotunn.jotunn"))
                            {
                                object PieceManagerjvl = Chainloader.PluginInfos["com.jotunn.jotunn"].Instance.GetType().Assembly.GetType("Jotunn.Managers.PieceManager").GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                                object cr = AccessTools.Method(PieceManagerjvl.GetType(), "AddPieceCategory").Invoke(PieceManagerjvl, new[] { data.piecehammerCategory });
                                if (cr != null)
                                {
                                    var piecejvl = (Piece)AccessTools.Property(cr.GetType(), "Piece").GetValue(cr);
                                }
                            }
                            */
                            // Now add to new Cat and hammer
                            if (piecehammer == null)
                            {
                                if (WMRecipeCust.selectedPiecehammer == null)
                                {
                                    WMRecipeCust.Dbgl($"piecehammer named {data.piecehammer} will not be used because the Item prefab was not found and it is not a PieceTable, so setting the piece to Hammer in Misc");
                                    piecehammer = Instant.GetItemPrefab("Hammer");

                                    ItemComp.m_category = Piece.PieceCategory.Misc; // set the category
                                    piecehammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(go);
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(data.piecehammerCategory))
                                    {
                                        try
                                        { ItemComp.m_category = (Piece.PieceCategory)Enum.Parse(typeof(Piece.PieceCategory), data.piecehammerCategory); }
                                        catch { WMRecipeCust.Dbgl($"piecehammerCategory named {data.piecehammerCategory} did not set correctly "); }
                                    }
                                    WMRecipeCust.selectedPiecehammer.m_pieces.Add(go); // adding item to PiceTable
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(data.piecehammerCategory))
                                {
                                    try
                                    { ItemComp.m_category = (Piece.PieceCategory)Enum.Parse(typeof(Piece.PieceCategory), data.piecehammerCategory); }
                                    catch { WMRecipeCust.Dbgl($"piecehammerCategory named {data.piecehammerCategory} did not set correctly "); }
                                }
                                piecehammer?.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(go); // if piecehammer is the actual item and not the PieceTable
                            }
                        } // HammerTime
                    }
                }
            } //end Cat
            if (data.adminonly)
            {
                if (WMRecipeCust.Admin)
                {

                    WMRecipeCust.Dbgl($"{data.name} is set for Adminonly, and you are admin, enjoy this exclusive Piece");
                }
                else
                {
                    data.disabled = true;
                    WMRecipeCust.Dbgl($"{data.name} is set for Adminonly, you are not an admin");
                }
            }

            if (data.disabled)
            {
                WMRecipeCust.Dbgl($"Disabling Piece {data.name}");
                go.GetComponent<Piece>().m_enabled = false;
            }
            else
                WMRecipeCust.Dbgl("Setting Piece data for " + data.name);
            if (!string.IsNullOrEmpty(data.m_name))
            {
                go.GetComponent<Piece>().m_name = data.m_name;
                go.GetComponent<Piece>().m_description = data.m_description;
            }
            CraftingStation currentStation = DataHelpers.GetCraftingStation(data.craftingStation);
            CraftingStation checkifStation = null;
            bool CStationAdded = false;
            if (data.clone)
            {
                string tempnam = null;
                tempnam = go.GetComponent<CraftingStation>()?.m_name;
                if (tempnam != null)
                {
                    checkifStation = DataHelpers.GetCraftingStation(tempnam); // for forge and other items that change names between item and CraftingStation
                    if (checkifStation != null)
                        CStationAdded = WMRecipeCust.NewCraftingStations.Contains(checkifStation);
                }
            }
            if (data.clone && checkifStation != null && !CStationAdded)
            {
                //go.GetComponent<Piece>().m_craftingStation = ""; dont change crafting station hopefully it is empty already
                go.GetComponent<CraftingStation>().name = data.name;
                go.GetComponent<CraftingStation>().m_name = data.m_name;
                WMRecipeCust.NewCraftingStations.Add(go.GetComponent<CraftingStation>()); // keeping track of them is hard

                WMRecipeCust.Dbgl($"  new CraftingStation named {data.name} ");
            }
            go.GetComponent<Piece>().m_craftingStation = DataHelpers.GetCraftingStation(data.craftingStation);
            if (data.minStationLevel > 1)
            {
                WMRecipeCust.pieceWithLvl.Add(go.name + "." + data.minStationLevel);
            }
            List<Piece.Requirement> reqs = new List<Piece.Requirement>();
            foreach (string req in data.reqs)
            {
                string[] parts = req.Split(':');
                reqs.Add(new Piece.Requirement() { m_resItem = ObjectDB.instance.GetItemPrefab(parts[0]).GetComponent<ItemDrop>(), m_amount = int.Parse(parts[1]), m_amountPerLevel = int.Parse(parts[2]), m_recover = parts[3].ToLower() == "true" });
            }
            go.GetComponent<Piece>().m_resources = reqs.ToArray();

        }


        public static Component[] renderfinder;
        private static Renderer[] renderfinder2;

        private static void SetItemData(WItemData_json data, ObjectDB Instant)
        {
            // Dbgl("Loaded SetItemData!");
            bool skip = false;
            foreach (var citem in WMRecipeCust.ClonedI)
            {
                if (citem == data.name)
                    skip = true;
            }
            string tempname = data.name;
            if (data.clone && !skip)
            {
                data.name = data.clonePrefabName;
            }
            GameObject go = DataHelpers.CheckforSpecialObjects(data.name);// check for special cases
            if (go == null)
                go = Instant.GetItemPrefab(data.name); // normal check

            if (go == null)
            {
                WMRecipeCust.WLog.LogWarning(" item in SetItemData null " + data.name);
                return;
            }
            if (go.GetComponent<ItemDrop>() == null)
            {
                WMRecipeCust.WLog.LogWarning($"Item data in SetItemData for {data.name} not found!");
                return;
            } // it is a prefab and it is an item.
            if (string.IsNullOrEmpty(tempname) && data.clone)
            {
                WMRecipeCust.WLog.LogWarning($"Item cloned name is empty!");
                return;
            }
            for (int i = Instant.m_items.Count - 1; i >= 0; i--)  // need to handle clones
            {
                if (Instant.m_items[i]?.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name) //if (ObjectDB.instance.m_recipes[i].m_item?.m_itemData.m_shared.m_name == go.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)
                {
                    ItemDrop.ItemData PrimaryItemData = Instant.m_items[i].GetComponent<ItemDrop>().m_itemData;
                    if (data.clone && !skip) // object is a clone do clonethings
                    {
                        WMRecipeCust.Dbgl($"Item CLONE DATA in SetItemData for {tempname} ");
                        WMRecipeCust.ClonedI.Add(tempname);
                        Transform RootT = WMRecipeCust.Root.transform; // Root set to inactive to perserve components. 
                        GameObject newItem = WMRecipeCust.Instantiate(go, RootT, false);
                        ItemDrop NewItemComp = newItem.GetComponent<ItemDrop>();

                        NewItemComp.name = tempname; // added and seems to be the issue
                        newItem.name = tempname; // resets the orginal name- needs to be unquie
                        NewItemComp.m_itemData.m_shared.m_name = data.m_name; // ingame name
                        var hash = newItem.name.GetStableHashCode();
                        ObjectDB.instance.m_items.Add(newItem);

                        ZNetScene znet = ZNetScene.instance;
                        if (znet)
                        {
                            string name = newItem.name;
                            if (znet.m_namedPrefabs.ContainsKey(hash))
                                WMRecipeCust.WLog.LogWarning($"Prefab {name} already in ZNetScene");
                            else
                            {
                                if (newItem.GetComponent<ZNetView>() != null)
                                    znet.m_prefabs.Add(newItem);
                                else
                                    znet.m_nonNetViewPrefabs.Add(newItem);

                                znet.m_namedPrefabs.Add(hash, newItem);
                                WMRecipeCust.Dbgl($"Added prefab {name}");
                            }
                        }
                       


                        PrimaryItemData = Instant.GetItemPrefab(tempname).GetComponent<ItemDrop>().m_itemData; // get ready to set stuff
                        data.name = tempname; // putting back name

                    }

                    WMRecipeCust.Dbgl($"Item being Set in SetItemData for {data.name} ");

                    if (data.m_damages != null && data.m_damages != "")
                    {

                        WMRecipeCust.Dbgl($"   {data.name} Item has damage values ");
                        // has to be in order, should be
                        char[] delims = new[] { ',' };
                        string[] divideme = data.m_damages.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                        //Dbgl($"Item damge for 0 {divideme[0]} " + $" Item damge for 10 {divideme[10]} ");
                        HitData.DamageTypes damages = default(HitData.DamageTypes);
                        damages.m_blunt = stringtoFloat(divideme[0]);
                        damages.m_chop = stringtoFloat(divideme[1]);
                        damages.m_damage = stringtoFloat(divideme[2]);
                        damages.m_fire = stringtoFloat(divideme[3]);
                        damages.m_frost = stringtoFloat(divideme[4]);
                        damages.m_lightning = stringtoFloat(divideme[5]);
                        damages.m_pickaxe = stringtoFloat(divideme[6]);
                        damages.m_pierce = stringtoFloat(divideme[7]);
                        damages.m_poison = stringtoFloat(divideme[8]);
                        damages.m_slash = stringtoFloat(divideme[9]);
                        damages.m_spirit = stringtoFloat(divideme[10]);
                        PrimaryItemData.m_shared.m_damages = damages;
                    }
                    if (data.m_damagesPerLevel != null && data.m_damagesPerLevel != "")
                    {
                        char[] delims = new[] { ',' };
                        string[] divideme = data.m_damagesPerLevel.Split(delims, StringSplitOptions.RemoveEmptyEntries);

                        HitData.DamageTypes damagesPerLevel = default(HitData.DamageTypes);
                        damagesPerLevel.m_blunt = stringtoFloat(divideme[0]);
                        damagesPerLevel.m_chop = stringtoFloat(divideme[1]);
                        damagesPerLevel.m_damage = stringtoFloat(divideme[2]);
                        damagesPerLevel.m_fire = stringtoFloat(divideme[3]);
                        damagesPerLevel.m_frost = stringtoFloat(divideme[4]);
                        damagesPerLevel.m_lightning = stringtoFloat(divideme[5]);
                        damagesPerLevel.m_pickaxe = stringtoFloat(divideme[6]);
                        damagesPerLevel.m_pierce = stringtoFloat(divideme[7]);
                        damagesPerLevel.m_poison = stringtoFloat(divideme[8]);
                        damagesPerLevel.m_slash = stringtoFloat(divideme[9]);
                        damagesPerLevel.m_spirit = stringtoFloat(divideme[10]);
                        PrimaryItemData.m_shared.m_damagesPerLevel = damagesPerLevel;
                    }
                    PrimaryItemData.m_shared.m_name = data.m_name;
                    PrimaryItemData.m_shared.m_description = data.m_description;
                    PrimaryItemData.m_shared.m_weight = data.m_weight;
                    PrimaryItemData.m_shared.m_maxStackSize = data.m_maxStackSize;
                    PrimaryItemData.m_shared.m_food = data.m_foodHealth;
                    PrimaryItemData.m_shared.m_foodStamina = data.m_foodStamina;
                    PrimaryItemData.m_shared.m_foodRegen = data.m_foodRegen;
                    PrimaryItemData.m_shared.m_foodBurnTime = data.m_foodBurnTime;
                    PrimaryItemData.m_shared.m_foodEitr = data.m_FoodEitr;
                    // if (data.m_foodColor != null && data.m_foodColor != "" && data.m_foodColor.StartsWith("#"))
                    //{
                    //  PrimaryItemData.m_shared.m_foodColor = ColorUtil.GetColorFromHex(data.m_foodColor);
                    //}
                    PrimaryItemData.m_shared.m_armor = data.m_armor;
                    PrimaryItemData.m_shared.m_armorPerLevel = data.m_armorPerLevel;
                    PrimaryItemData.m_shared.m_blockPower = data.m_blockPower;
                    PrimaryItemData.m_shared.m_blockPowerPerLevel = data.m_blockPowerPerLevel;
                    PrimaryItemData.m_shared.m_canBeReparied = data.m_canBeReparied;
                    PrimaryItemData.m_shared.m_timedBlockBonus = data.m_timedBlockBonus;
                    PrimaryItemData.m_shared.m_deflectionForce = data.m_deflectionForce;
                    PrimaryItemData.m_shared.m_deflectionForcePerLevel = data.m_deflectionForcePerLevel;
                    PrimaryItemData.m_shared.m_backstabBonus = data.m_backstabbonus;
                    PrimaryItemData.m_shared.m_destroyBroken = data.m_destroyBroken;
                    PrimaryItemData.m_shared.m_dodgeable = data.m_dodgeable;
                    PrimaryItemData.m_shared.m_maxDurability = data.m_maxDurability;
                    PrimaryItemData.m_shared.m_durabilityDrain = data.m_durabilityDrain;
                    PrimaryItemData.m_shared.m_durabilityPerLevel = data.m_durabilityPerLevel;
                    PrimaryItemData.m_shared.m_equipDuration = data.m_equipDuration;
                    //PrimaryItemData.m_shared.m_holdDurationMin = data.m_holdDurationMin;
                    //PrimaryItemData.m_shared.m_holdStaminaDrain = data.m_holdStaminaDrain;
                    PrimaryItemData.m_shared.m_maxQuality = data.m_maxQuality;
                    PrimaryItemData.m_shared.m_useDurability = data.m_useDurability;
                    PrimaryItemData.m_shared.m_useDurabilityDrain = data.m_useDurabilityDrain;
                    PrimaryItemData.m_shared.m_questItem = data.m_questItem;
                    PrimaryItemData.m_shared.m_teleportable = data.m_teleportable;
                    PrimaryItemData.m_shared.m_toolTier = data.m_toolTier;
                    PrimaryItemData.m_shared.m_value = data.m_value;
                    PrimaryItemData.m_shared.m_movementModifier = data.m_movementModifier;
                    PrimaryItemData.m_shared.m_eitrRegenModifier = data.m_EitrRegen;

                    PrimaryItemData.m_shared.m_attack.m_attackHealthPercentage = data.m_attackHealthPercentage;
                    PrimaryItemData.m_shared.m_attack.m_attackStamina = data.m_attackStamina;
                    PrimaryItemData.m_shared.m_attack.m_attackEitr = data.m_EitrCost;

                    PrimaryItemData.m_shared.m_secondaryAttack.m_attackHealthPercentage = data.m_secAttackHealthPercentage;
                    PrimaryItemData.m_shared.m_secondaryAttack.m_attackStamina = data.m_secAttackStamina;
                    PrimaryItemData.m_shared.m_secondaryAttack.m_attackEitr = data.m_secEitrCost;

                    PrimaryItemData.m_shared.m_attackForce = data.m_knockback;
                    //PrimaryItemData.m_shared.m


                    PrimaryItemData.m_shared.m_damageModifiers.Clear(); // from aedenthorn start -  thx
                    foreach (string modString in data.damageModifiers)
                    {
                        string[] mod = modString.Split(':');
                        int modType = Enum.TryParse<NewDamageTypes>(mod[0], out NewDamageTypes result) ? (int)result : (int)Enum.Parse(typeof(HitData.DamageType), mod[0]);
                        PrimaryItemData.m_shared.m_damageModifiers.Add(new HitData.DamageModPair() { m_type = (HitData.DamageType)modType, m_modifier = (HitData.DamageModifier)Enum.Parse(typeof(HitData.DamageModifier), mod[1]) }); // end aedenthorn code
                    }
                    if (PrimaryItemData.m_shared.m_value > 0)
                    {
                        string valu = "              <color=#edd221>Valuable</color>";
                        PrimaryItemData.m_shared.m_description = data.m_description + valu;
                    }
                    return; // done, I don't need to continue?
                } // Dbgl("Amost done with SetItemData!");


            }

        }






    }
}
