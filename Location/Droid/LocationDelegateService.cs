#if __ANDROID__
using System;
using Android.OS;
using Android.Content;
using Android.App;
using Android.Gms.Common;
using Android.Gms.Location;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Android.Support.V4.App;

namespace DroidLocationServices
{
    public interface ILocationDelegateHandler
    {
        // Called when the service is first connected to this ILocationDelegateHandler via a binder
        void ServiceConnected( IBinder serviceBinder );

        // Called when the service is disconnected
        void ServiceDisconnected( );

        // Called when the LocationDelegateService binds to the LocationService and is
        // ready for the activity to start making requests.
        void OnReady( );

        // Called when the device has entered a region
        void OnRegionEntered( string region );

        // Called when the device has exited a region
        void OnRegionExited( string region, bool outsideAllRegions );

        // Called when the current landmark has changed (or gone to none)
        void OnLandmarkChanged( string landmark );

        // Called when intense scanning expires, and we revert to passive scanning
        void OnIntenseScanExpired( );
    }


    // define the type of class the binder will be
    public class LocationDelegateBinder : Binder
    {
        public LocationDelegateService Service { get; protected set; }
        public LocationDelegateBinder( LocationDelegateService service )
        {
            Service = service;
        }
    }

    public class LocationDelegateConnection : Java.Lang.Object, IServiceConnection
    {
        ILocationDelegateHandler DelegateHandler { get; set; }

        public LocationDelegateConnection( ILocationDelegateHandler serviceHandler )
        {
            DelegateHandler = serviceHandler;
        }

        public void OnServiceConnected( ComponentName name, IBinder serviceBinder )
        {
            LocationDelegateBinder binder = serviceBinder as LocationDelegateBinder;
            if ( binder != null )
            {
                DelegateHandler.ServiceConnected( binder );
            }
        }

        public void OnServiceDisconnected( ComponentName name )
        {
            DelegateHandler.ServiceDisconnected( );
        }
    }


    [Service( Label = "LocationDelegateService" )]
    public partial class LocationDelegateService : Service, ILocationServiceHandler
    {
        // The connection and binder used to bind THIS delegateService to the LocationService
        LocationManagerConnection DroidLocationManagerConnection { get; set; }
        LocationManagerBinder ServiceBinder { get; set; }

        // OUR binder that will be used to bind us to the activity
        IBinder Binder;

        // The handler that wants to manage events we receive.
        ILocationDelegateHandler ILocationDelegateHandler { get; set; }

        public LocationDelegateService( ) : base(  )
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::OnCreate()" );

            DroidLocationManagerConnection = new LocationManagerConnection( this );

            Binder = new LocationDelegateBinder( this );

            ILocationDelegateHandler = null;

            // start the location manager service.
            StartService( new Intent( this, typeof( DroidLocationServices.LocationManagerService ) ) );
        }

        public override void OnDestroy()
        {
            Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::OnDestroy() - Android must need memory. Unbinding from DroidLocationService." );

            ServiceBinder.Service.SetLocationServiceHandler( null );
            UnbindService( DroidLocationManagerConnection );

            base.OnDestroy();
        }

        public override IBinder OnBind(Intent intent)
        {
            Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::OnBind() - Someone is binding to us. (Probably the front-end activity.)" );
            return Binder;
        }

        public void SetHandler( ILocationDelegateHandler iLocationDelegateHandler )
        {
            Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::SetHandler() - Accepting a handler for our events (probably the front-end activity.)" );

            ILocationDelegateHandler = iLocationDelegateHandler;

            if ( ILocationDelegateHandler != null )
            {
                // we need to ensure we're bound to the location service before allowing
                // our handler to make any calls
                if ( ServiceBinder != null )
                {
                    ILocationDelegateHandler.OnReady( );
                }
                // we're not bound, so first bind, then we'll notify our handler
                else
                {
                    TryBindLocationService( );
                }
            }
        }

        void TryBindLocationService( )
        {
            // if we aren't bound, bind.
            if ( ServiceBinder == null )
            {
                Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::TryBindLocationService() - Requesting BIND to DroidLocationService." );
                BindService( new Intent( this, typeof( DroidLocationServices.LocationManagerService ) ), DroidLocationManagerConnection, Bind.AutoCreate );
            }
            else
            {
                Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::TryBindLocationService() - Do nothing. already bound to DroidLocationService." );
            }
        }

