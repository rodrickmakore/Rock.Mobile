#if __ANDROID__
using System;
using Android.App;
using Android.Widget;
using Android.Webkit;
using Android.Views;
using Android.Views.InputMethods;
using Android.Util;
using System.Drawing;
using Rock.Mobile.PlatformSpecific.Android.Animation;
using CCVApp.Shared.Config;
using Rock.Mobile.PlatformSpecific.Android.Graphics;

namespace Rock.Mobile.PlatformSpecific.Android.UI
{
    class WebLayout : RelativeLayout
    {
        class WebViewLayoutClient : WebViewClient
        {
            public WebLayout Parent { get; set; }

            public override void OnPageFinished(WebView view, string url)
            {
                base.OnPageFinished(view, url);

                Parent.OnPageFinished( view, url );
            }
        }

        WebView WebView { get; set; }
        ProgressBar ProgressBar { get; set; }
        Button CloseButton { get; set; }

        public delegate void PageLoaded( string url );
        PageLoaded PageLoadedHandler { get; set; }

        public WebLayout( global::Android.Content.Context context ) : base( context )
        {
            // required for pre-21 android
            CookieSyncManager.CreateInstance( context );

            CookieManager.Instance.RemoveAllCookie( );

            WebView = new WebView( Rock.Mobile.PlatformSpecific.Android.Core.Context );
            WebView.Settings.SaveFormData = false;

            WebView.ClearFormData( );
            WebView.SetWebViewClient( new WebViewLayoutClient( ) { Parent = this } );
            WebView.Settings.JavaScriptEnabled = true;
            WebView.Settings.SetSupportZoom(true);
            WebView.Settings.BuiltInZoomControls = true;
            WebView.Settings.LoadWithOverviewMode = true; //Load 100% zoomed out
            WebView.ScrollBarStyle = ScrollbarStyles.OutsideOverlay;
            WebView.ScrollbarFadingEnabled = true;

            WebView.VerticalScrollBarEnabled = true;
            WebView.HorizontalScrollBarEnabled = true;
            AddView( WebView );

            ProgressBar = new ProgressBar( Rock.Mobile.PlatformSpecific.Android.Core.Context );
            ProgressBar.Indeterminate = true;
            ProgressBar.SetBackgroundColor( Rock.Mobile.PlatformUI.Util.GetUIColor( 0 ) );
            ProgressBar.LayoutParameters = new RelativeLayout.LayoutParams( ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent );
            ( (RelativeLayout.LayoutParams)ProgressBar.LayoutParameters ).AddRule( LayoutRules.CenterInParent );
            AddView( ProgressBar );
            ProgressBar.BringToFront();
        }

        public void LoadUrl( string url, PageLoaded loadedHandler )
        {
            PageLoadedHandler = loadedHandler;

            WebView.LoadUrl( url );
        }

        public void OnPageFinished( WebView view, string url )
        {
            InputMethodManager imm = ( InputMethodManager )Rock.Mobile.PlatformSpecific.Android.Core.Context.GetSystemService( global::Android.Content.Context.InputMethodService );
            imm.HideSoftInputFromWindow( view.WindowToken, 0 );

            ProgressBar.Visibility = ViewStates.Invisible;
            PageLoadedHandler( url );
        }
    }

    /// <summary>
    /// A simple banner billerboard that sizes to fit the icon and label
    /// given it, and can animate in our out. A delegate is called
    /// when the banner is clicked.
    /// </summary>
    public class NotificationBillboard : RelativeLayout
    {
        /// <summary>
        /// The label representing the icon to display
        /// </summary>
        /// <value>The icon.</value>
        TextView Icon { get; set; }

        /// <summary>
        /// The label that displays the text to show
        /// </summary>
        /// <value>The label.</value>
        TextView Label { get; set; }

        /// <summary>
        /// The even invoked when the banner is clicked
        /// </summary>
        /// <value>The on click action.</value>
        EventHandler OnClickAction { get; set; }

        /// <summary>
        /// An invisible button that covers the entire banner and handles the click
        /// </summary>
        /// <value>The overlay button.</value>
        Button OverlayButton { get; set; }

        /// <summary>
        /// True if the banner is animating (prevents simultaneous animations)
        /// </summary>
        bool Animating { get; set; }

        /// <summary>
        /// The layout that wraps the Icon and Label
        /// </summary>
        LinearLayout TextLayout { get; set; }

        float ScreenWidth { get; set; }

        const float AnimationTime = .25f;

