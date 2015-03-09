#if __IOS__

using System;
using System.Drawing;
using UIKit;
using Foundation;
using Rock.Mobile.Util.Strings;
using CoreGraphics;
using Rock.Mobile.PlatformSpecific.iOS.Graphics;
using Rock.Mobile.Animation;

namespace Rock.Mobile.PlatformSpecific.iOS.UI
{
    public class WebLayout
    {
        public enum Result
        {
            Success,
            Fail,
            Cancel
        }

        public delegate void LoadResult( Result result, string url );

        public UIView ContainerView { get; set; }
        UIWebView WebView { get; set; }
        LoadResult LoadResultHandler { get; set; }
        UIActivityIndicatorView ProgressBar { get; set; }
        UIButton CancelButton { get; set; }

        public WebLayout( CGRect frame )
        {
            ContainerView = new UIView( frame );

            WebView = new UIWebView( );
            WebView.Layer.AnchorPoint = CGPoint.Empty;
            WebView.Frame = new CGRect( 0, 0, ContainerView.Frame.Width, ContainerView.Frame.Height );
            WebView.LoadStarted += (object s, EventArgs eArgs) => 
                {
                    ProgressBar.Hidden = false;
                };

            WebView.LoadError += (object s, UIWebErrorArgs eArgs) => 
                {
                    ProgressBar.Hidden = true;
                    LoadResultHandler( Result.Fail, WebView.Request.Url.ToString() );
                };

            WebView.LoadFinished += (object s, EventArgs eArgs) => 
                {
                    ProgressBar.Hidden = true;
                    LoadResultHandler( Result.Success, WebView.Request.Url.ToString() );
                };
            ContainerView.AddSubview( WebView );

            ProgressBar = new UIActivityIndicatorView( UIActivityIndicatorViewStyle.WhiteLarge );
            ProgressBar.Color = UIColor.Gray;
            ProgressBar.StartAnimating( );
            ContainerView.AddSubview( ProgressBar );

            ProgressBar.Layer.Position = new CGPoint (ContainerView.Bounds.Width / 2, ContainerView.Bounds.Height / 2);
            ProgressBar.Hidden = true;

            CancelButton = UIButton.FromType( UIButtonType.System );
            CancelButton.Frame = frame;
            CancelButton.SetTitle( "Cancel", UIControlState.Normal );
            CancelButton.SizeToFit( );
            CancelButton.Frame = new CGRect( (frame.Width - CancelButton.Frame.Width) / 2, frame.Bottom - CancelButton.Frame.Height, CancelButton.Frame.Width, CancelButton.Frame.Height );
            CancelButton.TouchUpInside += (object sender, EventArgs e ) =>
                {
                    // guard against a null handler, which might happen if cancel is pressed before LoadUrl is called.
                    if( LoadResultHandler != null )
                    {
                        LoadResultHandler( Result.Cancel, "" );
                    }
                };

            ContainerView.AddSubview( CancelButton );

            WebView.Bounds = new CGRect( 0, 0, WebView.Bounds.Width, WebView.Bounds.Height - CancelButton.Bounds.Height );
        }

        public void LoadUrl( string url, LoadResult resultHandler )
        {
            LoadResultHandler = resultHandler;

            // invoke a webview
            WebView.LoadRequest( new NSUrlRequest( new NSUrl( url ) ) );
        }

        public void DeleteCacheandCookies ()
        {
            NSUrlCache.SharedCache.RemoveAllCachedResponses ();
            NSHttpCookieStorage storage = NSHttpCookieStorage.SharedStorage;

            foreach (var item in storage.Cookies) {
                storage.DeleteCookie (item);
            }
            NSUserDefaults.StandardUserDefaults.Synchronize ();
        }

        public void SetCancelButtonColor( uint color )
        {
            CancelButton.SetTitleColor( Rock.Mobile.PlatformUI.Util.GetUIColor( color ), UIControlState.Normal );
        }
    }

