#if __IOS__
using System;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;
using MonoTouch.CoreText;

// This file is where you can put anything SPECIFIC to iOS that doesn't 
// require common base classes, and should be DYE-RECTLY referenced by iOS code.
using MonoTouch.UIKit;
using System.Drawing;

namespace Rock.Mobile
{
    namespace PlatformCommon
    {
        class BlockerView : UIView
        {
            public UIActivityIndicatorView ActivityIndicator { get; set; }

            public BlockerView( RectangleF frame ) : base( frame )
            {
                ActivityIndicator = new UIActivityIndicatorView( );
                ActivityIndicator.ActivityIndicatorViewStyle = UIActivityIndicatorViewStyle.WhiteLarge;
                ActivityIndicator.StartAnimating( );
                ActivityIndicator.SizeToFit( );
                ActivityIndicator.Layer.AnchorPoint = PointF.Empty;
                ActivityIndicator.Layer.Position = new PointF( ( frame.Width - ActivityIndicator.Bounds.Width ) / 2, ( frame.Height - ActivityIndicator.Bounds.Height ) / 2 );

                AddSubview( ActivityIndicator );

                BackgroundColor = UIColor.Black;

                Layer.Opacity = 0.00f;
                ActivityIndicator.Hidden = true;
            }

            public delegate void OnAnimComplete( );
            public void FadeIn( OnAnimComplete onCompletion )
            {
                ActivityIndicator.Hidden = false;

                UIView.Animate( .5f, 0, UIViewAnimationOptions.CurveEaseInOut, 
                    new NSAction( delegate 
                        { 
                            Layer.Opacity = .80f; 
                        } )
                    , new NSAction( delegate 
                        { 
                            // if provided, call their completion handler
                            if( onCompletion != null )
                            {
                                onCompletion( );
                            }
                        } )
                );
            }

            public void FadeOut( OnAnimComplete onCompletion )
            {
                UIView.Animate( .5f, 0, UIViewAnimationOptions.CurveEaseInOut, 
                    new NSAction( delegate 
                        { 
                            Layer.Opacity = 0.00f;
                        } )
                    , new NSAction( delegate 
                        { 
                            ActivityIndicator.Hidden = true;

                            // if provided, call their completion handler
                            if( onCompletion != null )
                            {
                                onCompletion( );
                            }
                        } )
                );
            }
        }

        /// <summary>
        /// Utility class that makes sure a text field being edited is in view and not obstructed
        /// by the software keyboard. 
        /// Usage: Instantiate and pass in the parent view and child scroll view (Hierarchy must be View->UIScrollView->Whatever)
        /// Your class should add notification handlers for the following:
        /// For text fields: 
        /// KeyboardAdjustManager.TextFieldDidBeginEditingNotification -> KeyboardAdjustManager.OnTextFieldDidBeginEditing
        /// KeyboardAdjustManager.TextFieldChangedNotification -> KeyboardAdjustManager.OnTextFieldChanged
        /// Text fields of interest need to send the two TextField notifications.
        /// 
        /// For the software keyboard:
        /// UIKeyboard.WillShowNotification -> KeyboardAdjustManager.OnKeyboardChanged
        /// UIKeyboard.WillHideNotification -> KeyboardAdjustManager.OnKeyboardChanged
        /// 
        /// Lastly, in the handlers simply pass the notifications to the keyboard manager. That's it!
        /// </summary>
        public class KeyboardAdjustManager
        {
            public const string TextFieldDidBeginEditingNotification = "TextFieldDidBeginEditing";

            public const string TextFieldChangedNotification = "TextFieldChanged";

            /// <summary>
            /// True when a keyboard is present due to UIKeyboardWillShowNotification.
            /// Important because this will be FALSE if a hardware keyboard is attached.
            /// </summary>
            /// <value><c>true</c> if displaying keyboard; otherwise, <c>false</c>.</value>
            public bool DisplayingKeyboard { get; set; }


