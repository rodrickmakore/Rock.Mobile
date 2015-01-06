#if __ANDROID__
using System;

// This file is contains animation classes that should only be used in Android specific code.
using Android.Graphics;
using Java.IO;
using System.Collections.Generic;
using Android.Widget;
using Android.Content;
using Android.Views;
using Rock.Mobile.PlatformUI;
using Android.Animation;

namespace Rock.Mobile.PlatformSpecific.Android.Animation
{
    /// <summary>
    /// An animator that will animate a float from start to end along duration, 
    /// and provides optional update and completion callbacks
    /// </summary>
    public class SimpleAnimatorFloat : SimpleAnimator
    {
        public SimpleAnimatorFloat( float start, float end, long durationMS, AnimationUpdate updateDelegate, AnimationComplete completeDelegate )
        {
            // create the type-specific animator
            Animator = ValueAnimator.OfFloat( start, end );

            Init( start, end, durationMS, updateDelegate, completeDelegate );
        }

        public override void OnAnimationUpdate(ValueAnimator animation)
        {
            // get the current value and provide it to the caller
            if ( AnimationUpdateDelegate != null )
            {
                float value = ((Java.Lang.Float)animation.GetAnimatedValue("")).FloatValue();
                AnimationUpdateDelegate( System.Math.Min( (float)animation.CurrentPlayTime / (float)animation.Duration, 1.00f ), value );
            }
        }

        public override void OnAnimationEnd(Animator animation)
        {
            if ( AnimationCompleteDelegate != null )
            {
                AnimationCompleteDelegate( );
            }
        }
    }

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

        protected void Init( float start, float end, long duration, AnimationUpdate updateDelegate, AnimationComplete completeDelegate )
        {
            Animator.AddUpdateListener( this );
            Animator.AddListener( this );
            Animator.SetDuration( duration );

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

        public abstract void OnAnimationUpdate(ValueAnimator animation);
        public abstract void OnAnimationEnd(Animator animation);

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
