#if __IOS__
using System;
using System.Drawing;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;
using MonoTouch.CoreText;
using Rock.Mobile.PlatformCommon;
using Rock.Mobile.PlatformUI.iOSNative;

namespace Rock.Mobile
{
    namespace PlatformUI
    {
        /// <summary>
        /// The iOS implementation of a text field.
        /// </summary>
        public class iOSTextField : PlatformTextField
        {
            DynamicUITextView TextField { get; set; }

            public iOSTextField( )
            {
                TextField = new DynamicUITextView( );
                TextField.Layer.AnchorPoint = new PointF( 0, 0 );
                TextField.TextAlignment = UITextAlignment.Left;

                TextField.Editable = true;
                TextField.ClipsToBounds = true;
            }

            // Properties
            public override void SetFont( string fontName, float fontSize )
            {
                try
                {
                    TextField.Font = iOSCommon.LoadFontDynamic(fontName, fontSize);
                } 
                catch
                {
                    throw new Exception( string.Format( "Failed to load font: {0}", fontName ) );
                }
            }

            protected override void setBorderColor( uint borderColor )
            {
                TextField.Layer.BorderColor = GetUIColor( borderColor ).CGColor;
            }

            protected override float getBorderWidth()
            {
                return TextField.Layer.BorderWidth;
            }
            protected override void setBorderWidth( float width )
            {
                TextField.Layer.BorderWidth = width;
            }

            protected override float getCornerRadius()
            {
                return TextField.Layer.CornerRadius;
            }
            protected override void setCornerRadius( float radius )
            {
                TextField.Layer.CornerRadius = radius;
            }

            protected override void setBackgroundColor( uint backgroundColor )
            {
                TextField.Layer.BackgroundColor = GetUIColor( backgroundColor ).CGColor;
            }

            protected override float getOpacity( )
            {
                return TextField.Opacity;
            }

            protected override void setOpacity( float opacity )
            {
                TextField.Opacity = opacity;
            }

            protected override float getZPosition( )
            {
                return TextField.ZPosition;
            }

            protected override void setZPosition( float zPosition )
            {
                TextField.ZPosition = zPosition;
            }

            protected override RectangleF getBounds( )
            {
                return TextField.Bounds;
            }

            protected override void setBounds( RectangleF bounds )
            {
                TextField.Bounds = bounds;
            }

            protected override RectangleF getFrame( )
            {
                return TextField.Frame;
            }

            protected override void setFrame( RectangleF frame )
            {
                TextField.Frame = frame;
            }

            protected override  PointF getPosition( )
            {
                return TextField.Position;
            }

            protected override void setPosition( PointF position )
            {
                TextField.Position = position;
            }

            protected override bool getHidden( )
            {
                return TextField.Hidden;
            }

            protected override void setHidden( bool hidden )
            {
                TextField.Hidden = hidden;
            }

            protected override void setTextColor( uint color )
            {
                TextField.TextColor = GetUIColor( color );
            }

            protected override void setPlaceholderTextColor( uint color )
            {
                TextField.PlaceholderTextColor = GetUIColor( color );
            }

            protected override string getText( )
            {
                return TextField.Text;
            }

            protected override void setText( string text )
            {
                TextField.Text = text;
            }

            protected override TextAlignment getTextAlignment( )
            {
                return ( TextAlignment )TextField.TextAlignment;
            }

            protected override void setTextAlignment( TextAlignment alignment )
            {
                TextField.TextAlignment = ( UITextAlignment )alignment;
            }

            protected override string getPlaceholder( )
            {
                return TextField.Placeholder;
            }

            protected override void setPlaceholder( string placeholder )
            {
                TextField.Placeholder = placeholder;
            }

            protected override bool getScaleHeightForText( )
            {
                return TextField.ScaleHeightForText;
            }

            protected override void setScaleHeightForText( bool scale )
            {
                TextField.ScaleHeightForText = scale;
            }

            public override void ResignFirstResponder( )
            {
                TextField.ResignFirstResponder( );
            }

            protected override void setDynamicTextMaxHeight( float height )
            {
                TextField.DynamicTextMaxHeight = height;
            }

            protected override float getDynamicTextMaxHeight( )
            {
                return TextField.DynamicTextMaxHeight;
            }

            public override void AddAsSubview( object masterView )
            {
                // we know that masterView will be an iOS View.
                UIView view = masterView as UIView;
                if( view == null )
                {
                    throw new Exception( "Object passed to iOS AddAsSubview must be a UIView." );
                }

                TextField.AddAsSubview( view );
            }

            public override void RemoveAsSubview( object obj )
            {
                // Obj is only needed by Android, so we ignore it
                TextField.RemoveFromSuperview( );
            }

            public override void SizeToFit( )
            {
                TextField.SizeToFit( );
            }
        }
    }
}
#endif
