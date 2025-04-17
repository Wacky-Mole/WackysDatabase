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
        public string? sizeMultiplier;
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
        public bool? notOnWood;


        public WearNTearData? wearNTearData;

        public CraftingStationData? craftingStationData;

        public CSExtensionData? cSExtensionData; // Old

        public List<CSExtensionData>? cSExtensionDataList;

        public ContainerData? contData;

        public SmelterData? smelterData;

        public CookingStationData? cookingStationData;

        public BeehiveData? beehiveData;

        public FermenterData? fermStationData;

        public SapData? sapData;

        public ObliteratorData? incineratorData;

        public FireplaceData? fireplaceData;

        public TeleportWorldData? teleportWorldData;

        public PlantData? plantData;

        public ShipData? shipData;

        public ShieldGenData? shieldGenData;

        public BatteringRamData? batteringRamData;

        public List<string>? build = new List<string>();

    }
    public class ComfortData
    {
        //Confort
        public int? comfort;
        public Piece.ComfortGroup? comfortGroup;
        public string? comfortObjectName;
    }

    public class WearNTearData {
        //WearNTear
        public float? health;
        public HitData.DamageModifiers damageModifiers; 
        public bool? noRoofWear;
        public bool? noSupportWear;
        public bool? supports;
        public bool? triggerPrivateArea;
        public WearNTear.MaterialType? materialType;

    }

    public class ShipData
    {
        public bool? ashlandProof;
       // public float? shipHealth;
    }

    public class ShieldGenData
    {
        public string? name;
        public string? nameAdd;

        public int? maxFuel;
        public int? spawnWithFuel;
        public float? fuelPerDamage;
        public float? minRadius;
        public float? maxRadius;
        public bool? offWhenOutofFuel;

        public bool? attack;
        public float? attackChargeTime;
        public bool? attackPlayers;

        public List<string>? fuel;

    }

    public class BatteringRamData
    {
        public float? chargeTime;
        public int? maxFuel;
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
       // public string? smelterName;
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


        public string? addItemTooltip;

        public string? overcookedItem;
        public string? fuelItem;
        public bool? requireFire;
        public int? maxFuel;
        public int? secPerFuel;

        public List<CookStationConversionList>? cookConversion;
    }
    
    public class FermenterData
    {
        public float? fermDuration;
        public List<FermenterConversionList>? fermConversion;
    }

    public class SapData
    {
        public float? secPerUnit;
        public int? maxLevel;
        public string? producedItem;
        public string? connectedToWhat;

        public string? extractText;
        public string? drainingText;
        public string? drainingSlowText;
        public string? notConnectedText;
        public string? fullText;

    }

    public class BeehiveData
    {
        public bool? effectOnlyInDaylight;
        public float? maxCover;
        public Heightmap.Biome? biomes;
        public float? secPerUnit;
        public int? maxAmount;
        public string? dropItem;

        public string[]? effects;
        public EffectVerse[]? effectsPLUS;

        public string? extractText;
        public string? checkText;
        public string? areaText;
        public string? freespaceText;
        public string? sleepText;
        public string? happyText;
    }
    public class ObliteratorData
    {
        public string? defaultDrop;
        public int? defaultCostPerDrop;
        public List<ObliteratorList>? incineratorConversion;
    }

    public class fuelItemData
    {
        public string? name;
    }

    public class SmelterConversionList
    {
        public string? FromName;
        public string? ToName;
        public bool? Remove = false;

    }

    public class CookStationConversionList
    {
        public string? FromName;
        public string? ToName;
        public float? CookTime;
        public bool? Remove = false;

    }

    public class FermenterConversionList 
    {
        public string? FromName;
        public string? ToName;
        public int? Amount;
        public bool? Remove = false;
    }
    public class ObliteratorList
    {
        public string? Result;
        public int? ResultAmount;
        public int? Priority;
        public bool? RequireOnlyOne;
        public List<ObRequirementList>? Requirements;
    }

    public class FireplaceData
    {
        public float? StartFuel;
        public float? MaxFuel;
        public float? SecPerFuel;
        public bool? InfiniteFuel;
        public string? FuelType;
        public float? IgniteInterval;
        public float? IgniteChance;
        public int? IgniteSpread;
    }

    public class PlantData
    {
        public string? m_name;
        public float? GrowTime;
        public float? MaxGrowTime;
        public string? GrowPrefab;
        public float? MinSize;
        public float? MaxSize;
        public float? GrowRadius;
        public float? GrowRadiusVines;
        public bool? CultivatedGround;
        public bool? DestroyIfCantGrow;
        public bool? TolerateHeat;
        public bool? TolerateCold;
        public Heightmap.Biome? Biomes;
    }

    public class ObRequirementList
    {
        public string? Name;
        public int? Amount;
    }
    public class TeleportWorldData
    {
        public bool? AllowAllItems;
    }

}