using System;
using System.Drawing;

namespace Rock.Mobile
{
    namespace PlatformUI
    {
        /// <summary>
        /// Reusable "carousel" that gives the illusion of an infinitely long list.
        /// All you have to do is create it, and set views for its 5 "cards". Each
        /// "card" should be the same dimensions, as these are the repeating items.
        /// </summary>
        public abstract class PlatformCardCarousel
        {
            public static PlatformCardCarousel Create( float cardWidth, float cardHeight, RectangleF boundsInParent, float animationDuration, ViewingIndexChanged changedDelegate )
            {
                #if __IOS__
                return new iOSCardCarousel( cardWidth, cardHeight, boundsInParent, animationDuration, changedDelegate );
                #endif

                #if __ANDROID__
                return new DroidCardCarousel( cardWidth, cardHeight, boundsInParent, animationDuration, changedDelegate );
                #endif
            }

            public enum PanGestureState
            {
                Began,
                Changed,
                Ended
            };

            float AnimationDuration { get; set; }

            // Create 5 cards so that we're guaranteed to always have cards visible on screen
            public PlatformView SubLeftCard = null;
            public PlatformView LeftCard = null;
            public PlatformView CenterCard = null;
            public PlatformView RightCard = null;
            public PlatformView PostRightCard = null;

            // the default positions for each card
            PointF SubLeftPos { get; set; }
            PointF LeftPos { get; set; }
            PointF CenterPos { get; set; }
            PointF RightPos { get; set; }
            PointF PostRightPos { get; set; }

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
            protected bool Animating = false;

            RectangleF BoundsInParent { get; set; }
            float CardWidth { get; set; }
            float CardHeight { get; set; }

            int _Numitems;
            public int NumItems 
            { 
                get { return _Numitems; } 
                set
                {
                    _Numitems = value;
                    ClampCards( );
                }
            }

            public bool Hidden
            {
                set
                {
                    SubLeftCard.Hidden = value;
                    LeftCard.Hidden = value;
                    CenterCard.Hidden = value;
                    RightCard.Hidden = value;
                    PostRightCard.Hidden = value;
                }
            }

            public delegate void ViewingIndexChanged( int viewingIndex );
            ViewingIndexChanged ViewingIndexChangedDelegate;

            protected PlatformCardCarousel( float cardWidth, float cardHeight, RectangleF boundsInParent, float animationDuration, ViewingIndexChanged changedDelegate )
            {
                AnimationDuration = animationDuration;
                BoundsInParent = boundsInParent;
                CardWidth = cardWidth;
                CardHeight = cardHeight;

                ViewingIndexChangedDelegate = changedDelegate;
            }

            float CardXSpacing { get; set; }

            /// <summary>
            /// This should be called when UI is ready, like in ViewDidLoad or OnCreateView()
            /// </summary>
            public virtual void Init( object parentView )
            {
                if( SubLeftCard == null ||
                    LeftCard == null ||
                    CenterCard == null ||
                    RightCard == null ||
                    PostRightCard == null )
                {
                    throw new Exception( "Card Views must be set before Init()" );
                }

                // the center position should be center on screen
                CenterPos = new PointF( ((BoundsInParent.Width - CardWidth) / 2), BoundsInParent.Y );

                // left should be exactly one screen width to the left, and right one screen width to the right
                CardXSpacing = BoundsInParent.Width * .88f;
                LeftPos = new PointF( CenterPos.X - CardXSpacing, BoundsInParent.Y );
                RightPos = new PointF( CenterPos.X + CardXSpacing, BoundsInParent.Y );

                // sub left and post right should be two screens to the left / right of center
                SubLeftPos = new PointF( LeftPos.X - CardXSpacing, BoundsInParent.Y );
                PostRightPos = new PointF( RightPos.X + CardXSpacing, BoundsInParent.Y );

                // default the initial position of the cards
                SubLeftCard.Position = SubLeftPos;
                LeftCard.Position = LeftPos;
                CenterCard.Position = CenterPos;
                RightCard.Position = RightPos;
                PostRightCard.Position = PostRightPos;

                // defualt all the cards to hidden and wait for numItems to be set
                SubLeftCard.Hidden = true;
                LeftCard.Hidden = true;

                RightCard.Hidden = true;
                PostRightCard.Hidden = true;
            }

            public void ViewWillAppear(bool animated)
            {
                ViewingIndex = 0;
            }

            //int numSamples { get; set; }
            //PointF mAvgPan = new PointF( );

