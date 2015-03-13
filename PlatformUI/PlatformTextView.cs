using System;

namespace Rock.Mobile
{
    namespace PlatformUI
    {
        // put common utility things here (enums, etc)
        public enum TextAlignment
        {
            Left,
            Center,
            Right,
            Justified,
            Natural
        }

        /// <summary>
        /// The base text field that provides an interface to platform specific text fields.
        /// </summary>
        public abstract class PlatformTextView : PlatformBaseLabelUI
        {
            public static PlatformTextView Create( )
            {
                #if __IOS__
                return new iOSTextView( );
                #endif

                #if __ANDROID__
                return new DroidTextView( );
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

            public bool ScaleHeightForText
            {
                get { return getScaleHeightForText( ); }
                set { setScaleHeightForText( value ); }
            }
            protected abstract bool getScaleHeightForText( );
            protected abstract void setScaleHeightForText( bool scale );

            public float DynamicTextMaxHeight
            {
                get { return getDynamicTextMaxHeight( ); }
                set { setDynamicTextMaxHeight( value ); }
            }
            protected abstract float getDynamicTextMaxHeight( );
            protected abstract void setDynamicTextMaxHeight( float height );

            public int KeyboardAppearance
            {
                get { return getKeyboardAppearance( ); }
                set { setKeyboardAppearance( value ); }
            }
            protected abstract int getKeyboardAppearance( );
            protected abstract void setKeyboardAppearance( int style );

            public abstract void ResignFirstResponder( );

            public abstract void AnimateOpen( );
            public abstract void AnimateClosed( );
        }
    }
}
