#if __ANDROID__
#if USE_LOCATION_SERVICES
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
using Java.Lang;

namespace DroidLocationServices
{
    /* USAGE:
     * Droid Location support consists of two primary services. LocationManagerService and LocationDelegateService.
     * 
     * LocationManagerService
     * This is the 'core' service that interacts directly with the Google Play location api.
     * It is a long-running 'sticky' service that should always be running, and will be restarted
     * by Android anytime it is killed. Locations are stored in a file cache, so if the service is restarted,
     * the locations are reloaded and added to monitoring again immediately.
     * 
     * It stores geofence 'Regions' and landmarks within each 'Region', and sends an intent
     * whenever one of these regions is entered or exited. A seperate intent is sent when a
     * landmark is reached (or, technically, when a landmark is left.)
     * 
     * Your activity will not directly interact with LocationManagerService. Instead, it will communicate with
     * LocationDelegateService.
     * 
     * LocationDelegateService
     * This is a delegate service that your activity and LocationManagerService both communicate with.
     * When a location event occurs, LocationManagerService sends an intent to LocationDelegateService.
     * LocationDelegateService provides an abstract function that should be application specific
     * and allows your application to handle the event however it wants. This keeps LocationManagerService
     * abstract and reusable across applications.
     * 
     * Your activity sends regions and landmarks to LocationManagerService via LocationDelegateService.
     * Your activity will bind to LocationDelegateService and directly call methods to add necessary regions.
     * LocationDelegateService, in turn, is bound to LocationManagerService and passes these to it.
     * 
     * 
     * To Begin:
     * Your activity should implement ILocationDelegateHandler.
     * Add Binder and Connection members.
     * LocationDelegateConnection DelegateConnection { get; set; }
     * LocationDelegateBinder DelegateBinder { get; set; }
     * 
     * In OnCreate()
     * Start the delegate service and create a connection object for binding.
     *      StartService( new Intent( this, typeof( DroidLocationServices.LocationDelegateService ) ) );
     *      DelegateConnection = new LocationDelegateConnection( this );
     * 
     * In OnResume()
     * Bind the delegate service to your activity via the Connection you created in OnCreate 
     *      BindService( new Intent( this, typeof( DroidLocationServices.LocationDelegateService )  ), DelegateConnection, Bind.AutoCreate );
     * 
     * In OnPause()
     * Unbind the activity from the delegate service.
     *      DelegateBinder.Service.SetHandler( null );
     *      UnbindService( DelegateConnection );
     * 
     * In ServiceConnected()
     * Store the binder, and give the DelegateService a reference to yourself (technically your implementation of the DelegateHandler interface)
     *      DelegateBinder = (LocationDelegateBinder)binder;
     *      DelegateBinder.Service.SetHandler( this );
     * 
     * In ServiceDisconnected()
     * We're unbound. Remove our handler from the delegate (which we may have already done).
     *      DelegateBinder.Service.SetHandler( null );
     * 
     * In OnReady() (This is called once the DelegateService is bound and communicating with the ManagerService )
     * Add your regions and landmarks.
     *      DelegateBinder.Service.BeginAddRegionsForTrack( );
     *      Call as many AddRegions and AddLocations as you want.
     *      DelegateBinder.Service.CommitRegionsForTrack( );
     * 
     * Wait for OnRegionEntered, OnRegionExited, OnLandmarkChanged, OnIntenseScanExpired. These are your notifications that a location event occured.
     * 
     * 
     * MANAGING EVENTS IN THE BACKGROUND
     * LocationDelegateService allows you to implement application specific behavior on location events while keeping
     * the LocationManager and LocationDelegate abstract.
     * 
     * Note that LocationDelegateService is a partial class. In your code, you will declare the class and one required function, "HandleLocationEvent".
     * namespace DroidLocationServices
     * {
     *      public partial class LocationDelegateService
     *      {
     *          void HandleLocationEvent( string eventStr, string major, string minor )
     *          {
     *          }
     *      }
     * }
     * 
     * In here you can do whatever you'd like with the location info. (Upload to a website, post a notification, etc.)
     * Because LocationDelegate is a service, this can be executed in the background without interrupting the user.
     * 
     * LOCATION MANAGER SERVICE DESIGN:
     * The Location Scanner itself sets a priority of high (100). This is required in order to get GPS Geofence updates. Normally this
     * would drain the battery, but we have two scan rates.
     * 
     * When not in a region, we are in Passive Scan mode, which scans once every 10 minutes. Android places a location symbol in the system tray
     * briefly when it scans, one every 10 minutes.
     * 
     * If you enter a region, as soon as the next 10 minute scan fires, we switch to Intense Scan, which is once every ten seconds and IS
     * enough for Android to put a location symbol in the system tray. Note that this will revert to passive scanning after 30 minutes or
     * when a landmark is hit, whichever comes first. If after the 30 minutes it goes to passive, you will have to exit/enter the region
     * or run the app to have it go back to Intense Scan and pick up your location.
     * 
     * If we hit a landmark, it goes back to Passive Scan, and won't go back to Intense until you leave the region and re-enter.
     * If the service is restarted, it will immediately receive location events as if it hadn't been running prior, so it's possible that
     * you will get a Region Entered even tho you were already there, and even a Landmark hit even tho you were already there. Not a big deal.
     */
    internal class TrackedRegion
    {
        public IGeofence Region { get; protected set; }
        public bool InRegion { get; set; }

