﻿#if __IOS__
using System;
using MonoTouch.UIKit;
using MonoTouch.AssetsLibrary;
using MonoTouch.Foundation;
using MonoTouch.CoreImage;
using MonoTouch.CoreGraphics;
using System.Runtime.InteropServices;
using MonoTouch.ImageIO;
using MonoTouch.CoreFoundation;

namespace Rock.Mobile
{
    namespace Media
    {
        class iOSCamera : PlatformCamera
        {
            class CameraController : UIImagePickerController
            {
                public override bool ShouldAutorotate()
                {
                    if ( UIDeviceOrientation.Portrait == UIDevice.CurrentDevice.Orientation )
                    {
                        return true;
                    }
                    return false;
                }

                public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations( )
                {
                    return UIInterfaceOrientationMask.Portrait;
                }

                public override UIInterfaceOrientation PreferredInterfaceOrientationForPresentation( )
                {
                    return UIInterfaceOrientation.Portrait;
                }
            }

            protected CaptureImageEvent CaptureImageEventDelegate { get; set; }

            public override bool IsAvailable( )
            {
                return UIImagePickerController.IsSourceTypeAvailable( UIImagePickerControllerSourceType.Camera );
            }

            public override void CaptureImage( object imageDest, object context, CaptureImageEvent callback )
            {
                // first ensure they passed in the correct context type
                UIViewController controller = context as UIViewController;
                if( context == null )
                {
                    throw new Exception( "Context must be a UIViewController" );
                }

                string imageDestStr = imageDest as string;
                if( imageDestStr == null )
                {
                    throw new Exception( "imageDest must be of type string." );
                }

                // store our callback event
                CaptureImageEventDelegate = callback;


                // create our camera controller
                CameraController cameraController = new CameraController( );
                cameraController.Delegate = new UIImagePickerControllerDelegate( );
                cameraController.SourceType = UIImagePickerControllerSourceType.Camera;

                // when media is chosen
                cameraController.FinishedPickingMedia += (object sender, UIImagePickerMediaPickedEventArgs e) => 
                    {
                        bool result = false;
                        string imagePath = null;

                        // create a url of the path for the file to write
                        NSUrl imageDestUrl = NSUrl.CreateFileUrl( new string[] { imageDestStr } );

                        // create a CGImage destination that converts the image to jpeg
                        CGImageDestination cgImageDest = CGImageDestination.FromUrl( imageDestUrl, MonoTouch.MobileCoreServices.UTType.JPEG, 1 );

                        if( cgImageDest != null )
                        {
                            // note: the edited image is saved "correctly", so we don't have to rotate.

                            // rotate the image 0 degrees since we consider portrait to be the default position.
                            CIImage ciImage = new CIImage( e.OriginalImage.CGImage );

                            float rotationDegrees = 0.00f;
                            switch ( e.OriginalImage.Orientation )
                            {
                                case UIImageOrientation.Up:
                                {
                                    // don't do anything. The image space and the user space are 1:1
                                    break;
                                }
                                case UIImageOrientation.Left:
                                {
                                    // the image space is rotated 90 degrees from user space,
                                    // so do a CCW 90 degree rotation
                                    rotationDegrees = 90.0f;
                                    break;
                                }
                                case UIImageOrientation.Right:
                                {
                                    // the image space is rotated -90 degrees from user space,
                                    // so do a CW 90 degree rotation
                                    rotationDegrees = -90.0f;
                                    break;
                                }
                                case UIImageOrientation.Down:
                                {
                                    rotationDegrees = 180;
                                    break;
                                }
                            }

                            // create our transform and apply it to the image
                            CGAffineTransform transform = CGAffineTransform.MakeIdentity( );
                            transform.Rotate( rotationDegrees * Rock.Mobile.Math.Util.DegToRad );
                            CIImage rotatedImage = ciImage.ImageByApplyingTransform( transform );

                            // create a context and render it back out to a CGImage.
                            CIContext ciContext = CIContext.FromOptions( null );
                            CGImage rotatedCGImage = ciContext.CreateCGImage( rotatedImage, rotatedImage.Extent );

                            // put the image in the destination, converting it to jpeg.
                            cgImageDest.AddImage( rotatedCGImage, null );


                            // close and dispose.
                            if( cgImageDest.Close( ) )
                            {
                                result = true;
                                imagePath = imageDestStr;

                                cgImageDest.Dispose( );
                            }
                        }

                        // notify the caller we're finished.
                        CameraFinishedCallback( result, imagePath, cameraController );
                    };

                // when picking is cancelled.
                cameraController.Canceled += (object sender, EventArgs e) => 
                    {
                        CameraFinishedCallback( true, null, cameraController );
                    };

                controller.PresentViewController( cameraController, true, null );
            }

            /// <summary>
            /// Wrapper for closing the camera controller and notifying our callback
            /// </summary>
            /// <param name="result">If set to <c>true</c> result.</param>
            /// <param name="imagePath">Image path.</param>
            /// <param name="cameraController">Camera controller.</param>
            protected void CameraFinishedCallback( bool result, string imagePath, UIImagePickerController cameraController )
            {
                // notify the callback on the UI thread
                Rock.Mobile.Threading.UIThreading.PerformOnUIThread( delegate
                    {
                        cameraController.DismissViewController( true, null );
                        CaptureImageEventDelegate( this, new CaptureImageEventArgs( result, imagePath ) );
                    });
            }
        }
    }
}
#endif
