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
using Rock.Mobile.PlatformUI.DroidNative;
using Android.Util;
using Android.Text;
using Java.Lang;
using Java.Lang.Reflect;
using Rock.Mobile.Animation;

namespace Rock.Mobile
{
    namespace PlatformUI
    {
        /// <summary>
        /// Subclassed length filter to allow us to prevent the textField
        /// from growing larger than our limit.
        /// </summary>
        public class HeightFilter : InputFilterLengthFilter
        {
            public DroidTextField Parent { get; set; }

            public HeightFilter( int max ) : base (max)
            {
            }

            public override Java.Lang.ICharSequence FilterFormatted(Java.Lang.ICharSequence source, int start, int end, ISpanned dest, int dstart, int dend)
            {
                //JHM 1-15-15: Don't limit the text anymore.
                //if( Parent.AllowInput( source ) )
                {
                    return base.FilterFormatted(source, start, end, dest, dstart, dend);
                }
                /*else
                {
                    return new Java.Lang.String("");
                }*/
            }
        }

        /// <summary>
        /// Android implementation of a text field.
        /// </summary>
        public class DroidTextField : PlatformTextField
        {
            BorderedRectEditText TextField { get; set; }

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
            View DummyView { get; set; }

            bool mScaleHeightForText = false;
            float mDynamicTextMaxHeight = float.MaxValue;