    /// <summary>
    /// A simple banner billerboard that sizes to fit the icon and label
    /// given it, and can animate in our out. A delegate is called
    /// when the banner is clicked.
    /// The parent class is a fullscreen overlay, and the banner is a subview within it.
    /// This allows us to place a button covering the entire screen (but behind the banner)
    /// and dismiss the banner if the screen is tapped away.
    /// </summary>
    public class NotificationBillboard : UIView
    {
        public override UIView HitTest(CGPoint point, UIEvent uievent)
        {
            // transform the point into absolute coords
            CGPoint absolutePoint = new CGPoint( point.X + Frame.Left,
                                                 point.Y + Frame.Top );

            // now is that tap within the frame bounds?
            if ( Frame.Contains( absolutePoint ) )
            {
                // yup, now is the point in frame space (which is what we got in the first place)
                // within the banner? (Which is also in frame space, not absolute)
                if ( Banner.Frame.Contains( point ) )
                {
                    // only fire off the action if the banner is visible
                    if ( Hidden == false )
                    {
                        // fire off the clicked notification
                        OnClickAction( null, null );
                    }
                }

                // hide the billboard
                Hide( );
            }

            // no matter what, don't consume the touch input.
            return null;
        }

        /// <summary>
        /// The actual visible notification banner that the icon and label are in.
        /// </summary>
        /// <value>The banner.</value>
        UIView Banner { get; set; }

        /// <summary>
        /// The label representing the icon to display
        /// </summary>
        /// <value>The icon.</value>
        UILabel Icon { get; set; }

        /// <summary>
        /// The label that displays the text to show
        /// </summary>
        /// <value>The label.</value>
        UILabel Label { get; set; }

        /// <summary>
        /// The even invoked when the banner is clicked
        /// </summary>
        /// <value>The on click action.</value>
        EventHandler OnClickAction { get; set; }

        /// <summary>
        /// True if the banner is animating (prevents simultaneous animations)
        /// </summary>
        bool Animating { get; set; }

        /// <summary>
        /// The size of the screen, needed for setting up the layout in SetLabel,
        /// and for animating on and off screen.
        /// </summary>
        /// <value>The size of the screen.</value>
        CGSize ScreenSize { get; set; }

        const float AnimationTime = .25f;

        public void Reveal( )
        {
            // if we're not animating
            if ( Animating == false )
            {
                // reveal the banner and flag that we're animating
                Superview.BringSubviewToFront( this );

                Hidden = false;
                Animating = true;

                // create an animator and animate us into view
                SimpleAnimator_Float revealer = new SimpleAnimator_Float( (float)Layer.Position.X, 0, AnimationTime, 
                    delegate(float percent, object value )
                    {
                        Layer.Position = new CGPoint( (float)value, Layer.Position.Y );
                    },
                    delegate
                    {
                        Animating = false;
                    } );

                revealer.Start( );
            }
        }

        public void Hide( )
        {
            // if we're not animating
            if ( Animating == false )
            {
                Animating = true;

                // create a simple animator and animate the banner out of view
                SimpleAnimator_Float revealer = new SimpleAnimator_Float( (float)Layer.Position.X, (float)ScreenSize.Width, AnimationTime, 
                    delegate(float percent, object value )
                    {
                        Layer.Position = new CGPoint( (float)value, Layer.Position.Y );
                    },
                    delegate
                    {
                        // when complete, hide the banner, since there's no need to render it
                        Animating = false;
                        Hidden = true;
                    } );

                revealer.Start( );
            }
        }

        public NotificationBillboard( nfloat displayWidth, nfloat displayHeight )
        {
            Layer.AnchorPoint = CGPoint.Empty;

            Banner = new UIView();
            Banner.Layer.AnchorPoint = CGPoint.Empty;
            AddSubview( Banner );

            Icon = new UILabel();
            Icon.Layer.AnchorPoint = CGPoint.Empty;
            Banner.AddSubview( Icon );

            Label = new UILabel();
            Label.Layer.AnchorPoint = CGPoint.Empty;
            Banner.AddSubview( Label );

            Layer.Position = new CGPoint( displayWidth, Layer.Position.Y );

            ScreenSize = new CGSize( displayWidth, displayHeight );

            Hidden = true;
            Animating = false;
        }

