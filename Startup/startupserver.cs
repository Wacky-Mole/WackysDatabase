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

namespace wackydatabase.Startup
{
    public class Startupserver : WMRecipeCust
    {

         public void GetRecipeDataFromFilesForServer()
        {
            CheckModFolder();
            var amber = new System.Text.StringBuilder();
            foreach (string file in Directory.GetFiles(assetPathconfig, "*.json", SearchOption.AllDirectories))
            {
                if (file.Contains("Item") || file.Contains("item")) // items are being rather mean with the damage classes
                {
                    try
                    {
                        WItemData data = JsonUtility.FromJson<WItemData>(File.ReadAllText(file));
                        amber.Append(File.ReadAllText(file));
                        amber.Append("@");
                        ItemDatas.Add(data);
                        ArmorData data3 = JsonUtility.FromJson<ArmorData>(File.ReadAllText(file));
                        armorDatas.Add(data3);
                    }
                    catch { WackysRecipeCustomizationLogger.LogWarning("Something went wrong in file " + file); }

                }
                else if (file.Contains("Piece") || file.Contains("piece"))
                {
                    try
                    {
                        PieceData data = JsonUtility.FromJson<PieceData>(File.ReadAllText(file));
                        amber.Append(File.ReadAllText(file));
                        amber.Append("@");
                        PieceDatas.Add(data);
                    }
                    catch { WackysRecipeCustomizationLogger.LogWarning("Something went wrong in file " + file); }
                }
                else // recipes
                {
                    try
                    {
                        RecipeData data = JsonUtility.FromJson<RecipeData>(File.ReadAllText(file));
                        amber.Append(File.ReadAllText(file));
                        amber.Append("@");
                        recipeDatas.Add(data);
                    }
                    catch { WackysRecipeCustomizationLogger.LogWarning("Something went wrong in file " + file); }

                }
            }
            jsonstring = amber.ToString();
            skillConfigData.Value = jsonstring;

            WackysRecipeCustomizationLogger.LogDebug("Loaded files");
            Dbgl($"Loaded files");
            jsonstring = amber.ToString();
            // skillConfigData.Value = jsonstring; Only for server 1st time
            if (isSetStringisDebug)
                Dbgl(jsonstring);
        }
    }
}
