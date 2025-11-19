using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Comic_Manager
{
    // 实现 IComparer 接口，用于 List.Sort()
    public class NaturalStringComparer : IComparer<string>
    {
        // 调用 Windows 核心库的字符串比较函数
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string psz1, string psz2);

        public int Compare(string x, string y)
        {
            return StrCmpLogicalW(x, y);
        }
    }
}