        public double Latitude { get; protected set; }
        public double Longitude { get; protected set; }
        public float Radius { get; protected set; }

        public List<Android.Locations.Location> Landmarks { get; protected set; }

        public LocationManagerService ParentService { get; set; }


        public System.Timers.Timer GeofenceEventTimer { get; set; }
        public GeofencingEvent PendingGeofenceEvent { get; set; }

        public TrackedRegion( double latitude, double longitude, float radius, string description, LocationManagerService parentService )
        {
            Landmarks = new List<Android.Locations.Location>();

            Latitude = latitude;
            Longitude = longitude;
            Radius = radius;

            GeofenceBuilder geoBuilder = new Android.Gms.Location.GeofenceBuilder();

            // Set the request ID of the geofence. This is a string to identify this
            // geofence.
            geoBuilder.SetRequestId( description );

            // Set the circular region of this geofence.
            geoBuilder.SetCircularRegion( latitude, longitude, radius );

            // Set the expiration duration of the geofence. This geofence gets automatically
            // removed after this period of time.
            geoBuilder.SetExpirationDuration( Geofence.NeverExpire );

            // Set the transition types of interest. Alerts are only generated for these
            // transition. We track entry and exit transitions in this sample.
            geoBuilder.SetTransitionTypes( Geofence.GeofenceTransitionEnter | Geofence.GeofenceTransitionExit );

            Region = geoBuilder.Build( );

            InRegion = false;

            ParentService = parentService;

            // Monitor the geofence events, and wait 90 seconds to handle it, incase we get a false
            // positive (exit/enter when we're still in the region). This can happen due to cell-tower swapping,
            // wifi proximity, etc.
            GeofenceEventTimer = new System.Timers.Timer();
            GeofenceEventTimer.AutoReset = false;
            GeofenceEventTimer.Interval = LocationManagerService.GeofenceFilterTimer;
            GeofenceEventTimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e ) =>
            {
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "Timer for region {0} TICK", Region.RequestId ) );

                    Handler mainHandler = new Handler( ParentService.BaseContext.MainLooper );

                    Action action = new Action( delegate
                        {
                            ParentService.HandlePendingGeofencingEvent( PendingGeofenceEvent, this );
                        });

