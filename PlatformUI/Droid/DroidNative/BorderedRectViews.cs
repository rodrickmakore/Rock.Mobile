#if __ANDROID__
using System;
using Android.Graphics;
using Android.Graphics.Drawables.Shapes;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;
using Android.Views;

namespace Rock.Mobile
{
    namespace PlatformUI
    {
        namespace DroidNative
        {
            /// <summary>
            /// Special drawable that manages both an outer and inner paint.
            /// Designed for use only with BorderedRectTextView and BorderedRectTextField
            /// </summary>
            internal class BorderedRectPaintDrawable : PaintDrawable
            {
                /// <summary>
                /// The paint to use for rendering the border
                /// </summary>
                /// <value>The border paint.</value>
                public Paint BorderPaint { get; set; }

                /// <summary>
                /// The width of the border in DP units.
                /// </summary>
                public float _BorderWidthDP;

                /// <summary>
                /// The actual width of the border in converted pixel unites
                /// </summary>
                public float _BorderWidth;

                /// <summary>
                /// Property for managing the border width, including conversion from DP to pixel
                /// </summary>
                /// <value>The width of the border.</value>
                public float BorderWidth 
                { 
                    get 
                    {
                        return _BorderWidthDP;
                    }
                    set
                    {
                        _BorderWidthDP = value;
                        _BorderWidth = TypedValue.ApplyDimension(ComplexUnitType.Dip, value, Rock.Mobile.PlatformCommon.Droid.Context.Resources.DisplayMetrics);
                    }
                }

                /// <summary>
                /// The radius in DP units.
                /// </summary>
                protected float _RadiusDP;

                /// <summary>
                /// The actual radius in converted pixel unites
                /// </summary>
                protected float _Radius;

                /// <summary>
                /// Property for managing the radius, including converstion from DP to pixel
                /// </summary>
                /// <value>The radius.</value>
                public float Radius 
                { 
                    get 
                    { 
                        return _RadiusDP; 
                    } 

                    set
                    {
                        // first store the radius in DP value in case we must later return it
                        _RadiusDP = value;

                        // convert to pixels
                        _Radius = TypedValue.ApplyDimension(ComplexUnitType.Dip, value, Rock.Mobile.PlatformCommon.Droid.Context.Resources.DisplayMetrics);

                        // create a shape with the new radius
                        Shape = new RoundRectShape( new float[] { _Radius, 
                            _Radius, 
                            _Radius, 
                            _Radius, 
                            _Radius, 
                            _Radius, 
                            _Radius, 
                            _Radius }, null, null );
                    }
                }

                public BorderedRectPaintDrawable( ) : base( )
                {
                    BorderPaint = new Paint( );
                }

                protected override void OnDraw( Shape shape, Canvas canvas, Paint paint )
                {
                    // Render the 'border'
                    base.OnDraw( shape, canvas, BorderPaint );

                    // Render the 'fill'
                    float xOffset = _BorderWidth;
                    float yOffset = _BorderWidth;

                    canvas.Translate( xOffset, yOffset );

                    // shrink it down
                    shape.Resize( shape.Width - (_BorderWidth * 2), shape.Height - (_BorderWidth * 2) );

                    // render
                    base.OnDraw( shape, canvas, paint );

                    // restore the original size
                    shape.Resize( shape.Width + (_BorderWidth * 2), shape.Height + (_BorderWidth * 2) );
                }
            }

            public class BorderedRectTextView : TextView
            {
                BorderedRectPaintDrawable BorderedPaintDrawable { get; set; }

                public float BorderWidth 
                { 
                    get { return BorderedPaintDrawable.BorderWidth; } 
                    set { BorderedPaintDrawable.BorderWidth = value; }
                }

                public float Radius
                {
                    get { return BorderedPaintDrawable.Radius; }
                    set { BorderedPaintDrawable.Radius = value; }
                }

                public BorderedRectTextView( Android.Content.Context context ) : base( context )
                {
                    // create our special bordered rect
                    BorderedPaintDrawable = new BorderedRectPaintDrawable( );
                    BorderedPaintDrawable.Paint.SetStyle( Android.Graphics.Paint.Style.Fill );
                    BorderedPaintDrawable.BorderPaint = new Paint( BorderedPaintDrawable.Paint );
                    SetBackgroundDrawable( BorderedPaintDrawable );
                }

                public override void SetBackgroundColor( Android.Graphics.Color color )
                {
                    // put the color in the regular 'paint', which is really our fill color,
                    // but to the end user is the background color.
                    BorderedPaintDrawable.Paint.Color = color;
                }

                public void SetBorderColor( Android.Graphics.Color color )
                {
                    // set the color of the border paint, which is the paint used
                    // for our border outline
                    BorderedPaintDrawable.BorderPaint.Color = color;
                }

                protected override void OnDraw(Android.Graphics.Canvas canvas)
                {
                    BorderedRectPaintDrawable paintDrawable = Background as BorderedRectPaintDrawable;
                    if( paintDrawable != null )
                    {
                        float borderWidth = TypedValue.ApplyDimension(ComplexUnitType.Dip, paintDrawable.BorderWidth, Rock.Mobile.PlatformCommon.Droid.Context.Resources.DisplayMetrics);
                        canvas.Translate( borderWidth, borderWidth );
                    }

                    base.OnDraw(canvas);
                }

