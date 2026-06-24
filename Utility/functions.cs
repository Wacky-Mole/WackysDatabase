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
using YamlDotNet.Serialization.ObjectGraphVisitors;

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
        private static readonly Vector3 PieceSnapshotOrigin = new Vector3(10000f, 10000f, 10000f);

        private static void PrepareSnapshotClone(GameObject visual, int layer)
        {
            foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }

            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (Rigidbody rigidbody in visual.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.detectCollisions = false;
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }

            foreach (Joint joint in visual.GetComponentsInChildren<Joint>(true))
            {
                Object.DestroyImmediate(joint);
            }

            foreach (Behaviour behaviour in visual.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour is Renderer)
                    continue;

                Object.DestroyImmediate(behaviour);
            }
        }

        private static bool TryGetRenderBounds(GameObject visual, out Bounds bounds)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            Renderer firstRenderer = renderers.FirstOrDefault(renderer => renderer != null && renderer.enabled);

            if (firstRenderer == null)
            {
                bounds = default;
                return false;
            }

            bounds = firstRenderer.bounds;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                bounds.Encapsulate(renderer.bounds);
            }

            return true;
        }

        internal static FieldInfo CompField(Type ClassTyp, string fieldname)
        {
            // Look for public or non-public instance fields
            FieldInfo hello = ClassTyp.GetField(fieldname, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return hello; // may be null
        }

        internal static T getCast<T>(Type ClassTyp, string fieldname, StatusEffect effect)
        {
            var hello = CompField(ClassTyp, fieldname);

            if (hello == null)
            {
                return default(T); // return the default value for type T
            }

            // Ensure the type of the field is assignable to T
            if (typeof(T).IsAssignableFrom(hello.FieldType))
            {
                return (T)hello.GetValue(effect);
            }
            else
            {
                throw new InvalidCastException($"Cannot cast {hello.FieldType} to {typeof(T)}.");
            }
        }

        internal static void setValue(Type type, object go, string name, float? value = null, int? value2 = null, string? value3 = null, List<HitData.DamageModPair>? value4 = null, Skills.SkillType? value5 = null, Vector3? value6 = null, HitData.DamageTypes? value7 = null)
        {
            var field = Functions.CompField(type, name);
            if (field == null)
                return;

            // choose the first non-null input
            object? raw = null;
            if (value != null) raw = value.Value;
            else if (value2 != null) raw = value2.Value;
            else if (value3 != null) raw = value3;
            else if (value4 != null) raw = value4;
            else if (value5 != null) raw = value5.Value;
            else if (value6 != null) raw = value6.Value;
            else if (value7 != null) raw = value7.Value;

            if (raw == null)
                return;

            try
            {
                // determine target type (handle Nullable<T>)
                Type targetType = field.FieldType;
                Type nonNullableTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;

                // If raw is already assignable, set directly
                if (nonNullableTarget.IsInstanceOfType(raw))
                {
                    field.SetValue(go, raw);
                    return;
                }

                object? converted = null;

                // handle enums from string or numeric
                if (nonNullableTarget.IsEnum)
                {
                    if (raw is string rs)
                    {
                        converted = Enum.Parse(nonNullableTarget, rs);
                    }
                    else
                    {
                        converted = Enum.ToObject(nonNullableTarget, raw);
                    }
                }
                else if (raw is IConvertible)
                {
                    // convert primitives and common value types
                    converted = Convert.ChangeType(raw, nonNullableTarget, System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (nonNullableTarget == typeof(Vector3) && raw is Vector3)
                {
                    converted = raw;
                }
                else if (nonNullableTarget.IsAssignableFrom(raw.GetType()))
                {
                    converted = raw;
                }

                if (converted != null)
                {
                    field.SetValue(go, converted);
                }
                else
                {
                    // last resort: attempt direct set (may throw)
                    field.SetValue(go, raw);
                }
            }
            catch (Exception ex)
            {
                WMRecipeCust.WLog.LogDebug($"Reflection setValue failed for field '{name}' on '{type.FullName}': {ex.Message}");
            }
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
            HashSet<string> added = new HashSet<string>();

            foreach (GameObject val2 in array4)
            {
                if (added.Contains(val2.name)) continue;

                if (val2.GetComponent<ItemDrop>() != null || val2.GetComponent<Piece>() != null || val2.GetComponent<Character>() != null)
                    continue;

                if (val2.name.ToLower().StartsWith("vfx") || val2.GetComponentInChildren<ParticleSystem>() != null)
                {
                    WMRecipeCust.originalVFX[val2.name] = val2;
                    TheString = TheString + val2.name + System.Environment.NewLine;
                    added.Add(val2.name);
                }
            }
            return TheString;
        }

        public static string GetAllSFXFile()
        {
            string TheString = "";
            GameObject[] array4 = Resources.FindObjectsOfTypeAll<GameObject>();
            WMRecipeCust.originalSFX = new Dictionary<string, GameObject>();
            HashSet<string> added = new HashSet<string>();

            foreach (GameObject val2 in array4)
            {
                if (added.Contains(val2.name)) continue;

                if (val2.GetComponent<ItemDrop>() != null || val2.GetComponent<Piece>() != null || val2.GetComponent<Character>() != null)
                    continue;

                if (val2.name.ToLower().StartsWith("sfx") || val2.GetComponentsInChildren<Component>().Any(c => c != null && c.GetType().Name == "AudioSource"))
                {
                    WMRecipeCust.originalSFX[val2.name] = val2;
                    TheString = TheString + val2.name + System.Environment.NewLine;
                    added.Add(val2.name);
                }
            }
            return TheString;
        }

        public static string GetAllFXFile()
        {
            string TheString = "";
            GameObject[] array4 = Resources.FindObjectsOfTypeAll<GameObject>();
            WMRecipeCust.originalFX = new Dictionary<string, GameObject>();
            HashSet<string> added = new HashSet<string>();

            foreach (GameObject val2 in array4)
            {
                if (added.Contains(val2.name)) continue;

                if (val2.GetComponent<ItemDrop>() != null || val2.GetComponent<Piece>() != null || val2.GetComponent<Character>() != null)
                    continue;

                if (val2.name.ToLower().StartsWith("fx_") || (val2.GetComponentInChildren<ParticleSystem>() != null && val2.GetComponentsInChildren<Component>().Any(c => c != null && c.GetType().Name == "AudioSource")))
                {
                    WMRecipeCust.originalFX[val2.name] = val2;
                    TheString = TheString + val2.name + System.Environment.NewLine;
                    added.Add(val2.name);
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
            
            void Do2()
            {
                const int layer = 3;
                if (prefab == null) return;
                Piece prefabPiece = prefab.GetComponent<Piece>();
                if (prefabPiece == null)
                {
                    return;
                }

                if (!prefab.GetComponentsInChildren<Renderer>(true).Any() && !prefab.GetComponentsInChildren<MeshFilter>(true).Any())
                {
                    return;
                }

                Camera camera = new GameObject("CameraIcon", typeof(Camera)).GetComponent<Camera>();
                camera.backgroundColor = Color.clear;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.transform.position = PieceSnapshotOrigin;
                camera.transform.rotation = cameraRotation ?? Quaternion.Euler(0f, 180f, 0f);
                camera.fieldOfView = 0.5f;
                camera.farClipPlane = 100000;
                camera.cullingMask = 1 << layer;

                Light sideLight = new GameObject("LightIcon", typeof(Light)).GetComponent<Light>();
                sideLight.transform.position = PieceSnapshotOrigin;
                sideLight.transform.rotation = Quaternion.Euler(5f, 180f, 5f);
                sideLight.type = LightType.Directional;
                sideLight.cullingMask = 1 << layer;
                sideLight.intensity = lightIntensity;

                GameObject visual = null;
                RenderTexture pieceTexture = null;
                try
                {
                    ZNetView.m_forceDisableInit = true;
                    visual = Object.Instantiate(prefab.gameObject);
                    ZNetView.m_forceDisableInit = false;

                    visual.SetActive(false);
                    visual.transform.position = PieceSnapshotOrigin;
                    visual.transform.rotation = Quaternion.Euler(23, 51, 25.8f);

                    PrepareSnapshotClone(visual, layer);

                    visual.SetActive(true);

                    if (!TryGetRenderBounds(visual, out Bounds bounds))
                    {
                        return;
                    }

                    visual.transform.position += PieceSnapshotOrigin - bounds.center;

                    Vector3 size = bounds.size;
                    Rect rect = new(0, 0, 128, 128);
                    pieceTexture = RenderTexture.GetTemporary((int)rect.width, (int)rect.height);
                    camera.targetTexture = pieceTexture;

                    camera.fieldOfView = 20f;
                    float maxMeshSize = Mathf.Max(size.x, size.y, size.z) + 0.1f;
                    float distance = maxMeshSize / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 1.1f;

                    camera.transform.position = PieceSnapshotOrigin + new Vector3(0, 0, distance);

                    camera.Render();

                    RenderTexture currentRenderTexture = RenderTexture.active;
                    RenderTexture.active = camera.targetTexture;

                    Texture2D previewImage = new((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
                    previewImage.ReadPixels(new Rect(0, 0, (int)rect.width, (int)rect.height), 0, 0);
                    previewImage.Apply();

                    RenderTexture.active = currentRenderTexture;

                    prefabPiece.m_icon = Sprite.Create(previewImage, new Rect(0, 0, (int)rect.width, (int)rect.height), Vector2.one / 2f);
                }
                finally
                {
                    ZNetView.m_forceDisableInit = false;

                    if (camera != null)
                    {
                        if (camera.targetTexture != null)
                        {
                            camera.targetTexture = null;
                        }

                        Object.Destroy(camera.gameObject);
                    }

                    if (pieceTexture != null)
                    {
                        RenderTexture.ReleaseTemporary(pieceTexture);
                    }

                    if (sideLight != null)
                    {
                        Object.Destroy(sideLight.gameObject);
                    }

                    if (visual != null)
                    {
                        Object.Destroy(visual);
                    }
                }
            }
            IEnumerator Delay2()
            {
                yield return null;
                Do2();
            }
            IEnumerator WackyDelay()
            {
                yield return 20f;
                Do2();
            }
            if (ObjectDB.instance)
            {
                Do2();
                // plugin.StartCoroutine(Delay2());
                // WMRecipeCust.context.StartCoroutine(WackyDelay());
                // Do();
            }
            else
            {
               // WMRecipeCust.context.StartCoroutine(WackyDelay());
                //plugin.StartCoroutine(Delay2());
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