        public void SetLabel( string iconStr, string iconFont, float iconSize, string labelStr, string labelFont, float labelSize, uint textColor, uint bgColor, EventHandler onClick )
        {
            // setup the icon
            Icon.Text = iconStr;
            Icon.Font = FontManager.GetFont( iconFont, iconSize );
            Icon.TextColor = Rock.Mobile.PlatformUI.Util.GetUIColor( textColor );
            Icon.SizeToFit( );

            // setup the label
            Label.Text = labelStr;
            Label.TextColor = Rock.Mobile.PlatformUI.Util.GetUIColor( textColor );
            Label.Font = FontManager.GetFont( labelFont, labelSize );
            Label.SizeToFit( );

            // get the dimensions that the banner needs to be
            nfloat totalTextWidth = (Icon.Frame.Width * 2) + Label.Frame.Width;
            nfloat totalTextHeight = Label.Frame.Height;

            // setup the banner
            Banner.UserInteractionEnabled = false;
            Banner.BackgroundColor = Rock.Mobile.PlatformUI.Util.GetUIColor( bgColor );
            Banner.Bounds = new CGRect( 0, 0, totalTextWidth + ( totalTextWidth * .25f ), totalTextHeight * 2 );
            Banner.Layer.Position = new CGPoint( ScreenSize.Width - Banner.Bounds.Width, 0 );

            nfloat centerPosX = ( Banner.Bounds.Width - totalTextWidth ) / 2;
            nfloat centerPosY = ( Banner.Bounds.Height - totalTextHeight ) / 2;

            // get the icon vs text difference so we can make sure the icon is centered within the totalTextHeight.
            nfloat heightDelta = Label.Bounds.Height - Icon.Bounds.Height;

            Icon.Layer.Position = new CGPoint( centerPosX, centerPosY + (heightDelta / 2) );
            Label.Layer.Position = new CGPoint( centerPosX + Icon.Bounds.Width * 2, centerPosY );

            OnClickAction = onClick;


            // setup the dismiss button
            Bounds = new CGRect( 0, 0, ScreenSize.Width, ScreenSize.Height );
        }
    }

    /// <summary>
    /// Maintains phone number formatting on any UITextField
    /// </summary>
    public class PhoneNumberFormatterDelegate : UITextFieldDelegate
    {
        public override bool ShouldChangeCharacters(UITextField textField, NSRange range, string replacementString)
        {
            string newString = "";

            // What are we doing?
            if ( range.Location == textField.Text.Length )
            {
                // we're adding text to the END of the string. 
                // Easy, we don't need to do anything to the caret
                Console.WriteLine( "Appending" );

                // just append the new character(s)
                newString = textField.Text.Insert( (int)range.Location, replacementString );

                // next, we'll strip the symbols and re-format it as a phone number
                string numericString = newString.AsNumeric( );
                string finalString = numericString.AsPhoneNumber( );
                textField.Text = finalString;

                return false;
            }
            else if ( replacementString == "" )
            {
                // this means our text is going to shrink. No matter what we started with, we'll
                // end up with less
                Console.WriteLine( "Deleting" );

                // See if we need to adjust the positioning to remove a number.
                string deleteString = textField.Text.Substring( (int)range.Location, (int)range.Length );
                bool hasNumbers = HasNumericChars( deleteString );

                // if it DOES NOT have a number, their intention is to
                // delete the number nearest their cursor
                if ( hasNumbers == false )
                {
                    Console.WriteLine( "No number for delete. Finding nearest.", hasNumbers );

                    // find the number they intended to delete
                    int nearestNumber = FindNearestNumber( textField.Text, (int)range.Location, -1 );

                    // so assign the range so it deletes that.
                    range.Location = nearestNumber;
                    range.Length = 1;
                }

                // remove the chunk of the string
                newString = textField.Text.Remove( (int)range.Location, (int)range.Length );

                // next, we'll strip the symbols and re-format it as a phone number
                string numericString = newString.AsNumeric( );
                string finalString = numericString.AsPhoneNumber( );


                // now the critical part. Figure out how many symbols lead up to the location
                // in the original AND new strings.
                int numSymbolsToIndexStart = GetNumSymbolsToIndex( textField.Text, (int)range.Location );
                int numSymbolsToIndexFinal = GetNumSymbolsToIndex( finalString, (int)range.Location );

                // that difference is how many characters back we need to adjust
                int deltaSymbols = System.Math.Abs( numSymbolsToIndexStart - numSymbolsToIndexFinal );

                textField.Text = finalString;

                // lastly place the caret where it belongs
                UITextPosition currPos = textField.GetPosition( textField.BeginningOfDocument, range.Location - deltaSymbols );
                UITextPosition endPos = textField.GetPosition( currPos, 0 );

                textField.SelectedTextRange = textField.GetTextRange( currPos, endPos );
                return false;
            }
            else
            {
                // We're INSERTING text to somewhere within the string.
                if ( range.Length == 0 )
                {
                    Console.WriteLine( "Inserting" );
                    if ( range.Length > 0 )
                    {
                        // if there's a length, part of the existing string is being replaced
                        newString = textField.Text.Remove( (int)range.Location, (int)range.Length );
                        newString = newString.Insert( (int)range.Location, replacementString );
                    }
                    else
                    {
                        // otherwise something is being inserted or appended
                        newString = textField.Text.Insert( (int)range.Location, replacementString );
                    }

                    // next, we'll strip the symbols and re-format it as a phone number
                    string numericString = newString.AsNumeric( );
                    string finalString = numericString.AsPhoneNumber( );

                    int deltaLen = finalString.Length - textField.Text.Length;

                    textField.Text = finalString;

                    // lastly place the caret where it belongs
                    UITextPosition currPos = textField.GetPosition( textField.BeginningOfDocument, range.Location + deltaLen );
                    UITextPosition endPos = textField.GetPosition( currPos, 0 );

                    textField.SelectedTextRange = textField.GetTextRange( currPos, endPos );
                    return false;
                }
                else
                {
                    // don't handle craziness of pasting WITHIN the field. Let them do that
                    // and then adjust it themselves.
                    Console.WriteLine( "Pasting in. Ignoring" );
                    return true;
                }
            }
        }

