#if __IOS__
using System;
using System.Drawing;
using UIKit;
using Foundation;
using CoreGraphics;
using CoreText;
using Rock.Mobile.PlatformUI.iOSNative;
using Rock.Mobile.PlatformSpecific.Util;
using Rock.Mobile.Animation;

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

            /// <summary>
            /// The time to animate the text box as it grows
            /// </summary>
            float SCALE_TIME_SECONDS = .20f;

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
                    TextField.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont(fontName, fontSize);
                } 
                catch
                {
                    throw new Exception( string.Format( "Failed to load font: {0}", fontName ) );
                }
            }

            protected override int getKeyboardAppearance( )
            {
                return (int) TextField.KeyboardAppearance;
            }

            protected override void setKeyboardAppearance( int style )
            {
                TextField.KeyboardAppearance = (UIKeyboardAppearance)style;
            }

            protected override void setBorderColor( uint borderColor )
            {
                TextField.Layer.BorderColor = Rock.Mobile.PlatformUI.Util.GetUIColor( borderColor ).CGColor;
            }

            protected override float getBorderWidth()
            {
                return (float) TextField.Layer.BorderWidth;
            }
            protected override void setBorderWidth( float width )
            {
                TextField.Layer.BorderWidth = width;
            }

            protected override float getCornerRadius()
            {
                return (float) TextField.Layer.CornerRadius;
            }
            protected override void setCornerRadius( float radius )
            {
                TextField.Layer.CornerRadius = radius;
            }

            protected override void setBackgroundColor( uint backgroundColor )
            {
                TextField.Layer.BackgroundColor = Rock.Mobile.PlatformUI.Util.GetUIColor( backgroundColor ).CGColor;
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
                return TextField.Bounds.ToRectF( );
            }

            protected override void setBounds( RectangleF bounds )
            {
                TextField.Bounds = bounds;
            }

            protected override RectangleF getFrame( )
            {
                return TextField.Frame.ToRectF( );
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

            protected override bool getUserInteractionEnabled( )
            {
                return TextField.UserInteractionEnabled;
            }

            protected override void setUserInteractionEnabled( bool enabled )
            {
                TextField.UserInteractionEnabled = enabled;
            }

            protected override void setTextColor( uint color )
            {
                TextField.TextColor = Rock.Mobile.PlatformUI.Util.GetUIColor( color );
            }

            protected override void setPlaceholderTextColor( uint color )
            {
                TextField.PlaceholderTextColor = Rock.Mobile.PlatformUI.Util.GetUIColor( color );
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

            public override void AnimateOpen( )
            {
                if ( TextField.Animating == false && TextField.Hidden == true )
                {
                    // unhide and flag it as animating
                    TextField.Hidden = false;
                    TextField.Animating = true;

                    // and force it to a 0 size so it grows correctly
                    TextField.Bounds = RectangleF.Empty;

                    SimpleAnimator_SizeF animator = new SimpleAnimator_SizeF( TextField.Bounds.Size.ToSizeF( ),
                                                                              TextField.NaturalSize.ToSizeF( ), SCALE_TIME_SECONDS, 
                        delegate(float percent, object value )
                        {
                            SizeF currSize = (SizeF)value;
                            TextField.Bounds = new RectangleF( 0, 0, currSize.Width, currSize.Height );
                        },
                        delegate
                        {
                            TextField.Animating = false;
                        } );

                    animator.Start( );
                }
            }

            public override void AnimateClosed( )
            {
                if ( TextField.Animating == false && TextField.Hidden == false )
                {
                    TextField.Animating = true;

                    SimpleAnimator_SizeF animator = new SimpleAnimator_SizeF( TextField.Bounds.Size.ToSizeF( ), new SizeF( 0, 0 ), SCALE_TIME_SECONDS, 
                        delegate(float percent, object value )
                        {
                            SizeF currSize = (SizeF)value;
                            TextField.Bounds = new RectangleF( 0, 0, currSize.Width, currSize.Height );
                        },
                        delegate
                        {
                            TextField.Hidden = true;
                            TextField.Animating = false;
                        } );

                    animator.Start( );
                }
            }
        }
    }
}
#endif
