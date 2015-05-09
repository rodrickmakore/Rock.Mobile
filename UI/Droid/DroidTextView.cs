#if __ANDROID__
using System;
using System.Drawing;
using Android.Widget;
using Android.Graphics;
using Android.Views;
using Android.Views.InputMethods;
using Android.App;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Rock.Mobile.UI.DroidNative;
using Android.Util;
using Android.Text;
using Java.Lang;
using Java.Lang.Reflect;
using Rock.Mobile.Animation;

namespace Rock.Mobile
{
    namespace UI
    {
        /// <summary>
        /// Android implementation of a text view.
        /// </summary>
        public class DroidTextView : PlatformTextView
        {
            protected BorderedRectEditText TextView { get; set; }
            protected uint _BackgroundColor { get; set; }
            protected uint _BorderColor { get; set; }
            protected uint _TextColor { get; set; }
            protected uint _PlaceholderTextColor { get; set; }

            /// <summary>
            /// The size when the view isn't being animated
            /// </summary>
            /// <value>The size of the natural.</value>
            public System.Drawing.SizeF NaturalSize { get; set; }

            /// <summary>
            /// Lets us know whether we should alter NaturalSize on a size change or not.
            /// </summary>
            /// <value><c>true</c> if animating; otherwise, <c>false</c>.</value>
            public bool Animating { get; set; }

            /// <summary>
            /// A dummy view that absorbs focus when the text field isn't being edited.
            /// </summary>
            /// <value>The dummy view.</value>
            //View DummyView { get; set; }

            bool mScaleHeightForText = false;

            static Field CursorResource { get; set; }

            public DroidTextView( )
            {
                TextView = new BorderedRectEditText( Rock.Mobile.PlatformSpecific.Android.Core.Context );
                TextView.LayoutParameters = new ViewGroup.LayoutParams( ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent );
                TextView.SetScrollContainer( true );
                TextView.InputType |= Android.Text.InputTypes.TextFlagMultiLine;
                TextView.SetHorizontallyScrolling( false );
                TextView.Gravity = GravityFlags.Top | GravityFlags.Left;

                // create a dummy view that can take focus to de-select the text field
                /*DummyView = new View( Rock.Mobile.PlatformSpecific.Android.Core.Context );
                DummyView.Focusable = true;
                DummyView.FocusableInTouchMode = true;

                // let the dummy request focus so that the edit field doesn't get it and bring up the keyboard.
                DummyView.RequestFocus();*/

                // use reflection to get a reference to the TextView's cursor resource
                if ( CursorResource == null )
                {
                    CursorResource = Java.Lang.Class.ForName( "android.widget.TextView" ).GetDeclaredField( "mCursorDrawableRes" );
                    CursorResource.Accessible = true;
                }
            }

            protected override object getPlatformNativeObject()
            {
                return TextView;
            }

            // Properties
            protected override KeyboardAppearanceStyle getKeyboardAppearance( )
            {
                // android doesn't support modifying the keyboard appearance
                return (KeyboardAppearanceStyle) 0;
            }

            protected override void setKeyboardAppearance( KeyboardAppearanceStyle style )
            {
                // android doesn't support modifying the keyboard appearance
            }

            protected override AutoCorrectionType getAutoCorrectionType()
            {
                return 0;
            }

            protected override void setAutoCorrectionType(AutoCorrectionType style)
            {
            }

            protected override AutoCapitalizationType getAutoCapitalizationType()
            {
                return 0;
            }

            protected override void setAutoCapitalizationType(AutoCapitalizationType style)
            {
            }

            public override void SetFont( string fontName, float fontSize )
            {
                try
                {
                    Typeface fontFace = Rock.Mobile.PlatformSpecific.Android.Graphics.FontManager.Instance.GetFont( fontName );
                    TextView.SetTypeface( fontFace, TypefaceStyle.Normal );
                    TextView.SetTextSize( Android.Util.ComplexUnitType.Dip, fontSize );

                    if( mScaleHeightForText )
                    {
                        SizeToFit( );
                    }
                } 
                catch
                {
                    throw new System.Exception( string.Format( "Unable to load font: {0}", fontName ) );
                }
            }

            protected override uint getBackgroundColor()
            {
                return _BackgroundColor;
            }