        /// <summary>
        /// Given an index, returns the number of symbols (non-digits) leading up to index.
        /// </summary>
        int GetNumSymbolsToIndex( string source, int index )
        {
            int numSymbols = 0;
            for( int i = 0; i < System.Math.Min( source.Length, index ); i++ )
            {
                if( IsNumeric( source[ i ] ) == false )
                {
                    numSymbols++;
                }
            }

            return numSymbols;
        }

        /// <summary>
        /// Given an index and direction, finds the nearest digit (non-alpha) and returns that index.
        /// </summary>
        int FindNearestNumber( string text, int startLocation, int direction )
        {
            // to our left?
            if ( direction == -1 )
            {
                startLocation = System.Math.Min( startLocation, text.Length - 1 );
                for ( int i = startLocation; i >= 0; i-- )
                {
                    if ( IsNumeric( text[ i ] ) )
                    {
                        return i;
                    }
                }
            }
            // to our right?
            else
            {
                startLocation = System.Math.Max( startLocation, 0 );
                for ( int i = startLocation; i < text.Length; i++ )
                {
                    if ( IsNumeric( text[ i ] ) )
                    {
                        return i;
                    }
                }
            }

            return startLocation;
        }

        /// <summary>
        /// True if a string contains digits
        /// </summary>
        bool HasNumericChars( string source )
        {
            for( int i = 0; i < source.Length; i++ )
            {
                if( IsNumeric( source[ i ] ) == true )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// True if the character is a digit
        /// </summary>
        bool IsNumeric( char character )
        {
            if( character >= '0' && character <= '9' )
            {
                return true;
            }

            return false;
        }
    }

    class BlockerView : UIView
    {
        public UIActivityIndicatorView ActivityIndicator { get; set; }

        public BlockerView( CGRect frame ) : base( frame )
        {
            ActivityIndicator = new UIActivityIndicatorView( );
            ActivityIndicator.ActivityIndicatorViewStyle = UIActivityIndicatorViewStyle.WhiteLarge;
            ActivityIndicator.StartAnimating( );
            ActivityIndicator.SizeToFit( );
            ActivityIndicator.Layer.AnchorPoint = CGPoint.Empty;
            ActivityIndicator.Layer.Position = new CGPoint( ( frame.Width - ActivityIndicator.Bounds.Width ) / 2, ( frame.Height - ActivityIndicator.Bounds.Height ) / 2 );

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
                new Action( delegate 
                    { 
                        Layer.Opacity = .80f; 
                    } )
                , new Action( delegate 
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
                new Action( delegate 
                    { 
                        Layer.Opacity = 0.00f;
                    } )
                , new Action( delegate 
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
    /// Utility class that makes sure a TargetView isn't obstructed by a picker.
    /// Usage: Instantiate and pass in the parent view and child scroll view (Hierarchy must be View->UIScrollView->Whatever)
    /// Additionally, create a label and set its styling and text ONLY. This is used as the "header text" for the picker.
    /// Lastly pass in the target view that should be in focus, and the model for your picker.
    /// To reveal, call TogglePicker
    /// </summary>
    public class PickerAdjustManager
    {
        /// <summary>
        /// The picker used for selecting an item
        /// </summary>
        public UIView Picker { get; protected set; }

        /// <summary>
        /// The picker label that tells the user what they're picking
        /// </summary>
        UILabel PickerLabel { get; set; }

        /// <summary>
        /// True when the picker is revealed
        /// </summary>
        public bool Revealed { get; protected set; }

        /// <summary>
        /// The parent view
        /// </summary>
        /// <value>The parent view.</value>
        UIView ParentView { get; set; }

        /// <summary>
        /// The parent scroll view
        /// </summary>
        /// <value>The parent scroll view.</value>
        UIScrollView ParentScrollView { get; set; }

        /// <summary>
        /// The view the picker interacts with and scrolls into view
        /// </summary>
        /// <value>The target label.</value>
        UIView Target { get; set; }

        /// <summary>
        /// The starting position of the scrollView so we can restore after the user uses the UIPicker
        /// </summary>
        /// <value>The starting scroll position.</value>
        CGPoint StartingScrollPos { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rock.Mobile.PlatformSpecific.iOS.UI.PickerAdjustManager"/> class.
        /// </summary>
        /// <param name="parentView">Parent view.</param>
        /// <param name="parentScrollView">Parent scroll view. (must be direct descendant of parent view)</param>
        /// <param name="pickerLabel">Header label for the picker. Set only the text and styling.</param>
        /// <param name="targetView">Target view that should go into focus.</param>
        /// <param name="pickerModel">Picker model.</param>
        public PickerAdjustManager( UIView parentView, UIScrollView parentScrollView, UILabel pickerLabel, UIView targetView )
        {
            ParentView = parentView;
            ParentScrollView = parentScrollView;
            Target = targetView;

            // setup the category picker and selector button
            PickerLabel = pickerLabel;
            PickerLabel.Layer.AnchorPoint = CGPoint.Empty;
            PickerLabel.SizeToFit( );
            PickerLabel.Layer.Position = new CGPoint( ( ParentView.Bounds.Width - PickerLabel.Bounds.Width ) / 2, ParentView.Frame.Bottom );
        }

        public void SetPicker( UIView picker )
        {
            Picker = picker;
            Picker.Layer.AnchorPoint = CGPoint.Empty;
            Picker.Layer.Position = new CGPoint( 0, PickerLabel.Frame.Top + 10 );
            Picker.Bounds = new CGRect( 0, 0, ParentView.Bounds.Width, 100 ); //force the width smaller so it fits on all devices.
            ParentView.AddSubview( Picker );

            ParentView.AddSubview( PickerLabel );
        }

        /// <summary>
        /// Shows / Hides the category picker by animating the picker onto the screen and scrolling
        /// the ScrollView to reveal the category field.
        /// </summary>
        public void TogglePicker( bool enabled )
        {
            if ( Picker == null )
            {
                throw new Exception( "Call SetPicker before using TogglePicker!" );
            }

            // only do something if there's a state change
            if ( Revealed != enabled )
            {
                //Start an animation
                UIView.BeginAnimations( "AnimateForPicker" );
                UIView.SetAnimationBeginsFromCurrentState( true );
                UIView.SetAnimationDuration( .5f );
                UIView.SetAnimationCurve( UIViewAnimationCurve.EaseInOut );

                if ( enabled == true )
                {
                    // stamp the scroll position of the scrollview
                    StartingScrollPos = ParentScrollView.ContentOffset;

                    // set the picker to be on screen
                    PickerLabel.Layer.Position = new CGPoint( PickerLabel.Layer.Position.X, ParentView.Bounds.Height - (PickerLabel.Bounds.Height + Picker.Bounds.Height) );
                    Picker.Layer.Position = new CGPoint( 0, PickerLabel.Frame.Top + 10 );

                    // scroll the category field into view and lock scrolling
                    ParentScrollView.ContentOffset = new CGPoint( 0, Target.Frame.Top - Target.Frame.Height );
                    ParentScrollView.ScrollEnabled = false;
                }
                else
                {
                    // we're hiding the picker, so restore the original scroll position and enable it
                    ParentScrollView.ContentOffset = StartingScrollPos;
                    ParentScrollView.ScrollEnabled = true;

                    // move the picker off screen
                    PickerLabel.Layer.Position = new CGPoint( PickerLabel.Layer.Position.X, ParentView.Frame.Bottom );
                    Picker.Layer.Position = new CGPoint( 0, PickerLabel.Frame.Top + 10 );
                }

                Revealed = enabled;

                //Commit the animation
                UIView.CommitAnimations( ); 
            }
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
        public static NSString TextFieldDidBeginEditingNotification = new NSString( "TextFieldDidBeginEditing" );

        public static NSString TextFieldChangedNotification = new NSString( "TextFieldChanged" );

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
        CGRect Edit_TappedTextFieldFrame { get; set; }

        /// <summary>
        /// The amount the scrollView was scrolled when editing began.
        /// Used so we can restore the scrollView position when editing is finished.
        /// </summary>
        /// <value>The edit start scroll offset.</value>
        CGPoint Edit_StartScrollOffset { get; set; }

        /// <summary>
        /// The position of the UIScrollView when text editing began.
        /// </summary>
        /// <value>The edit start screen offset.</value>
        CGPoint Edit_StartScreenOffset { get; set; }

        /// <summary>
        /// The bottom position of the visible area when the keyboard is up.
        /// </summary>
        /// <value>The edit visible area with keyboard bot.</value>
        nfloat Edit_VisibleAreaWithKeyboardBot { get; set; }

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
                CGRect keyboardFrame = UIKeyboard.FrameEndFromNotification (notification);
                keyboardFrame = ParentView.ConvertRectToView( keyboardFrame, null );

                // first, get the bottom point of the visible area.
                Edit_VisibleAreaWithKeyboardBot = ParentView.Bounds.Height - keyboardFrame.Height;

                // now get the dist between the bottom of the visible area and the text field (text field's pos also changes as we scroll)
                MaintainEditTextVisibility( );
            }
            else if ( DisplayingKeyboard == true )
            {
                // get the keyboard frame and transform it into our view's space
                CGRect keyboardFrame = UIKeyboard.FrameBeginFromNotification (notification);
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

        CGRect GetTappedTextFieldFrame( RectangleF textFrame )
        {
            // first subtract the amount scrolled by the view.
            nfloat yPos = textFrame.Y - ParentScrollView.ContentOffset.Y;
            nfloat xPos = textFrame.X - ParentScrollView.ContentOffset.X;

            // now add in however far down the scroll view is from the top.
            yPos += ParentScrollView.Frame.Y;
            xPos += ParentScrollView.Frame.X;

            return new CGRect( xPos, yPos, textFrame.Width, textFrame.Height );
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
                nfloat scrollAmount = (Edit_VisibleAreaWithKeyboardBot - Edit_TappedTextFieldFrame.Bottom);

                // clamp to the legal amount we can scroll "down"
                // Don't factor in a negative ContentOffset. That could only happen if the view isn't actually going to scroll because all content fits on it.
                scrollAmount = System.Math.Min( (float)scrollAmount, System.Math.Max( 0, (float) ParentScrollView.ContentOffset.Y ) ); 

                // Now determine the amount of "up" scroll remaining
                nfloat maxScrollAmount = ParentScrollView.ContentSize.Height - ParentScrollView.Bounds.Height;
                nfloat scrollAmountDistRemainingDown = -(maxScrollAmount - ParentScrollView.ContentOffset.Y);

                // and clamp the scroll amount to that, so we don't scroll "up" beyond the contraints
                nfloat allowedScrollAmount = System.Math.Max( (float)scrollAmount, (float)scrollAmountDistRemainingDown );
                ParentScrollView.ContentOffset = new CGPoint( ParentScrollView.ContentOffset.X, ParentScrollView.ContentOffset.Y - allowedScrollAmount );

                // if we STILL haven't scrolled enough "up" because of scroll contraints, we'll allow the window itself to move up.
                nfloat scrollDistNeeded = -System.Math.Min( 0, (float) (scrollAmount - scrollAmountDistRemainingDown) );
                ParentScrollView.Layer.Position = new CGPoint( ParentScrollView.Layer.Position.X, ParentScrollView.Layer.Position.Y - scrollDistNeeded );
            }
        }
    }
}
#endif
