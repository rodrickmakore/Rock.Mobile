#if __ANDROID__
using System;

// This file is where you can put anything SPECIFIC to Android that doesn't 
// require common base classes, and should be DYE-RECTLY referenced by Android code.

namespace RockMobile
{
    namespace PlatformCommon
    {
        public class Droid
        {
            // beeee sure to set this for android!
            public static Android.Content.Context Context = null;
        }
    }
}
#endif