            public void OnPanGesture(PanGestureState state, PointF currVelocity, PointF deltaPan) 
            {
                switch( state )
                {
                    case PanGestureState.Began:
                    {
                        //numSamples = 1;
                        //mAvgPan = deltaPan;

                        // when panning begins, clear our pan values
                        PanDir = 0;
                        break;
                    }

                    case PanGestureState.Changed:
                    {
                        //PointF filteredPan = new PointF();

                        /*numSamples++;
                        mAvgPan.X += deltaPan.X;
                        mAvgPan.Y += deltaPan.Y;

                        filteredPan.X = mAvgPan.X / numSamples;
                        filteredPan.Y = mAvgPan.Y / numSamples;*/

                        // use the velocity to determine the direction of the pan
                        if( currVelocity.X < 0 )
                        {
                            PanDir = -1;
                        }
                        else
                        {
                            PanDir = 1;
                        }

                        //Console.WriteLine( "Delta Pan: {0}, {1}", deltaPan, filteredPan );

                        // Update the positions of the cards
                        TryPanCards( deltaPan );

                        // sync the positions, which will adjust cards as they scroll so the center
                        // remains in the center (it's a fake infinite list)
                        SyncCardPositionsForPan( );

                        // ensure the cards don't move beyond their boundaries
                        ClampCards( );
                        break;
                    }

                    case PanGestureState.Ended:
                    {
                        // when panning is complete, restore the cards to their natural positions
                        AnimateCardsToNeutral( );

                        //numSamples = 0;
                        //mAvgPan = PointF.Empty;
                        break;
                    }
                }
            }

            void ClampCards( )
            {
                // don't allow the left or right cards to move if 
                // we're at the edge of the list.
                if ( ViewingIndex - 2 < 0 )
                {
                    SubLeftCard.Hidden = true;
                    SubLeftCard.Position = SubLeftPos;
                }
                else
                {
                    SubLeftCard.Hidden = false;
                }

                if ( ViewingIndex - 1 < 0 )
                {
                    LeftCard.Hidden = true;
                    LeftCard.Position = LeftPos;
                }
                else
                {
                    LeftCard.Hidden = false;
                }

                if ( ViewingIndex + 1 >= NumItems )
                {
                    RightCard.Hidden = true;
                    RightCard.Position = RightPos;
                }
                else
                {
                    RightCard.Hidden = false;
                }

                if ( ViewingIndex + 2 >= NumItems )
                {
                    PostRightCard.Hidden = true;
                    PostRightCard.Position = PostRightPos;
                }
                else
                {
                    PostRightCard.Hidden = false;
                }
            }

            void TryPanCards( PointF panPos )
            {
                // adjust all the cards by the amount panned (this should be a delta value)
                if( ViewingIndex - 2 >= 0 )
                {
                    SubLeftCard.Position = new PointF( SubLeftCard.Position.X + panPos.X, LeftPos.Y );
                }

                if( ViewingIndex - 1 >= 0 )
                {
                    LeftCard.Position = new PointF( LeftCard.Position.X + panPos.X, LeftPos.Y );
                }

                CenterCard.Position = new PointF( CenterCard.Position.X + panPos.X, CenterPos.Y );

                if( ViewingIndex + 1 < NumItems )
                {
                    RightCard.Position = new PointF( RightCard.Position.X + panPos.X, RightPos.Y );
                }

                if( ViewingIndex + 2 < NumItems )
                {
                    PostRightCard.Position = new PointF( PostRightCard.Position.X + panPos.X, RightPos.Y );
                }
            }

            /// <summary>
            /// Only called if the user didn't pan. Used primarly to detect
            /// the user tapping DURING an animation so we can pause the card movement.
            /// </summary>
            public abstract void TouchesBegan( );

            /// <summary>
            /// Only called if the user didn't pan. Used primarly to detect
            /// which direction to resume the cards if the user touched and
            /// released without panning.
            /// </summary>
            public virtual void TouchesEnded( )
            {
                // Attempt to restore the cards to their natural position. This
                // will NOT be called if the user invoked the pan gesture. (which is a good thing)
                AnimateCardsToNeutral( );

                //Console.WriteLine( "Touches Ended" );
            }

            /// <summary>
            /// Animates a card from startPos to endPos over time
            /// </summary>
            protected abstract void AnimateCard( object platformObject, string animName, PointF startPos, PointF endPos, float duration, PlatformCardCarousel parentDelegate );

