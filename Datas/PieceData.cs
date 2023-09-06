using System;
using UnityEngine;
using System.Collections.Generic;

namespace wackydatabase.Datas
{
    [Serializable]
    public class PieceData
    {
#nullable enable
        public string name; //must have
        public string piecehammer; // must have
        public string? m_name;
        public float? sizeMultiplier;
        public string? m_description;
        public string? customIcon;
        public string? clonePrefabName;
        //public string cloneEffects;
        public string? material;
        public string? damagedMaterial;
        public string? craftingStation;
        public string? piecehammerCategory;
        public int? minStationLevel;
        public int? amount;
        public bool? disabled;
        public bool? adminonly;
        
        public ComfortData? comfort;

        //Placement
        public bool? groundPiece;
        public bool? ground;
        public bool? waterPiece;
        public bool? noInWater;
        public bool? notOnFloor;
        public bool? onlyinTeleportArea;
        public bool? allowedInDungeons;
        public bool? canBeRemoved;


        public WearNTearData? wearNTearData;

        public CraftingStationData? craftingStationData;

        public CSExtensionData? cSExtensionData;

        public ContainerData? contData;

        public SmelterData? smelterData;

        public CookingStationData? cookingStationData;

        public List<string>? build = new List<string>();

    }
    public class ComfortData
    {
        //Confort
        public int? comfort;
        public Piece.ComfortGroup? comfortGroup;
        public GameObject? comfortObject;
    }

    public class WearNTearData {
        //WearNTear
        public float? health;
        public HitData.DamageModifiers damageModifiers; 
        public bool? noRoofWear;
        public bool? noSupportWear;
        public bool? supports;
        public bool? triggerPrivateArea;


    }


    public class CraftingStationData {

        //CraftingStation
       // public string? cStationName;
        public string? cStationCustomIcon;
        public float? discoveryRange;
        public float? buildRange;
        public bool? craftRequiresRoof;
        public bool? craftRequiresFire;
        public bool? showBasicRecipes;
        public float? useDistance;
        public int? useAnimation;

    }
    public class CSExtensionData
    {
        //Station Extension
        public string? MainCraftingStationName;
        public float? maxStationDistance;
        public bool? continousConnection;
        public bool? stack;


    }
    public class ContainerData
    {
        public int? Width;
        public int? Height;
        public bool? CheckWard;
        public bool? AutoDestoryIfEmpty;
        //public string? Privacy;

    }

    public class SmelterData
    {
        //Smelter Script
        public string? smelterName;
        public string? addOreTooltip;
        public string? emptyOreTooltip;

       // public Switch? addFuelSwitch;
       // public Switch? addOreSwitch;
       // public Switch? emptyOreSwitch;

        public fuelItemData? fuelItem;

        public int? maxOre;
        public int? maxFuel;
        public int? fuelPerProduct;
        public float? secPerProduct;
        public bool? spawnStack;
        public bool? requiresRoof;
        public float? addOreAnimationLength;

        public List<SmelterConversionList>? smelterConversion;
    }

    public class CookingStationData
    {
        //Cooking Script
       // public string? stationName;
       // public string? displayName;

        public string? addItemTooltip;

        public string? overcookedItem;
        public string? fuelItem;
        public bool? requireFire;
        public int? maxFuel;
        public int? secPerFuel;

        public List<CookStationConversionList>? cookConversion;
    }

    public class fuelItemData
    {
        public string? name;
    }

    public class SmelterConversionList
    {
        public string? FromName;
        public string? ToName;

    }

    public class CookStationConversionList
    {
        public string? FromName;
        public string? ToName;
        public float? CookTime;

    }

}