using System;
using System.Drawing;

namespace Rock.Mobile
{
    namespace PlatformUI
    {
        /// <summary>
        /// The base platformUI that provides an interface to platform specific UI controls. All our platform wrappers derive from this.
        /// </summary>
        public abstract class PlatformBaseUI
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

            public static float UnitToPx( float unit )
            {
                return Android.Util.TypedValue.ApplyDimension(Android.Util.ComplexUnitType.Dip, unit, Rock.Mobile.PlatformCommon.Droid.Context.Resources.DisplayMetrics);
            }
            #endif

            #if __IOS__
            public static MonoTouch.UIKit.UIColor GetUIColor( uint color )
            {
                // break out the colors and convert to 0-1 for iOS
                return new MonoTouch.UIKit.UIColor(
                ( float )( ( color & 0xFF000000 ) >> 24 ) / 255,
                ( float )( ( color & 0x00FF0000 ) >> 16 ) / 255, 
                ( float )( ( color & 0x0000FF00 ) >> 8 ) / 255, 
                ( float )( ( color & 0x000000FF ) ) / 255 );
            }

            public static float UnitToPx( float unit )
            {
                return unit;
            }
            #endif

            // Properties
            public uint BackgroundColor
            {
                set { setBackgroundColor( value ); }
            }
            protected abstract void setBackgroundColor( uint backgroundColor );

            public uint BorderColor
            {
                set { setBorderColor( value ); }
            }
            protected abstract void setBorderColor( uint borderColor );

            public float BorderWidth
            {
                get { return getBorderWidth( ); }
                set { setBorderWidth( value ); }
            }
            protected abstract float getBorderWidth( );
            protected abstract void setBorderWidth( float width );

            public float CornerRadius
            {
                get { return getCornerRadius( ); }
                set { setCornerRadius( value ); }
            }
            protected abstract float getCornerRadius( );
            protected abstract void setCornerRadius( float width );

            public float Opacity
            {
                get { return getOpacity( ); }
                set { setOpacity( value ); }
            }
            protected abstract float getOpacity( );
            protected abstract void setOpacity( float opacity );

            public float ZPosition
            {
                get { return getZPosition( ); }
                set { setZPosition( value ); }
            }
            protected abstract float getZPosition( );
            protected abstract void setZPosition( float zPosition );

            public RectangleF Bounds
            {
                get { return getBounds( ); }
                set { setBounds( value ); }
            }
            protected abstract RectangleF getBounds( );
            protected abstract void setBounds( RectangleF bounds );

            public RectangleF Frame
            {
                get { return getFrame( ); }
                set { setFrame( value ); }
            }
            protected abstract RectangleF getFrame( );
            protected abstract void setFrame( RectangleF frame );

            public PointF Position
            {
                get { return getPosition( ); }
                set { setPosition( value ); }
            }
            protected abstract PointF getPosition( );
            protected abstract void setPosition( PointF position );

            public bool Hidden
            {
                get { return getHidden( ); }
                set { setHidden( value ); }
            }
            protected abstract bool getHidden( );
            protected abstract void setHidden( bool hidden );

            public abstract void AddAsSubview( object masterView );

            public abstract void RemoveAsSubview( object masterView );
        }
    }
}

