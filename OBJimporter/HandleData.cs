using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using UnityEngine;

namespace wackydatabase.OBJimporter
{
    internal class HandleData
    {
        // scheme is going to be filename:type;base64==filename:type;base64==
        internal static string bigDataR;
        internal static List<string> bigDataRChucks = new List<string>();
        internal static string bigDataS;
        internal static List<string> bigDataSChucks = new List<string>();   

        public static void RecievedData()
        {
            bigDataR = WMRecipeCust.largeTransfer.Value;
            if (string.IsNullOrEmpty(bigDataR) || ZNet.instance.IsServer() && ZNet.instance.IsDedicated())
            {
                return;
            }
                
            bigDataRChucks.Clear();
            string[] checkfor = { "==" };
            bigDataRChucks = bigDataR.Split(checkfor, System.StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (var image in bigDataRChucks)
            {
               var index =  image.IndexOf(":");
                var index2 = image.IndexOf(";");
                var filename = image.Substring(0, index);
                WMRecipeCust.WLog.LogInfo("filename " + filename);
                int leng = index2 - index;
               var type = image.Substring(index + 1, leng -1);
                WMRecipeCust.WLog.LogInfo("type " + type);
                var imagebase64 = image.Substring(index2 + 1);
                imagebase64 = imagebase64 + "==";
                WMRecipeCust.WLog.LogInfo("image  " + imagebase64);
                byte[] decodedBytes = Convert.FromBase64String(imagebase64);
               //string decodedText = Encoding.UTF8.GetString(decodedBytes);
                if (type == "icon")
                {
                    var path = Path.Combine(WMRecipeCust.assetPathIcons, filename + ".png");
                    File.WriteAllBytes(path, decodedBytes);
                }
                else if (type == "png"){
                    var path = Path.Combine(WMRecipeCust.assetPathObjects, filename +".png");
                    File.WriteAllBytes(path, decodedBytes);

                }
                else if ( type == "obj") {
                    var path = Path.Combine(WMRecipeCust.assetPathObjects, filename +".obj");
                    File.WriteAllBytes(path, decodedBytes);

                }

                WMRecipeCust.WLog.LogInfo("Congrats you downloaded some huge files, restart game to apply them to gameplay");
            }
        }

        public static void SendData(long peer, ZPackage go) // should probably be a console command because this will send it to everyone and be huge!
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }
            WMRecipeCust.WLog.LogInfo("Starting Object and Icon folder base64ing");
            var Iconpathstrings = Directory.GetFiles(WMRecipeCust.assetPathIcons );
            var Objectpathstrings = Directory.GetFiles(WMRecipeCust.assetPathObjects, "*.obj", SearchOption.AllDirectories);
            var Pngpathstrings = Directory.GetFiles(WMRecipeCust.assetPathObjects, "*.png", SearchOption.AllDirectories);
            bigDataSChucks.Clear();

            foreach (var im in Iconpathstrings )
            {
                string filename = Path.GetFileNameWithoutExtension(im);
                string type = "icon";
                var goodbytes = File.ReadAllBytes(im);
                string data = Convert.ToBase64String(goodbytes);
                string Chunk = filename + ":" + type + ";" + data;
                bigDataSChucks.Add(Chunk);
            }
            foreach (var im in Objectpathstrings )
            {
                string filename = Path.GetFileNameWithoutExtension(im);
                string type = "obj";
                var goodbytes = File.ReadAllBytes(im);
                string data = Convert.ToBase64String(goodbytes);
                string Chunk = filename + ":" + type + ";" + data;
                bigDataSChucks.Add(Chunk);
            }
            foreach(var im in Pngpathstrings)
            {
                string filename = Path.GetFileNameWithoutExtension(im);
                string type = "png";
                var goodbytes = File.ReadAllBytes(im);
                string data = Convert.ToBase64String(goodbytes);
                string Chunk = filename + ":" + type + ";" + data;
                bigDataSChucks.Add(Chunk);
            }

            bigDataS = string.Join("", bigDataSChucks.ToArray());
            WMRecipeCust.WLog.LogInfo("Preparing to Send, hold onto your CPU");
            WMRecipeCust.largeTransfer.Value = bigDataS;
            //WMRecipeCust.WLog.LogWarning(bigDataS);

            // wait
            HandleData holdme = new HandleData();
            WMRecipeCust.context.StartCoroutine(holdme.WaittoReset());
        }

        IEnumerator WaittoReset()
        {
            yield return new WaitForSeconds(20);
            WMRecipeCust.WLog.LogInfo("Reset largeTransfer so new players don't get data");
            WMRecipeCust.largeTransfer.Value = ""; // so new players joining don't get the huge amount of data and crash
        }

    }
}
