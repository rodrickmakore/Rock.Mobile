using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;
using RestSharp;
using Newtonsoft.Json;
using RestSharp.Deserializers;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;


namespace Rock.Mobile
{
    namespace Network
    {
        internal class WebRequestManager
        {
            /// <summary>
            /// The timeout after which the REST call attempt is given up.
            /// </summary>
            const int RequestTimeoutMS = 15000;

            /// <summary>
            /// Common interface to allow different generics in the same queue
            /// </summary>
            internal interface IWebRequestObject
            {
                void ProcessRequest( );
            }

            /// <summary>
            /// Implementation of our request object. Stores the URL, request and handler to be executed later
            /// on the worker thread.
            /// </summary>
            internal class WebRequestObject<TModel> : IWebRequestObject where TModel : new( )
            {
                /// <summary>
                /// URL for the web request
                /// </summary>
                string RequestUrl { get; set; }

                /// <summary>
                /// The request object containing the relavent HTTP request data
                /// </summary>
                RestRequest Request { get; set; }

                /// <summary>
                /// The handler to call when the request is complete
                /// </summary>
                HttpRequest.RequestResult<TModel> ResultHandler { get; set; }

                /// <summary>
                /// If relavant, the cookie container this request should use.
                /// </summary>
                /// <value>The cookie container.</value>
                CookieContainer CookieContainer { get; set; }

                public WebRequestObject( string requestUrl, RestRequest request, HttpRequest.RequestResult<TModel> callback, CookieContainer cookieContainer )
                {
                    RequestUrl = requestUrl;
                    Request = request;
                    ResultHandler = callback;
                    CookieContainer = cookieContainer;
                }

                public void ProcessRequest( )
                {
                    RestClient restClient = new RestClient();
                    restClient.CookieContainer = CookieContainer;

                    // RestSharp for some reason uses a string on Android, and Uri on iOS. This happened when they migrated to
                    // the Unified API for iOS.
                    #if __IOS__
                    restClient.BaseUrl = new System.Uri( RequestUrl );
                    #elif __ANDROID__
                    restClient.BaseUrl = RequestUrl;
                    #endif

                    // replace the existing json deserializer with Json.Net
                    restClient.AddHandler( "application/json", new JsonDeserializer() );


                    // don't wait longer than 15 seconds
                    Request.Timeout = RequestTimeoutMS;

                    // if the TModel is RestResponse, that implies they want the actual response, and no parsing.
                    if ( typeof( TModel ) == typeof( RestResponse ) )
                    {
                        IRestResponse response = restClient.Execute( Request );

                        // exception or not, notify the caller of the desponse
                        Rock.Mobile.Threading.Util.PerformOnUIThread( delegate
                            { 
                                ResultHandler( response != null ? response.StatusCode : HttpStatusCode.RequestTimeout, 
                                    response != null ? response.StatusDescription : "Client has no connection.", 
                                    (TModel)response );
                            } );
                    }
                    else
                    {
                        IRestResponse<TModel> response = restClient.Execute<TModel>( Request );

                        // exception or not, notify the caller of the desponse
                        Rock.Mobile.Threading.Util.PerformOnUIThread( delegate
                            { 
                                ResultHandler( response != null ? response.StatusCode : HttpStatusCode.RequestTimeout, 
                                    response != null ? response.StatusDescription : "Client has no connection.", 
                                    response != null ? response.Data : new TModel() );
                            } );       
                    }
                }
            }

            /// <summary>
            /// The singleton for our request manager. All web requests will funnel thru this.
            /// </summary>
            static WebRequestManager _Instance = new WebRequestManager( );
            public static WebRequestManager Instance { get { return _Instance; } }

            /// <summary>
            /// The queue of web requests that need to be executed
            /// </summary>
            ConcurrentQueue<IWebRequestObject> RequestQueue { get; set; }

            /// <summary>
            /// Pointer to the worker thread for downloading
            /// </summary>
            System.Threading.Thread DownloadThread { get; set; }

            EventWaitHandle WaitHandle { get; set; }

            public WebRequestManager( )
            {
                // create our queue and fire up the download thread
                RequestQueue = new ConcurrentQueue<IWebRequestObject>();

                DownloadThread = new System.Threading.Thread( ThreadProc );
                DownloadThread.Start( );

                WaitHandle = new EventWaitHandle( false, EventResetMode.AutoReset );
            }

