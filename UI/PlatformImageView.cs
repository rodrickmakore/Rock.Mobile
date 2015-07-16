using System;
using System.IO;

namespace Rock.Mobile
{
    namespace UI
    {
        /// <summary>
        /// The base Platform View that provides an interface to platform specific views.
        /// </summary>
        public abstract class PlatformImageView : PlatformBaseUI
        {
            public enum ScaleType
            {
                Center,
                ScaleAspectFill,
                ScaleAspectFit
            }

            /// <summary>
            /// Creates an image. If scaleForDPI is true, it will assume the image is high DPI and downsize it,
            /// so expect it to be half the normal size.
            /// </summary>
            /// <param name="scaleForDPI">If set to <c>true</c> scale for DP.</param>
            public static PlatformImageView Create( bool scaleForDPI )
            {
                #if __IOS__
                return new iOSImageView( scaleForDPI );
                #endif

                #if __ANDROID__
                return new DroidImageView( scaleForDPI );
                #endif
            }

            /// <summary>
            /// If we want to use UI broadly, one concession
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

            public float CornerRadius
            {
                get { return getCornerRadius( ); }
                set { setCornerRadius( value ); }
            }
            protected abstract float getCornerRadius( );
            protected abstract void setCornerRadius( float width );

            public bool ScaleForDPI
            {
                get { return getScaleForDPI( ); }
            }
            protected abstract bool getScaleForDPI( );

            public MemoryStream Image
            {
                set { setImage( value ); }
            }
            protected abstract void setImage( MemoryStream image );

            public ScaleType ImageScaleType
            {
                set { setImageScaleType( value ); }
            }
            protected abstract void setImageScaleType( ScaleType scaleType );

            public abstract void SizeToFit( );

            public abstract void Destroy( );
        }
    }
}

