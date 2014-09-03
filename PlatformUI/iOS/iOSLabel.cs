#if __IOS__
using System;
using System.Drawing;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;
using MonoTouch.CoreText;

namespace Rock.Mobile
{
    namespace PlatformUI
    {
        /// <summary>
        /// The iOS implementation of a text label.
        /// </summary>
        public class iOSLabel : PlatformLabel
        {
            protected UILabel Label { get; set; }

            public iOSLabel( )
            {
                Label = new UILabel( );
                Label.Layer.AnchorPoint = new PointF( 0, 0 );
                Label.TextAlignment = UITextAlignment.Left;
                Label.LineBreakMode = UILineBreakMode.WordWrap;
                Label.Lines = 0;
            }

            // Properties
            public override void SetFont( string fontName, float fontSize )
            {
                try
                {
                    Label.Font = PlatformCommon.iOS.LoadFontDynamic(fontName, fontSize);
                } 
                catch
                {
                    throw new Exception( string.Format( "Failed to load font: {0}", fontName ) );
                }
            }

            protected override void setBackgroundColor( uint backgroundColor )
            {
                Label.Layer.BackgroundColor = GetUIColor( backgroundColor ).CGColor;
            }

            protected override float getOpacity( )
            {
                return Label.Layer.Opacity;
            }

            protected override void setOpacity( float opacity )
            {
                Label.Layer.Opacity = opacity;
            }

            protected override float getZPosition( )
            {
                return Label.Layer.ZPosition;
            }

            protected override void setZPosition( float zPosition )
            {
                Label.Layer.ZPosition = zPosition;
            }

            protected override RectangleF getBounds( )
            {
                return Label.Bounds;
            }

            protected override void setBounds( RectangleF bounds )
            {
                Label.Bounds = bounds;
            }

            protected override RectangleF getFrame( )
            {
                return Label.Frame;
            }

            protected override void setFrame( RectangleF frame )
            {
                Label.Frame = frame;
            }

            protected override  PointF getPosition( )
            {
                return Label.Layer.Position;
            }

            protected override void setPosition( PointF position )
            {
                Label.Layer.Position = position;
            }

            protected override void setTextColor( uint color )
            {
                Label.TextColor = GetUIColor( color );
            }

            protected override string getText( )
            {
                return Label.Text;
            }

            protected override void setText( string text )
            {
                Label.Text = text;
            }

            protected override TextAlignment getTextAlignment( )
            {
                return ( TextAlignment )Label.TextAlignment;
            }

            protected override void setTextAlignment( TextAlignment alignment )
            {
                Label.TextAlignment = ( UITextAlignment )alignment;
            }

            protected override bool getHidden( )
            {
                return Label.Hidden;
            }

            protected override void setHidden( bool hidden )
            {
                Label.Hidden = hidden;
            }

            public override void AddAsSubview( object masterView )
            {
                // we know that masterView will be an iOS View.
                UIView view = masterView as UIView;
                if( view == null )
                {
                    throw new Exception( "Object passed to iOS AddAsSubview must be a UIView." );
                }

                view.AddSubview( Label );
            }

            public override void RemoveAsSubview( object obj )
            {
                //obj is only for Android, so we don't use it.
                Label.RemoveFromSuperview( );
            }

            public override void SizeToFit( )
            {
                Label.SizeToFit( );
            }

            public override float GetFade()
            {
                return 1.00f;
            }

            public override void SetFade( float fadeAmount )
            {
            }

            public override void AnimateToFade( float fadeAmount )
            {
            }
        }
    }
}
#endif
