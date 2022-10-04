using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace VisualsModifier.Effects
{
    public interface ISetEffect
    {
        public abstract void Apply(ref SE_Stats stats);
    }
}
