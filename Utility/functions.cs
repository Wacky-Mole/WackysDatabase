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

namespace wackydatabase.Util
{

    public static class CheckIt
    {
        public static void SetWhenNotNull(string mightBeNull, ref string notNullable)
        {
            if (mightBeNull != null)
            {
                notNullable = mightBeNull;
            }
        }
    }
    public class Functions : WMRecipeCust
    {
        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }

        }
        public static float stringtoFloat(string data)
        {
            data = data.Split(':').Last();
            float value = float.Parse(data, CultureInfo.InvariantCulture.NumberFormat);
            return value;
        }



        public static string GetAllMaterialsFile()
        {
            string TheString = "";
            Material[] array = Resources.FindObjectsOfTypeAll<Material>();
            Material[] array2 = array;
            foreach (Material val in array2)
            {
                Dbgl($"Material {val.name}");
                TheString = TheString + val.name + System.Environment.NewLine;
            }
            return TheString;
        }

        public static string GetAllVFXFile()
        {

            string TheString = "";

            GameObject[] array4 = Resources.FindObjectsOfTypeAll<GameObject>();
            originalVFX = new Dictionary<string, GameObject>();
            foreach (GameObject val2 in array4)
            {
                if (val2.name.Contains("vfx"))
                {
                    Dbgl($"VFX {val2.name}");
                    TheString = TheString + val2.name + System.Environment.NewLine;
                }
            }
            return TheString;
        }

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? ModName + " " : "") + str);
        }

    }
}