            public bool AllowInput( Java.Lang.ICharSequence source )
            {
                // allow it as long as we're within the height limit
                if( TextField.Height < mDynamicTextMaxHeight || source.Length( ) == 0 )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            static Field CursorResource { get; set; }

            public DroidTextField( )
            {
                TextField = new BorderedRectEditText( Rock.Mobile.PlatformSpecific.Android.Core.Context );
                TextField.LayoutParameters = new ViewGroup.LayoutParams( ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent );
                TextField.SetScrollContainer( true );
                TextField.InputType |= Android.Text.InputTypes.TextFlagMultiLine;
                TextField.SetHorizontallyScrolling( false );
                TextField.SetFilters( new IInputFilter[] { new HeightFilter(int.MaxValue) { Parent = this } } );

                // create a dummy view that can take focus to de-select the text field
                DummyView = new View( Rock.Mobile.PlatformSpecific.Android.Core.Context );
                DummyView.Focusable = true;
                DummyView.FocusableInTouchMode = true;

                // let the dummy request focus so that the edit field doesn't get it and bring up the keyboard.
                DummyView.RequestFocus();

                // use reflection to get a reference to the textField's cursor resource
                if ( CursorResource == null )
                {
                    CursorResource = Java.Lang.Class.ForName( "android.widget.TextView" ).GetDeclaredField( "mCursorDrawableRes" );
                    CursorResource.Accessible = true;
                }
            }

            // Properties
            protected override int getKeyboardAppearance( )
            {
                // android doesn't support modifying the keyboard appearance
                return (int) 0;
            }

            protected override void setKeyboardAppearance( int style )
            {
                // android doesn't support modifying the keyboard appearance
            }

            public override void SetFont( string fontName, float fontSize )
            {
                try
                {
                    Typeface fontFace = Rock.Mobile.PlatformSpecific.Android.Graphics.FontManager.Instance.GetFont( fontName );
                    TextField.SetTypeface( fontFace, TypefaceStyle.Normal );
                    TextField.SetTextSize( Android.Util.ComplexUnitType.Dip, fontSize );

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

            protected override void setBackgroundColor( uint backgroundColor )
            {
                TextField.SetBackgroundColor( Rock.Mobile.PlatformUI.Util.GetUIColor( backgroundColor ) );

                // normalize the color so we can determine what color to use for the cursor
                float normalizedColor = (float) backgroundColor / (float)0xFFFFFFFF;

                // background is closer to white, use a dark cursor
                if ( normalizedColor > .50f )
                {
                    CursorResource.Set( TextField, Droid.Resource.Drawable.dark_cursor );
                }
                else
                {
                    // background is closer to black, use a light cursor
                    CursorResource.Set( TextField, Droid.Resource.Drawable.light_cursor );
                }
            }

            protected override void setBorderColor( uint borderColor )
            {
                TextField.SetBorderColor( Rock.Mobile.PlatformUI.Util.GetUIColor( borderColor ) );
            }

            protected override float getBorderWidth()
            {
                return TextField.BorderWidth;
            }
            protected override void setBorderWidth( float width )
            {
                TextField.BorderWidth = width;
            }

            protected override float getCornerRadius()
            {
                return TextField.Radius;
            }
            protected override void setCornerRadius( float radius )
            {
                TextField.Radius = radius;
            }

            protected override float getOpacity( )
            {
                return TextField.Alpha;
            }

            protected override void setOpacity( float opacity )
            {
                TextField.Alpha = opacity;
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
                return new RectangleF( 0, 0, TextField.LayoutParameters.Width, TextField.LayoutParameters.Height );
            }

            protected override void setBounds( RectangleF bounds )
            {
                //Bounds is simply the localSpace coordinates of the edges.
                // NOTE: On android we're not supporting a non-0 left/top. I don't know why you'd EVER
                // want this, but it's possible to set on iOS.
                TextField.SetMinWidth( (int)bounds.Width );
                TextField.SetMaxWidth( TextField.LayoutParameters.Width );

                if( mScaleHeightForText == false )
                {
                    TextField.LayoutParameters.Height = ( int )bounds.Height;
                }

                if( Animating == false )
                {
                    NaturalSize = new System.Drawing.SizeF( bounds.Width, bounds.Height );
                }
            }

            protected override RectangleF getFrame( )
            {
                //Frame is the transformed bounds to include position, so the Right/Bottom will be absolute.
                RectangleF frame = new RectangleF( TextField.GetX( ), TextField.GetY( ), TextField.LayoutParameters.Width, TextField.LayoutParameters.Height );
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
                return new System.Drawing.PointF( TextField.GetX( ), TextField.GetY( ) );
            }

            protected override void setPosition( System.Drawing.PointF position )
            {
                TextField.SetX( position.X );
                TextField.SetY( position.Y );
            }

            protected override bool getHidden( )
            {
                return TextField.Visibility == ViewStates.Gone ? true : false;
            }

            protected override void setHidden( bool hidden )
            {
                TextField.Visibility = hidden == true ? ViewStates.Gone : ViewStates.Visible;
            }

            protected override bool getUserInteractionEnabled( )
            {
                // doesn't matter if we return this or regular Focusable,
                // because we set them both, guaranteeing the same value.
                return TextField.FocusableInTouchMode;
            }

            protected override void setUserInteractionEnabled( bool enabled )
            {
                TextField.FocusableInTouchMode = enabled;
                TextField.Focusable = enabled;
            }

            protected override void setTextColor( uint color )
            {
                TextField.SetTextColor( Rock.Mobile.PlatformUI.Util.GetUIColor( color ) );
            }

            protected override string getText( )
            {
                return TextField.Text;
            }

            protected override void setText( string text )
            {
                TextField.Text = text;
            }

            protected override void setPlaceholderTextColor( uint color )
            {
                TextField.SetHintTextColor( Rock.Mobile.PlatformUI.Util.GetUIColor( color ) );
            }

            protected override string getPlaceholder( )
            {
                return TextField.Hint;
            }

            protected override void setPlaceholder( string placeholder )
            {
                TextField.Hint = placeholder;
            }

            protected override TextAlignment getTextAlignment( )
            {
                // gonna have to do a stupid transform
                switch( TextField.Gravity )
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
                        TextField.Gravity = GravityFlags.Center;
                        break;
                    case TextAlignment.Left:
                        TextField.Gravity = GravityFlags.Left;
                        break;
                    case TextAlignment.Right:
                        TextField.Gravity = GravityFlags.Right;
                        break;
                    default:
                        TextField.Gravity = GravityFlags.Left;
                        break;
                }
            }

            protected override void setScaleHeightForText( bool scale )
            {
                mScaleHeightForText = scale;

                // if scaling is turned on, restore the content wrapping
                if( scale == true )
                {
                    TextField.LayoutParameters = new ViewGroup.LayoutParams( ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent );
                }
            }

            protected override bool getScaleHeightForText( )
            {
                return mScaleHeightForText;
            }

            protected override void setDynamicTextMaxHeight( float height )
            {
                mDynamicTextMaxHeight = height;
            }

            protected override float getDynamicTextMaxHeight( )
            {
                return mDynamicTextMaxHeight;
            }

            public override void AddAsSubview( object masterView )
            {
                // we know that masterView will be an iOS View.
                RelativeLayout view = masterView as RelativeLayout;
                if( view == null )
                {
                    throw new System.Exception( "Object passed to Android AddAsSubview must be a RelativeLayout." );
                }

                view.AddView( TextField );
                view.AddView( DummyView );
            }

            public override void RemoveAsSubview( object masterView )
            {
                // we know that masterView will be an iOS View.
                RelativeLayout view = masterView as RelativeLayout;
                if( view == null )
                {
                    throw new System.Exception( "Object passed to Android RemoveAsSubview must be a RelativeLayout." );
                }

                view.RemoveView( TextField );
                view.RemoveView( DummyView );
            }

            public override void SizeToFit( )
            {
                Measure( );

                // update its width
                TextField.SetMinWidth( TextField.MeasuredWidth );
                TextField.SetMaxWidth( TextField.MeasuredWidth );

                // set the height which will include the wrapped lines
                if( mScaleHeightForText == false )
                {
                    TextField.LayoutParameters.Height = TextField.MeasuredHeight;
                }

                if( Animating == false )
                {
                    NaturalSize = new System.Drawing.SizeF( TextField.MeasuredWidth, TextField.LayoutParameters.Height );
                }
            }

            public override void ResignFirstResponder( )
            {
                // only allow this text edit to hide the keyboard if it's the text field with focus.
                Activity activity = ( Activity )Rock.Mobile.PlatformSpecific.Android.Core.Context;
                if( activity.CurrentFocus != null && ( activity.CurrentFocus as EditText ) == TextField )
                {
                    InputMethodManager imm = ( InputMethodManager )Rock.Mobile.PlatformSpecific.Android.Core.Context.GetSystemService( Android.Content.Context.InputMethodService );

                    imm.HideSoftInputFromWindow( TextField.WindowToken, 0 );

                    // yeild focus to the dummy view so the text field clears it's caret and the selected outline
                    TextField.ClearFocus( );
                    DummyView.RequestFocus( );
                }
            }

            void Measure( )
            {
                // create the specs we want for measurement
                int widthMeasureSpec = View.MeasureSpec.MakeMeasureSpec( TextField.LayoutParameters.Width, MeasureSpecMode.Unspecified );
                int heightMeasureSpec = View.MeasureSpec.MakeMeasureSpec( 0, MeasureSpecMode.Unspecified );

                // measure the label given the current width/height/text
                TextField.Measure( widthMeasureSpec, heightMeasureSpec );
            }

            public override void AnimateOpen( )
            {
                if ( Animating == false && Hidden == true )
                {
                    // unhide and flag it as animating
                    Hidden = false;
                    Animating = true;

                    // measure so we know the height
                    Measure( );

                    // start the size at 0
                    TextField.LayoutParameters.Width = 0;
                    TextField.LayoutParameters.Height = 0;

                    SimpleAnimator_SizeF animator = new SimpleAnimator_SizeF( System.Drawing.SizeF.Empty, new System.Drawing.SizeF( NaturalSize.Width, TextField.MeasuredHeight ), .2f, 
                        delegate(float percent, object value )
                        {
                            System.Drawing.SizeF currSize = (System.Drawing.SizeF)value;

                            Rock.Mobile.Threading.Util.PerformOnUIThread( delegate {

                                TextField.LayoutParameters.Width = (int) currSize.Width;
                                TextField.LayoutParameters.Height = (int) currSize.Height;

                                // redundantly set the min width so it redraws
                                TextField.SetMinWidth( (int) currSize.Width );
                            });
                        },
                        delegate
                        {
                            Animating = false;

                            // restore the original settings for dimensions
                            TextField.LayoutParameters.Width = ViewGroup.LayoutParams.WrapContent;
                            TextField.LayoutParameters.Height = ViewGroup.LayoutParams.WrapContent;
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

                    SimpleAnimator_SizeF animator = new SimpleAnimator_SizeF( new System.Drawing.SizeF( NaturalSize.Width, TextField.MeasuredHeight ), System.Drawing.SizeF.Empty, .2f, 
                        delegate(float percent, object value )
                        {
                            // animate it to 0
                            System.Drawing.SizeF currSize = (System.Drawing.SizeF)value;

                            Rock.Mobile.Threading.Util.PerformOnUIThread( delegate {

                                TextField.LayoutParameters.Width = (int) currSize.Width;
                                TextField.LayoutParameters.Height = (int) currSize.Height;

                                // redundantly set the min width so it redraws
                                TextField.SetMinWidth( TextField.MeasuredWidth );
                            });
                        },
                        delegate
                        {
                            Hidden = true;
                            Animating = false;

                            // restore the original settings for dimensions
                            TextField.LayoutParameters.Width = ViewGroup.LayoutParams.WrapContent;
                            TextField.LayoutParameters.Height = ViewGroup.LayoutParams.WrapContent;
                        } );

                    animator.Start( );
                }
            }
        }
    }
}
#endif
