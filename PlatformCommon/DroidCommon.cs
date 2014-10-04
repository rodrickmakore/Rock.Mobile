#if __ANDROID__
using System;

// This file is where you can put anything SPECIFIC to Android that doesn't 
// require common base classes, and should be DYE-RECTLY referenced by Android code.
using Android.Graphics;
using Java.IO;

namespace Rock.Mobile
{
    namespace PlatformCommon
    {
        public class Droid
        {
            // beeee sure to set this for android!
            public static Android.Content.Context Context = null;

            public static Bitmap ApplyMaskToBitmap( Bitmap image, Bitmap mask )
            {
                // create a bitmap that will be our result
                Bitmap result = Bitmap.CreateBitmap( image.Width, image.Height, Bitmap.Config.Argb8888 );

                // create a canvas and render the image
                //Canvas canvas = new Canvas( result );
                using( Canvas canvas = new Canvas( result ) )
                {
                    canvas.DrawBitmap( image, 0, 0, null );

                    // Render our mask with a paint that Xor's out the blank area of the mask (showing the underlying pic)
                    //Paint paint = new Paint( PaintFlags.AntiAlias );
                    using( Paint paint = new Paint( PaintFlags.AntiAlias ) )
                    {
                        paint.SetXfermode( new PorterDuffXfermode( PorterDuff.Mode.DstOut ) );
                        canvas.DrawBitmap( mask, 0, 0, paint );
                    }
                }

                return result;
            }
        }
    }
}
#endif