            protected override void setBackgroundColor( uint backgroundColor )
            {
                _BackgroundColor = backgroundColor;
                
                TextView.SetBackgroundColor( Rock.Mobile.UI.Util.GetUIColor( backgroundColor ) );
                TextView.Invalidate( );

                // normalize the color so we can determine what color to use for the cursor
                float normalizedColor = (float) backgroundColor / (float)0xFFFFFFFF;

                // background is closer to white, use a dark cursor
                if ( normalizedColor > .50f )
                {
                    CursorResource.Set( TextView, Droid.Resource.Drawable.dark_cursor );
                }
                else
                {
                    // background is closer to black, use a light cursor
                    CursorResource.Set( TextView, Droid.Resource.Drawable.light_cursor );
                }
            }

            protected override void setBorderColor( uint borderColor )
            {
                _BorderColor = borderColor;
                TextView.SetBorderColor( Rock.Mobile.UI.Util.GetUIColor( borderColor ) );
                TextView.Invalidate( );
            }

            protected override uint getBorderColor( )
            {
                return _BorderColor;
            }

            protected override float getBorderWidth()
            {
                return TextView.BorderWidth;
            }
            protected override void setBorderWidth( float width )
            {
                TextView.BorderWidth = width;
            }

            protected override float getCornerRadius()
            {
                return TextView.Radius;
            }
            protected override void setCornerRadius( float radius )
            {
                TextView.Radius = radius;
            }

            protected override float getOpacity( )
            {
                return TextView.Alpha;
            }

            protected override void setOpacity( float opacity )
            {
                TextView.Alpha = opacity;
            }

            protected override float getZPosition( )
            {
                //Android doesn't use/need a Z position for its layers. (It goes based on order added)
                return 0.0f;
            }

            protected override void setZPosition( float zPosition )
            {
                //Android doesn't use/need a Z position for its layers. (It goes based on order added)
            }

            protected override RectangleF getBounds( )
            {
                //Bounds is simply the localSpace coordinates of the edges.
                // NOTE: On android we're not supporting a non-0 left/top. I don't know why you'd EVER
                // want this, but it's possible to set on iOS.
                return new RectangleF( 0, 0, TextView.LayoutParameters.Width, TextView.LayoutParameters.Height );
            }

            protected override void setBounds( RectangleF bounds )
            {
                //Bounds is simply the localSpace coordinates of the edges.
                // NOTE: On android we're not supporting a non-0 left/top. I don't know why you'd EVER
                // want this, but it's possible to set on iOS.
                TextView.SetMinWidth( (int)bounds.Width );
                TextView.SetMaxWidth( TextView.LayoutParameters.Width );

                if( mScaleHeightForText == false )
                {
                    TextView.LayoutParameters.Height = ( int )bounds.Height;
                }

                if( Animating == false )
                {
                    NaturalSize = new System.Drawing.SizeF( bounds.Width, bounds.Height );
                }
            }

            protected override RectangleF getFrame( )
            {
                //Frame is the transformed bounds to include position, so the Right/Bottom will be absolute.
                RectangleF frame = new RectangleF( TextView.GetX( ), TextView.GetY( ), TextView.LayoutParameters.Width, TextView.LayoutParameters.Height );
                return frame;
            }

            protected override void setFrame( RectangleF frame )
            {
                //Frame is the transformed bounds to include position, so the Right/Bottom will be absolute.
                setPosition( new System.Drawing.PointF( frame.X, frame.Y ) );

                RectangleF bounds = new RectangleF( frame.Left, frame.Top, frame.Width, frame.Height );
                setBounds( bounds );

                if( Animating == false )
                {
                    NaturalSize = new System.Drawing.SizeF( bounds.Width, bounds.Height );
                }
            }

            protected override System.Drawing.PointF getPosition( )
            {
                return new System.Drawing.PointF( TextView.GetX( ), TextView.GetY( ) );
            }

            protected override void setPosition( System.Drawing.PointF position )
            {
                TextView.SetX( position.X );
                TextView.SetY( position.Y );
            }

            protected override bool getHidden( )
            {
                return TextView.Visibility == ViewStates.Gone ? true : false;
            }

            protected override void setHidden( bool hidden )
            {
                TextView.Visibility = hidden == true ? ViewStates.Gone : ViewStates.Visible;
            }

