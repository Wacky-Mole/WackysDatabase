using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using System.Collections;
using BepInEx;
using Object = UnityEngine.Object;

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

    public static class Ob
    {
        public static T Cast<T>(this UnityEngine.Object myobj)
        {
            Type objectType = myobj.GetType();
            Type target = typeof(T);
            var x = Activator.CreateInstance(target, false);
            var z = from source in objectType.GetMembers().ToList()
                    where source.MemberType == MemberTypes.Property
                    select source;
            var d = from source in target.GetMembers().ToList()
                    where source.MemberType == MemberTypes.Property
                    select source;
            List<MemberInfo> members = d.Where(memberInfo => d.Select(c => c.Name)
               .ToList().Contains(memberInfo.Name)).ToList();
            PropertyInfo propertyInfo;
            object value;
            foreach (var memberInfo in members)
            {
                propertyInfo = typeof(T).GetProperty(memberInfo.Name);
                value = myobj.GetType().GetProperty(memberInfo.Name).GetValue(myobj, null);

                propertyInfo.SetValue(x, value, null);
            }
            return (T)x;
        }
    }
    public class Functions 
    {

        internal static FieldInfo CompField(Type ClassTyp, string fieldname)
        {
            FieldInfo hello = ClassTyp.GetField(fieldname, BindingFlags.Instance | BindingFlags.Public);
            if (hello == null)
            {
                return null;
            }
            return hello;
        }

        internal static dynamic getCast<T>(Type ClassTyp, string fieldname, StatusEffect effect)
        {

            var hello = CompField(ClassTyp, fieldname);

            if (hello == null)
                return null;

            if (typeof(T) == typeof(int))
            {
                return (int)hello.GetValue(effect);
            }
            else if (typeof(T) == typeof(string))
            {
                return (string)hello.GetValue(effect);
            }
            else if (typeof(T) == typeof(double))
            {
                return (double)hello.GetValue(effect);
            }
            else if (typeof(T) == typeof(float))
            {
                return (float)hello.GetValue(effect);
            }
            else if (typeof(T) == typeof(Skills.SkillType))
            {
                return (Skills.SkillType)hello.GetValue(effect);
            }
            else if (typeof(T) == typeof(List<HitData.DamageModPair>))
            {
                return (List<HitData.DamageModPair>)hello.GetValue(effect);
            }
            else
            {
                return null;
            }
        }
        internal static void setValue(Type type,object go, string name, float? value=null, int? value2 = null, string? value3 = null, List<HitData.DamageModPair>? value4 =null, Skills.SkillType? value5 = null)
        {
            var field = Functions.CompField(type, name);
            if (field == null)
                return;

            if (value != null)
            {
                field.SetValue(go, value);
                return;
            }

            if (value2 != null)
            {
                field.SetValue(go, value2);
                return;
            }        

            if (value3 != null)
            {
                field.SetValue(go, value3);
                return;
            }          

            if (value4 != null)
            {
                field.SetValue(go, value4);
                return;
            }
                
            if (value5 != null)
                field.SetValue(go, value5);

        }


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
                if (val2.name.ToLower().Contains("vfx"))
                {
                    Dbgl($"VFX {val2.name}");
                    TheString = TheString + val2.name + System.Environment.NewLine;
                }
            }
            return TheString;
        }

        public static string GetAllSFXFile()
        {

            string TheString = "";

            GameObject[] array4 = Resources.FindObjectsOfTypeAll<GameObject>();
            WMRecipeCust.originalSFX = new Dictionary<string, GameObject>();
            foreach (GameObject val2 in array4)
            {
                if (val2.name.ToLower().Contains("sfx"))
                {
                    Dbgl($"SFX {val2.name}");
                    TheString = TheString + val2.name + System.Environment.NewLine;
                }
            }
            return TheString;
        }

        public static string GetAllFXFile()
        {

            string TheString = "";

            GameObject[] array4 = Resources.FindObjectsOfTypeAll<GameObject>();
            WMRecipeCust.originalFX = new Dictionary<string, GameObject>();
            foreach (GameObject val2 in array4)
            {
                if (val2.name.ToLower().StartsWith("fx_"))
                {
                    Dbgl($"FX {val2.name}");
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


        private static BaseUnityPlugin? _plugin;

        private static BaseUnityPlugin plugin
        {
            get
            {
                if (_plugin is null)
                {
                    IEnumerable<TypeInfo> types;
                    try
                    {
                        types = Assembly.GetExecutingAssembly().DefinedTypes.ToList();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        types = e.Types.Where(t => t != null).Select(t => t.GetTypeInfo());
                    }
                    _plugin = (BaseUnityPlugin)BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(types.First(t => t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));
                }
                return _plugin;
            }
        }

        public static void SnapshotPiece(GameObject prefab, float lightIntensity = 1.3f, Quaternion? cameraRotation = null)
        {
            void Do()
            {
                const int layer = 3;
                if (prefab == null) return;
                if (!prefab.GetComponentsInChildren<Renderer>().Any() && !prefab.GetComponentsInChildren<MeshFilter>().Any())
                {
                    return;
                }

                Camera camera = new GameObject("CameraIcon", typeof(Camera)).GetComponent<Camera>();
                camera.backgroundColor = Color.clear;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.transform.position = new Vector3(10000f, 10000f, 10000f);
                camera.transform.rotation = cameraRotation ?? Quaternion.Euler(0f, 180f, 0f);
                camera.fieldOfView = 0.5f;
                camera.farClipPlane = 100000;
                camera.cullingMask = 1 << layer;

                Light sideLight = new GameObject("LightIcon", typeof(Light)).GetComponent<Light>();
                sideLight.transform.position = new Vector3(10000f, 10000f, 10000f);
                sideLight.transform.rotation = Quaternion.Euler(5f, 180f, 5f);
                sideLight.type = LightType.Directional;
                sideLight.cullingMask = 1 << layer;
                sideLight.intensity = lightIntensity;

                GameObject visual = Object.Instantiate(prefab);
                foreach (Transform child in visual.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = layer;
                }

                visual.transform.position = Vector3.zero;
                visual.transform.rotation = Quaternion.Euler(23, 51, 25.8f);
                visual.name = prefab.name;

                MeshRenderer[] renderers = visual.GetComponentsInChildren<MeshRenderer>();
                Vector3 min = renderers.Aggregate(Vector3.positiveInfinity,
                    (cur, renderer) => Vector3.Min(cur, renderer.bounds.min));
                Vector3 max = renderers.Aggregate(Vector3.negativeInfinity,
                    (cur, renderer) => Vector3.Max(cur, renderer.bounds.max));
                // center the prefab
                visual.transform.position = (new Vector3(10000f, 10000f, 10000f)) - (min + max) / 2f;
                Vector3 size = max - min;

                // just in case it doesn't gets deleted properly later
                TimedDestruction timedDestruction = visual.AddComponent<TimedDestruction>();
                timedDestruction.Trigger(1f);
                Rect rect = new(0, 0, 128, 128);
                camera.targetTexture = RenderTexture.GetTemporary((int)rect.width, (int)rect.height);

                camera.fieldOfView = 20f;
                // calculate the Z position of the prefab as it needs to be far away from the camera
                float maxMeshSize = Mathf.Max(size.x, size.y) + 0.1f;
                float distance = maxMeshSize / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad) * 1.1f;

                camera.transform.position = (new Vector3(10000f, 10000f, 10000f)) + new Vector3(0, 0, distance);

                camera.Render();

                RenderTexture currentRenderTexture = RenderTexture.active;
                RenderTexture.active = camera.targetTexture;

                Texture2D previewImage = new((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
                previewImage.ReadPixels(new Rect(0, 0, (int)rect.width, (int)rect.height), 0, 0);
                previewImage.Apply();

                RenderTexture.active = currentRenderTexture;

                prefab.GetComponent<Piece>().m_icon = Sprite.Create(previewImage, new Rect(0, 0, (int)rect.width, (int)rect.height), Vector2.one / 2f);
                sideLight.gameObject.SetActive(false);
                camera.targetTexture.Release();
                camera.gameObject.SetActive(false);
                visual.SetActive(false);
                Object.DestroyImmediate(visual);

                Object.Destroy(camera);
                Object.Destroy(sideLight);
                Object.Destroy(camera.gameObject);
                Object.Destroy(sideLight.gameObject);
            }
            IEnumerator Delay()
            {
                yield return null;
                Do();
            }
            IEnumerator WackyDelay()
            {
                yield return 1f;
                Do();
            }
            if (ObjectDB.instance)
            {
                WMRecipeCust.context.StartCoroutine(WackyDelay());
                // Do();
            }
            else
            {
                plugin.StartCoroutine(Delay());
            }
        }


        public static void SnapshotItem(ItemDrop item, float lightIntensity = 1.3f, Quaternion? cameraRotation = null, Quaternion? itemRotation = null)
        {
            void Do()
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

                GameObject visual;
                if (item.transform.Find("attach") is { } attach)
                {
                    visual = UnityEngine.Object.Instantiate(attach.gameObject);
                }
                else
                {
                    ZNetView.m_forceDisableInit = true;
                    visual = UnityEngine.Object.Instantiate(item.gameObject);
                    ZNetView.m_forceDisableInit = false;
                }
                if (itemRotation is not null)
                {
                    visual.transform.rotation = itemRotation.Value;
                }

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
                Transform cameraTransform = camera.transform;
                cameraTransform.position = ((min + max) / 2) with { y = max.y } + new Vector3(0, yDist, 0);
                topLight.transform.position = cameraTransform.position + new Vector3(-2, 0, 0.2f) / 3 * -yDist;

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
            IEnumerator Delay()
            {
                yield return null;
                Do();
            }
            IEnumerator WackyDelay()
            {
                yield return 1f;
                Do();
            }
            if (ObjectDB.instance)
            {
                WMRecipeCust.context.StartCoroutine(WackyDelay());
                // Do();
            }
            else
            {            
                plugin.StartCoroutine(Delay());
            }
        }
    }
}
