using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Rock.Mobile
{
    namespace Network
    {
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

        //TODO: Either make this not a singleton, or manage a queue ourselves

        public class HttpWebRequest
        {
            // singleton pattern
            static HttpWebRequest _instance;

            static public HttpWebRequest Instance
            {
                get
                {
                    if( _instance == null )
                    {
                        _instance = new HttpWebRequest( );
                    }
                    return _instance;
                }
            }

            System.Net.WebRequest WebRequest { get; set; }

            // setup a response delegate to provide the web response.
            public delegate void Callback( Exception e, Dictionary<string, string> responseHeaders, string body );

            private Callback ResponseDelegate { get; set; }

            private HttpWebRequest( )
            {
                ResponseDelegate = null;
            }

            public void MakeAsyncRequest( string url, Callback responseDelegate )
            {
                ResponseDelegate = responseDelegate;

                // note - in a better version, we could pass query or body args
                // instead of doing it all in the caller.
                WebRequest = System.Net.WebRequest.Create( url );
                WebRequest.Method = "GET";
                WebRequest.Timeout = 10000;

                WebRequest.BeginGetResponse( new AsyncCallback( FinishWebRequest ), null );
            }

            void FinishWebRequest( IAsyncResult result )
            {
                Exception ex = null;
                Dictionary<string, string> responseHeaders = null;
                string responseBody = null;

                try
                {
                    // get the response
                    WebResponse response = WebRequest.EndGetResponse( result );

                    // parse the headers
                    responseHeaders = new Dictionary<string, string>( );
                    for( int i = 0; i < response.Headers.Count; i++ )
                    {
                        responseHeaders.Add( response.Headers.GetKey( i ), response.Headers.Get( i ) );
                    }

                    // get the body
                    Stream dataStream = response.GetResponseStream( );
                    StreamReader reader = new StreamReader( dataStream );
                    responseBody = reader.ReadToEnd( );
                } 
                catch( Exception e )
                {
                    ex = e;
                }

                // and notify the original caller
                Callback responder = ResponseDelegate;
                ResponseDelegate = null;

                responder( ex, responseHeaders, responseBody );
            }
        }
    }
}

