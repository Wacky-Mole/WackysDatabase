using VisualsModifier.Effects;
using System.Collections.Generic;
using UnityEngine;

namespace VisualsModifier
{
    public class ItemSet
    {
        public string Name { get; }

        public string Description { get; }
        public int Size { get; }

        public List<ItemDrop> SetItems { get; }
        
        public SE_Stats Stats { get; }

        public ItemSet(string name, int size, string description, List<ISetEffect> effects)
        {
            SE_Stats stats = ScriptableObject.CreateInstance<SE_Stats>();
            stats.name = name;
            stats.m_tooltip = this.Description;

            // TODO Set Icon 
            // stats.m_icon 

            // Apply all of the effect types to the item set
            effects.ForEach((effect => { effect.Apply(ref stats); }));

            this.Name = name;
            this.Size = size;
            this.Description = description;
            this.Stats = stats;
            this.SetItems = new List<ItemDrop>(size);
        }

        /// <summary>
        /// Adds an item to the set
        /// </summary>
        /// <param name="item">The item to add</param>
        public void AddItem(ItemDrop item)
        {
            item.m_itemData.m_shared.m_setStatusEffect = Stats;
            item.m_itemData.m_shared.m_setName = this.Name;
            item.m_itemData.m_shared.m_setSize = this.Size;

            this.SetItems.Add(item);
        }

        /// <summary>
        /// Applies the item set entry to the Object DB
        /// </summary>
        /// <param name="odb">The Object DB</param>
        public void Apply(ObjectDB odb)
        {
            if (odb.m_StatusEffects.Contains(this.Stats))
            {
                return;
            }

            odb.m_StatusEffects.Add(Stats);
        }
    }
}
