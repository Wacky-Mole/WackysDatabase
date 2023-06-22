using System;
using System.Collections.Generic;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase
{
    public class VisualDataManager : DataManager<VisualData>
    {
        public static VisualDataManager Instance = new VisualDataManager();

        public Dictionary<string, int> _visualsByName = new Dictionary<string, int>();
        public List<VisualData> _visuals = new List<VisualData>();

        public event EventHandler<DataEventArgs<VisualData>> OnVisualChanged;

        public VisualDataManager() : base(WMRecipeCust.assetPathVisuals, "Visual") { }

        public override void Cache(VisualData item)
        {
            if (!_visualsByName.ContainsKey(item.PrefabName))
            {
                _visuals.Add(item);
                _visualsByName.Add(item.PrefabName, _visuals.Count - 1);
            } else
            {
                _visuals[_visualsByName[item.PrefabName]] = item;
            }

            OnVisualChanged.Invoke(this, new DataEventArgs<VisualData>(item));
        }

        public VisualData GetVisualByName(string name)
        {
            if (_visualsByName.ContainsKey(name))
            {
                return _visuals[_visualsByName[name]];
            }

            return null;
        }
    }
}
