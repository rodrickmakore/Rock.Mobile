#if __IOS__

using System;
using MonoTouch.CoreAnimation;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;

namespace Rock.Mobile
{
    namespace PlatformUI
    {
        /// <summary>
        /// Subclass of UIView to render an underline under the fading text. 
        /// </summary>
        public class UnderlineUIView : UIView
        {
            public override void Draw(RectangleF rect)
            {
                base.Draw(rect);

                using( CGContext context = UIGraphics.GetCurrentContext() )
                {
                    context.AddRect( new RectangleF( 0, 0, Frame.Width, 1 ) );
                    context.Clip();

                    UIColor color = PlatformBaseUI.GetUIColor( 0x777777FF );

                    // probably should change this to not being a gradient
                    CGGradient gradiant = new CGGradient( CGColorSpace.CreateDeviceRGB(), new CGColor[] { color.CGColor, color.CGColor });
                    context.DrawLinearGradient( gradiant, new PointF( 0, 0 ), new PointF( Frame.Width, 0 ), CGGradientDrawingOptions.DrawsBeforeStartLocation );
                }
            }
        }


        /// <summary>
        /// Derived from iOSLabel, this is a custom label that 
        /// can hide a word and reveal it with a fade in using an animation.
        /// </summary>
        public class iOSRevealLabel : iOSLabel
        {
            /// <summary>
            /// The amount to scale the border by relative to the text width.
            /// Useful if a using gradiants that fade out too early, or in the
            /// case where too solid lines butt against each other.
            /// </summary>
            static float BORDER_WIDTH_SCALER = .99f;


            UnderlineUIView UnderlineView { get; set; }
            float Scale { get; set; }


            // This defines the rate we wish to update animations at, which happens to be 60fps.
            static float ANIMATION_TICK_RATE = (1.0f / 60.0f); 

            // This is the frequency at which we'll get an animation callback. It's basically 60fps, but needs to be in hundredth-nanoseconds
            // Tick Rate - 60fps
            // Tick Rate in ms - 0.016ms
            // Tick Rate in hundredth nanoseconds = 166666
            static float MILLISECONDS_TO_HUNDREDTH_NANOSECONDS = 10000.0f;
            static long ANIMATION_TICK_FREQUENCY = (long) ((ANIMATION_TICK_RATE * 1000) * MILLISECONDS_TO_HUNDREDTH_NANOSECONDS);

            /// <summary>
            /// The amount of time to take to scale. This should be adjusted based on
            /// the width of the text label
            /// </summary>
            float SCALE_TIME_SECONDS = .20f; 

            float MaxScale { get; set; }

            public iOSRevealLabel( ) : base()
            {
                MaxScale = 2.0f;
                Scale = .01f;

                // get a path to our custom fonts folder
                String imagePath = NSBundle.MainBundle.BundlePath + "/spot_mask.png";

                Label.Layer.Mask = new CALayer();
                Label.Layer.Mask.Contents = new UIImage( imagePath ).CGImage;
                Label.Layer.Mask.AnchorPoint = Label.Layer.AnchorPoint;
                ApplyMaskScale( Scale );


                // create our border
                UnderlineView = new UnderlineUIView();
                UnderlineView.Layer.AnchorPoint = Label.Layer.AnchorPoint;
                UnderlineView.BackgroundColor = UIColor.Clear;
            }

            protected override void setBounds(RectangleF bounds)
            {
                base.setBounds( bounds );

                UpdateUnderline();

                // Update the mask
                Label.Layer.Mask.Bounds = bounds;
                ApplyMaskScale(Scale);
            }

            protected override void setFrame( RectangleF frame )
            {
                base.setFrame( frame );

                UpdateUnderline();

                // Update the mask
                Label.Layer.Mask.Bounds = new RectangleF( 0, 0, frame.Width, frame.Height );
                ApplyMaskScale(Scale);
            }

