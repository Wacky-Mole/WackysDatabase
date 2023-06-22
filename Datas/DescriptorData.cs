using System;
using System.Collections.Generic;

namespace wackydatabase.Datas
{
    [Serializable]
    public class DescriptorData
    {
        public string Name;
        public List<RendererDescriptor> Renderers = new();
    }

    public class MaterialPropertyDescriptor
    {
        public string Name;
        public string Type;
        public string Value;
        public string Range;
    }

    public class MaterialDescriptor
    {
        public string Name;
        public string Shader;
        public List<MaterialPropertyDescriptor> MaterialProperties = new();
    }

    public class RendererDescriptor
    {
        public string Name;
        public List<MaterialDescriptor> Materials = new();
    }
}
