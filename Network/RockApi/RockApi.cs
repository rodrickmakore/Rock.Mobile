using System;
using RestSharp;

namespace Rock.Mobile
{
    namespace Network
    {
        public sealed class RockApi
        {
            // don't wait longer than 15 seconds
            const int RequestTimeoutMS = 15000;

            const string BaseUrl = "http://rock.ccvonline.com/api";

            System.Net.CookieContainer CookieContainer { get; set; }

            public delegate void LoginResult(System.Net.HttpStatusCode statusCode, string statusDescription);

            public delegate void RequestResult<TModel>(System.Net.HttpStatusCode statusCode, string statusDescription, TModel model);

            static RockApi _Instance = new RockApi();
            public static RockApi  Instance { get { return _Instance; } }

            RockApi( )
            {
                CookieContainer = new System.Net.CookieContainer();
            }

            public void Login( string username, string password, LoginResult resultHandler )
            {
                RestRequest request = new RestRequest( Method.POST );
                request.Resource = "Auth/Login";

                request.AddParameter( "Username", username );
                request.AddParameter( "Password", password );
                request.AddParameter( "Persisted", true );

                request.Timeout = RequestTimeoutMS;

                // make the request
                RestClient restClient = new RestClient( );
                restClient.BaseUrl = BaseUrl;
                restClient.CookieContainer = CookieContainer;

                restClient.ExecuteAsync( request, response =>
                    {
                        if ( response.ErrorException == null )
                        {
                            if( response.StatusCode == System.Net.HttpStatusCode.NoContent )
                            {
                                // if we received No Content, we're logged in
                                MobileUser.Instance.Login( username, password );
                            }

                            Rock.Mobile.Threading.UIThreading.PerformOnUIThread( delegate 
                                { 
                                    // notify our caller so they can do whatever it is they wanna do
                                    resultHandler( response.StatusCode, response.StatusDescription );
                                });
                        }
                        else
                        {
                            throw response.ErrorException;
                        }
                    });
            }

            public void Logout()
            {
                // reset our cookies
                CookieContainer = new System.Net.CookieContainer();

                // logout the user
                MobileUser.Instance.Logout( );
            }

            public void GetProfile( string userName, RequestResult<Rock.Client.Person> resultHandler )
            {
                // request a profile by the username. If no username is specified, we'll use the logged in user's name.
                RestRequest request = new RestRequest( Method.GET );
                request.Resource = "People/GetByUserName/";
                request.Resource += string.IsNullOrEmpty( userName ) == true ? MobileUser.Instance.Username : userName;

                ExecuteAsync<Rock.Client.Person>( request, delegate(System.Net.HttpStatusCode code, string desc, Rock.Client.Person model)
                    {
                        MobileUser.Instance.Person = model;

                        // forward to the result handler
                        resultHandler( code, desc, model );
                    });
            }

            public void ExecuteAsync<TModel>( RestRequest request, RequestResult<TModel> resultHandler ) where TModel : new( )
            {
                // do not allow api calls if not logged
                if( MobileUser.Instance.LoggedIn == false ) throw new Exception( "Must log in before making API calls. Call Login()!" );

                RestClient restClient = new RestClient( );
                restClient.BaseUrl = BaseUrl;
                restClient.CookieContainer = CookieContainer;

                // don't wait longer than 15 seconds
                request.Timeout = RequestTimeoutMS;

                restClient.ExecuteAsync<TModel>( request, response => 
                    {
                        if ( response.ErrorException == null )
                        {
                            Rock.Mobile.Threading.UIThreading.PerformOnUIThread( delegate 
                                { 
                                    resultHandler( response.StatusCode, response.StatusDescription, response.Data );
                                });
                        }
                        else
                        {
                            throw response.ErrorException;
                        }
                    });
            }
        }
    }
}
