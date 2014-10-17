#if __IOS__
using System;
using MonoTouch.CoreAnimation;
using MonoTouch.UIKit;
using System.Drawing;
using MonoTouch.Foundation;

namespace Rock.Mobile
{
    namespace PlatformCommon
    {
        /// <summary>
        /// Reusable "carousel" that gives the illusion of an infinitely long list.
        /// All you have to do is create it, and set views for its 5 "cards". Each
        /// "card" should be the same dimensions, as these are the repeating items.
        /// </summary>
        public class iOSCardCarousel
        {
            class CaourselAnimDelegate : CAAnimationDelegate
            {
                public iOSCardCarousel Parent { get; set; }

                public override void AnimationStarted(CAAnimation anim)
                {

                }

                public override void AnimationStopped(CAAnimation anim, bool finished)
                {
                    Parent.AnimationStopped( anim, finished );
                }
            }

            // Create 5 cards so that we're guaranteed to always have cards visible on screen
            public UIView SubLeftCard = null;
            public UIView LeftCard = null;
            public UIView CenterCard = null;
            public UIView RightCard = null;
            public UIView PostRightCard = null;

            // the default positions for each card
            PointF SubLeftPos { get; set; }
            PointF LeftPos { get; set; }
            PointF CenterPos { get; set; }
            PointF RightPos { get; set; }
            PointF PostRightPos { get; set; }

            /// <summary>
            /// Tracks the last position of panning so delta can be applied
            /// </summary>
            PointF PanLastPos { get; set; }

            /// <summary>
            /// Direction we're currently panning. Important for syncing the card positions
            /// </summary>
            int PanDir { get; set; }

            /// <summary>
            /// The prayer currently being viewed (always the center card)
            /// </summary>
            int ViewingIndex { get; set; }

            /// <summary>
            /// True when an animation to restore card positions is playing.
            /// Needed so we know when to allow "fast" panning.
            /// </summary>
            bool Animating = false;

            UIView ParentView { get; set; }
            RectangleF BoundsInParent { get; set; }
            float CardWidth { get; set; }
            float CardHeight { get; set; }

            public int NumItems { get; set; }

            public delegate void ViewingIndexChanged( int viewingIndex );
            ViewingIndexChanged ViewingIndexChangedDelegate;

            public iOSCardCarousel( float cardWidth, float cardHeight, UIView parentView, RectangleF boundsInParent, ViewingIndexChanged changedDelegate )
            {
                ParentView = parentView;
                BoundsInParent = boundsInParent;
                CardWidth = cardWidth;
                CardHeight = cardHeight;

                ViewingIndexChangedDelegate = changedDelegate;
            }

            public void ViewDidLoad()
            {
                if( SubLeftCard == null ||
                    LeftCard == null ||
                    CenterCard == null ||
                    RightCard == null ||
                    PostRightCard == null )
                {
                    throw new Exception( "Card Views must be set before ViewDidLoad()" );
                }

                // the center position should be center on screen
                CenterPos = new PointF( ((BoundsInParent.Width - CardWidth) / 2), BoundsInParent.Y );

                // left should be exactly one screen width to the left, and right one screen width to the right
                LeftPos = new PointF( CenterPos.X - BoundsInParent.Width, BoundsInParent.Y );
                RightPos = new PointF( CenterPos.X + BoundsInParent.Width, BoundsInParent.Y );

                // sub left and post right should be two screens to the left / right of center
                SubLeftPos = new PointF( LeftPos.X - BoundsInParent.Width, BoundsInParent.Y );
                PostRightPos = new PointF( RightPos.X + BoundsInParent.Width, BoundsInParent.Y );

                // default the initial position of the cards
                SubLeftCard.Layer.Position = SubLeftPos;
                LeftCard.Layer.Position = LeftPos;
                CenterCard.Layer.Position = CenterPos;
                RightCard.Layer.Position = RightPos;
                PostRightCard.Layer.Position = PostRightPos;

                // setup our pan gesture
                UIPanGestureRecognizer panGesture = new UIPanGestureRecognizer( OnPanGesture );
                panGesture.MinimumNumberOfTouches = 1;
                panGesture.MaximumNumberOfTouches = 1;

                // add the gesture and all cards to our view
                ParentView.AddGestureRecognizer( panGesture );
                ParentView.AddSubview( SubLeftCard );
                ParentView.AddSubview( LeftCard );
                ParentView.AddSubview( CenterCard );
                ParentView.AddSubview( RightCard );
                ParentView.AddSubview( PostRightCard );
            }

            public void ViewWillAppear(bool animated)
            {
                ViewingIndex = 0;
            }

