using System;
using System.Drawing;

namespace Rock.Mobile
{
    namespace PlatformUI
    {
        /// <summary>
        /// The base Platform View that provides an interface to platform specific views.
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

