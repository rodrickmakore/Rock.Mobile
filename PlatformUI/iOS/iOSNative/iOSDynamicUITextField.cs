#if __IOS__

using System;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using System.Drawing;

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

                public override bool ShouldBeginEditing(UITextView textView)
                {
                    NSNotificationCenter.DefaultCenter.PostNotificationName( "TextFieldDidBeginEditing", NSValue.FromRectangleF( textView.Frame ) );
                    return true;
                }

                public override bool ShouldChangeText(UITextView textView, NSRange range, string text)
                {
                    // don't allow lengths past the height limit imposed.
                    if( text.Length > 0 && textView.Frame.Height > DynamicTextMaxHeight )
                    {
                        return false;
                    }
                    return true;
                }

                public override void Changed(UITextView textView)
                {
                    NSNotificationCenter.DefaultCenter.PostNotificationName( "TextFieldChanged", NSValue.FromRectangleF( textView.Frame ) );
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
                /// When true, the height is always set to whatever is needed to display all the text.
                /// Any passed in height is ignored.
                /// </summary>
                /// <value><c>true</c> if scale height for text; otherwise, <c>false</c>.</value>
                public bool ScaleHeightForText { get; set; }

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
                    PlaceholderLabel.Layer.ZPosition = Layer.ZPosition + 1;
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
                            SizeF size = SizeThatFits( new SizeF( base.Bounds.Width, base.Bounds.Height ) );

                            // round up
                            base.Bounds = new RectangleF( base.Bounds.X, base.Bounds.Y, base.Bounds.Width, (float) System.Math.Ceiling( size.Height ) );

                            // update our content size AND placeholder
                            ContentSize = base.Bounds.Size;
                            PlaceholderLabel.Bounds = base.Bounds;
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
                        return Layer.ZPosition;
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
                        return Layer.Position;
                    }
                    set
                    {
                        Layer.Position = value;
                        PlaceholderLabel.Layer.Position = value;
                    }
                }

                public override RectangleF Bounds
                {
                    get
                    {
                        return base.Bounds;
                    }
                    set
                    {
                        // create the bounds with either the text's required height, or the height the user wanted.
                        base.Bounds = new RectangleF( value.X, value.Y, value.Width, ScaleHeightForText ? ContentSize.Height : value.Height );
                        PlaceholderLabel.Bounds = base.Bounds;
                    }
                }

                public override RectangleF Frame
                {
                    get
                    {
                        return base.Frame;
                    }
                    set
                    {
                        // create the bounds with either the text's required height, or the height the user wanted.
                        base.Frame = new RectangleF( value.X, value.Y, value.Width, ScaleHeightForText ? ContentSize.Height : value.Height );
                        PlaceholderLabel.Frame = base.Frame;
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
                        if( ScaleHeightForText )
                        {
                            base.Bounds = new RectangleF( base.Bounds.X, 
                                                          base.Bounds.Y, 
                                                          base.Bounds.Width,
                                                          ContentSize.Height );
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
                    }
                }
            }
        }
    }
}

#endif