            void OnPanGesture(UIPanGestureRecognizer obj) 
            {
                switch( obj.State )
                {
                    case UIGestureRecognizerState.Began:
                    {
                        // when panning begins, clear our pan values
                        PanLastPos = new PointF( 0, 0 );
                        PanDir = 0;
                        break;
                    }

                    case UIGestureRecognizerState.Changed:
                    {
                        // use the velocity to determine the direction of the pan
                        PointF currVelocity = obj.VelocityInView( ParentView );
                        if( currVelocity.X < 0 )
                        {
                            PanDir = -1;
                        }
                        else
                        {
                            PanDir = 1;
                        }

                        // Update the positions of the cards
                        PointF absPan = obj.TranslationInView( ParentView );
                        PointF delta = new PointF( absPan.X - PanLastPos.X, 0 );
                        PanLastPos = absPan;

                        TryPanCards( delta );

                        // sync the positions, which will adjust cards as they scroll so the center
                        // remains in the center (it's a fake infinite list)
                        SyncCardPositionsForPan( );

                        // ensure the cards don't move beyond their boundaries
                        ClampCards( );
                        break;
                    }

                    case UIGestureRecognizerState.Ended:
                    {
                        // when panning is complete, restore the cards to their natural positions
                        AnimateCardsToNeutral( );
                        break;
                    }
                }
            }

            void ClampCards( )
            {
                // don't allow the left or right cards to move if 
                // we're at the edge of the list.
                if( ViewingIndex - 2 < 0 )
                {
                    SubLeftCard.Layer.Position = SubLeftPos;
                }

                if( ViewingIndex - 1 < 0 )
                {
                    LeftCard.Layer.Position = LeftPos;
                }

                if( ViewingIndex + 1 >= NumItems )
                {
                    RightCard.Layer.Position = RightPos;
                }

                if( ViewingIndex + 2 >= NumItems )
                {
                    PostRightCard.Layer.Position = PostRightPos;
                }
            }

            void TryPanCards( PointF panPos )
            {
                // adjust all the cards by the amount panned (this should be a delta value)
                if( ViewingIndex - 2 >= 0 )
                {
                    SubLeftCard.Layer.Position = new PointF( SubLeftCard.Layer.Position.X + panPos.X, LeftPos.Y );
                }

                if( ViewingIndex - 1 >= 0 )
                {
                    LeftCard.Layer.Position = new PointF( LeftCard.Layer.Position.X + panPos.X, LeftPos.Y );
                }

                CenterCard.Layer.Position = new PointF( CenterCard.Layer.Position.X + panPos.X, CenterPos.Y );

                if( ViewingIndex + 1 < NumItems )
                {
                    RightCard.Layer.Position = new PointF( RightCard.Layer.Position.X + panPos.X, RightPos.Y );
                }

                if( ViewingIndex + 2 < NumItems )
                {
                    PostRightCard.Layer.Position = new PointF( PostRightCard.Layer.Position.X + panPos.X, RightPos.Y );
                }
            }

            /// <summary>
            /// Only called if the user didn't pan. Used primarly to detect
            /// the user tapping DURING an animation so we can pause the card movement.
            /// </summary>
            public void TouchesBegan(NSSet touches, UIEvent evt)
            {
                // when touch begins, remove all animations
                SubLeftCard.Layer.RemoveAllAnimations();
                LeftCard.Layer.RemoveAllAnimations();
                CenterCard.Layer.RemoveAllAnimations();
                RightCard.Layer.RemoveAllAnimations();
                PostRightCard.Layer.RemoveAllAnimations();

                // and commit the animated positions as the actual card positions.
                SubLeftCard.Layer.Position = SubLeftCard.Layer.PresentationLayer.Position;
                LeftCard.Layer.Position = LeftCard.Layer.PresentationLayer.Position;
                CenterCard.Layer.Position = CenterCard.Layer.PresentationLayer.Position;
                RightCard.Layer.Position = RightCard.Layer.PresentationLayer.Position;
                PostRightCard.Layer.Position = PostRightCard.Layer.PresentationLayer.Position;

                // this has the effect of freezing & stopping the animation in motion.
                // OnAnimationEnded will be called, but finished will be false, so
                // we'll know it was stopped manually
                Console.WriteLine( "Touches Began" );
            }

            /// <summary>
            /// Only called if the user didn't pan. Used primarly to detect
            /// which direction to resume the cards if the user touched and
            /// released without panning.
            /// </summary>
            public void TouchesEnded(NSSet touches, UIEvent evt)
            {
                // Attempt to restore the cards to their natural position. This
                // will NOT be called if the user invoked the pan gesture. (which is a good thing)
                AnimateCardsToNeutral( );

                Console.WriteLine( "Touches Ended" );
            }

            /// <summary>
            /// Animates a card from startPos to endPos over time
            /// </summary>
            void AnimateCard( UIView cardView, string animName, PointF startPos, PointF endPos, float duration, iOSCardCarousel parentDelegate )
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
                if( parentDelegate != null )
                {
                    cardAnim.Delegate = new CaourselAnimDelegate() { Parent = this };
                }

                // 
                cardView.Layer.AddAnimation( cardAnim, animName );
            }

