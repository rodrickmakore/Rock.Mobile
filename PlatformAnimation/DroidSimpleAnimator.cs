#if __ANDROID__
using System;
using Android.Animation;

namespace Rock.Mobile.Animation
{
    /// <summary>
    /// The base implementation for our animators
    /// </summary>
    public abstract class SimpleAnimator : Java.Lang.Object, global::Android.Animation.ValueAnimator.IAnimatorUpdateListener, global::Android.Animation.ValueAnimator.IAnimatorListener
    {
        public delegate void AnimationUpdate( float percent, object value );
        public delegate void AnimationComplete( );

        protected ValueAnimator Animator { get; set; }
        protected AnimationUpdate AnimationUpdateDelegate;
        protected AnimationComplete AnimationCompleteDelegate;

        protected void Init( float duration, AnimationUpdate updateDelegate, AnimationComplete completeDelegate )
        {
            Animator = ValueAnimator.OfFloat( 0.00f, 1.00f );

            Animator.AddUpdateListener( this );
            Animator.AddListener( this );

            // convert duration to milliseconds
            Animator.SetDuration( (int) (duration * 1000.0f) );

            AnimationUpdateDelegate = updateDelegate;
            AnimationCompleteDelegate = completeDelegate;
        }

        public void Start( )
        {
            if ( Animator != null )
            {
                Animator.Start( );
            }
        }

        protected abstract void AnimTick( float percent, AnimationUpdate updateDelegate );

        public void OnAnimationUpdate(ValueAnimator animation)
        {
            float percent = System.Math.Min( (float)animation.CurrentPlayTime / (float)animation.Duration, 1.00f );
            AnimTick( percent, AnimationUpdateDelegate );
        }

        public void OnAnimationEnd(Animator animation)
        {
            if ( AnimationCompleteDelegate != null )
            {
                AnimationCompleteDelegate( );
            }
        }

        public void OnAnimationStart(Animator animation)
        {
        }

        public void OnAnimationRepeat(Animator animation)
        {
        }

        public void OnAnimationCancel(Animator animation)
        {
        }
    }
}
#endif
