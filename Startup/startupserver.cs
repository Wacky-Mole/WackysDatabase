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

namespace wackydatabase.Startup
{
    public class Startupserver
    {

         public void CheckForJsons()
        {
            WMRecipeCust.CheckModFolder();
            var amber = new System.Text.StringBuilder();
            foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathconfig, "*.json", SearchOption.AllDirectories))
            {
                
                WMRecipeCust.jsonsFound = true; // stupid but works
                if (file.Contains("Item") || file.Contains("item")) // items are being rather mean with the damage classes
                {
                    try
                    {
                        WItemData_json data = JsonUtility.FromJson<WItemData_json>(File.ReadAllText(file));
                        amber.Append(File.ReadAllText(file));
                        amber.Append(WMRecipeCust.StringSeparator);
                        WMRecipeCust.ItemDatas.Add(data);
                        ArmorData_json data3 = JsonUtility.FromJson<ArmorData_json>(File.ReadAllText(file));
                        WMRecipeCust.armorDatas.Add(data3);
                    }
                    catch { WMRecipeCust.WLog.LogWarning("Something went wrong in file " + file); }

                }
                else if (file.Contains("Piece") || file.Contains("piece"))
                {
                    try
                    {
                        PieceData_json data = JsonUtility.FromJson<PieceData_json>(File.ReadAllText(file));
                        amber.Append(File.ReadAllText(file));
                        amber.Append(WMRecipeCust.StringSeparator);
                        WMRecipeCust.PieceDatas.Add(data);
                    }
                    catch { WMRecipeCust.WLog.LogWarning("Something went wrong in file " + file); }
                }
                else // recipes
                {
                    try
                    {
                        RecipeData_json data = JsonUtility.FromJson<RecipeData_json>(File.ReadAllText(file));
                        amber.Append(File.ReadAllText(file));
                        amber.Append(WMRecipeCust.StringSeparator);
                        WMRecipeCust.recipeDatas.Add(data);
                    }
                    catch { WMRecipeCust.WLog.LogWarning("Something went wrong in file " + file); }

                }
            }
            if (WMRecipeCust.jsonsFound)
            {
                WMRecipeCust.jsonstring = amber.ToString();
                WMRecipeCust.WLog.LogWarning("Jsons Found");
            }
        }
        internal void BeginConvertingJsons()
        {
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