                    mainHandler.Post( action );
                    
            };
        }

        public bool LandmarkInRegion( Android.Locations.Location landmark )
        {
            // make sure the landmark passed in to check is valid. (Could be null if, say, we leave a region
            // and never hit a landmark, and are checking to remove 'CurrentLandmark')
            if ( landmark != null )
            {
                // consider a landmark matching if the latitude and logitude both match.
                foreach ( Android.Locations.Location currLandmark in Landmarks )
                {
                    if ( currLandmark.Latitude == landmark.Latitude &&
                        currLandmark.Longitude == landmark.Longitude )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void AddLandmark( string description, double latitude, double longitude )
        {
            // consider only allowing unique adds. For now, whatever.
            Android.Locations.Location landmark = new Android.Locations.Location( description ) { Latitude = latitude, Longitude = longitude };

            Landmarks.Add( landmark );
        }
    }

    // define the type of class the binder will be
    public class LocationManagerBinder : Binder
    {
        public LocationManagerService Service { get; protected set; }
        public LocationManagerBinder( LocationManagerService  service )
        {
            Service = service;
        }
    }

    public interface ILocationServiceHandler
    {
        // Called when the service is first connected to this ILocationServiceHandler via a binder
        void ServiceConnected( IBinder serviceBinder );

        // Called when the service is disconnected
        void ServiceDisconnected( );
    }

    public class LocationManagerConnection : Java.Lang.Object, IServiceConnection
    {
        ILocationServiceHandler ServiceHandler { get; set; }

        public LocationManagerConnection( ILocationServiceHandler serviceHandler )
        {
            ServiceHandler = serviceHandler;
        }

        public void OnServiceConnected( ComponentName name, IBinder serviceBinder )
        {
            LocationManagerBinder binder = serviceBinder as LocationManagerBinder;
            if ( binder != null )
            {
                ServiceHandler.ServiceConnected( binder );
            }
        }

        public void OnServiceDisconnected( ComponentName name )
        {
            ServiceHandler.ServiceDisconnected( );
        }
    }


    [BroadcastReceiver( Label="Bootstrapper" )]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    public class Bootstrapper : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Rock.Mobile.Util.Debug.WriteToLog( "Starting service from Bootstrapper" );
            context.StartService( new Intent( context, typeof( DroidLocationServices.LocationManagerService ) ) );
        }
    }

    [Service( Label="LocationManagerService" )]
    public class LocationManagerService  : Service, Android.Gms.Common.Apis.IGoogleApiClientConnectionCallbacks, Android.Gms.Common.Apis.IGoogleApiClientOnConnectionFailedListener, Android.Gms.Location.ILocationListener, Android.Gms.Common.Apis.IResultCallback
    {
        public const string LocationEvent_EnteredRegion = "entered_region";
        public const string LocationEvent_ExitedRegion = "exited_region";
        public const string LocationEvent_LandmarkChanged = "landmark_changed";
        public const string LocationEvent_IntenseScanExpired = "intense_scan_expired";

        public const long GeofenceFilterTimer = 90000; //90 seconds, this is the time we wait before processing geofenceEvents, to prevent false positives (suddent exit/enter)
        const long LocationScanningFrequency_Passive = 60000 * 2; //once every TEN minutes
        const long LocationScanningFrequency_Intense = 10000; //once every ten seconds

        List<TrackedRegion> Regions { get; set; }

        Android.Locations.Location CurrentLandmark { get; set; }

        // create our binder object that will be used to pass an instance of ourselves around
        IBinder Binder;

        // Reference to the interface for the location service.
        Android.Gms.Common.Apis.IGoogleApiClient ILocationServiceApi { get; set; }

        // The handler that receives callbacks on events defined in the interface.
        ILocationServiceHandler LocationServiceHandler { get; set; }

        // true if we're using intense scanning (once every 10 seconds)
        public bool IntenseScanningEnabled { get; protected set; }

        // The time Intense Scan began, so that it can be turned off after 30 minutes.
        DateTime IntenseScanStartTime { get; set; }
        const float MaxIntenseScanTimeMinutes = 30;

        // Adding regions for tracking is an asynchronous operation. Because of this,
        // we define these states to manage the process.
        enum RegionCommitState
        {
            // No region track changes happening
            None,

            // After BeginAddRegionsForTrack, so we know the user is adding regions.
            QueuingRegions,

            // Used after we tell the GooglePlayApi to REMOVE locations, while waiting for Android's callback.
            Removing,

            // Used after we tell the GooglePlayApi to ADD locations, while waiting for Android's callbakc.
            Adding,

            // Used on initial load after we told GooglePlayApi to ADD locations that we loaded from the cache.
            Restoring
        };
        RegionCommitState CommitState;

        // Stores the regions the user adds with AddRegionForTrack so that we can add them when GooglePlay is ready for us to.
        List<TrackedRegion> PendingRegionsForAdd { get; set; }

        // Stores the regions we should remove before we add new ones.
        List<string> PendingRegionsForRemove { get; set; }

        // If we receive a geofence event while disconnected from Google Services, we need to
        // store it and process it after we do connect.
        GeofencingEvent PendingGeofenceEvent { get; set; }

        // If we receive a region add while disconnected from Google Services, we need to
        // store it and prcess it after we connect
        bool PendingRegionCommitEvent { get; set; }

        // True if since service creation we've connected to the google API.
        // This prevents us from running 'first time connection' stuff more than once.
        bool GoogleServicesFirstConnectionComplete { get; set; }

        // The filename for the region cache.
        static string CachedRegionsFileName 
        {
            get
            {
                string filePath = System.Environment.GetFolderPath( System.Environment.SpecialFolder.Personal );
                return filePath + "/" + "regions.bin";
            }
        }

        public override void OnCreate()
        {
            base.OnCreate();

            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::OnCreate" ) );

            GoogleServicesFirstConnectionComplete = false;
            PendingRegionCommitEvent = false;

            IntenseScanningEnabled = false;

            Binder = new LocationManagerBinder( this );

            // Build our interface to the google play location services.
            Android.Gms.Common.Apis.GoogleApiClientBuilder apiBuilder = new Android.Gms.Common.Apis.GoogleApiClientBuilder( this, this, this );
            apiBuilder.AddApi( LocationServices.Api );
            apiBuilder.AddConnectionCallbacks( this );
            apiBuilder.AddOnConnectionFailedListener( this );

            ILocationServiceApi = apiBuilder.Build( );
            //

            // establish a connection
            ILocationServiceApi.Connect( );

            // setup our regions
            Regions = new List<TrackedRegion>( );
        }

        public void SetLocationServiceHandler( ILocationServiceHandler locationServiceHandler )
        {
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::Set LocationServiceHandler" ) );
            LocationServiceHandler = locationServiceHandler;
        }

        public bool BeginAddRegionsForTrack( )
        {
            // make sure the commit state is none so the user doesn't call this WHILE we're trying to add/remove regions.
            if( CommitState == RegionCommitState.None )
            {
                CommitState = RegionCommitState.QueuingRegions;
                
                // Adding / Removing regions is an asynchronous operation in Android. Additionally,
                // we need to remove any regions that should no longer be tracked (say the list we're about to receive doesn't have
                // all the regions we are currently tracking).

                // Because of this, we first need to build a list of regions to remove, and queue all the regions
                // the user wants to add.
                // Then, if there are regions to remove, we request their removal, and on that callback, add the queued regions to START tracking.
                // If there are NOT regions to remove, we can immediately add the queued regions to start tracking.
                
                // since we're going to be tracking a new set of regions, first build a list of 
                // our existing regions so we can remove them.
                PendingRegionsForRemove = new List<string>();
                foreach ( TrackedRegion trackedRegion in Regions )
                {
                    PendingRegionsForRemove.Add( trackedRegion.Region.RequestId );
                }

                // reset our pending regions for add, and we'll begin storing 
                // regions that the caller adds in here
                PendingRegionsForAdd = new List<TrackedRegion>();

                // we're ready for the user to begin adding regions
                return true;
            }

            return false;
        }

        public bool AddRegionForTrack( string description, double latitude, double longitude, float radius )
        {
            // make sure the user called 'BeginAddRegionsForTrack'
            if ( CommitState == RegionCommitState.QueuingRegions )
            {
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::Adding Region For Tracking: {0}", description ) );

                TrackedRegion newRegion = new TrackedRegion( latitude, longitude, radius, description, this );
                PendingRegionsForAdd.Add( newRegion );

                return true;
            }

            return false;
        }

        public bool AddLandmarkToRegion( string regionDescription, string description, double latitude, double longitude )
        {
            // make sure the user called 'BeginAddRegionsForTrack'
            if ( CommitState == RegionCommitState.QueuingRegions )
            {
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::Adding Landmark {0} for Region {1}", description, regionDescription ) );

                TrackedRegion region = PendingRegionsForAdd.Where( tr => tr.Region.RequestId == regionDescription ).Single( );
                if ( region != null )
                {
                    region.AddLandmark( description, latitude, longitude );
                    return true;
                }
            }

            return false;
        }

        public void CommitRegionsForTrack( )
        {
            // if we're connected to google services, update our regions.
            if ( ILocationServiceApi.IsConnected )
            {
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::CommitRegionsForTrack - GooglePlay connected. Comitting regions." ) );

                // make sure the user called 'BeginAddRegionsForTrack'
                if ( CommitState == RegionCommitState.QueuingRegions )
                {
                    // request their removal NOW (why not, might allow them to be done by the time the new ones are added)
                    if ( PendingRegionsForRemove.Count > 0 )
                    {
                        Android.Gms.Common.Apis.IPendingResult iRemovePendingResult = LocationServices.GeofencingApi.RemoveGeofences( ILocationServiceApi, PendingRegionsForRemove );
                        iRemovePendingResult.SetResultCallback( this );

                        CommitState = RegionCommitState.Removing;
                    }
                    else
                    {
                        InternalCommitRegionsForTrack( );
                    }
                }
            }
            else
            {
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::CommitRegionsForTrack - GooglePlay NOT CONNECTED. Queuing region commit, and connecting to GooglePlay." ) );

                // otherwise, flag that we want to commit, and connect the service.
                PendingRegionCommitEvent = true;
                ILocationServiceApi.Connect( );
            }
        }

        void InternalCommitRegionsForTrack( )
        {
            CommitState = RegionCommitState.Adding;
            
            // go ahead and take the pending regions as our new regions.
            // we can safely assume that this was called after removing the existing regions.
            Regions = new List<TrackedRegion>( PendingRegionsForAdd );

            // reset our pending lists, because we're done with buffering.
            PendingRegionsForAdd.Clear( );
            PendingRegionsForRemove.Clear( );

            // immediately cache the regions to disk, so that if this service is restarted, we can restore them.
            SaveRegionsToDisk( );

            // actually add the regions to the API
            AddRegionsToApi( );
        }

        void AddRegionsToApi( )
        {
            // setup our Geofencing API.
            GeofencingRequest.Builder builder = new GeofencingRequest.Builder();
            builder.SetInitialTrigger( GeofencingRequest.InitialTriggerEnter );

            // add each geofence to the Geofence API
            List<IGeofence> geoFences = new List<IGeofence>();
            foreach ( TrackedRegion region in Regions )
            {
                geoFences.Add( region.Region );
            }
            builder.AddGeofences( geoFences );
            GeofencingRequest geofenceRequest = builder.Build( );


            // create the intent that should be launched when a geofence is triggered
            Intent intent = new Intent( this, typeof( LocationManagerService ) );
            PendingIntent pendingIntent = PendingIntent.GetService( this, 0, intent, PendingIntentFlags.UpdateCurrent );

            Android.Gms.Common.Apis.IPendingResult iAddPendingResult = LocationServices.GeofencingApi.AddGeofences( ILocationServiceApi, geofenceRequest, pendingIntent );
            iAddPendingResult.SetResultCallback( this );
        }

        public void OnResult( Java.Lang.Object status )
        {
            //Status code 1000 means user has disallowed location tracking.
            Android.Gms.Common.Apis.IResult result = status as Android.Gms.Common.Apis.IResult;
            if ( result != null && result.Status.StatusCode == 1000  )
            {
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LOCATION SERVICE PERMISSION DISABLED BY USER. WE WILL NOT GET ANY UPDATES." ) );
            }
            else
            {
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::OnResult() - (From GooglePlay) iPendingResult: {0} for Commit State: {1}", status, CommitState ) );
            }
                
            switch( CommitState )
            {
                case RegionCommitState.Removing:
                {
                    InternalCommitRegionsForTrack( );
                    break;
                }

                case RegionCommitState.Adding:
                {
                    // if we're adding, reset the state, because we're now done.
                    CommitState = RegionCommitState.None;
                    break;
                }

                case RegionCommitState.Restoring:
                {
                    CommitState = RegionCommitState.None;

                    // This path occurs if there were cached regions to load, in which case
                    // we did NOT handle pending events, because we wanted to wait
                    // until we restored our locations.
                    HandlePendingEvents( );

                    break;
                }

                case RegionCommitState.QueuingRegions:
                case RegionCommitState.None:
                {
                    // nothing here. How would this even be called from these states?
                    break;
                }
            }
        }

        [Obsolete]
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::OnStartCommand received. Returning Sticky" ) );

            // is this being called by Location Services?
            GeofencingEvent geoEvent = GeofencingEvent.FromIntent( intent );
            if ( geoEvent != null && geoEvent.TriggeringLocation != null )
            {
                // if the location service api is connected, process the event now
                if ( ILocationServiceApi.IsConnected )
                {
                    HandleGeofencingEvent( geoEvent );
                }
                else
                {
                    // then store the event as pending
                    PendingGeofenceEvent = geoEvent;

                    // otherwise establish a google api connection and when it calls us back we'll
                    // process this event.
                    ILocationServiceApi.Connect( );
                }
            }

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            ILocationServiceApi.Disconnect( );
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::OnDestroy" ) );
        }

        // when bound, return the Binder object containing our instance
        public override IBinder OnBind(Intent intent)
        {
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::OnBind (Someone requested to bind to us. Probably LocationDelegateService)" ) );
            return Binder;
        }

        LocationRequest CreateLocationRequest( long interval )
        {
            LocationRequest locationRequest = new LocationRequest();
            locationRequest.SetPriority( 100 );
            locationRequest.SetFastestInterval( interval );
            locationRequest.SetInterval( interval );
            return locationRequest;
        }

        // Google Play Services Callbacks
        public void OnConnected (Bundle bundle)
        {
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::OnConnected (Google Play Services ready)" ) );

            // only setup our scanning, load the cache and notify our handler if
            // this is our first connection since being created.
            if ( GoogleServicesFirstConnectionComplete == false )
            {
                GoogleServicesFirstConnectionComplete = true;

                //Note, this is REQUIRED to use even the GPS scanning. See this:
                //http://stackoverflow.com/questions/24090584/does-priority-balanced-power-accuracy-exclude-the-gps-provider
                // I tried using only GPS, and not location scanning, OR lowering the priority, but that shuts off GPS altogether.
                LocationRequest locationRequest = CreateLocationRequest( LocationScanningFrequency_Passive );
                LocationServices.FusedLocationApi.RequestLocationUpdates( ILocationServiceApi, locationRequest, this );

                // now either restore any necessary regions
                if ( File.Exists( CachedRegionsFileName ) == true )
                {
                    Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService - Found cached regions: restoring" ) );
                    CommitState = RegionCommitState.Restoring;
                    LoadRegionsFromDisk( );
                    AddRegionsToApi( );

                    // when these are done, we'll attempt to handle pending events.
                }
                else
                {
                    // nothing to load from cache, see if there were any pending events to handle
                    HandlePendingEvents( );
                }
            }
            else
            {
                // we had no first time stuff to do, so just handle pending events
                HandlePendingEvents( );
            }
        }

        void HandlePendingEvents( )
        {
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::HandlePendingEvents()" ) );

            // if there's a pending geo event (say we've been running a long time, got disconnected from GooglePlay,
            // and just reconnected), process the event.
            if ( PendingGeofenceEvent != null )
            {
                HandleGeofencingEvent( PendingGeofenceEvent );
                PendingGeofenceEvent = null;
            }

            // if there's a pending region commit (again we ran a long time and got disconnected from GooglePlay,
            // and just reconnected), process the event.
            if ( PendingRegionCommitEvent == true )
            {
                CommitRegionsForTrack( );
                PendingRegionCommitEvent = false;
            }
        }

        public void OnDisconnected (Bundle bundle)
        {
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService: OnDisconnected" ) );
        }

        public void OnConnectionFailed (Bundle bundle)
        {
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService: OnConnectionFailed" ) );
        }

        public void OnConnectionSuspended( int cause )
        {
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService: OnConnectionSuspended Cause: {0}", cause ) );
        }

        public void OnConnectionFailed( ConnectionResult result )
        {
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService: OnConnectionFailed Cause: {0}", result ) );
        }
        //

        public void OnLocationChanged( Android.Locations.Location location )
        {
            // only check these if intense scanning is enabled. Otherwise, we want to wait
            // for the region entered event.
            if ( IntenseScanningEnabled == true )
            {
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "START LOCATION RECEIVED" ) );
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "-----------------" ) );
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "Longitude: " + location.Longitude ) );
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "Latitude: " + location.Latitude ) );
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "" ) );

                // see if any of these locations are within the region
                float closestDist = 20;
                Android.Locations.Location closestLandmark = null;

                foreach ( TrackedRegion trackedRegion in Regions )
                {
                    foreach ( Android.Locations.Location landmark in trackedRegion.Landmarks )
                    {
                        float locationDist = landmark.DistanceTo( location );
                        if ( locationDist < closestDist )
                        {
                            closestDist = locationDist;
                            closestLandmark = landmark;
                        }
                    }
                }

                // make sure we aren't already using this location
                if ( CurrentLandmark != closestLandmark )
                {
                    CurrentLandmark = closestLandmark;

                    if( CurrentLandmark != null )
                    {
                        Rock.Mobile.Util.Debug.WriteToLog( string.Format( "HIT LANDMARK. STOPPING INTENSE SCANNING" ) );
                        StopIntenseLocationScanning( );
                    }

                    SendLocationUpdateIntent( LocationEvent_LandmarkChanged, CurrentLandmark != null ? CurrentLandmark.Provider : "", "" );
                }

                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "-----------------" ) );
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "END LOCATION RECEIVED\n" ) );


                // if we run an intense scan for more tham MaxMinutes, force it to stop.
                // We assume we were in the area but never hit a landmark, and will have to wait for the
                // next region change.
                TimeSpan elapsedScanTime = DateTime.Now - IntenseScanStartTime;
                if ( elapsedScanTime.TotalMinutes > MaxIntenseScanTimeMinutes )
                {
                    Rock.Mobile.Util.Debug.WriteToLog( string.Format( "INTENSE SCAN TIME HAS EXPIRED. STOPPING INTENSE SCANNING TO SAVE BATTERY." ) );
                    StopIntenseLocationScanning( );

                    // broadcast that the intense scan has expired. This allows the app to
                    // begin a new one if it wants to.
                    SendLocationUpdateIntent( LocationEvent_LandmarkChanged, "", "" );
                }
            }
            else
            {
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LOCATION RECEIVED. (PASSIVE SCAN HEARTBEAT)" ) );
            }
        }

        public void StartIntenseLocationScanning( )
        {
            if ( IntenseScanningEnabled == false )
            {
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::STARTING INTENSE SCANNING" ) );
                IntenseScanningEnabled = true;
                IntenseScanStartTime = DateTime.Now;

                LocationRequest locationRequest = CreateLocationRequest( LocationScanningFrequency_Intense );
                LocationServices.FusedLocationApi.RequestLocationUpdates( ILocationServiceApi, locationRequest, this );
            }
        }

        public void StopIntenseLocationScanning( )
        {
            // Guard against multiple stop requests. Technically this isn't thread safe, BUT
            // the updates come in on the thread calling this, so there's no risk that when our timer expires,
            // this will be called by multiple threads.
            if ( IntenseScanningEnabled == true )
            {
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::STOP INTENSE SCANNING" ) );
                IntenseScanningEnabled = false;
                LocationRequest locationRequest = CreateLocationRequest( LocationScanningFrequency_Passive );
                LocationServices.FusedLocationApi.RequestLocationUpdates( ILocationServiceApi, locationRequest, this );
            }
        }

        protected void HandleGeofencingEvent( GeofencingEvent geoEvent )
        {
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::Received Geofence event: {0}", geoEvent.GeofenceTransition ) );
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::Num Geofences event: {0}", geoEvent.TriggeringGeofences.Count ) );

            // find the region
            TrackedRegion trackedRegion = Regions.Where( gf => gf.Region.RequestId == geoEvent.TriggeringGeofences[ 0 ].RequestId ).Single( );
            if ( trackedRegion != null )
            {
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "Starting timer for region {0}", trackedRegion.Region.RequestId ) );
                
                // don't process this geoevent NOW. instead, queue it, and we'll wait 10 seconds.
                // that way, if it's a false positive (like a quit exit-enter) we won't process it.
                trackedRegion.GeofenceEventTimer.Stop( );
                trackedRegion.PendingGeofenceEvent = geoEvent;
                trackedRegion.GeofenceEventTimer.Start( );
            }
            else
            {
                Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::Received geofence event for unknown region: {0}", geoEvent.TriggeringGeofences[ 0 ].RequestId ) );
            }
        }

        internal void HandlePendingGeofencingEvent( GeofencingEvent geoEvent, TrackedRegion trackedRegion )
        {
            Rock.Mobile.Util.Debug.WriteToLog( string.Format( "Handling event for region {0}", trackedRegion.Region.RequestId ) );

            // if we're entering, turn on intense scanning
            if ( geoEvent.GeofenceTransition == Geofence.GeofenceTransitionEnter )
            {
                // make sure we're not IN the region yet (we could be if this is the result of a false positive, 
                // like, we're in a region, and suddently get an 'exit-entered')
                if ( trackedRegion.InRegion == false )
                {
                    trackedRegion.InRegion = true;

                    // start intense scanning right away
                    StartIntenseLocationScanning( );

                    // notify the handler if they're interested and there is one
                    SendLocationUpdateIntent( LocationEvent_EnteredRegion, trackedRegion.Region.RequestId, "" );
                }
                else
                {
                    Rock.Mobile.Util.Debug.WriteToLog( string.Format( "Ignoring ENTER for region {0} because we're already in it.", trackedRegion.Region.RequestId ) );
                }
            }
            // else we're leaving...
            else
            {
                // make sure we're not OUT of the region yet (we could be if this is the result of a false positive, 
                // like, we're outside a region, and suddently get an 'entered-exit')
                if ( trackedRegion.InRegion == true )
                {
                    trackedRegion.InRegion = false;

                    // if this region owned our current landmark, reset that as well.
                    if ( trackedRegion.LandmarkInRegion( CurrentLandmark ) )
                    {
                        CurrentLandmark = null;
                    }


                    // and turn off intensive scanning IF we're not in anymore regions at all
                    bool outsideAllRegions = true;

                    foreach ( TrackedRegion region in Regions )
                    {
                        // if we find at least one region still being tracked, don't stop scanning.
                        if ( region.InRegion == true )
                        {
                            outsideAllRegions = false;
                            break;
                        }
                    }

                    if ( outsideAllRegions )
                    {   
                        Rock.Mobile.Util.Debug.WriteToLog( string.Format( "LocationManagerService::OUTSIDE ALL REGIONS. STOPPING INTENSE SCANNING" ) );
                        StopIntenseLocationScanning( );      
                    }

                    // notify the handler if they're interested and there is one
                    SendLocationUpdateIntent( LocationEvent_ExitedRegion, trackedRegion.Region.RequestId, outsideAllRegions.ToString( ) );
                }
                else
                {
                    Rock.Mobile.Util.Debug.WriteToLog( string.Format( "Ignoring EXIT for region {0} because we're already in it.", trackedRegion.Region.RequestId ) );
                }
            }
        }

        private object locker = new object();
        void SaveRegionsToDisk( )
        {
            // first lock to make this asynchronous
            lock ( locker )
            {
                try
                {
                    // open the file
                    FileStream fileStream = new FileStream( CachedRegionsFileName, FileMode.Create );
                    BinaryWriter writer = new BinaryWriter( fileStream );

                    // write the number of regions
                    writer.Write( Regions.Count );

                    foreach ( TrackedRegion region in Regions )
                    {
                        // write the region properties
                        writer.Write( region.Region.RequestId );
                        writer.Write( region.Latitude );
                        writer.Write( region.Longitude );
                        writer.Write( region.Radius );

                        // write the number of region landmarks
                        writer.Write( region.Landmarks.Count );
                        foreach ( Android.Locations.Location landmark in region.Landmarks )
                        {
                            // write each landmark
                            writer.Write( landmark.Provider );
                            writer.Write( landmark.Latitude );
                            writer.Write( landmark.Longitude );
                        }
                    }

                    // done
                    writer.Close( );
                    fileStream.Close( );
                }
                catch
                {
                    Rock.Mobile.Util.Debug.WriteToLog( string.Format( "Failed to save regions to disk. If the service is restarted, these are gonna be gone" ) );
                }
            }
        }

        void LoadRegionsFromDisk( )
        {
            // first lock to make this synchronous
            lock ( locker )
            {
                try
                {
                    FileStream fileStream = new FileStream( CachedRegionsFileName, FileMode.Open );
                    BinaryReader binaryReader = new BinaryReader( fileStream );

                    // reset our region list
                    Regions = new List<TrackedRegion>( );

                    // read the number of cached regions
                    int regionCount = binaryReader.ReadInt32( );
                    for ( int i = 0; i < regionCount; i++ )
                    {
                        // read the region properties
                        string regionId = binaryReader.ReadString( );
                        double regionLatitude = binaryReader.ReadDouble( );
                        double regionLongitude = binaryReader.ReadDouble( );
                        float regionRadius = binaryReader.ReadSingle( );

                        // create the region
                        TrackedRegion region = new TrackedRegion( regionLatitude, regionLongitude, regionRadius, regionId, this );

                        // read the landmarks for this region
                        int landmarkCount = binaryReader.ReadInt32( );
                        for ( int c = 0; c < landmarkCount; c++ )
                        {
                            // read the landmark properties
                            string provider = binaryReader.ReadString( );
                            double latitude = binaryReader.ReadDouble( );
                            double longitude = binaryReader.ReadDouble( );

                            // add the landmark
                            region.AddLandmark( provider, latitude, longitude );
                        }

                        // add the region
                        Regions.Add( region );
                    }

                    // done
                    binaryReader.Close( );
                    fileStream.Close( );
                }
                catch
                {
                    Rock.Mobile.Util.Debug.WriteToLog( string.Format( "Failed to load regions from disk. Until the activity runs and feeds us some, we can't scan for anything." ) );
                }
            }
        }

        void SendLocationUpdateIntent( string action, string location, string extra )
        {
            Android.Net.Uri uri = Android.Net.Uri.FromParts( action, location, extra );
            StartService( new Intent("Location", uri, this, typeof( DroidLocationServices.LocationDelegateService ) ) );
        }
    }
}
#endif
#endif