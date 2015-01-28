using System;

#if __IOS__
using Foundation;

namespace Rock.Mobile.Threading
{
    public class Util
    {
        public delegate void ThreadTask( );

        public static void PerformOnUIThread( ThreadTask task )
        {
            new NSObject().InvokeOnMainThread( new Action( task ) );
        }
    }
}
#endif

#if __ANDROID__
using Android.App;
using Droid;

namespace Rock.Mobile.Threading
{
    public class Util
    {
        public delegate void ThreadTask( );

        public static void PerformOnUIThread( ThreadTask task )
        {
            ((Activity)Rock.Mobile.PlatformSpecific.Android.Core.Context).RunOnUiThread( new System.Action( task ) );
        }
    }
}
#endif