            /// <summary>
            /// The one entry point, this is where requests should be sent
            /// </summary>
            public void PushRequest( IWebRequestObject requestObj )
            {
                RequestQueue.Enqueue( requestObj );

                Console.WriteLine( "Setting Wait Handle" );
                WaitHandle.Set( );
            }

            void ThreadProc( )
            {
                while ( true )
                {
                    Console.WriteLine( "ThreadProc: Sleeping..." );
                    WaitHandle.WaitOne( );
                    Console.WriteLine( "ThreadProc: Waking for work" );

                    // while there are requests pending, process them
                    while ( RequestQueue.Count != 0 )
                    {
                        Console.WriteLine( "ThreadProc: Processing Request" );

                        // get the web request out of the queue
                        IWebRequestObject requestObj = null;
                        RequestQueue.TryDequeue( out requestObj );

                        if( requestObj != null )
                        {
                            // execute it
                            requestObj.ProcessRequest( );
                        }
                    }
                }
            }
        }
        
        public class HttpRequest
        {
            /// <summary>
            /// Request Response delegate that does not require a returned object
            /// </summary>
            public delegate void RequestResult(System.Net.HttpStatusCode statusCode, string statusDescription);

            /// <summary>
            /// Request response delegate that does require a returned object
            /// </summary>
            public delegate void RequestResult<TModel>(System.Net.HttpStatusCode statusCode, string statusDescription, TModel model);

            public CookieContainer CookieContainer { get; set; }

            /// <summary>
            /// Wrapper for ExecuteAsync<> that requires no generic Type.
            /// </summary>
            /// <param name="request">Request.</param>
            /// <param name="resultHandler">Result handler.</param>
            public void ExecuteAsync( string requestUrl, RestRequest request, RequestResult resultHandler )
            {
                ExecuteAsync<object>( requestUrl, request, delegate(HttpStatusCode statusCode, string statusDescription, object model) 
                    {
                        // call the provided handler and drop the dummy object
                        if ( resultHandler != null )
                        {
                            resultHandler( statusCode, statusDescription );
                        }
                    });
            }

            /// <summary>
            /// Wrapper for ExecuteAsync<> that returns to the user the raw bytes of the request (useful for image retrieval or any non-object based return type)
            /// </summary>
            /// <param name="request">Request.</param>
            /// <param name="resultHandler">Result handler.</param>
            public void ExecuteAsync( string requestUrl, RestRequest request, RequestResult<byte[]> resultHandler )
            {
                // to give them the raw data, we'll call ExecuteAsync<> and pass in the acutal response object as the type.
                ExecuteAsync<RestSharp.RestResponse>( requestUrl, request, delegate(HttpStatusCode statusCode, string statusDescription, RestSharp.RestResponse model) 
                    {
                        // then, we'll call the result handler and pass the raw bytes.
                        resultHandler( statusCode, statusDescription, model.RawBytes );
                    });
            }

            public void ExecuteAsync<TModel>( string requestUrl, RestRequest request, RequestResult<TModel> resultHandler ) where TModel : new( )
            {
                WebRequestManager.WebRequestObject<TModel> requestObj = new WebRequestManager.WebRequestObject<TModel>( requestUrl, request, resultHandler, CookieContainer );
                WebRequestManager.Instance.PushRequest( requestObj );
            }
        }

        /// <summary>
        /// Implements a RestSharp Json deserializer that uses Json.Net,
        /// which has better compatibility with things like ICollection
        /// </summary>
        class JsonDeserializer : IDeserializer
        {
            //
            // Properties
            //
            public string DateFormat
            {
                get;
                set;
            }

            public string Namespace
            {
                get;
                set;
            }

            public string RootElement
            {
                get;
                set;
            }

            //
            // Methods
            //
            public T Deserialize<T>( IRestResponse response )
            {
                return (T)JsonConvert.DeserializeObject<T>( response.Content );
            }
        }

        public class Util
        {
            /// <summary>
            /// Convenience method when you just need to know if a return was in the 200 range.
            /// </summary>
            /// <returns><c>true</c>, if in success range was statused, <c>false</c> otherwise.</returns>
            /// <param name="code">Code.</param>
            public static bool StatusInSuccessRange( HttpStatusCode code )
            {
                switch( code )
                {
                    case HttpStatusCode.Accepted:
                    case HttpStatusCode.Created:
                    case HttpStatusCode.NoContent:
                    case HttpStatusCode.NonAuthoritativeInformation:
                    case HttpStatusCode.OK:
                    case HttpStatusCode.PartialContent:
                    case HttpStatusCode.ResetContent:
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}

