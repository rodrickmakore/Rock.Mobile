#if __ANDROID__
using System;

// This file is where you can put anything SPECIFIC to Android that doesn't 
// require common base classes, and should be DYE-RECTLY referenced by Android code.
using Android.Graphics;
using Java.IO;
using System.Collections.Generic;
using Android.Widget;
using Android.Content;
using Android.Views;
using Rock.Mobile.PlatformUI;

namespace Rock.Mobile
{
    namespace PlatformCommon
    {
        class MaskLayer : View
        {
            /// <summary>
            /// Represents the bitmap that contains the fullscreen mask with a cutout for the "masked" portion
            /// </summary>
            /// <value>The masked cutout.</value>
            Bitmap Layer { get; set; }

            System.Drawing.SizeF AlphaMask { get; set; }

            /// <summary>
            /// The opacity of the layered region
            /// </summary>
            /// <value>The opacity.</value>
            int _Opacity;
            public float Opacity
            { 
                get
                {
                    return (float)  _Opacity / 255.0f;
                }

                set
                {
                    _Opacity = (int)( value * 255.0f );
                }
            }

            PointF _Position;
            public PointF Position
            {
                get
                {
                    return _Position;
                }
                set
                {
                    _Position = value;
                    Invalidate( );
                }
            }

            public MaskLayer( int layerWidth, int layerHeight, int maskWidth, int maskHeight, Context context ) : base( context )
            {
                Position = new PointF( );
                Opacity = 1.00f;

                // first create the full layer
                Layer = Bitmap.CreateBitmap( layerWidth, layerHeight, Bitmap.Config.Alpha8 );
                Layer.EraseColor( Color.Black );

                // now define the mask portion
                AlphaMask = new System.Drawing.SizeF( maskWidth, maskHeight );
            }

            protected override void OnDraw(Canvas canvas)
            {
                // Note: The reason we do it like this, so simply, is that there's a huge performanceh it to rendering into our own canvas. I'm not sure why,
                // because i haven't taken the time to R&D it, but I think it has something to do with the canvas they provide being GPU based and mine being on the CPU, or
                // at least causing the GPU texture to have to be flushed and re-DMA'd.

                // Source is what this MaskLayer contains and will draw into canvas.
                // Destination is the buffer IN canvas
                using( Paint paint = new Paint( ) )
                {
                    paint.Alpha = _Opacity;

                    // set a clipping region that excludes the alpha mask region
                    canvas.ClipRect( new Rect( (int)Position.X, (int)Position.Y, (int)Position.X + (int)AlphaMask.Width, (int)Position.Y + (int)AlphaMask.Height ), Region.Op.Xor );

                    // and render the full image into the canvas (which will have the effect of masking only the area we care about
                    canvas.DrawBitmap( Layer, 0, 0, paint );
                }
            }
        }

        public class CircleView : View
        {
            public float Radius { get; set; }

            float _StrokeWidth;
            public float StrokeWidth
            {
                get
                {
                    return _StrokeWidth;
                }
                set
                {
                    _StrokeWidth = value;
                    UpdatePaint( );
                }
            }

            Color _Color;
            public Color Color
            {
                get
                {
                    return _Color;
                }
                set
                {
                    _Color = value;
                    UpdatePaint( );
                }
            }

            Android.Graphics.Paint.Style _Style;
            public Android.Graphics.Paint.Style Style
            {
                get
                {
                    return _Style;
                }
                set
                {
                    _Style = value;
                    UpdatePaint( );
                }
            }

            Paint Paint { get; set; }

            void UpdatePaint( )
            {
                Paint.SetStyle( Style );
                Paint.Color = Color;
                Paint.StrokeWidth = PlatformBaseUI.UnitToPx( StrokeWidth );
            }

            public CircleView( Android.Content.Context c ) : base( c )
            {
                Paint = new Paint();

                // default the style to stroke only
                Style = Android.Graphics.Paint.Style.Stroke;

                UpdatePaint( );
            }

            protected override void OnDraw(Canvas canvas)
            {
                base.OnDraw( canvas );

                // center the drawing area
                float xPos = canvas.Width / 2;
                float yPos = canvas.Height / 2;

                // and render
                canvas.DrawCircle( xPos, yPos, PlatformBaseUI.UnitToPx( Radius ), Paint );
            }
        }

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

            public static Bitmap ApplyMaskToBitmap( Bitmap image, Bitmap mask, int x, int y )
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
                        canvas.DrawBitmap( mask, x, y, paint );
                    }
                }

                return result;
            }
        }
    }
}
#endif
