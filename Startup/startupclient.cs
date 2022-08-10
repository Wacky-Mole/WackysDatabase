using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wackydatabase.Startup
{
    internal class Startupclient
    {

        private static void GetRecipeDataFromFiles()
        {
            if (Firstrun)
            {
                CheckModFolder();
                GetAllMaterials();
                GetPieceStations();
                GetPiecesatStart();
                Firstrun = false;
            }
            recipeDatas.Clear();
            ItemDatas.Clear();
            PieceDatas.Clear();
            armorDatas.Clear();
            pieceWithLvl.Clear(); // ready for new
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
        }
}
