#if __IOS__
using System;
using Foundation;
using System.Drawing;

namespace Rock.Mobile.PlatformSpecific.iOS.Animation
{
    public class SimpleAnimator_Float : SimpleAnimator
    {
        float StartValue { get; set; }
        float Delta { get; set; }

        public SimpleAnimator_Float( float start, float end, float duration, AnimationUpdate updateDelegate, AnimationComplete completionDelegate )
        {
            Init( duration, updateDelegate, completionDelegate );

            StartValue = start;
            Delta = end - start;
        }

        protected override void AnimTick( float percent, AnimationUpdate updateDelegate )
        {
            float currValue = StartValue + ( Delta * percent );
            updateDelegate( percent, currValue );
        }
    }

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

        public SimpleAnimator_Color( uint start, uint end, float duration, AnimationUpdate updateDelegate, AnimationComplete completionDelegate )
        {
            Init( duration, updateDelegate, completionDelegate );

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
        }

        protected override void AnimTick( float percent, AnimationUpdate updateDelegate )
        {
            // cast to int so we don't lose the sign when adding a negative delta
            uint currR = (uint) ((int)StartR + (int) ( (float)DeltaR * percent ));
            uint currG = (uint) ((int)StartG + (int) ( (float)DeltaG * percent ));
            uint currB = (uint) ((int)StartB + (int) ( (float)DeltaB * percent ));
            uint currA = (uint) ((int)StartA + (int) ( (float)DeltaA * percent ));

            uint currValue = currR << 24 | currG << 16 | currB << 8 | currA;

            updateDelegate( percent, currValue );
        }
    }

    public class SimpleAnimator_SizeF : SimpleAnimator
    {
        SizeF StartValue { get; set; }
        SizeF Delta { get; set; }

        public SimpleAnimator_SizeF( SizeF start, SizeF end, float duration, AnimationUpdate updateDelegate, AnimationComplete completionDelegate )
        {
            Init( duration, updateDelegate, completionDelegate );

            StartValue = start;
            Delta = new SizeF( end.Width - start.Width, end.Height - start.Height );
        }

        protected override void AnimTick( float percent, AnimationUpdate updateDelegate )
        {
            SizeF currValue = new SizeF( StartValue.Width + ( Delta.Width * percent ), StartValue.Height + ( Delta.Height * percent ) );
            updateDelegate( percent, currValue );
        }
    }

    public abstract class SimpleAnimator
    {
        // This defines the rate we wish to update animations at, which happens to be 60fps.
        static float ANIMATION_TICK_RATE = (1.0f / 60.0f); 

        // This is the frequency at which we'll get an animation callback. It's basically 60fps, but needs to be in hundredth-nanoseconds
        // Tick Rate - 60fps
        // Tick Rate in ms - 0.016ms
        // Tick Rate in hundredth nanoseconds = 166666
        static float MILLISECONDS_TO_HUNDREDTH_NANOSECONDS = 10000.0f;
        static long ANIMATION_TICK_FREQUENCY = (long) ((ANIMATION_TICK_RATE * 1000) * MILLISECONDS_TO_HUNDREDTH_NANOSECONDS);

        float DurationTicksPerSec { get; set; }
        float CurrentTime { get; set; }

        NSTimer AnimTimer = null;

        public delegate void AnimationUpdate( float percent, object value );
        public delegate void AnimationComplete( );

        protected abstract void AnimTick( float percent, AnimationUpdate updateDelegate );

        protected void Init( float durationSeconds, AnimationUpdate updateDelegate, AnimationComplete completeDelegate )
        {
            // create our timer at 60hz
            AnimTimer = NSTimer.CreateRepeatingTimer( new TimeSpan( ANIMATION_TICK_FREQUENCY ), new Action<NSTimer>( 
                    delegate
                    {
                        // update our timer
                        CurrentTime += ANIMATION_TICK_RATE;

                        // let the animation implementation do what it needs to
                        AnimTick( System.Math.Min( CurrentTime / durationSeconds, 1.00f ), updateDelegate );
                        
                        // see if we're finished.
                        if( CurrentTime >= durationSeconds )
                        {
                            // we are, so notify the completion delegate
                            completeDelegate( );

                            // and kill the timer
                            AnimTimer.Invalidate( );
                        }
                    } )
            );
        }

        public void Start( )
        {
            // launch the timer
            NSRunLoop.Current.AddTimer( AnimTimer, NSRunLoop.NSDefaultRunLoopMode );
        }

    }
}
#endif
