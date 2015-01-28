using System;

namespace Rock.Mobile
{
    namespace PlatformUI
    {

        /// <summary>
        /// Provides abstract interface for commonly used LABEL methods (Text, Font, etc)
        /// </summary>
        public abstract class PlatformBaseLabelUI : PlatformBaseUI
        {
            public abstract void SetFont( string fontName, float fontSize );

            public uint TextColor
            {
                //get { return getTextColor(); }
                set { setTextColor( value ); }
            }
            //protected abstract uint getTextColor();
            protected abstract void setTextColor( uint color );

            public string Text
            {
                get { return getText( ); }
                set { setText( value ); }
            }

            protected abstract string getText( );

            protected abstract void setText( string text );

            public TextAlignment TextAlignment
            {
                get { return getTextAlignment( ); }
                set { setTextAlignment( value ); }
            }

            protected abstract TextAlignment getTextAlignment( );

            protected abstract void setTextAlignment( TextAlignment alignment );

            public abstract void SizeToFit( );
        }

        /// <summary>
        /// The base Platform Label that provides an interface to platform specific text labels.
        /// </summary>
        public abstract class PlatformLabel : PlatformBaseLabelUI
        {
            public static PlatformLabel CreateRevealLabel( )
            {
                #if __IOS__
                return new iOSRevealLabel( );
                #endif

                #if __ANDROID__
                return new DroidRevealLabel( );
                #endif
            }

            public static PlatformLabel Create( )
            {
                #if __IOS__
                return new iOSLabel( );
                #endif

                #if __ANDROID__
                return new DroidLabel( );
                #endif
            }

            public abstract float GetFade();
            public abstract void SetFade( float fadeAmount );
            public abstract void AnimateToFade( float fadeAmount );
            public abstract void AddUnderline( );
        }
    }
}

