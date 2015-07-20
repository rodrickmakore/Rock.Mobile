using System;
#if __ANDROID__
using Android.Graphics;
using Rock.Mobile.IO;

namespace Rock.Mobile.PlatformSpecific.Android.Util
{
    public class AsyncLoader
    {
        public delegate bool OnLoaded( Bitmap image );
        public static void LoadImage( string imageName, bool bundled, bool scaleForDpi, OnLoaded onLoaded )
        {
            // load it on another thread
            Rock.Mobile.Threading.Util.PerformOnWorkerThread( delegate
                {
                    BitmapFactory.Options decodeOptions = new BitmapFactory.Options( );
                    Bitmap imageBmp = null;

                    try
                    {
                        // if true, the image is a hdpi image and should be scaled to an appropriate size of the device
                        if( scaleForDpi == true )
                        {
                            decodeOptions.InSampleSize = (int)System.Math.Ceiling( Rock.Mobile.PlatformSpecific.Android.Core.Context.Resources.DisplayMetrics.Density );
                        }

                        // if it was bundled, take it from our assets
                        if( bundled == true )
                        {
                            System.IO.Stream bundleStream = Rock.Mobile.PlatformSpecific.Android.Core.Context.Assets.Open( imageName );
                            if( bundleStream != null )
                            {
                                imageBmp = BitmapFactory.DecodeStream( bundleStream, null, decodeOptions );
                                bundleStream.Dispose( );
                            }
                            else
                            {
                                Rock.Mobile.Util.Debug.WriteLine( string.Format( "ASYNCLOAD ERROR: Failed to load image {0}", imageName ) );
                            }

                        }
                        // else filecache
                        else
                        {
                            System.IO.MemoryStream assetStream = (System.IO.MemoryStream)FileCache.Instance.LoadFile( imageName );
                            if( assetStream != null )
                            {
                                imageBmp = BitmapFactory.DecodeStream( assetStream, null, decodeOptions );
                                if( imageBmp == null )
                                {
                                    Rock.Mobile.Util.Debug.WriteLine( string.Format( "ASYNCLOAD ERROR: Image loaded null. {0}", imageName ) );
                                }
                                assetStream.Dispose( );
                            }
                            else
                            {
                                Rock.Mobile.Util.Debug.WriteLine( string.Format( "ASYNCLOAD ERROR: Failed to load image {0}", imageName ) );
                            }
                        }
                    }
                    catch( Exception e )
                    {
                        Rock.Mobile.Util.Debug.WriteLine( string.Format( "ASYNCLOAD ERROR: Failed to load image {0}", imageName ) );
                        Xamarin.Insights.Report( e );
                    }

                    // update the image banner on the UI thread
                    Rock.Mobile.Threading.Util.PerformOnUIThread( delegate
                        {
                            // if they didn't consume this callback, we need to dispose the bitmap ourselves.
                            if( onLoaded( imageBmp ) == false )
                            {
                                // dispose only if it loaded safely
                                if( imageBmp != null )
                                {
                                    imageBmp.Dispose( );
                                }
                            }
                        });
                } );
        }
    }
}
#endif