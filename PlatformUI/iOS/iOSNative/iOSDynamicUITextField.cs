#if __IOS__

using System;
using UIKit;
using Foundation;
using System.Drawing;
using Rock.Mobile.PlatformSpecific.iOS.Animation;
using CoreGraphics;
using Rock.Mobile.PlatformSpecific.Util;

namespace Rock.Mobile
{
    namespace PlatformUI
    {
        namespace iOSNative
        {
            // setup a delegate to manage text editing notifications
            public class TextViewDelegate : UITextViewDelegate
            {
                public float DynamicTextMaxHeight { get; set; }

                public TextViewDelegate( ) : base( )
                {
                    // default to WAY over the limit
                    DynamicTextMaxHeight = float.MaxValue;
                }

                public override bool ShouldBeginEditing(UITextView textView)
                {
                    NSNotificationCenter.DefaultCenter.PostNotificationName( Rock.Mobile.PlatformSpecific.iOS.UI.KeyboardAdjustManager.TextFieldDidBeginEditingNotification, NSValue.FromCGRect( textView.Frame ) );
                    return true;
                }

                public override bool ShouldChangeText(UITextView textView, NSRange range, string text)
                {
                    // don't allow lengths past the height limit imposed.
                    /*if( text.Length > 0 && textView.Frame.Height > DynamicTextMaxHeight )
                    {
                        return false;
                    }*/
                    return true;
                }

                public override void Changed(UITextView textView)
                {
                    NSNotificationCenter.DefaultCenter.PostNotificationName( Rock.Mobile.PlatformSpecific.iOS.UI.KeyboardAdjustManager.TextFieldChangedNotification, NSValue.FromCGRect( textView.Frame ) );
                }
            }

            /// <summary>
            /// A subclassed text view that grows vertically as text is added.
            /// </summary>
            public class DynamicUITextView : UITextView
            {
                /// <summary>
                /// A UI label that displays placeholder text when no text is in the view.
                /// </summary>
                protected UILabel PlaceholderLabel = new UILabel( );

                /// <summary>
                /// The height of a single line, used for padding the box while the user edits.
                /// </summary>
                /// <value>The height of the single line.</value>
                protected float SingleLineHeight { get; set; }

                /// <summary>
                /// When true, the height is always set to whatever is needed to display all the text.
                /// Any passed in height is ignored.
                /// </summary>
                /// <value><c>true</c> if scale height for text; otherwise, <c>false</c>.</value>
                public bool ScaleHeightForText { get; set; }

                /// <summary>
                /// The size when the view isn't being animated
                /// </summary>
                /// <value>The size of the natural.</value>
                public CGSize NaturalSize { get; set; }

                /// <summary>
                /// Lets us know whether we should alter NaturalSize on a size change or not.
                /// </summary>
                /// <value><c>true</c> if animating; otherwise, <c>false</c>.</value>
                public bool Animating { get; set; }

                /// <summary>
                /// Limits the height of the dynamic text box
                /// </summary>
                /// <value>The height of the dynamic text max.</value>
                public float DynamicTextMaxHeight 
                { 
                    get 
                    { 
                        return ((TextViewDelegate)Delegate).DynamicTextMaxHeight; 
                    } 

                    set 
                    {
                        ((TextViewDelegate)Delegate).DynamicTextMaxHeight = value;
                    }
                }

                public DynamicUITextView( ) : base( )
                {
                    NSNotificationCenter.DefaultCenter.AddObserver( UITextView.TextDidChangeNotification, OnTextChanged );
                    Delegate = new TextViewDelegate();

                    Layer.BackgroundColor = UIColor.Clear.CGColor;

                    // initialize our placeholder label. Its z pos should ALWAYS be just in front of us.
                    PlaceholderLabel.Layer.AnchorPoint = new PointF( 0, 0 );
                    PlaceholderLabel.Layer.ZPosition = Layer.ZPosition;
                    PlaceholderLabel.BackgroundColor = UIColor.Clear;
                }

                public void AddAsSubview(UIView view)
                {
                    view.AddSubview( this );
                    view.AddSubview( PlaceholderLabel );
                }

                public override void RemoveFromSuperview( )
                {
                    PlaceholderLabel.RemoveFromSuperview( );
                    base.RemoveFromSuperview( );
                }

                // Property Overrides so we can keep the placeholder text sync'd
                public override UIFont Font
                {
                    get
                    {
                        return base.Font;
                    }
                    set
                    {
                        base.Font = value;
                        PlaceholderLabel.Font = value;

                        // determine the base height of this text field.
                        if( ScaleHeightForText )
                        {
                            // measure the font
                            CGSize size = SizeThatFits( new CGSize( base.Bounds.Width, base.Bounds.Height ) );

                            // round up
                            base.Bounds = new CGRect( base.Bounds.X, base.Bounds.Y, base.Bounds.Width, (float) System.Math.Ceiling( size.Height ) );

                            // update our content size AND placeholder
                            ContentSize = base.Bounds.Size;
                            PlaceholderLabel.Bounds = base.Bounds;
                            SingleLineHeight = (float) size.Height;

                            if( Animating == false )
                            {
                                NaturalSize = new CGSize( Bounds.Width, Bounds.Height );
                            }
                        }
                    }
                }

