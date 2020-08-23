using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obj2Nav2.Nav2
{
    class Utils
    {
        public static uint GetPaddingSize(uint size)
        {
            uint padding = size % 16;
            if (padding != 0)
            {
                padding = 16 - padding;
            }

            return padding;
        }
    }
}