            /// <summary>
            /// This turns the cards into a "carousel" that will push cards forward and pull
            /// prayers back, or pull cards back and push cards forward, giving the illusion
            /// of endlessly panning thru cards.
            /// </summary>
            protected void SyncCardPositionsForPan( )
            {
                // see if the center of either the left or right card crosses the threshold for switching
                float deltaLeftX = CardDistFromCenter( LeftCard );
                float deltaRightX = CardDistFromCenter( RightCard );

                // if animating is true, we want the tolerance to be MUCH higher,
                // allowing easier flicking to the next card.
                // The real world effect is that if the user flicks cards,
                // they will quickly and easily move. If the user pans on the cards,
                // it will be harder to get them to switch.
                float fastPixelTolerance = (float)CardXSpacing * 1.25f;
                float slowPixelTolerance = (float)CardXSpacing * .81f;
                float tolerance = ( Animating == true ) ? fastPixelTolerance : slowPixelTolerance;

                //Console.WriteLine( "Right Delta: {0} Left Delta {1} Tolerance {2} Anim {3}", deltaRightX, deltaLeftX, tolerance, Animating );

                // if we're panning LEFT, that means the right hand card might be in range to sync
                if( System.Math.Abs(deltaRightX) < tolerance && PanDir == -1)
                {
                    if( ViewingIndex + 1 < NumItems )
                    {
                        Rock.Mobile.Profiler.Instance.Start( "Sync Card Pos" );

                        //Console.WriteLine( "Syncing Card Positions Right" );

                        //Console.WriteLine( "Before CenterCard: {0}, RightCard: {1}", CenterCard.Position, RightCard.Position );

                        ViewingIndex = ViewingIndex + 1;
                        ViewingIndexChangedDelegate( ViewingIndex );

                        // reset the card positions, creating the illusion that the cards really moved
                        SubLeftCard.Position = new PointF( deltaRightX + SubLeftPos.X, LeftPos.Y );
                        LeftCard.Position = new PointF( deltaRightX + LeftPos.X, LeftPos.Y );
                        CenterCard.Position = new PointF( deltaRightX + CenterPos.X, CenterPos.Y );
                        RightCard.Position = new PointF( deltaRightX + RightPos.X, RightPos.Y );
                        PostRightCard.Position = new PointF( deltaRightX + PostRightPos.X, RightPos.Y );
                        //Rock.Mobile.Profiler.Instance.Stop( "Update Positions?" );

                        Rock.Mobile.Profiler.Instance.Stop( "Sync Card Pos" );

                        //Console.WriteLine( "After CenterCard: {0}, RightCard: {1}", CenterCard.Position, RightCard.Position );

                    }
                }
                // if we're panning RIGHT, that means the left hand card might be in range to sync
                else if( System.Math.Abs( deltaLeftX ) < tolerance && PanDir == 1)
                {
                    if( ViewingIndex - 1 >= 0 )
                    {
                        Rock.Mobile.Profiler.Instance.Start( "Sync Card Pos" );

                        //Console.WriteLine( "Syncing Card Positions Left" );

                        //Console.WriteLine( "Before CenterCard: {0}, LeftCard: {1}", CenterCard.Position, LeftCard.Position );

                        ViewingIndex = ViewingIndex - 1;
                        ViewingIndexChangedDelegate( ViewingIndex );

                        // reset the card positions, creating the illusion that the cards really moved
                        SubLeftCard.Position = new PointF( deltaLeftX + SubLeftPos.X, LeftPos.Y );
                        LeftCard.Position = new PointF( deltaLeftX + LeftPos.X, LeftPos.Y );
                        CenterCard.Position = new PointF( deltaLeftX + CenterPos.X, CenterPos.Y );
                        RightCard.Position = new PointF( deltaLeftX + RightPos.X, RightPos.Y );
                        PostRightCard.Position = new PointF( deltaLeftX + PostRightPos.X, RightPos.Y );

                        Rock.Mobile.Profiler.Instance.Stop( "Sync Card Pos" );

                        //Console.WriteLine( "After CenterCard: {0}, LeftCard: {1}", CenterCard.Position, LeftCard.Position );
                    }
                }
            }

            /// <summary>
            /// Helper to get the distance from the center
            /// </summary>
            /// <returns>The dist from center.</returns>
            float CardDistFromCenter( PlatformView card )
            {
                float cardHalfWidth = CardWidth / 2;
                return ( card.Position.X + cardHalfWidth ) - (CenterPos.X + cardHalfWidth);
            }

            void AnimateCardsToNeutral( )
            {
                // this will animate each card to its neutral resting point
                Animating = true;

                AnimateCard( SubLeftCard.PlatformNativeObject, "SubLeftCard", SubLeftCard.Position, SubLeftPos, AnimationDuration, this );
                AnimateCard( LeftCard.PlatformNativeObject, "LeftCard", LeftCard.Position, LeftPos, AnimationDuration, this );
                AnimateCard( CenterCard.PlatformNativeObject, "CenterCard", CenterCard.Position, CenterPos, AnimationDuration, this );
                AnimateCard( RightCard.PlatformNativeObject, "RightCard", RightCard.Position, RightPos, AnimationDuration, this );
                AnimateCard( PostRightCard.PlatformNativeObject, "PostRightCard", PostRightCard.Position, PostRightPos, AnimationDuration, this );
            }
        }
    }
}
