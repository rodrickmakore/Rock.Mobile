using System;

namespace Rock.Mobile.Util
{
    public static class Debug
    {
        public static void WriteLine( string output )
        {
            #if DEBUG
            Console.WriteLine( output );
            #endif
        }

        public static void DisplayError( string errorTitle, string errorMessage )
        {
            Rock.Mobile.Threading.Util.PerformOnUIThread( delegate
                {
                    #if __IOS__
                    UIKit.UIAlertView alert = new UIKit.UIAlertView();
                    alert.Title = errorTitle;
                    alert.Message = errorMessage;
                    alert.AddButton( "Ok" );
                    alert.Show( ); 
                    #elif __ANDROID__
                    #endif
                } );
        }
    }
}