        public void Reveal( )
        {
            // if we're not animating and AREN'T visible
            if ( Animating == false  && Visibility == ViewStates.Gone )
            {
                // reveal the banner and flag that we're animating
                Visibility = ViewStates.Visible;
                Animating = true;

                // create an animator and animate us into view
                SimpleAnimator_Float revealer = new SimpleAnimator_Float( ScreenWidth, 0, AnimationTime, 
                    delegate(float percent, object value )
                    {
                        SetX( (float)value );
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
            // if we're not animating and ARE visible
            if ( Animating == false && Visibility == ViewStates.Visible )
            {
                Animating = true;

                // create a simple animator and animate the banner out of view
                SimpleAnimator_Float revealer = new SimpleAnimator_Float( 0, ScreenWidth, AnimationTime, 
                    delegate(float percent, object value )
                    {
                        SetX( (float)value );
                    },
                    delegate
                    {
                        // when complete, hide the banner, since there's no need to render it
                        Animating = false;
                        Visibility = ViewStates.Gone;
                    } );

                revealer.Start( );
            }
        }

        RelativeLayout BannerLayout { get; set; }
        Button DismissButton { get; set; }

        public NotificationBillboard( float deviceWidth, global::Android.Content.Context context ) : base( context )
        {
            DismissButton = new Button( context );
            DismissButton.LayoutParameters = new RelativeLayout.LayoutParams( ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent );
            DismissButton.SetBackgroundDrawable( null );
            AddView( DismissButton );

            LayoutParameters = new RelativeLayout.LayoutParams( ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent );

            BannerLayout = new RelativeLayout( context );
            BannerLayout.LayoutParameters = new RelativeLayout.LayoutParams( ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent );
            ( (RelativeLayout.LayoutParams)BannerLayout.LayoutParameters ).AddRule( LayoutRules.AlignParentRight );
            AddView( BannerLayout );

            // create a layout that will horizontally align the icon and label
            TextLayout = new LinearLayout( context );
            TextLayout.LayoutParameters = new RelativeLayout.LayoutParams( ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent );
            ( (RelativeLayout.LayoutParams)TextLayout.LayoutParameters ).AddRule( LayoutRules.CenterInParent );
            BannerLayout.AddView( TextLayout );

            Icon = new TextView( context );
            Icon.LayoutParameters = new LinearLayout.LayoutParams( ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent );
            ( (LinearLayout.LayoutParams)Icon.LayoutParameters ).LeftMargin = 10;
            ( (LinearLayout.LayoutParams)Icon.LayoutParameters ).RightMargin = 25;

            ( (LinearLayout.LayoutParams)Icon.LayoutParameters ).TopMargin = 10;
            ( (LinearLayout.LayoutParams)Icon.LayoutParameters ).BottomMargin = 10;
            TextLayout.AddView( Icon );

            Label = new TextView( context );
            Label.LayoutParameters = new LinearLayout.LayoutParams( ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent );
            ( (LinearLayout.LayoutParams)Label.LayoutParameters ).RightMargin = 10;

            ( (LinearLayout.LayoutParams)Label.LayoutParameters ).TopMargin = 10;
            ( (LinearLayout.LayoutParams)Label.LayoutParameters ).BottomMargin = 10;
            TextLayout.AddView( Label );

            // create the button that wraps the layout and handles input
            OverlayButton = new Button( context );
            OverlayButton.LayoutParameters = new RelativeLayout.LayoutParams( ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent );
            OverlayButton.SetBackgroundDrawable( null );
            BannerLayout.AddView( OverlayButton );

            ScreenWidth = deviceWidth;
        }

        public void SetLabel( string iconStr, string labelStr, uint textColor, uint bgColor, EventHandler onClick )
        {
            // don't allow changing WHILE we're animating
            if ( Animating == false )
            {
                // setup the banner
                BannerLayout.SetBackgroundColor( Rock.Mobile.PlatformUI.Util.GetUIColor( bgColor ) );

                // setup the icon
                Icon.Text = iconStr;
                Icon.SetTypeface( FontManager.Instance.GetFont( ControlStylingConfig.Icon_Font_Primary ), global::Android.Graphics.TypefaceStyle.Normal );
                Icon.SetTextSize( ComplexUnitType.Dip, ControlStylingConfig.Small_FontSize );
                Icon.SetTextColor( Rock.Mobile.PlatformUI.Util.GetUIColor( textColor ) );

                // setup the label
                Label.Text = labelStr;
                Label.SetTypeface( FontManager.Instance.GetFont( ControlStylingConfig.Small_Font_Light ), global::Android.Graphics.TypefaceStyle.Normal );
                Label.SetTextSize( ComplexUnitType.Dip, ControlStylingConfig.Small_FontSize );
                Label.SetTextColor( Rock.Mobile.PlatformUI.Util.GetUIColor( textColor ) );

                if ( OnClickAction != null )
                {
                    OverlayButton.Click -= OnClickAction;
                }

                OverlayButton.Click += onClick;
                OnClickAction = onClick;

                // resize the button to fit over the full banner
                int widthMeasureSpec = View.MeasureSpec.MakeMeasureSpec( TextLayout.LayoutParameters.Width, MeasureSpecMode.Unspecified );
                int heightMeasureSpec = View.MeasureSpec.MakeMeasureSpec( TextLayout.LayoutParameters.Height, MeasureSpecMode.Unspecified );
                TextLayout.Measure( widthMeasureSpec, heightMeasureSpec );

                OverlayButton.LayoutParameters.Width = TextLayout.MeasuredWidth;
                OverlayButton.LayoutParameters.Height = TextLayout.MeasuredHeight;

                BannerLayout.LayoutParameters.Width = TextLayout.MeasuredWidth;
                BannerLayout.LayoutParameters.Height = TextLayout.MeasuredHeight;

                // default it to hidden and offscreen
                Visibility = ViewStates.Gone;
                SetX( ScreenWidth );
            }
        }

        public override bool DispatchTouchEvent(MotionEvent e)
        {
            // get the tapped position and the bannerPos's bounding box
            PointF tappedPos = new PointF( e.GetX( ), e.GetY( ) );
            RectangleF bannerBB = new RectangleF( BannerLayout.GetX( ), BannerLayout.GetY( ), BannerLayout.GetX( ) + BannerLayout.Width, BannerLayout.GetY( ) + BannerLayout.Height );

            // if they tapped inside the banner, send the click notification
            if ( bannerBB.Contains( tappedPos ) )
            {
                OnClickAction( null, null );
            }

            // either way dismiss the banner and let the touch input continue
            Hide( );
            return false;
        }
    }
}

#endif