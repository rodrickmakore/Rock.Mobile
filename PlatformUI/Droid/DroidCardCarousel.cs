#if __ANDROID__
using System;
using System.Drawing;
using Android.Views;
using Android.Widget;
using Android.Animation;
using System.Collections.Generic;

namespace Rock.Mobile
{
    namespace PlatformUI
    {
        /// <summary>
        /// Platform Specific implementations of the carousel need only to manage
        /// the animation requests made by PlatformCarousel, since we dont have a platform agnostic
        /// implementation of animation systems.
        /// </summary>
        public class DroidCardCarousel : PlatformCardCarousel
        {
            /// <summary>
            /// Manages forwarding gestures to the carousel
            /// </summary>
            public class CarouselGestureDetector : GestureDetector.SimpleOnGestureListener
            {
                public DroidCardCarousel Parent { get; set; }

                public CarouselGestureDetector( DroidCardCarousel parent )
                {
                    Parent = parent;
                }

                public override bool OnDown(MotionEvent e)
                {
                    // Unlike iOS, THIS will handle TouchesBegan rather than the user of the carousel
                    Parent.TouchesBegan( );
                    return true;
                }

                public override bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
                {
                    Parent.OnFling( e1, e2, velocityX, velocityY );
                    return base.OnFling(e1, e2, velocityX, velocityY);
                }

                public override bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
                {
                    Parent.OnScroll( e1, e2, distanceX, distanceY );
                    return base.OnScroll(e1, e2, distanceX, distanceY);
                }
            }

            /// <summary>
            /// Forwards the finished animation notification
            /// </summary>
            public class CarouselAnimationListener : Android.Animation.AnimatorListenerAdapter, Android.Animation.ValueAnimator.IAnimatorUpdateListener
            {
                public DroidCardCarousel Parent { get; set; }

                public override void OnAnimationEnd(Animator animation)
                {
                    base.OnAnimationEnd(animation);

                    // forward on this message to our parent
                    Parent.OnAnimationEnd( animation );
                }

                public void OnAnimationUpdate(ValueAnimator animation)
                {
                    // update the container position
                    Parent.OnAnimationUpdate( animation );
                }
            }

            /// <summary>
            /// Subclass value animator so we can track what card it's animating.
            /// </summary>
            public class CardValueAnimator : ValueAnimator
            {
                public View Card { get; set; }
            }
            List<CardValueAnimator> ActiveAnimators { get; set; }

            bool IsPanning { get; set; }
            PointF AbsolutePosition { get; set; }

            public override void TouchesBegan( )
            {
                Console.WriteLine( "OnDown" );

                foreach(CardValueAnimator animator in ActiveAnimators )
                {
                    animator.Cancel( );
                }
                ActiveAnimators.Clear( );

                IsPanning = false;
            }

            public override void TouchesEnded( )
            {
                if( IsPanning == true )
                {
                    Console.WriteLine( "Was panning. Don't call base.TouchesEnded" );
                    IsPanning = false;
                    base.OnPanGesture( PanGestureState.Ended, new PointF( 0, 0 ), new PointF( 0, 0 ) );
                }
                else
                {
                    Console.WriteLine( "TouchesEnded" );
                    base.TouchesEnded();
                }
            }

            public void OnFling( MotionEvent e1, MotionEvent e2, float velocityX, float velocityY )
            {
                IsPanning = true;
            }

            public void OnScroll( MotionEvent e1, MotionEvent e2, float distanceX, float distanceY )
            {
                // flip X so it's consistent with ios, where right is positive.
                distanceX = -distanceX;
                IsPanning = true;

                base.OnPanGesture( PanGestureState.Changed, new PointF( distanceX < 0 ? -1 : 1, 0 ), new PointF( distanceX, distanceY ) );
            }

            public GestureDetector GestureDetector { get; set; }

            public DroidCardCarousel( float cardWidth, float cardHeight, RectangleF boundsInParent, ViewingIndexChanged changedDelegate ) : base( cardWidth, cardHeight, boundsInParent, changedDelegate )
            {
                ActiveAnimators = new List<CardValueAnimator>( );
            }

            public override void Init(object parentView)
            {
                base.Init(parentView);

                RelativeLayout droidParentView = parentView as RelativeLayout;

                SubLeftCard.AddAsSubview( droidParentView );
                LeftCard.AddAsSubview( droidParentView );
                CenterCard.AddAsSubview( droidParentView );
                RightCard.AddAsSubview( droidParentView );
                PostRightCard.AddAsSubview( droidParentView );

                GestureDetector = new GestureDetector( Rock.Mobile.PlatformCommon.Droid.Context, new CarouselGestureDetector( this ) );
            }

            /// <summary>
            /// Animates a card from startPos to endPos over time
            /// </summary>
            protected override void AnimateCard( object platformObject, string animName, PointF startPos, PointF endPos, float duration, PlatformCardCarousel parentDelegate )
            {
                // setup an animation from our current mask scale to the new one.
                CardValueAnimator animator = new CardValueAnimator();
                animator.SetIntValues( (int)startPos.X, (int)endPos.X );

                CarouselAnimationListener listener = new CarouselAnimationListener( ) { Parent = this } ;

                animator.AddUpdateListener( listener );
                animator.AddListener(listener  );
                animator.SetDuration( 500 );
                animator.Card = platformObject as View;

                animator.Start();

                ActiveAnimators.Add( animator );
            }

            public void OnAnimationUpdate(ValueAnimator animation)
            {
                int xPos = ((Java.Lang.Integer)animation.GetAnimatedValue("")).IntValue();

                CardValueAnimator cardAnimator = animation as CardValueAnimator;
                cardAnimator.Card.SetX( xPos );
            }

            /// <summary>
            /// Called when card movement is complete.
            /// </summary>
            public void OnAnimationEnd(Animator animation)
            {
                Animating = false;
                Console.WriteLine( "Animation Stopped" );
            }
        }
    }
}
#endif
