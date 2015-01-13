using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;
using RestSharp;
using Newtonsoft.Json;
using RestSharp.Deserializers;

namespace Rock.Mobile
{
    namespace Network
    {
        public class HttpRequest
        {
            /// <summary>
            /// The timeout after which the REST call attempt is given up.
            /// </summary>
            const int RequestTimeoutMS = 15000;

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
                RestClient restClient = new RestClient( );
                restClient.BaseUrl = requestUrl;
                restClient.CookieContainer = CookieContainer;

                // replace the existing json deserializer with Json.Net
                restClient.AddHandler("application/json", new JsonDeserializer( ) );

                // set the request format
                //request.RequestFormat = Format;

                // don't wait longer than 15 seconds
                request.Timeout = RequestTimeoutMS;

                // if the TModel is RestResponse, that implies they want the actual response, and no parsing.
                if( typeof( TModel ) == typeof( RestResponse ) )
                {
                    restClient.ExecuteAsync( request, response => 
                        {
                            // exception or not, notify the caller of the desponse
                            Rock.Mobile.Threading.Util.PerformOnUIThread( delegate 
                                { 
                                    resultHandler( response != null ? response.StatusCode : HttpStatusCode.RequestTimeout, 
                                        response != null ? response.StatusDescription : "Client has no connection.", 
                                        (TModel)response );
                                });
                        });
                }
                else
                {
                    restClient.ExecuteAsync<TModel>( request, response => 
                        {
                            // exception or not, notify the caller of the desponse
                            Rock.Mobile.Threading.Util.PerformOnUIThread( delegate 
                                { 
                                    resultHandler( response != null ? response.StatusCode : HttpStatusCode.RequestTimeout, 
                                        response != null ? response.StatusDescription : "Client has no connection.", 
                                        response != null ? response.Data : new TModel() );
                                });
                        });
                }
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