            protected override bool getUserInteractionEnabled( )
            {
                // doesn't matter if we return this or regular Focusable,
                // because we set them both, guaranteeing the same value.
                return TextView.FocusableInTouchMode;
            }

            protected override void setUserInteractionEnabled( bool enabled )
            {
                TextView.FocusableInTouchMode = enabled;
                TextView.Focusable = enabled;
            }

            protected override uint getTextColor( )
            {
                return _TextColor;
            }

            protected override void setTextColor( uint color )
            {
                TextView.SetTextColor( Rock.Mobile.UI.Util.GetUIColor( color ) );
            }

            protected override string getText( )
            {
                return TextView.Text;
            }

            protected override void setText( string text )
            {
                TextView.Text = text;
            }

            protected override uint getPlaceholderTextColor( )
            {
                return _PlaceholderTextColor;
            }

            protected override void setPlaceholderTextColor( uint color )
            {
                _PlaceholderTextColor = color;
                TextView.SetHintTextColor( Rock.Mobile.UI.Util.GetUIColor( color ) );
            }

            protected override string getPlaceholder( )
            {
                return TextView.Hint;
            }

            protected override void setPlaceholder( string placeholder )
            {
                TextView.Hint = placeholder;
            }

            protected override TextAlignment getTextAlignment( )
            {
                // gonna have to do a stupid transform
                switch( TextView.Gravity )
                {
                    case GravityFlags.Center:
                        return TextAlignment.Center;
                    case GravityFlags.Left:
                        return TextAlignment.Left;
                    case GravityFlags.Right:
                        return TextAlignment.Right;
                    default:
                        return TextAlignment.Left;
                }
            }

            protected override void setTextAlignment( TextAlignment alignment )
            {
                switch( alignment )
                {
                    case TextAlignment.Center:
                    TextView.Gravity = GravityFlags.Center;
                        break;
                    case TextAlignment.Left:
                    TextView.Gravity = GravityFlags.Left;
                        break;
                    case TextAlignment.Right:
                    TextView.Gravity = GravityFlags.Right;
                        break;
                    default:
                    TextView.Gravity = GravityFlags.Left;
                        break;
                }
            }

            protected override void setScaleHeightForText( bool scale )
            {
                mScaleHeightForText = scale;

                // if scaling is turned on, restore the content wrapping
                if( scale == true )
                {
                    TextView.LayoutParameters = new ViewGroup.LayoutParams( ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent );
                }
            }

            protected override bool getScaleHeightForText( )
            {
                return mScaleHeightForText;
            }

            public override void AddAsSubview( object masterView )
            {
                ViewGroup view = masterView as ViewGroup;
                if( view == null )
                {
                    throw new System.Exception( "Object passed to Android AddAsSubview must be a ViewGroup or subclass." );
                }

                view.AddView( TextView );
                //view.AddView( DummyView );
            }

            public override void RemoveAsSubview( object masterView )
            {
                ViewGroup view = masterView as ViewGroup;
                if( view == null )
                {
                    throw new System.Exception( "Object passed to Android RemoveAsSubview must be a ViewGroup or subclass." );
                }

                view.RemoveView( TextView );
                //view.RemoveView( DummyView );
            }

            public override void SizeToFit( )
            {
                Measure( );

                // update its width
                TextView.SetMinWidth( TextView.MeasuredWidth );
                TextView.SetMaxWidth( TextView.MeasuredWidth );

                // set the height which will include the wrapped lines
                if( mScaleHeightForText == false )
                {
                    TextView.LayoutParameters.Height = TextView.MeasuredHeight;
                }

                if( Animating == false )
                {
                    NaturalSize = new System.Drawing.SizeF( TextView.MeasuredWidth, TextView.LayoutParameters.Height );
                }
            }

            public override void BecomeFirstResponder( )
            {
                InputMethodManager imm = ( InputMethodManager )Rock.Mobile.PlatformSpecific.Android.Core.Context.GetSystemService( Android.Content.Context.InputMethodService );

                //Activity activity = ( Activity )Rock.Mobile.PlatformSpecific.Android.Core.Context;
                //imm.HideSoftInputFromWindow( activity.CurrentFocus.WindowToken, 0 );

                TextView.FocusableInTouchMode = true;
                TextView.RequestFocus( );

                imm.ShowSoftInput( TextView, ShowFlags.Implicit );
                //imm.ShowSoftInputFromInputMethod( TextView.WindowToken, 0 );
            }

