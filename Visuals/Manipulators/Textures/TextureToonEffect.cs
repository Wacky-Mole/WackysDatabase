using UnityEngine;

namespace wackydatabase
{
    public class TextureToonEffect : IMaterialEffect
    {
        public Color[] Value { get; set; }
        public string Name { get; set; }

        public TextureToonEffect(string name, Color[] value)
        {
            Name = name;
            Value = value;
        }

        public void Apply(Material m)
        {
            Texture2D tex = (Texture2D)m.GetTexture(this.Name);

            if (tex != null)
            {
                if (this.Value.Length >= 3)
                {
                    Debug.Log(string.Format("Updating Texture {0}"));

                    m.SetTexture(this.Name, Colour.AsCell(tex, this.Value[0], this.Value[1], this.Value[2]));
                }
            }
            else
            {
                Debug.Log(string.Format("Failed to update texture {0}", this.Name));
            }
        }

        public void Apply(MaterialPropertyBlock m)
        {
            Texture2D tex = (Texture2D)m.GetTexture(this.Name);

            if (tex != null)
            {
                Debug.Log(string.Format("Updating Texture {0} - {1}", this.Name, this.Value));

                m.SetTexture(this.Name, Colour.AsCell(tex, this.Value[0], this.Value[1], this.Value[2]));
            }
            else
            {
                Debug.Log(string.Format("Failed to update texture {0}", this.Name));
            }
        }
    }
}
