using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using wackydatabase.Datas;

namespace wackydatabase.Datas
{
    public class DataHelpers : WMRecipeCust
    {

        public static CraftingStation GetCraftingStation(string name)
        {
            if (name == "" || name == null)
                return null;

            //Dbgl("Looking for crafting station " + name);

            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe?.m_craftingStation?.m_name == name)
                {

                    //  Dbgl("got crafting station " + name);

                    return recipe.m_craftingStation;
                }
            }
            foreach (GameObject piece in GetPieces())
            {

                if (piece.GetComponent<Piece>()?.m_craftingStation?.m_name == name)
                {

                    // Dbgl("got crafting station " + name);

                    return piece.GetComponent<Piece>().m_craftingStation;

                }
            }
            try
            {
                GameObject piecemod = GetModdedPieces(name); // last check not a good check/ rewrite
                if (piecemod.GetComponent<Piece>()?.m_craftingStation?.m_name == name)
                {
                    return piecemod.GetComponent<Piece>().m_craftingStation;
                }
            }
            catch { }

            // new craftingstatinos created keeping list
            foreach (CraftingStation station in NewCraftingStations)
            {
                if (station.name == name)
                    return station;
            }


            return null;
        }
        public static List<GameObject> GetPieces(ObjectDB Instant)
        {
            var pieces = new List<GameObject>();
            if (!Instant)
                return pieces;

            ItemDrop hammer = Instant.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();

            if (hammer)
                pieces.AddRange(Traverse.Create(hammer.m_itemData.m_shared.m_buildPieces).Field("m_pieces").GetValue<List<GameObject>>());

            ItemDrop hoe = Instant.GetItemPrefab("Hoe")?.GetComponent<ItemDrop>();
            if (hoe)
                pieces.AddRange(Traverse.Create(hoe.m_itemData.m_shared.m_buildPieces).Field("m_pieces").GetValue<List<GameObject>>());

            return pieces;

        }
        public static List<GameObject> GetPieces()
        {
            var pieces = new List<GameObject>();
            if (!ObjectDB.instance)
                return pieces;

            ItemDrop hammer = ObjectDB.instance.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();

            if (hammer)
                pieces.AddRange(Traverse.Create(hammer.m_itemData.m_shared.m_buildPieces).Field("m_pieces").GetValue<List<GameObject>>());

            ItemDrop hoe = ObjectDB.instance.GetItemPrefab("Hoe")?.GetComponent<ItemDrop>();
            if (hoe)
                pieces.AddRange(Traverse.Create(hoe.m_itemData.m_shared.m_buildPieces).Field("m_pieces").GetValue<List<GameObject>>());

            return pieces;

        }
        public static GameObject GetModdedPieces(string name)
        {
            selectedPiecehammer = null;
            GameObject Searchingfor = null;
            foreach (PieceTable Station in MaybePieceStations) // look for known modded hammers, Forget the indivual item, now just PieceTable
            {
                Searchingfor = Station.m_pieces.Find(g => Utils.GetPrefabName(g) == name);
                if (Searchingfor != null)
                {
                    selectedPiecehammer = Station;
                    return (Searchingfor);
                }
            }
            return Searchingfor;
        }

        public static GameObject FindPieceObjectName(string name)
        {
            GameObject go = GetPieces().Find(g => Utils.GetPrefabName(g) == name); // vanilla search
            if (go == null)
            {
                go = GetModdedPieces(name); // known modded Hammer search
                if (go == null)
                {
                    go = CheckforSpecialObjects(name); // check for special
                }
            }
            return go;
        }

        public static void GetPieceStations()
        {
            MaybePieceStations = Resources.FindObjectsOfTypeAll<PieceTable>(); // so easy
        }
        public static void GetPiecesatStart()
        {
            // PiecesinGame = Resources.FindObjectsOfTypeAll<Piece>();  don't call this bad bad idea for ram
        }

        public static GameObject CheckforSpecialObjects(string name) // should handle all times of special cases, manual entry
        {
            GameObject go = null;
            string ZnetName = null;
            switch (name)
            {
                case "stone_floor":
                    ZnetName = "stone_floor";  // going to restrict it to make sure mod can't get in trouble
                    break;
                case "bow":
                    ZnetName = "Bow";
                    break;
                case "Bow":
                    ZnetName = "Bow";
                    break;
                case "SpearBronze":
                    ZnetName = "SpearBronze";
                    break;
                case "ShieldBronzeBuckler":
                    ZnetName = "ShieldBronzeBuckler";
                    break;
                case "HelmetBronze":
                    ZnetName = "HelmetBronze";
                    break;
                case "AxeIron":
                    ZnetName = "AxeIron";
                    break;

                default:
                    go = null;
                    break;
            }
            if (ZnetName != null)
            {
                try
                {
                    go = ZNetScene.instance.GetPrefab(ZnetName); // damn why didn't I discover this sooner// that is sooo brutal. 
                }
                catch { }

            }

            return go;
        }


    }
}
