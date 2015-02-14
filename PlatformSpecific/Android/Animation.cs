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
    public class SimpleAnimator_Color : SimpleAnimator
    {
        uint StartR { get; set; }
        uint StartG { get; set; }
        uint StartB { get; set; }
        uint StartA { get; set; }

        int DeltaR { get; set; }
        int DeltaG { get; set; }
        int DeltaB { get; set; }
        int DeltaA { get; set; }

        public SimpleAnimator_Color( uint start, uint end, float duration, AnimationUpdate updateDelegate, AnimationComplete completeDelegate )
        {
            StartR = (start & 0xFF000000) >> 24;
            StartG = (start & 0x00FF0000) >> 16;
            StartB = (start & 0x0000FF00) >> 8;
            StartA = (start & 0xFF);

            uint endR = (end & 0xFF000000) >> 24;
            uint endG = (end & 0x00FF0000) >> 16;
            uint endB = (end & 0x0000FF00) >> 8;;
            uint endA = (end & 0xFF);

            DeltaR = (int) (endR - StartR);
            DeltaG = (int) (endG - StartG);
            DeltaB = (int) (endB - StartB);
            DeltaA = (int) (endA - StartA);

            Init( duration, updateDelegate, completeDelegate );
        }

        protected override void AnimTick(float percent, AnimationUpdate updateDelegate)
        {
            // get the current value and provide it to the caller

            // cast to int so we don't lose the sign when adding a negative delta
            uint currR = (uint) ((int)StartR + (int) ( (float)DeltaR * percent ));
            uint currG = (uint) ((int)StartG + (int) ( (float)DeltaG * percent ));
            uint currB = (uint) ((int)StartB + (int) ( (float)DeltaB * percent ));
            uint currA = (uint) ((int)StartA + (int) ( (float)DeltaA * percent ));

            uint value = currR << 24 | currG << 16 | currB << 8 | currA;

            if ( updateDelegate != null )
            {
                updateDelegate( percent, value );
            }
        }
    }

    public class SimpleAnimator_SizeF : SimpleAnimator
    {
        System.Drawing.SizeF StartValue { get; set; }
        System.Drawing.SizeF Delta { get; set; }

        public SimpleAnimator_SizeF( System.Drawing.SizeF start, System.Drawing.SizeF end, float duration, AnimationUpdate updateDelegate, AnimationComplete completeDelegate )
        {
            StartValue = start;
            Delta = new System.Drawing.SizeF( end.Width - start.Width, end.Height - start.Height );

            Init( duration, updateDelegate, completeDelegate );
        }

        protected override void AnimTick(float percent, AnimationUpdate updateDelegate)
        {
            // get the current value and provide it to the caller
            if ( AnimationUpdateDelegate != null )
            {
                System.Drawing.SizeF value = new System.Drawing.SizeF( StartValue.Width + (Delta.Width * percent), StartValue.Height + (Delta.Height * percent) );

                if ( updateDelegate != null )
                {
                    updateDelegate( percent, value );
                }
            }
        }
    }

    /// <summary>
    /// An animator that will animate a float from start to end along duration, 
    /// and provides optional update and completion callbacks
    /// </summary>
    public class SimpleAnimator_Float : SimpleAnimator
    {
        float StartValue { get; set; }
        float EndValue { get; set; }

        public SimpleAnimator_Float( float start, float end, float duration, AnimationUpdate updateDelegate, AnimationComplete completeDelegate )
        {
            StartValue = start;
            EndValue = end;

            Init( duration, updateDelegate, completeDelegate );
        }

        protected override void AnimTick(float percent, AnimationUpdate updateDelegate)
        {
            float value = StartValue + ((EndValue - StartValue) * percent);

            if ( updateDelegate != null )
            {
                updateDelegate( percent, value );
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
