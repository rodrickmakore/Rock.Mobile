#if __IOS__
using System;
using MonoTouch.CoreAnimation;
using MonoTouch.UIKit;
using System.Drawing;
using MonoTouch.Foundation;

namespace Rock.Mobile
{
    namespace PlatformUI
    {
        /// <summary>
        /// Platform Specific implementations of the carousel need only to manage
        /// the animation requests made by PlatformCarousel, since we dont have a platform agnostic
        /// implementation of animation systems.
        /// </summary>
        public class iOSCardCarousel : PlatformCardCarousel
        {
            class CaourselAnimDelegate : CAAnimationDelegate
            {
                public iOSCardCarousel Parent { get; set; }
                public UIView Card { get; set; }

                public override void AnimationStarted(CAAnimation anim)
                {

                }

                public override void AnimationStopped(CAAnimation anim, bool finished)
                {
                    Parent.AnimationStopped( anim, Card, finished );
                }
            }

            /// <summary>
            /// Tracks the last position of panning so delta can be applied
            /// </summary>
            PointF PanLastPos { get; set; }

            UIView ParentView { get; set; }

            public iOSCardCarousel( float cardWidth, float cardHeight, RectangleF boundsInParent, float animationDuration, ViewingIndexChanged changedDelegate ) : base( cardWidth, cardHeight, boundsInParent, animationDuration, changedDelegate )
            {
            }

            public override void Init(object parentView)
            {
                base.Init(parentView);

                ParentView = parentView as UIView;

                SubLeftCard.AddAsSubview( ParentView );
                LeftCard.AddAsSubview( ParentView );
                CenterCard.AddAsSubview( ParentView );
                RightCard.AddAsSubview( ParentView );
                PostRightCard.AddAsSubview( ParentView );

                // setup our pan gesture
                UIPanGestureRecognizer panGesture = new UIPanGestureRecognizer( iOSPanGesture );
                panGesture.MinimumNumberOfTouches = 1;
                panGesture.MaximumNumberOfTouches = 1;

                // add the gesture and all cards to our view
                ParentView.AddGestureRecognizer( panGesture );
            }

            public void iOSPanGesture( UIPanGestureRecognizer obj)
            {
                // get the required data from the gesture and call our base function
                PointF currVelocity = obj.VelocityInView( ParentView );
                PointF deltaPan = new PointF( 0, 0 );

                PlatformCardCarousel.PanGestureState state = PlatformCardCarousel.PanGestureState.Began;
                switch ( obj.State )
                {
                    case UIGestureRecognizerState.Began:
                    {
                        PanLastPos = new PointF( 0, 0 );

                        state = PlatformCardCarousel.PanGestureState.Began;
                        break;
                    }

                    case UIGestureRecognizerState.Changed:
                    {
                        PointF absolutePan = obj.TranslationInView( ParentView );
                        deltaPan = new PointF( absolutePan.X - PanLastPos.X, 0 );

                        PanLastPos = absolutePan;

                        state = PlatformCardCarousel.PanGestureState.Changed;
                        break;
                    }

                    case UIGestureRecognizerState.Ended:
                    {
                        state = PlatformCardCarousel.PanGestureState.Ended;
                        break;
                    }
                }

                base.OnPanGesture( state, currVelocity, deltaPan );
            }

            public override void TouchesBegan( )
            {
                // when touch begins, remove all animations

                // first get the UIViews backing these PlatformViews
                UIView subLeftCardView = SubLeftCard.PlatformNativeObject as UIView;
                UIView leftCardView = LeftCard.PlatformNativeObject as UIView;
                UIView centerCardView = CenterCard.PlatformNativeObject as UIView;
                UIView rightCardView = RightCard.PlatformNativeObject as UIView;
                UIView postRightCardView = PostRightCard.PlatformNativeObject as UIView;

                // and commit the animated positions as the actual card positions.
                subLeftCardView.Layer.Position = subLeftCardView.Layer.PresentationLayer.Position;
                leftCardView.Layer.Position = leftCardView.Layer.PresentationLayer.Position;
                centerCardView.Layer.Position = centerCardView.Layer.PresentationLayer.Position;
                rightCardView.Layer.Position = rightCardView.Layer.PresentationLayer.Position;
                postRightCardView.Layer.Position = postRightCardView.Layer.PresentationLayer.Position;

                // stop all animations
                subLeftCardView.Layer.RemoveAllAnimations( );
                leftCardView.Layer.RemoveAllAnimations( );
                centerCardView.Layer.RemoveAllAnimations( );
                rightCardView.Layer.RemoveAllAnimations( );
                postRightCardView.Layer.RemoveAllAnimations( );

                // this has the effect of freezing & stopping the animation in motion.
                // OnAnimationEnded will be called, but finished will be false, so
                // we'll know it was stopped manually
            }

            public override void TouchesEnded()
            {
                base.TouchesEnded();
            }

            /// <summary>
            /// Animates a card from startPos to endPos over time
            /// </summary>
            protected override void AnimateCard( object platformObject, string animName, PointF startPos, PointF endPos, float duration, PlatformCardCarousel parentDelegate )
            {
                // make sure we're not already running an animation
                UIView cardView = platformObject as UIView;
                if ( cardView.Layer.AnimationForKey( animName ) == null )
                {
                    CABasicAnimation cardAnim = CABasicAnimation.FromKeyPath( "position" );

                    cardAnim.From = NSValue.FromPointF( startPos );
                    cardAnim.To = NSValue.FromPointF( endPos );

                    cardAnim.Duration = duration;
                    cardAnim.TimingFunction = CAMediaTimingFunction.FromName( CAMediaTimingFunction.EaseInEaseOut );

                    // these ensure we maintain the card position when finished
                    cardAnim.FillMode = CAFillMode.Forwards;
                    cardAnim.RemovedOnCompletion = false;


                    // if a delegate was provided, give it to the card
                    if ( parentDelegate != null )
                    {
                        cardAnim.Delegate = new CaourselAnimDelegate() { Parent = this, Card = cardView };
                    }

                    // 
                    cardView.Layer.AddAnimation( cardAnim, animName );
                }
            }

            /// <summary>
            /// Called when card movement is complete.
            /// </summary>
            /// <param name="anim">Animation.</param>
            /// <param name="finished">If set to <c>true</c> finished.</param>
            void AnimationStopped( CAAnimation anim, UIView cardView, bool finished )
            {
                // all we need to do is flag Animating as false (if it FINISHED)
                // so we know how to control panning.
                if( finished == true )
                {
                    Animating = false;
                    //Console.WriteLine( "Animation Stopped" );
                }
            }
        }
    }
}
#endif