            /// <summary>
            /// The frame of the text field that was tapped when the keyboard was shown.
            /// Used so we know whether to scroll up the text field or not.
            /// </summary>
            RectangleF Edit_TappedTextFieldFrame { get; set; }

            /// <summary>
            /// The amount the scrollView was scrolled when editing began.
            /// Used so we can restore the scrollView position when editing is finished.
            /// </summary>
            /// <value>The edit start scroll offset.</value>
            PointF Edit_StartScrollOffset { get; set; }

            /// <summary>
            /// The position of the UIScrollView when text editing began.
            /// </summary>
            /// <value>The edit start screen offset.</value>
            PointF Edit_StartScreenOffset { get; set; }

            /// <summary>
            /// The bottom position of the visible area when the keyboard is up.
            /// </summary>
            /// <value>The edit visible area with keyboard bot.</value>
            float Edit_VisibleAreaWithKeyboardBot { get; set; }

            UIView ParentView { get; set; }
            UIScrollView ParentScrollView { get; set; }

            public KeyboardAdjustManager( UIView parentView, UIScrollView parentScrollView )
            {
                ParentView = parentView;
                ParentScrollView = parentScrollView;
            }

            public void OnKeyboardNotification( NSNotification notification )
            {
                //Start an animation, using values from the keyboard
                UIView.BeginAnimations ("AnimateForKeyboard");
                UIView.SetAnimationBeginsFromCurrentState (true);
                UIView.SetAnimationDuration (UIKeyboard.AnimationDurationFromNotification (notification));
                UIView.SetAnimationCurve ((UIViewAnimationCurve)UIKeyboard.AnimationCurveFromNotification (notification));

                // Check if the keyboard is becoming visible.
                // Sometimes iOS is kind enough to send us this notification 3 times in a row, so make sure
                // we haven't already handled it.
                if( notification.Name == UIKeyboard.WillShowNotification && DisplayingKeyboard == false )
                {
                    DisplayingKeyboard = true;

                    // store the original screen positioning / scroll. No matter what, we will
                    // undo any scrolling the user did while editing.
                    Edit_StartScrollOffset = ParentScrollView.ContentOffset;
                    Edit_StartScreenOffset = ParentScrollView.Layer.Position;

                    // get the keyboard frame and transform it into our view's space
                    RectangleF keyboardFrame = UIKeyboard.FrameEndFromNotification (notification);
                    keyboardFrame = ParentView.ConvertRectToView( keyboardFrame, null );

                    // first, get the bottom point of the visible area.
                    Edit_VisibleAreaWithKeyboardBot = ParentView.Bounds.Height - keyboardFrame.Height;

                    // now get the dist between the bottom of the visible area and the text field (text field's pos also changes as we scroll)
                    MaintainEditTextVisibility( );
                }
                else if ( DisplayingKeyboard == true )
                {
                    // get the keyboard frame and transform it into our view's space
                    RectangleF keyboardFrame = UIKeyboard.FrameBeginFromNotification (notification);
                    keyboardFrame = ParentView.ConvertRectToView( keyboardFrame, null );

                    // restore the screen to the way it was before editing
                    ParentScrollView.ContentOffset = Edit_StartScrollOffset;
                    ParentScrollView.Layer.Position = Edit_StartScreenOffset;

                    // reset the tapped textfield area
                    Edit_TappedTextFieldFrame = RectangleF.Empty;

                    DisplayingKeyboard = false;
                }

                //Commit the animation
                UIView.CommitAnimations (); 
            }

            RectangleF GetTappedTextFieldFrame( RectangleF textFrame )
            {
                // first subtract the amount scrolled by the view.
                float yPos = textFrame.Y - ParentScrollView.ContentOffset.Y;
                float xPos = textFrame.X - ParentScrollView.ContentOffset.X;

                // now add in however far down the scroll view is from the top.
                yPos += ParentScrollView.Frame.Y;
                xPos += ParentScrollView.Frame.X;

                return new RectangleF( xPos, yPos, textFrame.Width, textFrame.Height );
            }