            /// <summary>
            /// Called when card movement is complete.
            /// </summary>
            /// <param name="anim">Animation.</param>
            /// <param name="finished">If set to <c>true</c> finished.</param>
            void AnimationStopped( CAAnimation anim, bool finished )
            {
                // the only thing we realy want to do is update the carousel so
                // that the cards are once again L C R and the prayer indices are -1, 0, 1
                if( finished == true )
                {
                    Animating = false;
                    Console.WriteLine( "Animation Stopped" );
                }
            }

            /// <summary>
            /// This turns the cards into a "carousel" that will push cards forward and pull
            /// prayers back, or pull cards back and push cards forward, giving the illusion
            /// of endlessly panning thru cards.
            /// </summary>
            void SyncCardPositionsForPan( )
            {
                // see if the center of either the left or right card crosses the threshold for switching
                float deltaLeftX = CardDistFromCenter( LeftCard );
                float deltaRightX = CardDistFromCenter( RightCard );

                Console.WriteLine( "Right Delta: {0} Left Delta {1}", deltaRightX, deltaLeftX );

                // if animating is true, we want the tolerance to be MUCH higher,
                // allowing easier flicking to the next card.
                // The real world effect is that if the user flicks cards,
                // they will quickly and easily move. If the user pans on the cards,
                // it will be harder to get them to switch.
                float tolerance = (Animating == true) ? 400 : 260;

                // if we're panning LEFT, that means the right hand card might be in range to sync
                if( System.Math.Abs(deltaRightX) < tolerance && PanDir == -1)
                {
                    if( ViewingIndex + 1 < NumItems )
                    {
                        Console.WriteLine( "Syncing Card Positions Right" );

                        ViewingIndex = ViewingIndex + 1;
                        ViewingIndexChangedDelegate( ViewingIndex );

                        // reset the card positions, creating the illusion that the cards really moved
                        SubLeftCard.Layer.Position = new PointF( deltaRightX + SubLeftPos.X, LeftPos.Y );
                        LeftCard.Layer.Position = new PointF( deltaRightX + LeftPos.X, LeftPos.Y );
                        CenterCard.Layer.Position = new PointF( deltaRightX + CenterPos.X, CenterPos.Y );
                        RightCard.Layer.Position = new PointF( deltaRightX + RightPos.X, RightPos.Y );
                        PostRightCard.Layer.Position = new PointF( deltaRightX + PostRightPos.X, RightPos.Y );
                    }
                }
                // if we're panning RIGHT, that means the left hand card might be in range to sync
                else if( System.Math.Abs( deltaLeftX ) < tolerance && PanDir == 1)
                {
                    if( ViewingIndex - 1 >= 0 )
                    {
                        Console.WriteLine( "Syncing Card Positions Left" );

                        ViewingIndex = ViewingIndex - 1;
                        ViewingIndexChangedDelegate( ViewingIndex );

                        // reset the card positions, creating the illusion that the cards really moved
                        SubLeftCard.Layer.Position = new PointF( deltaLeftX + SubLeftPos.X, LeftPos.Y );
                        LeftCard.Layer.Position = new PointF( deltaLeftX + LeftPos.X, LeftPos.Y );
                        CenterCard.Layer.Position = new PointF( deltaLeftX + CenterPos.X, CenterPos.Y );
                        RightCard.Layer.Position = new PointF( deltaLeftX + RightPos.X, RightPos.Y );
                        PostRightCard.Layer.Position = new PointF( deltaLeftX + PostRightPos.X, RightPos.Y );
                    }
                }
            }

            /// <summary>
            /// Helper to get the distance from the center
            /// </summary>
            /// <returns>The dist from center.</returns>
            float CardDistFromCenter( UIView card )
            {
                float cardHalfWidth = CardWidth / 2;
                return ( card.Layer.Position.X + cardHalfWidth ) - (CenterPos.X + cardHalfWidth);
            }

            void AnimateCardsToNeutral( )
            {
                // this will animate each card to its neutral resting point
                Animating = true;
                AnimateCard( SubLeftCard, "SubLeftCard", SubLeftCard.Layer.Position, SubLeftPos, CCVApp.Shared.Config.Prayer.Card_AnimationDuration, null );
                AnimateCard( LeftCard, "LeftCard", LeftCard.Layer.Position, LeftPos, CCVApp.Shared.Config.Prayer.Card_AnimationDuration, null );
                AnimateCard( CenterCard, "CenterCard", CenterCard.Layer.Position, CenterPos, CCVApp.Shared.Config.Prayer.Card_AnimationDuration, this );
                AnimateCard( RightCard, "RightCard", RightCard.Layer.Position, RightPos, CCVApp.Shared.Config.Prayer.Card_AnimationDuration, null );
                AnimateCard( PostRightCard, "PostRightCard", PostRightCard.Layer.Position, PostRightPos, CCVApp.Shared.Config.Prayer.Card_AnimationDuration, null );
            }
        }
    }
}
#endif