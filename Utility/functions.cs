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
using System.Security.Cryptography;
using System.Globalization;
using System.Text.RegularExpressions;

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
    public class Functions 
    {
        internal static string ComputeSha256Hash(string rawData)
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
            WMRecipeCust.originalVFX = new Dictionary<string, GameObject>();
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
            if (WMRecipeCust.isDebug.Value)
                Debug.Log((pref ? WMRecipeCust.ModName + " " : "") + str);
        }

        public static void SnapshotItem(ItemDrop item, float lightIntensity = 1.3f, Quaternion? cameraRotation = null)
        {
            const int layer = 30;

            Camera camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.backgroundColor = Color.clear;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = 0.5f;
            camera.farClipPlane = 10000000;
            camera.cullingMask = 1 << layer;
            camera.transform.rotation = cameraRotation ?? Quaternion.Euler(90, 0, 45);

            Light topLight = new GameObject("Light", typeof(Light)).GetComponent<Light>();
            topLight.transform.rotation = Quaternion.Euler(150, 0, -5f);
            topLight.type = LightType.Directional;
            topLight.cullingMask = 1 << layer;
            topLight.intensity = lightIntensity;

            Rect rect = new(0, 0, 64, 64);

            GameObject visual = UnityEngine.Object.Instantiate(item.transform.Find("attach").gameObject);
            foreach (Transform child in visual.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = layer;
            }

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            Vector3 min = renderers.Aggregate(Vector3.positiveInfinity, (cur, renderer) => renderer is ParticleSystemRenderer ? cur : Vector3.Min(cur, renderer.bounds.min));
            Vector3 max = renderers.Aggregate(Vector3.negativeInfinity, (cur, renderer) => renderer is ParticleSystemRenderer ? cur : Vector3.Max(cur, renderer.bounds.max));
            Vector3 size = max - min;

            camera.targetTexture = RenderTexture.GetTemporary((int)rect.width, (int)rect.height);
            float maxDim = Mathf.Max(size.x, size.z);
            float minDim = Mathf.Min(size.x, size.z);
            float yDist = (maxDim + minDim) / Mathf.Sqrt(2) / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad);
            Transform transform = camera.transform;
            transform.position = ((min + max) / 2) with { y = max.y } + new Vector3(0, yDist, 0);
            topLight.transform.position = transform.position + new Vector3(-2, 0, 0.2f) / 3 * -yDist;

            camera.Render();

            RenderTexture currentRenderTexture = RenderTexture.active;
            RenderTexture.active = camera.targetTexture;

            Texture2D texture = new((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(rect, 0, 0);
            texture.Apply();

            RenderTexture.active = currentRenderTexture;

            item.m_itemData.m_shared.m_icons = new[] { Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f)) };

            UnityEngine.Object.DestroyImmediate(visual);
            camera.targetTexture.Release();

            UnityEngine.Object.Destroy(camera);
            UnityEngine.Object.Destroy(topLight);
        }


    }
}
