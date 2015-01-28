using System;

namespace Rock.Mobile.PlatformUI
{
    public class Util
    {
        #if __ANDROID__
        public static Android.Graphics.Color GetUIColor( uint color )
        {
            // break out the colors as 255 components for android
            return new Android.Graphics.Color(
                ( byte )( ( color & 0xFF000000 ) >> 24 ),
                ( byte )( ( color & 0x00FF0000 ) >> 16 ), 
                ( byte )( ( color & 0x0000FF00 ) >> 8 ), 
                ( byte )( ( color & 0x000000FF ) ) );
        }
        #endif

        #if __IOS__
        public static UIKit.UIColor GetUIColor( uint color )
        {
            // break out the colors and convert to 0-1 for iOS
            return new UIKit.UIColor(
            ( float )( ( color & 0xFF000000 ) >> 24 ) / 255,
            ( float )( ( color & 0x00FF0000 ) >> 16 ) / 255, 
            ( float )( ( color & 0x0000FF00 ) >> 8 ) / 255, 
            ( float )( ( color & 0x000000FF ) ) / 255 );
        }
        #endif
    }
}