                // hide the base methods for measurement so we can apply border dimensions
                public new int MeasuredWidth { get; set; }
                public new int MeasuredHeight { get; set; }

                public new void Measure( int widthMeasureSpec, int heightMeasureSpec )
                {
                    base.Measure( widthMeasureSpec, heightMeasureSpec );

                    // now adjust for the border
                    float borderSize = TypedValue.ApplyDimension(ComplexUnitType.Dip, BorderWidth, Rock.Mobile.PlatformCommon.Droid.Context.Resources.DisplayMetrics);

                    MeasuredWidth = base.MeasuredWidth + (int)(borderSize * 2);
                    MeasuredHeight = base.MeasuredHeight + (int)(borderSize * 2);
                }
            }


            public class BorderedRectEditText : EditText
            {
                BorderedRectPaintDrawable BorderedPaintDrawable { get; set; }

                public float BorderWidth 
                { 
                    get { return BorderedPaintDrawable.BorderWidth; } 
                    set { BorderedPaintDrawable.BorderWidth = value; }
                }

                public float Radius
                {
                    get { return BorderedPaintDrawable.Radius; }
                    set { BorderedPaintDrawable.Radius = value; }
                }

                public BorderedRectEditText( Android.Content.Context context ) : base( context )
                {
                    // create our special bordered rect
                    BorderedPaintDrawable = new BorderedRectPaintDrawable( );
                    BorderedPaintDrawable.Paint.SetStyle( Android.Graphics.Paint.Style.Fill );
                    BorderedPaintDrawable.BorderPaint = new Paint( BorderedPaintDrawable.Paint );
                    SetBackgroundDrawable( BorderedPaintDrawable );
                }

                public override void SetBackgroundColor( Android.Graphics.Color color )
                {
                    // put the color in the regular 'paint', which is really our fill color,
                    // but to the end user is the background color.
                    BorderedPaintDrawable.Paint.Color = color;
                }

                public void SetBorderColor( Android.Graphics.Color color )
                {
                    // set the color of the border paint, which is the paint used
                    // for our border outline
                    BorderedPaintDrawable.BorderPaint.Color = color;
                }

                protected override void OnDraw(Android.Graphics.Canvas canvas)
                {
                    BorderedRectPaintDrawable paintDrawable = Background as BorderedRectPaintDrawable;
                    if( paintDrawable != null )
                    {
                        float borderWidth = TypedValue.ApplyDimension(ComplexUnitType.Dip, paintDrawable.BorderWidth, Rock.Mobile.PlatformCommon.Droid.Context.Resources.DisplayMetrics);
                        canvas.Translate( borderWidth, borderWidth );
                    }

                    base.OnDraw(canvas);
                }

                // hide the base methods for measurement so we can apply border dimensions
                public new int MeasuredWidth { get; set; }
                public new int MeasuredHeight { get; set; }

                public new void Measure( int widthMeasureSpec, int heightMeasureSpec )
                {
                    base.Measure( widthMeasureSpec, heightMeasureSpec );

                    // now adjust for the border
                    float borderSize = TypedValue.ApplyDimension(ComplexUnitType.Dip, BorderWidth, Rock.Mobile.PlatformCommon.Droid.Context.Resources.DisplayMetrics);

                    MeasuredWidth = base.MeasuredWidth + (int)(borderSize * 2);
                    MeasuredHeight = base.MeasuredHeight + (int)(borderSize * 2);
                }
            }

            public class BorderedRectView : View
            {
                BorderedRectPaintDrawable BorderedPaintDrawable { get; set; }

                public float BorderWidth 
                { 
                    get { return BorderedPaintDrawable.BorderWidth; } 
                    set { BorderedPaintDrawable.BorderWidth = value; }
                }

                public float Radius
                {
                    get { return BorderedPaintDrawable.Radius; }
                    set { BorderedPaintDrawable.Radius = value; }
                }

                public BorderedRectView( Android.Content.Context context ) : base( context )
                {
                    // create our special bordered rect
                    BorderedPaintDrawable = new BorderedRectPaintDrawable( );
                    BorderedPaintDrawable.Paint.SetStyle( Android.Graphics.Paint.Style.Fill );
                    BorderedPaintDrawable.BorderPaint = new Paint( BorderedPaintDrawable.Paint );
                    SetBackgroundDrawable( BorderedPaintDrawable );
                }

                public override void SetBackgroundColor( Android.Graphics.Color color )
                {
                    // put the color in the regular 'paint', which is really our fill color,
                    // but to the end user is the background color.
                    BorderedPaintDrawable.Paint.Color = color;
                }

                public void SetBorderColor( Android.Graphics.Color color )
                {
                    // set the color of the border paint, which is the paint used
                    // for our border outline
                    BorderedPaintDrawable.BorderPaint.Color = color;
                }

                protected override void OnDraw(Android.Graphics.Canvas canvas)
                {
                    BorderedRectPaintDrawable paintDrawable = Background as BorderedRectPaintDrawable;
                    if( paintDrawable != null )
                    {
                        float borderWidth = TypedValue.ApplyDimension(ComplexUnitType.Dip, paintDrawable.BorderWidth, Rock.Mobile.PlatformCommon.Droid.Context.Resources.DisplayMetrics);
                        canvas.Translate( borderWidth, borderWidth );
                    }

                    base.OnDraw(canvas);
                }
            }
        }
    }
}
#endif
