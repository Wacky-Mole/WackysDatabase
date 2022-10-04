using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using wackydatabase.Datas;
using wackydatabase.Util;
using wackydatabase.Armor;

namespace wackydatabase.SetData
{
    public class SetData 
    {
        
        internal static void SetRecipeData(RecipeData_json data, ObjectDB Instant)
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
                WMRecipeCust.Dbgl("maybe null " + data.name + " Should not get here");
                return;
            }

            if (go.GetComponent<ItemDrop>() == null)
            {
                WMRecipeCust.Dbgl($"Item recipe data for {data.name} not found!");
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

        internal static void SetPieceRecipeData(PieceData_json data, ObjectDB Instant)
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
                /*
                if (!string.IsNullOrEmpty(data.cloneMaterial))
                {
                    Dbgl($"Material name searching for {data.cloneMaterial}");
                    try
                    {
                        renderfinder = newItem.GetComponentsInChildren<Renderer>();// "weapons1_fire" glowing orange
                        if (data.cloneMaterial.Contains(','))
                        {
                            string[] materialstr = data.cloneMaterial.Split(',');
                            Material mat = originalMaterials[materialstr[0]];
                            Material part = originalMaterials[materialstr[1]];

                            foreach (Renderer renderitem in renderfinder)
                            {
                                if (renderitem.receiveShadows && materialstr[0] != "none")
                                    renderitem.material = mat;
                                else if (!renderitem.receiveShadows)
                                    renderitem.material = part;
                            }
                        }
                        else
                        {
                            Material mat = originalMaterials[data.cloneMaterial];
                            foreach (Renderer renderitem in renderfinder)
                            {
                                if (renderitem.receiveShadows)
                                    renderitem.material = mat;
                            }
                        }
                    }
                    catch { WLog.LogWarning("Material was not found or was not set correctly"); }
                }
                */ //Set  later

                GameObject piecehammer = Instant.GetItemPrefab(data.piecehammer);
                skip = true;
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

            if (!string.IsNullOrEmpty(data.cloneMaterial)) // allows changing of any piece
            {
                WMRecipeCust.Dbgl($"Material name searching for {data.cloneMaterial} for piece {data.name}"); // need to take in account worn at %50
                try
                {

                    renderfinder = go.GetComponentsInChildren<Renderer>();
                    renderfinder2 = go.GetComponentsInChildren<Renderer>(true); // include inactives
                    if (data.cloneMaterial.Contains("same_mat") || data.cloneMaterial.Contains("no_wear"))
                    {
                        WMRecipeCust.Dbgl($"No Wear set for {data.name}");
                        Material samematerial = null;
                        foreach (Renderer renderitem in renderfinder) // get for piece at full heatlh
                        {
                            if (renderitem.receiveShadows)
                            {
                                samematerial = renderitem.material;
                                break;
                            }
                        }
                        foreach (Renderer renderitem in renderfinder2) // set for Pieces @ 50%
                        {
                            if (renderitem.receiveShadows)
                                renderitem.material = samematerial;
                        }
                    }
                    else
                    {
                        if (data.cloneMaterial.Contains(','))
                        {
                            string[] materialstr = data.cloneMaterial.Split(',');
                            Material mat = WMRecipeCust.originalMaterials[materialstr[0]];
                            Material part = WMRecipeCust.originalMaterials[materialstr[1]];

                            foreach (Renderer renderitem in renderfinder2) // for Pieces @ 50%
                            {
                                if (renderitem.receiveShadows)
                                    renderitem.material = part;
                            }

                            foreach (Renderer renderitem in renderfinder) // set after all of the piece for %50
                            {
                                if (renderitem.receiveShadows)
                                    renderitem.material = mat;
                            }

                        }
                        else
                        {

                            Material mat = WMRecipeCust.originalMaterials[data.cloneMaterial];
                            foreach (Renderer renderitem in renderfinder2)
                            {
                                if (renderitem.receiveShadows)
                                {
                                    renderitem.material = mat;
                                }
                            }

                        }
                    }
                }
                catch { WMRecipeCust.WLog.LogWarning("Material was not found or was not set correctly"); }
            }
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
                    }
                }
            } //end Cat
            if (data.adminonly)
            {
                if (WMRecipeCust.Admin)
                {
                    // do nothing, but search if it has been disabled before
                    /*
                    foreach(GameObject searc in AdminPiecesOnly.Keys)
                    {
                        if (searc == go)
                        { // found object so need to add it back because it was disabled previously

                           GameObject newhammer = AdminPiecesOnly[searc];
                           newhammer?.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(searc);
                           Dbgl($"Congrats on your new promotion. Enabling previously disabled piece.");
                           AdminPiecesOnly.Remove(searc); // delete so it is not found again
                        }
                    } */
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

                WMRecipeCust.Dbgl($"Removing recipe for {data.name} from some PieceStation, you need to reload session to get this piece back- including admins");
                GameObject piecehammer = Instant.GetItemPrefab(data.piecehammer);
                bool keyExists = false;
                try
                {
                    // keyExists = AdminPiecesOnly.ContainsKey(go); // make sure it only gets set once
                }
                catch { keyExists = false; } // bad wacky
                                             // Dbgl($"Check 0");
                if (piecehammer == null)
                {
                    if (WMRecipeCust.selectedPiecehammer == null)
                    {
                        //Dbgl($"Check 1");
                        piecehammer = ObjectDB.instance.GetItemPrefab("Hammer"); // default add // default delete
                        piecehammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Remove(go);
                        //if (data.adminonly && !keyExists) 
                        //  AdminPiecesOnly.Add(go, piecehammer);
                    }
                    else
                    {
                        //Dbgl($"Check 2");
                        WMRecipeCust.selectedPiecehammer.m_pieces.Remove(go); // found in modded hammers
                        GameObject temp2 = WMRecipeCust.selectedPiecehammer.gameObject;
                        //if (data.adminonly && !keyExists)
                        //AdminPiecesOnly.Add(go, temp2);
                    }
                }
                else
                {
                    piecehammer?.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Remove(go); // if piecehammer is the actual item and not the PieceTable
                                                                                                                 // if (data.adminonly && !keyExists)
                                                                                                                 //   AdminPiecesOnly.Add(go, piecehammer);
                }
            }
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
        internal static Renderer[] renderfinder2;

        internal static void SetItemData(WItemData_json data, ObjectDB Instant)
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
                WMRecipeCust.Dbgl(" item in SetItemData null " + data.name);
                return;
            }
            if (go.GetComponent<ItemDrop>() == null)
            {
                WMRecipeCust.Dbgl($"Item data in SetItemData for {data.name} not found!");
                return;
            } // it is a prefab and it is an item.
            if (string.IsNullOrEmpty(tempname) && data.clone)
            {
                WMRecipeCust.Dbgl($"Item cloned name is empty!");
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
                        /*
                               SpriteRenderer m_Spriter;
                               Color newtestcolor;
                               m_Spriter = newItem.AddComponent<SpriteRenderer>();
                               m_Spriter.color = Color.blue;

                               Texture2D val = new Texture2D(1, 1);
                               //ImageConversion.LoadImage(val, Convert.FromBase64String("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"));
                              // Sprite IconReplacement = Sprite.Create(val, new Rect(0f, 0f, (float)((Texture)val).get_width(), (float)((Texture)val).get_height()), Vector2.get_zero());
                               Sprite test = Instantiate<Sprite>(NewItemComp.m_itemData.m_shared.m_icons[0]);
                               newItem.AddComponent<SpriteRenderer>();
                               newItem.GetComponent<SpriteRenderer>().sprite = test;
                               newItem.GetComponent<SpriteRenderer>().flipY = true;
                               NewItemComp.m_itemData.m_shared.m_icons[0] = newItem.GetComponent<SpriteRenderer>().sprite;
                               //NewItemComp.m_itemData.m_shared.m_icons[0].
                               */


                        //ObjectDB.instance.UpdateItemHashes();
                        if (!string.IsNullOrEmpty(data.cloneMaterial))
                        {
                            WMRecipeCust.Dbgl($"Material name searching for {data.cloneMaterial}");
                            try
                            {
                                renderfinder = newItem.GetComponentsInChildren<Renderer>();// "weapons1_fire" glowing orange
                                if (data.cloneMaterial.Contains(','))
                                {
                                    string[] materialstr = data.cloneMaterial.Split(',');
                                    Material mat = WMRecipeCust.originalMaterials[materialstr[0]];
                                    Material part = WMRecipeCust.originalMaterials[materialstr[1]];

                                    foreach (Renderer renderitem in renderfinder)
                                    {
                                        if (renderitem.receiveShadows && materialstr[0] != "none")
                                            renderitem.material = mat;
                                        else if (!renderitem.receiveShadows)
                                            renderitem.material = part;
                                    }
                                }
                                else
                                {
                                    Material mat = WMRecipeCust.originalMaterials[data.cloneMaterial];
                                    foreach (Renderer renderitem in renderfinder)
                                    {
                                        if (renderitem.receiveShadows)
                                            renderitem.material = mat;
                                    }
                                }
                            }
                            catch { WMRecipeCust.WLog.LogWarning("Material was not found or was not set correctly"); }
                        }

                        PrimaryItemData = Instant.GetItemPrefab(tempname).GetComponent<ItemDrop>().m_itemData; // get ready to set stuff
                        data.name = tempname; // putting back name
                        try
                        {
                            // ObjectDB.instance.UpdateItemHashes();
                        }
                        catch
                        {
                            WMRecipeCust.Dbgl($"Item {tempname} failed to update Hashes");
                        }
                    }
                    WMRecipeCust.Dbgl($"Item being Set in SetItemData for {data.name} ");

                    if (data.m_damages != null && data.m_damages != "")
                    {
                        WMRecipeCust.Dbgl($"   {data.name} Item has damage values ");

                        // Separate the CSV value of weapon damages (perhaps this should be loaded/mapped differently?)
                        string[] entries = data.m_damages.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        PrimaryItemData.m_shared.m_damages = WeaponDamage.ParseDamageTypes(entries);
                    }

                    if (data.m_damagesPerLevel != null && data.m_damagesPerLevel != "")
                    {
                        string[] entries = data.m_damagesPerLevel.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        PrimaryItemData.m_shared.m_damagesPerLevel = WeaponDamage.ParseDamageTypes(entries);
                    }

                    PrimaryItemData.m_shared.m_name = data.m_name;
                    PrimaryItemData.m_shared.m_description = data.m_description;
                    PrimaryItemData.m_shared.m_weight = data.m_weight;
                    PrimaryItemData.m_shared.m_maxStackSize = data.m_maxStackSize;
                    PrimaryItemData.m_shared.m_food = data.m_foodHealth;
                    PrimaryItemData.m_shared.m_foodStamina = data.m_foodStamina;
                    PrimaryItemData.m_shared.m_foodRegen = data.m_foodRegen;
                    PrimaryItemData.m_shared.m_foodBurnTime = data.m_foodBurnTime;
                    if (data.m_foodColor != null && data.m_foodColor != "" && data.m_foodColor.StartsWith("#"))
                    {
                        PrimaryItemData.m_shared.m_foodColor = ColorUtil.GetColorFromHex(data.m_foodColor);
                    }
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
                    PrimaryItemData.m_shared.m_holdDurationMin = data.m_holdDurationMin;
                    PrimaryItemData.m_shared.m_holdStaminaDrain = data.m_holdStaminaDrain;
                    PrimaryItemData.m_shared.m_maxQuality = data.m_maxQuality;
                    PrimaryItemData.m_shared.m_useDurability = data.m_useDurability;
                    PrimaryItemData.m_shared.m_useDurabilityDrain = data.m_useDurabilityDrain;
                    PrimaryItemData.m_shared.m_questItem = data.m_questItem;
                    PrimaryItemData.m_shared.m_teleportable = data.m_teleportable;
                    PrimaryItemData.m_shared.m_toolTier = data.m_toolTier;
                    PrimaryItemData.m_shared.m_value = data.m_value;
                    PrimaryItemData.m_shared.m_movementModifier = data.m_movementModifier;
                    // PrimaryItemData.m_shared.m_attack.m_attackStamina = data.m_attackStamina;
                    //PrimaryItemData.m_shared.m_secondaryAttack.m_attackStamina = data.m_attackStamina; // set for both
                    PrimaryItemData.m_shared.m_attackForce = data.m_knockback;
                    //PrimaryItemData.m_shared.m

                    // someone is going to complain that I am adding too many... I just know it.
                    //int skillme = Enum.TryParse<Skills.SkillType>(data.m_skillType, out Skills.SkillType skillresult) ? (int)skillresult : (int)Enum.Parse(typeof(Skills.SkillType), data.m_skillType);
                    //PrimaryItemData.m_shared.m_skillType = (Skills.SkillType)skillme;
                    //PrimaryItemData.m_shared.m_attack = data.primaryAttack;
                    //PrimaryItemData.m_shared.m_holdAnimationState = data.m_holdAnimationState;
                    //PrimaryItemData.m_shared.m_animationState = (ItemDrop.ItemData.AnimationState)data.m_animationState;
                    //Attack
                    /* What do I want
                     * m_speedFactor
                     * m_speedFactorRotation
                     * m_staggerMultiplier
                     * AttackType
                     * m_attackChainLevels ? doesn't change animation so probably no?
                     * m_attackStamina
                     * m_forceMultiplier
                     * m_attackStartNoise
                     * m_attackHitNoise
                     * m_attackRange
                     * m_attackHeight
                     * m_consumeItem
                     * m_attackAnimation
                     * m_spawnOnTrigger
                     * m_lastChainDamageMultiplier
                     * m_multiHit
                     * m_attackProjectile
                     * m_projectileVel
                     * m_projectileVelMin
                     * m_projectileAccuracy
                     * m_projectileAccuracyMin
                     * m_projectiles
                     * m_projectileBursts
                     * m_burstInterval
                     * m_destroyPreviousProjectile
                     */
                    /*
                    foreach (string AttString in data.primaryAttack)
                    {
                        string[] mod = AttString.Split(':');
                        int modType = Enum.TryParse<NewDamageTypes>(mod[0], out NewDamageTypes result) ? (int)result : (int)Enum.Parse(typeof(HitData.DamageType), mod[0]);
                        PrimaryItemData.m_shared.m_damageModifiers.Add(new HitData.DamageModPair() { m_type = (HitData.DamageType)modType, m_modifier = (HitData.DamageModifier)Enum.Parse(typeof(HitData.DamageModifier), mod[1]) }); // end aedenthorn code
                    }

                    foreach (string SecAttString in data.secondaryAttack)
                    {
                        string[] mod = SecAttString.Split(':');
                        int modType = Enum.TryParse<NewDamageTypes>(mod[0], out NewDamageTypes result) ? (int)result : (int)Enum.Parse(typeof(HitData.DamageType), mod[0]);
                        PrimaryItemData.m_shared.m_damageModifiers.Add(new HitData.DamageModPair() { m_type = (HitData.DamageType)modType, m_modifier = (HitData.DamageModifier)Enum.Parse(typeof(HitData.DamageModifier), mod[1]) }); // end aedenthorn code
                    }
                    */

                    PrimaryItemData.m_shared.m_damageModifiers.Clear(); // from aedenthorn start -  thx
                    foreach (string modString in data.damageModifiers)
                    {
                        string[] mod = modString.Split(':');
                        int modType = Enum.TryParse<ArmorHelpers.NewDamageTypes>(mod[0], out ArmorHelpers.NewDamageTypes result) ? (int)result : (int)Enum.Parse(typeof(HitData.DamageType), mod[0]);
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
