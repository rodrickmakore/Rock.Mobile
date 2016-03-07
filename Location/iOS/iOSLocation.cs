#if __IOS__
#if USE_LOCATION_SERVICES
using System;
using Location;
using CoreLocation;

namespace iOSLocationServices
{
    public class iOSLocation : Location.Location
    {
        public LocationManager LocationManager { get; protected set; }

        public iOSLocation( )
        {
        }

        // override the ready delegate so that if we've been created
        // before this was set, we can notify the caller that we're ready.
        public override OnReadyCallback OnReadyDelegate 
        { 
            get 
            { 
                return base.OnReadyDelegate; 
            }

            set
            {
                if ( LocationManager != null )
                {
                    base.OnReadyDelegate = value;
                    base.OnReadyDelegate( );
                }
            }
        }

        public override void Create(object context)
        {
            LocationManager = new LocationManager( RegionEntered, RegionExited, LandmarkChanged, IntenseScanExpired, AuthorizationChanged );

            // iOS is immediately ready
            if ( OnReadyDelegate != null )
            {
                OnReadyDelegate( );
            }
        }

        public override void EnterBackground(object context)
        {
            // nothing we need to do here   
        }

        public override void EnterForeground(object context)
        {
            LocationManager.RefreshRegionsAndLandmarks( );
        }

        public override void BeginAddRegionsForTrack()
        {
            // this is for Android. we don't need to do anything here
        }

        public override void AddRegionForTrack( string region, double latitude, double longitude, float radius )
        {
            LocationManager.AddRegionToTrack( latitude, longitude, radius, region );
        }

        public override void AddLandmarkToRegion( string parentRegion, string landmark, double latitude, double longitude )
        {
            LocationManager.AddLandmarkToTrack( parentRegion, latitude, longitude, 20, landmark );
        }

        public override void CommitRegionsForTrack()
        {
            // actually begin the tracking for the regions we care about.
            LocationManager.BeginPassiveMonitoring( );
            LocationManager.RefreshRegionsAndLandmarks( );
        }

        void RegionEntered( CLRegion region )
        {
            if ( OnRegionEnteredDelegate != null )
            {
                OnRegionEnteredDelegate( region.Identifier );
            }
        }

        void RegionExited( CLRegion region, bool outsideAllRegions )
        {
            if ( OnRegionExitedDelegate != null )
            {
                OnRegionExitedDelegate( region.Identifier, outsideAllRegions );
            }
        }

        void LandmarkChanged( CLRegion landmark )
        {
            OnLandmarkChangedDelegate( landmark != null ? landmark.Identifier : string.Empty );
        }

        void AuthorizationChanged( CLAuthorizationStatus status )
        {
            // todo: should probably handle this
        }

        void IntenseScanExpired( )
        {
        }
    }
}
#endif
#endif