        [Obsolete]
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::OnStartCommand()" );

            TryBindLocationService( );

            if ( intent.Data != null )
            {
                // if there's a handler, call it so the normal application flow can occur
                if ( ILocationDelegateHandler != null )
                {
                    Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::Calling HandleForegroundLocationEvent." );
                    HandleForegroundLocationEvent( intent.Data.Scheme, intent.Data.SchemeSpecificPart, intent.Data.Fragment );
                }
                else
                {
                    Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::Calling HandleBackgroundLocationEvent. This should be implemented by your application." );
                    StartService( new Intent("Location", intent.Data, this, typeof( DroidLocationServices.ApplicationSpecific.BackgroundHandler ) ) );
                }
            }

            return StartCommandResult.NotSticky;
        }

        void HandleForegroundLocationEvent( string eventStr, string major, string minor )
        {
            switch ( eventStr )
            {
                case LocationManagerService.LocationEvent_EnteredRegion:
                {
                    Console.WriteLine( string.Format( "LocationDelegateService: Entered Region {0}", major ) );
                    ILocationDelegateHandler.OnRegionEntered( major );
                    break;
                }

                case LocationManagerService.LocationEvent_ExitedRegion:
                {
                    Console.WriteLine( string.Format( "LocationDelegateService: Exited Region {0} (Outside all regions: {1}", major, minor ) );
                    ILocationDelegateHandler.OnRegionExited( major, bool.Parse( minor ) );
                    break;
                }

                case LocationManagerService.LocationEvent_LandmarkChanged:
                {
                    Console.WriteLine( string.Format( "LocationDelegateService: Landmark Changed: {0}", major ) );
                    ILocationDelegateHandler.OnLandmarkChanged( major );
                    break;
                }

                case LocationManagerService.LocationEvent_IntenseScanExpired:
                {
                    Console.WriteLine( string.Format( "LocationDelegateService: Intense Scan Expired" ) );
                    ILocationDelegateHandler.OnIntenseScanExpired( );
                    break;
                }
            }
        }

        public bool BeginAddRegionsForTrack( )
        {
            if ( ServiceBinder != null )
            {
                return ServiceBinder.Service.BeginAddRegionsForTrack( );
            }

            Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::BeginAddRegionsForTrack() failed. Not bound to DroidLocationService." );
            return false;
        }

        public bool AddRegionForTrack( string description, double latitude, double longitude, float radius )
        {
            if ( ServiceBinder != null )
            {
                return ServiceBinder.Service.AddRegionForTrack( description, latitude, longitude, radius );
            }

            Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::AddRegionsForTrack() failed. Not bound to DroidLocationService." );
            return false;
        }

        public bool AddLandmarkToRegion( string regionDescription, string description, double latitude, double longitude )
        {
            if ( ServiceBinder != null )
            {
                return ServiceBinder.Service.AddLandmarkToRegion( regionDescription, description, latitude, longitude );
            }

            Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::AddLandmarkToRegion() failed. Not bound to DroidLocationService." );
            return false;
        }

        public bool CommitRegionsForTrack( )
        {
            if ( ServiceBinder != null )
            {
                ServiceBinder.Service.CommitRegionsForTrack( );
                return true;
            }

            Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::CommitRegionsForTrack() failed. Not bound to DroidLocationService." );
            return false;
        }

        public void ServiceConnected( IBinder binder )
        {
            Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::ServiceConnected(). Now BOUND to DroidLocationService." );

            ServiceBinder = (LocationManagerBinder)binder;

            // give the service an instance of ourselves so it can notify us of events
            ServiceBinder.Service.SetLocationServiceHandler( this );

            // if we have a handler, notify it we're ready
            if ( ILocationDelegateHandler != null )
            {
                ILocationDelegateHandler.OnReady( );
            }
        }

        public void ServiceDisconnected( )
        {
            Rock.Mobile.Util.Debug.WriteToLog( "LocationDelegateService::ServiceDisconnected(). UNBOUND from DroidLocationService." );

            // we were disconnected. Null our binder so we can re-obtain it when we need it.
            ServiceBinder = null;
        }
    }
}
#endif
