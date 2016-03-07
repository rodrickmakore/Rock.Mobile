#if __IOS__
#if USE_LOCATION_SERVICES
using System;
using CoreLocation;
using UIKit;
using Foundation;
using System.Collections.Generic;
using System.Linq;

namespace iOSLocationServices
{
    public class LocationManager
    {
        class TrackedRegion
        {
            public CLCircularRegion Region { get; protected set; }
            public bool InRegion { get; set; }

            public List<CLRegion> LandMarks { get; protected set; }

            public TrackedRegion( double latitude, double longitude, double radius, string description )
            {
                LandMarks = new List<CLRegion>( );
                
                Region = new CLCircularRegion( new CLLocationCoordinate2D( latitude, longitude ), radius, description );
                InRegion = false;
            }

            public bool LandmarkInRegion( CLRegion landmark )
            {
                // it's possible they'll offer null. Ignore that.
                if ( landmark != null )
                {
                    foreach ( CLRegion currLandmark in LandMarks )
                    {
                        // if we match the lat AND long, its safe to assume this is the landmark.
                        if ( currLandmark.Center.Latitude == landmark.Center.Latitude &&
                             currLandmark.Center.Longitude == landmark.Center.Longitude )
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public void AddLandmarkToTrack( double latitude, double longitude, double radius, string description )
            {
                // consider only allowing unique adds. For now, whatever.
                LandMarks.Add( new CLRegion( new CLLocationCoordinate2D( latitude, longitude ), radius, description ) );
            }
        }
        
        CLLocationManager CLLocationManager { get; set; }
        List<TrackedRegion> Regions { get; set; }

        public List<CLRegion> CurrentRegions
        {
            get
            {
                List<CLRegion> currRegions = new List<CLRegion>();

                foreach ( TrackedRegion region in Regions )
                {
                    if ( region.InRegion == true )
                    {
                        currRegions.Add( region.Region );
                    }
                }

                return currRegions;
            }
        }
        public CLRegion CurrentLandMark { get; set; }

        public delegate void EnteredRegionDelegate( CLRegion region );
        EnteredRegionDelegate EnteredRegion { get; set; }

        public delegate void ExitedRegionDelegate( CLRegion region, bool outsideAllRegions );
        ExitedRegionDelegate ExitedRegion { get; set; }

        public delegate void LandmarkChangedDelegate( CLRegion landmark );
        LandmarkChangedDelegate LandmarkChanged { get; set; }

        public delegate void IntenseScanExpiredDelegate( );
        IntenseScanExpiredDelegate IntenseScanExpired { get; set; }

        public delegate void AuthorizationChangedDelegate( CLAuthorizationStatus status );
        AuthorizationChangedDelegate AuthorizationChanged { get; set; }

        public CLAuthorizationStatus AuthorizationStatus { get; protected set; }

        public bool IntenseScanningEnabled { get; protected set; }
        DateTime IntenseScanStartTime { get; set; }
        const float MaxIntenseScanTimeMinutes = 30;

        public LocationManager( EnteredRegionDelegate entered, ExitedRegionDelegate exited, LandmarkChangedDelegate landmarkChanged, IntenseScanExpiredDelegate intenseScanExpired, AuthorizationChangedDelegate authorizationChanged )
        {
            Regions = new List<TrackedRegion>();

            EnteredRegion = entered;
            ExitedRegion = exited;
            LandmarkChanged = landmarkChanged;
            IntenseScanExpired = intenseScanExpired;
            AuthorizationChanged = authorizationChanged;

            IntenseScanningEnabled = false;

            // How This Works:
            // When the app starts from the user OR iOS:
            // 1. Begin passive monitoring with regions. (Hollow arrow)
            // 2. Check Region States. If in a region, turn on intense scanning. (Solid arrow)
            // 3. Wait for a region change
            // 4. Region change - In a region, turn on intense scanning. (Solid arrow) If no regions, turn OFF intense scanning. (hollow arrow)


            // create our location manager and request permission to use GPS tracking
            CLLocationManager = new CLLocationManager();
            CLLocationManager.ActivityType = CLActivityType.AutomotiveNavigation;
            CLLocationManager.PausesLocationUpdatesAutomatically = false;

            CLLocationManager.RequestAlwaysAuthorization( );
            CLLocationManager.RequestWhenInUseAuthorization( );


            CLLocationManager.AuthorizationChanged += (object sender, CLAuthorizationChangedEventArgs e ) =>
                {
                    AuthorizationStatus = e.Status;

                    AuthorizationChanged( e.Status );

                    switch( e.Status )
                    {
                        // if they just granted us permission, start scanning.
                        case CLAuthorizationStatus.AuthorizedAlways:
                        {
                            BeginPassiveMonitoring( );
                            RefreshRegionsAndLandmarks( );
                            break;
                        }

                        // if they just turned us down, shut it all off
                        //Note: THIS INCLUDES WHEN IN USE. WHEN IN USE does not include
                        // any services that can relaunch the app (region monitoring & significant change location)
                        case CLAuthorizationStatus.AuthorizedWhenInUse:
                        case CLAuthorizationStatus.Denied:
                        case CLAuthorizationStatus.NotDetermined:
                        case CLAuthorizationStatus.Restricted:
                        {
                            StopPassiveMonitoring( );
                            StopIntenseLocationScanning( );
                            break;
                        }
                    }
              };

            // setup our callback used when intense (real time) scanning is on and we get a new location from the device.
            CLLocationManager.LocationsUpdated += (object sender, CLLocationsUpdatedEventArgs e ) =>
                {
                    IntenseScanningLocationReceived( this, e.Locations );
                };

            // Setup our callacks for passive scanning, where the OS will wake us up
            // when it determines a region change has occured.
            CLLocationManager.RegionEntered += (object sender, CLRegionEventArgs e ) =>
            {
                HandleRegionEntered( e.Region );
            };

            CLLocationManager.RegionLeft += (object sender, CLRegionEventArgs e ) =>
            {
                HandleRegionExited( e.Region );
            };



            CLLocationManager.DidDetermineState += (object sender, CLRegionStateDeterminedEventArgs e ) =>
            {
                if ( e.State == CLRegionState.Inside )
                {
                    HandleRegionEntered( e.Region );
                }
                // if we're outside or it's unknown, consider us outside the region
                else
                {
                    HandleRegionExited( e.Region );
                }
            };
        }

        public void AddRegionToTrack( double latitude, double longitude, double radius, string description )
        {
            Regions.Add( new TrackedRegion( latitude, longitude, radius, description ) );
        }

        public bool AddLandmarkToTrack( string parentRegion, double latitude, double longitude, double radius, string description )
        {
            // find the region that this landmark will go into
            TrackedRegion region = Regions.Where( tr => tr.Region.Identifier == parentRegion ).SingleOrDefault( );
            if ( region != null )
            {
                region.AddLandmarkToTrack( latitude, longitude, radius, description );

                return true;
            }

            return false;
        }

        void HandleRegionEntered( CLRegion region )
        {
            Console.WriteLine( "LocationManager: HandleRegionEntered" );

            // find this region in our list and set the "InRegion" to true
            foreach ( TrackedRegion trackedRegion in Regions )
            {
                if ( trackedRegion.Region.Identifier == region.Identifier )
                {
                    trackedRegion.InRegion = true;
                }
            }

            // Start intense scanning
            StartIntenseLocationScanning( );

            // Invoke callback
            EnteredRegion( region );
        }

        void HandleRegionExited( CLRegion region )
        {
            Console.WriteLine( "LocationManager: HandleRegionExited" );

            // find this region in our list and set the "InRegion" to false
            bool wasInRegion = false;
            foreach ( TrackedRegion trackedRegion in Regions )
            {
                if ( trackedRegion.Region.Identifier == region.Identifier )
                {
                    if ( trackedRegion.InRegion == true )
                    {
                        wasInRegion = true;
                    }
                    trackedRegion.InRegion = false;

                    // also, if we're in the landmark owned by this region, clear it.
                    if ( trackedRegion.LandmarkInRegion( CurrentLandMark ) )
                    {
                        CurrentLandMark = null;
                    }
                }
            }

            // stop intense scanning if we leave ALL tracked regions.
            bool outsideAllRegions = true;

            foreach ( TrackedRegion trackedRegion in Regions )
            {
                // if we find at least one region still being tracked, don't stop scanning.
                if ( trackedRegion.InRegion == true )
                {
                    outsideAllRegions = false;
                    break;
                }
            }

            // Invoke callback
            if ( wasInRegion )
            {
                // if now outside all regions, stop intense scanning
                if ( outsideAllRegions )
                {
                    StopIntenseLocationScanning( );
                }

                ExitedRegion( region, outsideAllRegions );
            }
        }

        public void RefreshRegionsAndLandmarks( )
        {
            // first, are services enabled and is the device capable?
            if ( AuthorizationStatus == CLAuthorizationStatus.AuthorizedAlways )
            {
                Console.WriteLine( "LocationManager: RefreshRegionsAndLandmarks" );

                // This will reset our landmark, and get an update
                // from each region with our current state in it (in or out).
                // This allows us to "sync" the app's status with where the location service
                // thinks we are.
                CurrentLandMark = null;

                foreach ( TrackedRegion region in Regions )
                {
                    CLLocationManager.RequestState( region.Region );
                }
            }
            else
            {
                Console.WriteLine( "LocationManager: RefreshRegionsAndLandmarks NOT AUTHORIZED. PERMISSIONS: {0}", AuthorizationStatus );
            }
        }

        public void BeginPassiveMonitoring( )
        {
            // we define passive monitoring as low power scanning, using regions and "significant changes" as the criteria.
            Console.WriteLine( "LocationManager: BeginPassiveMonitoring" );

            // first, are services enabled and is the device capable?
            if ( AuthorizationStatus == CLAuthorizationStatus.AuthorizedAlways )
            {
                // the problem with this is it works great but will cause a solid arrow, misleading users
                //LocationManager.StartMonitoringSignificantLocationChanges( );

                foreach ( TrackedRegion region in Regions )
                {
                    CLLocationManager.StartMonitoring( region.Region );
                }
            }
            else
            {
                Console.WriteLine( "LocationManager: RefreshRegionsAndLandmarks NOT AUTHORIZED. PERMISSIONS: {0}", AuthorizationStatus );
            }
        }

        // If you turn off passive monitoring, the app cannot be launched by iOS due to
        // a location change. So be warned.
        public void StopPassiveMonitoring( )
        {
            Console.WriteLine( "LocationManager: StopPassiveMonitoring - THIS ENDS ALL LOCATION AWARENESS." );

            // this turns off our passive monitoring region monitoring.
            // Realistically, this should NEVER be called, because this is 
            // how iOS wakes the app.

            // the problem with this is it works great but will cause a solid arrow, misleading users
            //LocationManager.StopMonitoringSignificantLocationChanges( );

            foreach ( TrackedRegion region in Regions )
            {
                CLLocationManager.StopMonitoring( region.Region );
            }
        }

        public void StartIntenseLocationScanning( )
        {
            if ( AuthorizationStatus == CLAuthorizationStatus.AuthorizedAlways )
            {
                Console.WriteLine( "LocationManager: STARTING INTENSE SCANNING." );
                IntenseScanningEnabled = true;

                IntenseScanStartTime = DateTime.Now;

                // if allowed, this will start real-time GPS scanning. This is
                // battery intensive, which is why we'll only use it if the user
                // enters a known tracked region.
                if ( CLLocationManager.LocationServicesEnabled )
                {
                    CLLocationManager.PausesLocationUpdatesAutomatically = false;
                    CLLocationManager.StartMonitoringSignificantLocationChanges( );
                    CLLocationManager.AllowsBackgroundLocationUpdates = true;
                    CLLocationManager.StartUpdatingLocation( );
                }
            }
            else
            {
                Console.WriteLine( "LocationManager: STARTING INTENSE SCANNING NOT AUTHORIZED. PERMISSIONS {0}", AuthorizationStatus );
            }
        }

        public void StopIntenseLocationScanning( )
        {
            Console.WriteLine( "LocationManager: STOPPING INTENSE SCANNING." );
            IntenseScanningEnabled = false;

            // when we're outside all known regions, we'll stop intense scanning so the
            // user's battery isn't completely drained
            CLLocationManager.StopMonitoringSignificantLocationChanges( );
            CLLocationManager.AllowsBackgroundLocationUpdates = false;
            CLLocationManager.StopUpdatingLocation( );
        }

        public void IntenseScanningLocationReceived (object sender, CLLocation[] locationList )
        {
            Console.WriteLine( "START NEW LOCATION CHANGES" );
            Console.WriteLine( "--------------------------" );

            
            // Handle foreground updates
            foreach ( CLLocation location in locationList )
            {
                Console.WriteLine( "Longitude: " + location.Coordinate.Longitude );
                Console.WriteLine( "Latitude: " + location.Coordinate.Latitude );
                Console.WriteLine( "" );


                // see if any of these locations are within the region
                double closestDist = 20;
                CLRegion closestLandmark = null;

                foreach ( TrackedRegion trackedRegion in Regions )
                {
                    foreach ( CLRegion landmark in trackedRegion.LandMarks )
                    {
                        double locationDist = location.DistanceFrom( new CLLocation( landmark.Center.Latitude, landmark.Center.Longitude ) );
                        if ( locationDist < closestDist )
                        {
                            closestDist = locationDist;
                            closestLandmark = landmark;
                        }
                    }
                }

                // make sure we aren't already using this location
                if ( CurrentLandMark != closestLandmark )
                {
                    CurrentLandMark = closestLandmark;

                    if ( CurrentLandMark != null )
                    {
                        // once we hit a valid landmark, shut off the scanner.
                        Console.WriteLine( "LocationManager: Landmark reached." );
                        StopIntenseLocationScanning( );
                    }

                    LandmarkChanged( CurrentLandMark );
                }
            }

            Console.WriteLine( "--------------------------" );
            Console.WriteLine( "END NEW LOCATION CHANGES" );

            // if we run an intense scan for more tham MaxMinutes, force it to stop.
            // We assume we were in the area but never hit a landmark, and will have to wait for the
            // next region change.
            TimeSpan elapsedScanTime = DateTime.Now - IntenseScanStartTime;
            if ( elapsedScanTime.TotalMinutes > MaxIntenseScanTimeMinutes )
            {
                Console.WriteLine( "LocationManager: INTENSE SCAN TIME HAS EXPIRED. STOPPING INTENSE SCANNING TO SAVE BATTERY." );
                StopIntenseLocationScanning( );

                // broadcast that the intense scan has expired. This allows the app to
                // begin a new one if it wants to.
                IntenseScanExpired( );
            }
        }
    }
}
#endif
#endif