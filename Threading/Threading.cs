using System;

#if __IOS__
using MonoTouch.Foundation;

namespace Rock.Mobile
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

namespace Rock.Mobile
{
    namespace Threading
    {
        public class UIThreading
        {
            public delegate void ThreadTask( );

            public static void PerformOnUIThread( ThreadTask task )
            {
                ((Activity)Rock.Mobile.PlatformCommon.Droid.Context).RunOnUiThread( new System.Action( task ) );
            }
        }
    }
}
#endif
