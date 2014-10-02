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

            // Utility functions for bitmap loading / masking
            public static Bitmap LoadImageAtSize( File imagePath, int requiredWidth, int requiredHeight )
            {
                // If they want to maintain aspect ratio, get just the dimensions
                BitmapFactory.Options options = new BitmapFactory.Options { InJustDecodeBounds = true };

                BitmapFactory.DecodeFile( imagePath.AbsolutePath, options );

                // Now maintain aspect ratio and shrink down the larger dimension
                int inSampleSize = CalcSampleSize( options.OutWidth, options.OutHeight, requiredWidth, requiredHeight );

                // Now we will load the image and have BitmapFactory resize it for us.
                options.InSampleSize = inSampleSize;
                options.InJustDecodeBounds = false;
                return BitmapFactory.DecodeFile( imagePath.AbsolutePath, options );
            }

            public static Bitmap LoadImageAtSize( int resourceId, int requiredWidth, int requiredHeight )
            {
                // If they want to maintain aspect ratio, get just the dimensions
                BitmapFactory.Options options = new BitmapFactory.Options { InJustDecodeBounds = true };

                BitmapFactory.DecodeResource( Context.Resources, resourceId, options );

                // Now maintain aspect ratio and shrink down the larger dimension
                int inSampleSize = CalcSampleSize( options.OutWidth, options.OutHeight, requiredWidth, requiredHeight );

                // Now we will load the image and have BitmapFactory resize it for us.
                options.InSampleSize = inSampleSize;
                options.InJustDecodeBounds = false;
                return BitmapFactory.DecodeResource( Context.Resources, resourceId, options );
            }

            static int CalcSampleSize( int nativeWidth, int nativeHeight, int requiredWidth, int requiredHeight )
            {
                if ( nativeHeight > requiredHeight || nativeWidth > requiredWidth )
                {
                    return nativeWidth > nativeHeight ? nativeHeight / requiredHeight : nativeWidth / requiredWidth;
                }

                return 1;
            }

            public static Bitmap ApplyMaskToBitmap( Bitmap image, Bitmap mask )
            {
                // create a bitmap that will be our result
                Bitmap result = Bitmap.CreateBitmap( mask.Width, mask.Height, Bitmap.Config.Argb8888 );

                // create a canvas and render the image
                Canvas canvas = new Canvas( result );
                canvas.DrawBitmap( image, 0, 0, null );

                // Render our mask with a paint that Xor's out the blank area of the mask (showing the underlying pic)
                Paint paint = new Paint( PaintFlags.AntiAlias );
                paint.SetXfermode( new PorterDuffXfermode( PorterDuff.Mode.DstOut ) );
                canvas.DrawBitmap( mask, 0, 0, paint );

                return result;
            }
        }
    }
}
#endif
