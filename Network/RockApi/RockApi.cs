using System;
using RestSharp;
using System.IO;
using Newtonsoft.Json;
using System.Net;

namespace Rock.Mobile
{
    namespace Network
    {
        public sealed class RockApi
        {
            // don't wait longer than 15 seconds
            const int RequestTimeoutMS = 15000;

            const string BaseUrl = "http://rock.ccvonline.com/api";
            const string NETWORK_OBJECTS_FILENAME = "NetworkObjects.dat";

            CookieContainer CookieContainer { get; set; }

            public delegate void RequestResult(System.Net.HttpStatusCode statusCode, string statusDescription);
            public delegate void RequestResult<TModel>(System.Net.HttpStatusCode statusCode, string statusDescription, TModel model);

            static RockApi _Instance = new RockApi();
            public static RockApi  Instance { get { return _Instance; } }

            RockApi( )
            {
                CookieContainer = new System.Net.CookieContainer();
            }

            public void Login( string username, string password, RequestResult resultHandler )
            {
                RestRequest request = new RestRequest( Method.POST );
                request.Resource = "Auth/Login";

                request.AddParameter( "Username", username );
                request.AddParameter( "Password", password );
                request.AddParameter( "Persisted", true );

                //request.Timeout = RequestTimeoutMS;

                ExecuteAsync( request, resultHandler);

                // make the request
                /*RestClient restClient = new RestClient( );
                restClient.BaseUrl = BaseUrl;
                restClient.CookieContainer = CookieContainer;

                restClient.ExecuteAsync( request, response =>
                    {
                        Rock.Mobile.Threading.UIThreading.PerformOnUIThread( delegate 
                            { 
                                // notify our caller so they can do whatever it is they wanna do
                                resultHandler( response.StatusCode, response.StatusDescription );
                            });
                    });*/
            }

            public void Logout()
            {
                // reset our cookies
                CookieContainer = new CookieContainer();
            }

            public void GetProfile( string userName, RequestResult<Rock.Client.Person> resultHandler )
            {
                // request a profile by the username. If no username is specified, we'll use the logged in user's name.
                RestRequest request = new RestRequest( Method.GET );
                request.Resource = "People/GetByUserName/";
                request.Resource += string.IsNullOrEmpty( userName ) == true ? MobileUser.Instance.Username : userName;

                ExecuteAsync<Rock.Client.Person>( request, resultHandler);
            }

            public void UpdateProfile( Rock.Client.Person person, RequestResult resultHandler )
            {
                // request a profile by the username. If no username is specified, we'll use the logged in user's name.
                RestRequest request = new RestRequest( Method.PUT );
                request.Resource = "People/";
                request.Resource += person.Id;

                request.RequestFormat = DataFormat.Json;
                request.AddBody( person );

                ExecuteAsync( request, resultHandler);
            }

            /// <summary>
            /// Wrapper for ExecuteAsync<> that requires no generic Type.
            /// </summary>
            /// <param name="request">Request.</param>
            /// <param name="resultHandler">Result handler.</param>
            private void ExecuteAsync( RestRequest request, RequestResult resultHandler )
            {
                ExecuteAsync<object>( request, delegate(HttpStatusCode statusCode, string statusDescription, object model) 
                    {
                        // call the provided handler and drop the dummy object
                        resultHandler( statusCode, statusDescription );
                    });
            }

            private void ExecuteAsync<TModel>( RestRequest request, RequestResult<TModel> resultHandler ) where TModel : new( )
            {
                RestClient restClient = new RestClient( );
                restClient.BaseUrl = BaseUrl;
                restClient.CookieContainer = CookieContainer;

                // don't wait longer than 15 seconds
                request.Timeout = RequestTimeoutMS;

                restClient.ExecuteAsync<TModel>( request, response => 
                    {
                        // exception or not, notify the caller of the desponse
                        Rock.Mobile.Threading.UIThreading.PerformOnUIThread( delegate 
                            { 
                                resultHandler( response != null ? response.StatusCode : HttpStatusCode.RequestTimeout, 
                                               response != null ? response.StatusDescription : "Client has no connection.", 
                                               response != null ? response.Data : new TModel() );
                            });
                    });
            }

            public void SaveObjectsToDevice( )
            {
                // this will save the current state of all objects to the device,
                // which is obviously important so we maintain local copies of things
                // and can access as much as possible without a network connection
                string filePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), NETWORK_OBJECTS_FILENAME);

                // open a stream
                using (StreamWriter writer = new StreamWriter(filePath, false))
                {
                    // store our cookies. We cannot serialize the container, so we retrieve and save just the 
                    // cookies we care about.
                    CookieCollection cookieCollection = CookieContainer.GetCookies( new Uri( BaseUrl ) );
                    writer.WriteLine( cookieCollection.Count.ToString() );
                    for( int i = 0; i < cookieCollection.Count; i++ )
                    {
                        string cookieStr = JsonConvert.SerializeObject( cookieCollection[i] );
                        writer.WriteLine( cookieStr );
                    }

                    // store the mobile user
                    writer.WriteLine( MobileUser.Instance.Serialize( ) );
                }
            }

            public void LoadObjectsFromDevice( )
            {
                // at startup, this should be called to allow current objects to be restored.
                string filePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), NETWORK_OBJECTS_FILENAME);

                // if the file exists
                if(System.IO.File.Exists(filePath) == true)
                {
                    // read it
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        // load our cookies
                        int numCookies = int.Parse( reader.ReadLine() );
                        for( int i = 0; i < numCookies; i++ )
                        {
                            string cookieStr = reader.ReadLine();
                            Cookie cookie = JsonConvert.DeserializeObject<Cookie>( cookieStr ) as Cookie;
                            CookieContainer.Add( cookie );
                        }

                        // load the mobile user
                        MobileUser.Instance.Deserialize( reader.ReadLine() );

                        //jsonObj = reader.ReadLine();
                    }
                }
            }

            public void SyncWithServer()
            {
                // this is a chance for anything unsaved to go ahead and save
                MobileUser.Instance.SyncDirtyObjects( );
            }
        }
    }
}
