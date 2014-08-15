using System;
using System.Drawing;

namespace RockMobile
{
    namespace PlatformUI
    {
        /// <summary>
        /// The base Platform Label that provides an interface to platform specific text labels.
        /// </summary>
        public abstract class PlatformView : PlatformBaseUI
        {
            public static PlatformView Create( )
            {
                #if __IOS__
                return new iOSView( );
                #endif

                #if __ANDROID__
                return new DroidView( );
                #endif
            }
        }
    }
}

