using System;

namespace Rock.Mobile
{
    namespace PlatformUI
    {
        /// <summary>
        /// The base text field that provides an interface to platform specific text fields.
        /// Text Fields are subtly different in that they aren't designed to be multi-line.
        /// </summary>
        public abstract class PlatformTextField : PlatformBaseLabelUI
        {
            public static PlatformTextField Create( )
            {
                #if __IOS__
                return new iOSTextField( );
                #endif

                #if __ANDROID__
                return new DroidTextField( );
                #endif
            }

            /// <summary>
            /// If we want to use PlatformUI broadly, one concession
            /// that needs to be made is the ability to get the 
            /// native view so that features that aren't implemented can
            /// be performed in native code.
            /// </summary>
            /// <value>The platform native object.</value>
            public object PlatformNativeObject
            {
                get { return getPlatformNativeObject( ); }
            }
            protected abstract object getPlatformNativeObject( );

            public string Placeholder
            {
                get { return getPlaceholder( ); }
                set { setPlaceholder( value ); }
            }
            protected abstract string getPlaceholder( );
            protected abstract void setPlaceholder( string placeholder );

            public uint PlaceholderTextColor
            {
                set { setPlaceholderTextColor( value ); }
            }
            protected abstract void setPlaceholderTextColor( uint color );

            public int KeyboardAppearance
            {
                get { return getKeyboardAppearance( ); }
                set { setKeyboardAppearance( value ); }
            }
            protected abstract int getKeyboardAppearance( );
            protected abstract void setKeyboardAppearance( int style );

            public abstract void ResignFirstResponder( );
        }
    }
}
