using System;

#if __IOS__
using MonoTouch.Foundation;

namespace RockMobile
{
    namespace Threading
    {
        public class UIThreading
        {
            public delegate void ThreadTask( );

            public static void PerformOnUIThread( ThreadTask task )
            {
                new NSObject().InvokeOnMainThread( new NSAction( task ) );
            }
        }
    }
}
#endif

#if __ANDROID__
using Android.App;

namespace RockMobile
{
    namespace Threading
    {
        public class UIThreading
        {
            public delegate void ThreadTask( );

            public static void PerformOnUIThread( ThreadTask task )
            {
                ((Activity)RockMobile.PlatformCommon.Droid.Context).RunOnUiThread( new System.Action( task ) );
            }
        }
    }
}
#endif