            protected override void setPosition( PointF position )
            {
                // to position the border, first get the amount we'll be moving
                float deltaX = position.X - Label.Frame.X;
                float deltaY = position.Y - Label.Frame.Y;

                // let the label update
                base.setPosition( position );

                // now adjust the border by only the difference
                UnderlineView.Layer.Position = new PointF( UnderlineView.Layer.Position.X + deltaX, 
                                                           UnderlineView.Layer.Position.Y + deltaY);
            }

            public override void AddAsSubview( object masterView )
            {
                base.AddAsSubview( masterView );

                // we know that masterView will be an iOS View.
                UIView view = masterView as UIView;
                if( view == null )
                {
                    throw new Exception( "Object passed to iOS AddAsSubview must be a UIView." );
                }
                view.AddSubview( UnderlineView );
            }

            public override void RemoveAsSubview( object masterView )
            {
                UnderlineView.RemoveFromSuperview();

                base.RemoveAsSubview( masterView );
            }

            public override float GetFade()
            {
                return Scale / MaxScale;
            }

            public override void SetFade( float fadeAmount )
            {
                Scale = System.Math.Max(.01f, fadeAmount * MaxScale);
                ApplyMaskScale( Scale );
            }

            public override void AnimateToFade( float fadeAmount )
            {
                fadeAmount = System.Math.Max(.01f, fadeAmount * MaxScale);

                // convert the amount we'll need to change the scale to amount per tick
                float scalePerTick = (fadeAmount - Scale );
                scalePerTick /= (SCALE_TIME_SECONDS / ANIMATION_TICK_RATE);

                // Kick off a timer that will interpolate the scale at 60fps
                NSTimer animTimer = null;
                animTimer = NSTimer.CreateRepeatingTimer( new TimeSpan( ANIMATION_TICK_FREQUENCY ), new NSAction( 
                    delegate 
                    {
                        Scale += scalePerTick;

                        // if we reach our target amount, stop the timer and clamp
                        // the scale
                        if(Scale >= fadeAmount )
                        {
                            Scale = fadeAmount;
                            animTimer.Invalidate();
                        }

                        ApplyMaskScale( Scale );
                    })
                );

                // launch the timer
                NSRunLoop.Current.AddTimer( animTimer, NSRunLoop.NSDefaultRunLoopMode );
            }

            public override void SizeToFit( )
            {
                base.SizeToFit( );

                UpdateUnderline();

                // Update the mask
                Label.Layer.Mask.Bounds = new RectangleF( 0, 0, Label.Frame.Width, Label.Frame.Height );
                ApplyMaskScale(Scale);
            }

            void ApplyMaskScale( float scale)
            {
                // ultimately this scales the layer from the center out (rather than top/left)

                // create a transform that translates the layer by half its width/height
                // and then scales it
                CATransform3D translateScale = new CATransform3D();
                translateScale = CATransform3D.Identity;
                translateScale = translateScale.Scale( scale );
                translateScale = translateScale.Translate( -(Label.Layer.Mask.Bounds.Width / 2), -(Label.Layer.Mask.Bounds.Height / 2), 0 );

                // now apply a transform that puts it back by its width/height, effectively re-centering it.
                CATransform3D postScale = new CATransform3D();
                postScale = CATransform3D.Identity;
                postScale = postScale.Translate( (Label.Layer.Mask.Bounds.Width / 2), (Label.Layer.Mask.Bounds.Height / 2), 0 );

                // and now concat the post scale and apply
                Label.Layer.Mask.Transform = translateScale.Concat( postScale );
            }

            void UpdateUnderline()
            {
                // determine how far down the border should start.
                // The ascender is basically the distance from the top of the highest font to where the baseline is,
                // which is effectively the character height.
                float borderYOffset = Label.Font.Ascender + 2;

                // Same for X, horizontally
                float borderWidth = (int) ((float)Label.Frame.Width * BORDER_WIDTH_SCALER);
                float borderXOffset = (Label.Frame.Width - borderWidth) / 2;

                UnderlineView.Layer.Position = new PointF( Label.Frame.X + (int)borderXOffset, Label.Frame.Y + (int) borderYOffset );
                UnderlineView.Layer.Bounds = new RectangleF( 0, 0, borderWidth, 5 );
            }
        }
    }
}

#endif
