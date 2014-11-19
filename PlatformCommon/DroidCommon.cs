#if __ANDROID__
using System;

// This file is where you can put anything SPECIFIC to Android that doesn't 
// require common base classes, and should be DYE-RECTLY referenced by Android code.
using Android.Graphics;
using Java.IO;
using System.Collections.Generic;
using Android.Widget;
using Android.Content;

namespace Rock.Mobile
{
    namespace PlatformCommon
    {
        /// <summary>
        /// Subclass ImageView so we can override OnMeasure and scale up the image 
        /// maintaining aspect ratio
        /// </summary>
        public class DroidScaledImageView : ImageView
        {
            public DroidScaledImageView( Context context ) : base( context )
            {
            }

            protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
            {
                if ( Drawable != null )
                {
                    int width = MeasureSpec.GetSize( widthMeasureSpec );
                    int height = (int)System.Math.Ceiling( width * ( (float)Drawable.IntrinsicHeight / (float)Drawable.IntrinsicWidth ) );

                    SetMeasuredDimension( width, height );
                }
                else
                {
                    base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
                }
            }
        }

        /// <summary>
        /// Simple font manager so we lookup that stores fonts as we create them.
        /// That way we aren't creating new fonts for every singe label. It saves memory
        /// and speeds up our load times a lot.s
        /// </summary>
        public class DroidFontManager
        {
            static DroidFontManager _Instance = new DroidFontManager( );
            public static DroidFontManager Instance { get { return _Instance; } }

            class FontFace
            {
                public Typeface Font { get; set; }
                public string Name { get; set; }
            }

            List<FontFace> FontList { get; set; }

            public DroidFontManager( )
            {
                FontList = new List<FontFace>( );
            }

            public Typeface GetFont( string fontName )
            {
                FontFace fontFace = FontList.Find( f => f.Name == fontName );
                if( fontFace == null )
                {
                    fontFace = new FontFace()
                        {
                            Name = fontName,
                            Font = Typeface.CreateFromAsset( Rock.Mobile.PlatformCommon.Droid.Context.Assets, "Fonts/" + fontName + ".ttf" )
                        };

                    FontList.Add( fontFace );
                }

                return fontFace.Font;
            }
        }

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