            public void OnTextFieldDidBeginEditing( NSNotification notification )
            {
                Edit_TappedTextFieldFrame = GetTappedTextFieldFrame( ( (NSValue)notification.Object ).RectangleFValue );
            }

            public void OnTextFieldChanged( NSNotification notification )
            {
                Edit_TappedTextFieldFrame = GetTappedTextFieldFrame( ( (NSValue)notification.Object ).RectangleFValue );
                MaintainEditTextVisibility( );
            }

            protected void MaintainEditTextVisibility( )
            {
                // no need to do anything if a hardware keyboard is attached.
                if( DisplayingKeyboard == true )
                {
                    // PLUS makes it scroll "up"
                    // NEG makes it scroll "down"
                    // TextField position MOVES AS THE PAGE IS SCROLLED.
                    // It is always relative, however, to the screen. So, if it's near the top, it's 0,
                    // whether that's because it was moved down and the screen scrolled up, or it's just at the top naturally.

                    // Scroll the view so tha the bottom of the text field is as close as possible to
                    // the top of the keyboard without violating scroll constraints

                    // determine if they're typing near the bottom of the screen and it needs to scroll.
                    float scrollAmount = (Edit_VisibleAreaWithKeyboardBot - Edit_TappedTextFieldFrame.Bottom);

                    // clamp to the legal amount we can scroll "down"
                    // Don't factor in a negative ContentOffset. That could only happen if the view isn't actually going to scroll because all content fits on it.
                    scrollAmount = System.Math.Min( scrollAmount, System.Math.Max( 0, ParentScrollView.ContentOffset.Y ) ); 

                    // Now determine the amount of "up" scroll remaining
                    float maxScrollAmount = ParentScrollView.ContentSize.Height - ParentScrollView.Bounds.Height;
                    float scrollAmountDistRemainingDown = -(maxScrollAmount - ParentScrollView.ContentOffset.Y);

                    // and clamp the scroll amount to that, so we don't scroll "up" beyond the contraints
                    float allowedScrollAmount = System.Math.Max( scrollAmount, scrollAmountDistRemainingDown );
                    ParentScrollView.ContentOffset = new PointF( ParentScrollView.ContentOffset.X, ParentScrollView.ContentOffset.Y - allowedScrollAmount );

                    // if we STILL haven't scrolled enough "up" because of scroll contraints, we'll allow the window itself to move up.
                    float scrollDistNeeded = -System.Math.Min( 0, scrollAmount - scrollAmountDistRemainingDown );
                    ParentScrollView.Layer.Position = new PointF( ParentScrollView.Layer.Position.X, ParentScrollView.Layer.Position.Y - scrollDistNeeded );
                }
            }
        }

        public class iOSCommon
        {
            public static UIFont LoadFontDynamic( String name, float fontSize )
            {
                // first attempt to simpy load it (it may be loaded already)
                UIFont uiFont = UIFont.FromName(name, fontSize );

                // failed, so attempt to load it dynamically
                if( uiFont == null )
                {
                    // get a path to our custom fonts folder
                    String fontPath = NSBundle.MainBundle.BundlePath + "/Fonts/" + name + ".ttf";

                    // build a data model for the font
                    CGDataProvider fontProvider = MonoTouch.CoreGraphics.CGDataProvider.FromFile(fontPath);

                    // create a renderable font out of it
                    CGFont newFont = MonoTouch.CoreGraphics.CGFont.CreateFromProvider(fontProvider);

                    // get the legal loadable font name
                    String fontScriptName = newFont.PostScriptName;

                    // register the font with the CoreText / UIFont system.
                    NSError error = null;
                    bool result = CTFontManager.RegisterGraphicsFont(newFont, out error);
                    if(result == false) throw new NSErrorException( error );

                    uiFont = UIFont.FromName(fontScriptName, fontSize );

                    // release the CT reference to the font, leaving only the UI manager's referene.
                    result = CTFontManager.UnregisterGraphicsFont(newFont, out error);
                    if(result == false) throw new NSErrorException( error );
                }

                return uiFont;
            }
        }
    }
}

#endif
