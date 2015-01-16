using System;
using System.Drawing;

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
