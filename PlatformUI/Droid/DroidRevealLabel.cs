#if __ANDROID__

using System;
using Android.Views;
using Droid;
using Android.Graphics.Drawables;
using Android.Widget;
using RockMobile.PlatformUI.DroidNative;
using System.IO;

namespace RockMobile
{
    namespace PlatformUI
    {
        /// <summary>
        /// Derived from DroidLabel, this is a custom label that 
        /// can hide a word and reveal it with a fade in using an animation.
        /// </summary>
        public class DroidRevealLabel : DroidLabel
        {
            /// <summary>
            /// This defines the RATE of animation. The lower the number the faster
            /// the label will be revealed.
            /// </summary>
            static float MASK_TIME_SCALER = .013f;

            /// <summary>
            /// This should not be changed, and controls how wide to scale up the mask so the entire word is revealed.
            /// </summary>
            static float MASK_WIDTH_SCALER = .013f;

            /// <summary>
            /// The amount to scale the border by relative to the text width.
            /// Useful if a using gradiants that fade out too early
            /// </summary>
            static float BORDER_WIDTH_SCALER = 1.25f;

            /// <summary>
            /// Dependent on the label's width, determines how large
            /// the mask must scale to reveal the whole word.
            /// </summary>
            float MaxScale = 1;

            /// <summary>
            /// The view that draws the underline for the word.
            /// </summary>
            View UnderlineView { get; set; }

            public DroidRevealLabel( )
            {
                Label = new FadeTextView( RockMobile.PlatformCommon.Droid.Context ) as TextView;
                Label.LayoutParameters = new ViewGroup.LayoutParams( ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent );

                ((FadeTextView)Label).CreateAlphaMask( RockMobile.PlatformCommon.Droid.Context, "spot_mask.png" );


                // Define a gradiant underline that will be shown underneath the text
                int [] colors = new int[] { 0, int.MaxValue, int.MaxValue, 0 };
                GradientDrawable border = new GradientDrawable( GradientDrawable.Orientation.LeftRight, colors);
                border.SetGradientType( GradientType.LinearGradient );

                UnderlineView = new View( RockMobile.PlatformCommon.Droid.Context );
                UnderlineView.LayoutParameters = new ViewGroup.LayoutParams( ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent );
                UnderlineView.SetBackgroundDrawable( border );

                UnderlineView.LayoutParameters.Height = 5;
            }

            protected override void setBounds(System.Drawing.RectangleF bounds)
            {
                base.setBounds( bounds );

                UpdateUnderline();

                // update the scale value for the fading
                MaxScale = UnderlineView.LayoutParameters.Width * MASK_WIDTH_SCALER;
            }

            protected override void setPosition( System.Drawing.PointF position )
            {
                // to position the border, first get the amount we'll be moving
                float deltaX = position.X - Label.GetX();
                float deltaY = position.Y - Label.GetY();

                // let the label update
                base.setPosition( position );

                // now adjust the border by only the difference
                UnderlineView.SetX( UnderlineView.GetX() + deltaX );
                UnderlineView.SetY( UnderlineView.GetY() + deltaY );
            }

            public override void AddAsSubview( object masterView )
            {
                // do not call the base version

                // we know that masterView will be an iOS View.
                RelativeLayout view = masterView as RelativeLayout;
                if( view == null )
                {
                    throw new Exception( "Object passed to Android AddAsSubview must be a RelativeLayout." );
                }

                view.AddView( Label );
                view.AddView( UnderlineView );
            }

            public override void RemoveAsSubview( object masterView )
            {
                // do not call the base version

                // we know that masterView will be an iOS View.
                RelativeLayout view = masterView as RelativeLayout;
                if( view == null )
                {
                    throw new Exception( "Object passed to Android RemoveAsSubview must be a RelativeLayout." );
                }

                view.RemoveView( UnderlineView );
                view.RemoveView( Label );
            }

            public override float GetFade()
            {
                return ((FadeTextView)Label).MaskScale / MaxScale;
            }

            public override void SetFade( float fadeAmount )
            {
                // if we're setting the absolute fade, invalidate now so we redraw
                ((FadeTextView)Label).MaskScale = MaxScale * fadeAmount;

                Label.Invalidate();
            }

            public override void AnimateToFade( float fadeAmount )
            {
                ((FadeTextView)Label).AnimateMaskScale( MaxScale * fadeAmount, (long) (MaxScale / MASK_TIME_SCALER) );
            }

            public override void SizeToFit( )
            {
                base.SizeToFit( );

                UpdateUnderline();

                // update the scale value for the fading
                MaxScale = UnderlineView.LayoutParameters.Width * MASK_WIDTH_SCALER;
            }

            void UpdateUnderline()
            {
                // first get the Y starting point of the font, relative to the control.
                // (the control's top might start 5 pixels above the actual font, for example)
                float fontYStart = (Label.LayoutParameters.Height - Label.TextSize) / 2;

                // Update the Y position of the border here, because 
                // if the HEIGHT of the label changed, our starting Y position must change
                // so we stay at the bottom of the label.
                float borderYOffset = (fontYStart + Label.TextSize);

                UnderlineView.SetY( (int)Label.GetY() + (int) borderYOffset );


                // Same for X
                UnderlineView.LayoutParameters.Width = (int) ((float)Label.LayoutParameters.Width * BORDER_WIDTH_SCALER);

                float borderXOffset = (Label.LayoutParameters.Width - UnderlineView.LayoutParameters.Width) / 2;
                UnderlineView.SetX( (int)Label.GetX() + (int) borderXOffset );
            }
        }
    }
}

#endif
