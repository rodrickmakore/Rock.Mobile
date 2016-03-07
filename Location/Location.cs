
/// <summary>
/// ****IF YOU WANT TO USE THIS, DEFINE THE PRE-PROCESSOR DIRECTIVE USE_LOCATION_SERVICES ****
/// </summary>
#if USE_LOCATION_SERVICES

using System;

namespace Location
{
    
    public abstract class Location
    {
        static Location _Instance;
        public static Location Instance 
        {
            get 
            { 
                if ( _Instance == null )
                {
                    #if __ANDROID__
                    _Instance = new DroidLocationServices.DroidLocation();
                    #elif __IOS__
                    _Instance = new iOSLocationServices.iOSLocation();
                    #endif
                }
                return _Instance; 
            } 
        }

        public Location( )
        {   
        }

        public abstract void Create( object context );

        public abstract void EnterForeground( object context );

        public abstract void EnterBackground( object context );

        public abstract void BeginAddRegionsForTrack( );

        public abstract void AddRegionForTrack( string region, double latitude, double longitude, float radius );

        public abstract void AddLandmarkToRegion( string parentRegion, string landmark, double latitude, double longitude );

        public abstract void CommitRegionsForTrack( );

        public delegate void OnReadyCallback( );
        public virtual OnReadyCallback OnReadyDelegate { get; set; }

        public delegate void OnRegionEnteredCallback( string region );
        public OnRegionEnteredCallback OnRegionEnteredDelegate { get; set; }

        public delegate void OnRegionExitedCallback( string region, bool outsideAllRegions );
        public OnRegionExitedCallback OnRegionExitedDelegate { get; set; }

        public delegate void OnLandmarkChangedCallback( string landmark );
        public OnLandmarkChangedCallback OnLandmarkChangedDelegate { get; set; }
    }
}
#endif