            public override void ResignFirstResponder( )
            {
                // only allow this text edit to hide the keyboard if it's the text field with focus.
                Activity activity = ( Activity )Rock.Mobile.PlatformSpecific.Android.Core.Context;
                if( activity.CurrentFocus != null && ( activity.CurrentFocus as EditText ) == TextView )
                {
                    InputMethodManager imm = ( InputMethodManager )Rock.Mobile.PlatformSpecific.Android.Core.Context.GetSystemService( Android.Content.Context.InputMethodService );

                    imm.HideSoftInputFromWindow( TextView.WindowToken, 0 );

                    // yeild focus to the dummy view so the text field clears it's caret and the selected outline
                    TextView.ClearFocus( );
                    //DummyView.RequestFocus( );
                }
            }

            void Measure( )
            {
                // create the specs we want for measurement
                int widthMeasureSpec = View.MeasureSpec.MakeMeasureSpec( TextView.LayoutParameters.Width, MeasureSpecMode.Unspecified );
                int heightMeasureSpec = View.MeasureSpec.MakeMeasureSpec( 0, MeasureSpecMode.Unspecified );

                // measure the label given the current width/height/text
                TextView.Measure( widthMeasureSpec, heightMeasureSpec );
            }

            public override void AnimateOpen( bool becomeFirstResponder )
            {
                if ( Animating == false && Hidden == true )
                {
                    // unhide and flag it as animating
                    Hidden = false;
                    Animating = true;

                    // measure so we know the height
                    Measure( );

                    // start the size at 0
                    TextView.LayoutParameters.Width = 0;
                    TextView.LayoutParameters.Height = 0;

                    SimpleAnimator_SizeF animator = new SimpleAnimator_SizeF( System.Drawing.SizeF.Empty, new System.Drawing.SizeF( NaturalSize.Width, TextView.MeasuredHeight ), .2f, 
                        delegate(float percent, object value )
                        {
                            System.Drawing.SizeF currSize = (System.Drawing.SizeF)value;

                            Rock.Mobile.Threading.Util.PerformOnUIThread( delegate {

                                TextView.LayoutParameters.Width = (int) currSize.Width;
                                TextView.LayoutParameters.Height = (int) currSize.Height;

                                // redundantly set the min width so it redraws
                                TextView.SetMinWidth( (int) currSize.Width );
                            });
                        },
                        delegate
                        {
                            Animating = false;

                            // restore the original settings for dimensions
                            TextView.LayoutParameters.Width = ViewGroup.LayoutParams.WrapContent;
                            TextView.LayoutParameters.Height = ViewGroup.LayoutParams.WrapContent;

                            if( becomeFirstResponder == true )
                            {
                                BecomeFirstResponder( );
                            }
                        } );

                    animator.Start( );
                }
            }

            public override void AnimateClosed( )
            {
                if ( Animating == false && Hidden == false )
                {
                    // unhide and flag it as animating
                    Animating = true;

                    // get the measurements so we know how tall it currently is
                    Measure( );

                    SimpleAnimator_SizeF animator = new SimpleAnimator_SizeF( new System.Drawing.SizeF( NaturalSize.Width, TextView.MeasuredHeight ), System.Drawing.SizeF.Empty, .2f, 
                        delegate(float percent, object value )
                        {
                            // animate it to 0
                            System.Drawing.SizeF currSize = (System.Drawing.SizeF)value;

                            Rock.Mobile.Threading.Util.PerformOnUIThread( delegate {

                                TextView.LayoutParameters.Width = (int) currSize.Width;
                                TextView.LayoutParameters.Height = (int) currSize.Height;

                                // redundantly set the min width so it redraws
                                TextView.SetMinWidth( TextView.MeasuredWidth );
                            });
                        },
                        delegate
                        {
                            Hidden = true;
                            Animating = false;

                            // restore the original settings for dimensions
                            TextView.LayoutParameters.Width = ViewGroup.LayoutParams.WrapContent;
                            TextView.LayoutParameters.Height = ViewGroup.LayoutParams.WrapContent;
                        } );

                    animator.Start( );
                }
            }
        }
    }
}
#endif
