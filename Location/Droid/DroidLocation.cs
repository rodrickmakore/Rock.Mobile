#if __ANDROID__
using System;
using Android.App;
using Android.Content;
using Android.OS;
using DroidLocationServices;

namespace DroidLocationServices
{    
    public class DroidLocation : Location.Location, ILocationDelegateHandler
    {
        // our connection to the "delegate service" that is the go-between for us
        // and the actual locationService
        public LocationDelegateConnection LocationDelegateConnection { get; protected set; }
        public LocationDelegateBinder DelegateBinder { get; protected set; }

        public override void Create(object context)
        {
            ((Android.Content.Context)context).StartService( new Intent( (Android.Content.Context)context, typeof( DroidLocationServices.LocationDelegateService ) ) );
        }

        public override void EnterBackground(object context)
        {
            ((Android.Content.Context)context).UnbindService( LocationDelegateConnection );
        }

        public override void EnterForeground(object context)
        {
            ((Android.Content.Context)context).BindService( new Intent( (Android.Content.Context)context, typeof( DroidLocationServices.LocationDelegateService )  ), 
                                                            LocationDelegateConnection, Bind.AutoCreate );
        }

        public override void BeginAddRegionsForTrack()
        {
            DelegateBinder.Service.BeginAddRegionsForTrack( );
        }

        public override void AddRegionForTrack( string region, double latitude, double longitude, float radius )
        {
            DelegateBinder.Service.AddRegionForTrack( region, latitude, longitude, radius );
        }

        public override void AddLandmarkToRegion( string parentRegion, string landmark, double latitude, double longitude )
        {
            DelegateBinder.Service.AddLandmarkToRegion( parentRegion, landmark, latitude, longitude );
        }

        public override void CommitRegionsForTrack()
        {
            DelegateBinder.Service.CommitRegionsForTrack( );
        }


        public DroidLocation( ) : base( )
        {
            // establish our connection object
            LocationDelegateConnection = new LocationDelegateConnection( this );
        }

        public void ServiceConnected( IBinder binder )
        {
            Rock.Mobile.Util.Debug.WriteToLog( "DroidLocation::ServiceConnected() - We are bound to LocationDelegateService" );

            DelegateBinder = (LocationDelegateBinder)binder;

            // give the service an instance of ourselves so it can notify us of events
            DelegateBinder.Service.SetHandler( this );

            // now wait for "OnReady", which means it can being processing our requests
        }

        public void ServiceDisconnected( )
        {
            Rock.Mobile.Util.Debug.WriteToLog( "DroidLocation::ServiceDisconnected() - We are UNBOUND from LocationDelegateService" );

            if ( DelegateBinder != null )
            {
                DelegateBinder.Service.SetHandler( null );
                DelegateBinder = null;
            }
        }

        public void OnReady( )
        {
            Rock.Mobile.Util.Debug.WriteToLog( "OnReady() - We are bound to LocationDelegate, and it is ready for us to give it work." );

            if ( OnReadyDelegate != null )
            {
                OnReadyDelegate( );
            }
        }

        public void OnRegionEntered( string region )
        {
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "DroidLocation::ENTERING REGION {0}", region ) );

            if ( OnRegionEnteredDelegate != null )
            {
                OnRegionEnteredDelegate( region );
            }
        }

        public void OnRegionExited( string region, bool outsideAllRegions )
        {
            // display that we're leaving this region
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "DroidLocation::LEAVING REGION {0} (and OutsideAllRegions: {1}", region, outsideAllRegions ) );

            if ( OnRegionExitedDelegate != null )
            {
                OnRegionExitedDelegate( region, outsideAllRegions );
            }
        }

        public void OnLandmarkChanged( string landmark )
        {
            Console.WriteLine( string.Format( "DroidLocation::LANDMARK CHANGED {0}", landmark ) );

            OnLandmarkChangedDelegate( landmark );
        }

        public void OnIntenseScanExpired( )
        {
            Console.WriteLine( string.Format( "DroidLocation::OnIntenseScanExpired" ) );
        }
    }
}
#endif
