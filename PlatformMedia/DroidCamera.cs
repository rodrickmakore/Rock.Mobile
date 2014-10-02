#if __ANDROID__
using System;
using System.Collections.Generic;

using Android.Content;
using Android.Content.PM;
using Android.Provider;
using Java.IO;
using Android.Graphics;
using Android.App;

using Uri = Android.Net.Uri;
using Environment = Android.OS.Environment;

namespace Rock.Mobile
{
    namespace Media
    {
        /// <summary>
        /// CameraActivity is purely a wrapper / manager for the camera itself. It immediatley forwards
        /// callbacks to the DroidCamera class below this one.
        /// </summary>
        [Activity( Label = "CameraActivity" )]
        class CameraActivity : Activity
        {
            string ImageFile { get; set; }

            protected override void OnCreate( Android.OS.Bundle savedInstanceState )
            {
                base.OnCreate( savedInstanceState );

                // retrieve the desired location

                File imageFile = (Java.IO.File) this.Intent.Extras.Get( "ImageDest" );

                // create our intent and launch the camera
                Intent intent = new Intent( MediaStore.ActionImageCapture );

                // notify the intent where the captured image should go.
                intent.PutExtra( MediaStore.ExtraOutput, Uri.FromFile( imageFile ) );

                // store it as an aboslute string
                ImageFile = imageFile.AbsolutePath;

                StartActivityForResult( intent, 0 );
            }

            protected override void OnResume( )
            {
                base.OnResume( );
            }

            protected override void OnActivityResult( int requestCode, Result resultCode, Intent data )
            {
                base.OnActivityResult( requestCode, resultCode, data );

                // forward this to the camera
                (Rock.Mobile.Media.PlatformCamera.Instance as DroidCamera).CameraResult( resultCode, ImageFile );

                Finish( );
            }
        }

        class DroidCamera : PlatformCamera
        {
            protected CaptureImageEvent CaptureImageEventDelegate { get; set; }
            public File ImageFileDest { get; set; }

            public override bool IsAvailable( )
            {
                // is there an activity that can get pictures (which is true only if there's a camera)
                Intent intent = new Intent( MediaStore.ActionImageCapture );
                IList<ResolveInfo> availableActivities = Rock.Mobile.PlatformCommon.Droid.Context.PackageManager.QueryIntentActivities( intent, PackageInfoFlags.MatchDefaultOnly );

                return availableActivities != null && availableActivities.Count > 0;
            }


            public override void CaptureImage( object imageDest, object context, CaptureImageEvent callback )
            {
                // ensure the context passed in is valid.
                Activity activity = Rock.Mobile.PlatformCommon.Droid.Context as Activity;
                if( activity == null )
                {
                    throw new Exception( "Rock.Mobile.PlatformCommon.Droid.Context must be of type Activity." );
                }

                // store the location they want the file to be in.
                File imageFileDest = imageDest as File;
                if( imageFileDest == null )
                {
                    throw new Exception( "imageDest must be of type File" );
                }

                CaptureImageEventDelegate = callback;

                // kick off the activity that will manage the camera
                Intent intent = new Intent( activity, typeof( CameraActivity ) );
                intent.PutExtra( "ImageDest", imageFileDest );

                activity.StartActivity( intent );
            }

            public void CameraResult( Result resultCode, string imageFile )
            {
                if( resultCode == Result.Ok )
                {
                    // notify our caller it went ok and provide the image
                    CaptureImageEventDelegate( this, new CaptureImageEventArgs( true, imageFile ) );
                }
                else
                {
                    // or provide nothing if it didn't work
                    CaptureImageEventDelegate( this, new CaptureImageEventArgs( false, null ) );
                }

            }
        }
    }
}
#endif
