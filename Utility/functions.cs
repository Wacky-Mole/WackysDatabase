using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public class functions
    {


    }
}