                public string Placeholder 
                { 
                    get
                    {
                        return PlaceholderLabel.Text;
                    }
                    set
                    {
                        // give some buffer room for the caret
                        PlaceholderLabel.Text = "  " + value;
                    }
                }

                public UIColor PlaceholderTextColor
                {
                    get
                    {
                        return PlaceholderLabel.TextColor;
                    }
                    set
                    {
                        PlaceholderLabel.TextColor = value;
                    }
                }

                public float Opacity
                {
                    get
                    {
                        return Layer.Opacity;
                    }
                    set
                    {
                        Layer.Opacity = value;
                        PlaceholderLabel.Layer.Opacity = value;
                    }
                }

                public float ZPosition
                {
                    get
                    {
                        return (float) Layer.ZPosition;
                    }
                    set
                    {
                        // always make the placeholder be in front of us.
                        Layer.ZPosition = value;
                        PlaceholderLabel.Layer.ZPosition = value + 1;
                    }
                }

                public override bool Hidden
                {
                    get
                    {
                        return base.Hidden;
                    }
                    set
                    {
                        base.Hidden = value;

                        // only take the hidden state if it's hiding, or we have no text,
                        // in which case we'll allow showing as well.
                        if( value == true || string.IsNullOrEmpty( Text ) )
                        {
                            PlaceholderLabel.Hidden = value;
                        }
                    }
                }

                public PointF Position
                {
                    get
                    {
                        return Layer.Position.ToPointF( );
                    }
                    set
                    {
                        Layer.Position = value;
                        PlaceholderLabel.Layer.Position = value;
                    }
                }

                public override CGRect Bounds
                {
                    get
                    {
                        return base.Bounds;
                    }
                    set
                    {
                        // create the bounds with either the text's required height, or the height the user wanted.
                        if ( Animating == true )
                        {
                            base.Bounds = new CGRect( value.X, value.Y, value.Width, value.Height );
                        }
                        else
                        {
                            base.Bounds = new CGRect( value.X, value.Y, value.Width, ScaleHeightForText ? ContentSize.Height : value.Height );
                            NaturalSize = new CGSize( Bounds.Width, Bounds.Height );
                        }

                        PlaceholderLabel.Bounds = base.Bounds;
                    }
                }

                public override CGRect Frame
                {
                    get
                    {
                        return base.Frame;
                    }
                    set
                    {
                        // create the bounds with either the text's required height, or the height the user wanted.
                        base.Frame = new CGRect( value.X, value.Y, value.Width, ScaleHeightForText ? ContentSize.Height : value.Height );
                        PlaceholderLabel.Frame = base.Frame;

                        if( Animating == false )
                        {
                            NaturalSize = new CGSize( Bounds.Width, Bounds.Height );
                        }
                    }
                }

                public override string Text
                {
                    get
                    {
                        return base.Text;
                    }
                    set
                    {
                        base.Text = value;

                        // if directly setting text to the control, hide the label if
                        // it's a non-emty string.
                        if( string.IsNullOrEmpty( value ) == false )
                        {
                            PlaceholderLabel.Hidden = true;
                        }
                    }
                }
                //

                protected void OnTextChanged( NSNotification notification )
                {
                    if( notification.Object == this )
                    {
                        // Update the height to match the content height
                        // if scaling is on.
                        if( ScaleHeightForText && Animating == false )
                        {
                            // do it via animation for a nice growth effect
                            Animating = true;
                            SizeF newSize = new SizeF( (float) base.Bounds.Width, (float) ContentSize.Height );

                            SimpleAnimator_SizeF animator = new SimpleAnimator_SizeF( base.Bounds.Size.ToSizeF( ), newSize, .10f, 
                                delegate(float percent, object value )
                                {
                                    SizeF currSize = (SizeF)value;
                                    base.Bounds = new CGRect( 0, 0, currSize.Width, currSize.Height );
                                },
                                delegate
                                {
                                    Animating = false;
                                    NaturalSize = new CGSize( Bounds.Width, Bounds.Height );
                                } );

                            animator.Start( );
                        }

                        // reveal the placeholder only when text is gone.
                        if( Text == "" )
                        {
                            PlaceholderLabel.Hidden = false;
                        }
                        else
                        {
                            PlaceholderLabel.Hidden = true;
                        }
                    }
                }

                public override void SizeToFit()
                {
                    // only allow size to fit if height scaling is off.
                    if( ScaleHeightForText == false )
                    {
                        base.SizeToFit( );

                        if( Animating == false )
                        {
                            NaturalSize = new CGSize( Bounds.Width, Bounds.Height );
                        }
                    }
                }
            }
        }
    }
}

#endif
