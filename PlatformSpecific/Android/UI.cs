#if __ANDROID__
using System;
using Android.App;
using Android.Widget;
using Android.Webkit;
using Android.Views;
using Android.Views.InputMethods;
using Android.Util;

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
}

#endif