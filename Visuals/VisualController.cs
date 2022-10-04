using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BepInEx;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using wackydatabase.Datas;

//Hierarchy
// Player
//  - body
//  - Armature
//  - attach_skin [Equipment components] (any order)

namespace wackydatabase
{
    public static class VisualController
    {
        public static Dictionary<string, int> _visualsByName = new Dictionary<string, int>();
        public static Dictionary<int, int> _visualsByHash = new Dictionary<int, int>();
        public static List<VisualData> _visuals = new List<VisualData>();

        private static ISerializer _serializer;
        private static IDeserializer _deserializer;

        static VisualController()
        {
            ColorConverter cc = new ColorConverter();
            ValheimTimeConverter vtc = new ValheimTimeConverter();

            _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(cc)
            .WithTypeConverter(vtc)
            .Build();

            _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(cc)
            .WithTypeConverter(vtc)
            .Build();
        }

        public static void Add(string prefabName, VisualData data)
        {
            int hash = prefabName.GetStableHashCode();

            if (_visualsByName.ContainsKey(prefabName))
            {
                Debug.Log(string.Format("[VisualsModifer]: Updating {0}", prefabName, hash.ToString()));
                int index = GetVisualIndex(prefabName);

                _visuals[index] = data;
                _visualsByHash[hash] = index;
                _visualsByName[prefabName] = index;
            }
            else
            {
                Debug.Log(string.Format("[VisualsModifer]: Adding {0}", prefabName, hash.ToString()));

                _visuals.Add(data);
                _visualsByHash.Add(hash, _visuals.Count - 1);
                _visualsByName.Add(prefabName, _visuals.Count - 1);
            }
        }

        public static void Import(string file)
        {
            try
            {
                VisualData data = _deserializer.Deserialize<VisualData>(File.ReadAllText(file));

                Add(data.PrefabName, data);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                Debug.Log(ex.InnerException);
            }
        }

        public static void Export(VisualData visual)
        {
            string contents = _serializer.Serialize(visual);
            string storage = Path.Combine(Paths.ConfigPath, "Visuals");

            if (!Directory.Exists(storage))
            {
                Directory.CreateDirectory(storage);
            }

            File.WriteAllText(Path.Combine(storage, "Visual_" + visual.PrefabName + ".yml"), contents);
        }

        public static void UpdateVisuals(string prefabName, ObjectDB instance)
        {
            GameObject armor = instance.GetItemPrefab(prefabName);

            if (armor == null)
            {
                Debug.Log("Failed to find armor");
                return;
            }

            Transform skin = armor.transform.Find("attach_skin") ?? armor.transform.Find("attach");

            if (skin == null)
            {
                Debug.Log("Failed to find skin");
                return;
            }

            Renderer[] renderers = skin.GetComponentsInChildren<Renderer>();

            try
            {
                //Transform material = armor.transform.Find("default") ?? armor.transform;
                //Material m = material.GetComponentInChildren<Renderer>().sharedMaterial;

                List<IManipulator> materialChanges = VisualController.GetManipulations(prefabName);
                List<IManipulator> particleChanges = VisualController.GetParticleChanges(prefabName);

                for (uint i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i].GetType() == typeof(ParticleSystemRenderer))
                    {
                        particleChanges.ForEach(change => { change.Invoke(renderers[i], armor); });
                    }
                    else
                    {
                        materialChanges.ForEach(change => { change.Invoke(renderers[i], armor); });
                    }
                }


                // This light stuff should be moved into a manipulation
                VisualData data = VisualController.GetVisualByName(prefabName);

                if (data.Light != null)
                {
                    Light l = skin.GetComponentInChildren<Light>(true);
                    if (l != null)
                    {
                        if (data.Light.Color != null)
                        {
                            l.color = data.Light.Color;
                        }

                        if (data.Light.Range != null)
                        {
                            l.range = data.Light.Range;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(string.Format("VisualsModifier: Failed to update material - {0}", ex.Message));
                Debug.Log(ex.InnerException);
            }
        }

        public static int GetVisualIndex(string name)
        {
            return _visualsByName[name];
        }

        public static int GetVisualIndex(int hash)
        {
            return _visualsByHash[hash];
        }

        public static VisualData GetVisualByIndex(int index)
        {
            return _visuals[index];
        }

        public static VisualData GetVisualByName(string name)
        {
            if (_visualsByName.ContainsKey(name))
            {
                return _visuals[_visualsByName[name]];
            }

            return null;
        }

        public static VisualData GetVisualByHash(int hash)
        {
            if (_visualsByHash.ContainsKey(hash))
            {
                return _visuals[_visualsByHash[hash]];

            }
            return null;
        }

        public static List<IManipulator> GetManipulations(string prefab)
        {
            List<IManipulator> manipulations = new List<IManipulator>();
            VisualData data = GetVisualByName(prefab);

            if (data == null)
            {
                return manipulations;
            }

            if (data.Material != null)
            {
                manipulations.Add(new MaterialManipulator(data.Material));
            }

            if (data.Effect != null)
            {
                manipulations.Add(new RealtimeManipulator(data.Effect));
            }

            if (data.Shader != null)
            {
                manipulations.Add(new RendererManipulator(data.Shader));
            }

            return manipulations;
        }

        public static List<IManipulator> GetParticleChanges(string prefab)
        {
            List<IManipulator> manipulations = new List<IManipulator>();
            VisualData data = GetVisualByName(prefab);

            if (data.Particle != null)
            {
                manipulations.Add(new MaterialManipulator(data.Particle));
            }

            return manipulations;
        }
    }
}
