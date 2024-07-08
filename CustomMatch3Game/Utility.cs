using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomMatch3Game
{
    public static class Utility
    {
        public static void Swap<T>(ref T l, ref T r)
        {
            T temp = l;
            l = r;
            r = temp;
        }
    }
}
