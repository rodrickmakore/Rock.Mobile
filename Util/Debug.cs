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
    }
}

