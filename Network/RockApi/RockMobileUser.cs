using System;
using Rock.Client;
using Newtonsoft.Json;

namespace Rock.Mobile
{
    namespace Network
    {
        /// <summary>
        /// Basically a wrapper for Rock.Models that make up the "user" of this mobile app.
        /// </summary>
        public sealed class MobileUser
        {
            private static MobileUser _Instance = new MobileUser();
            public static MobileUser Instance { get { return _Instance; } }

            /// <summary>
            /// Account - Username
            /// </summary>
            /// <value>The username.</value>
            public string Username { get; set; }

            /// <summary>
            /// Account - Password
            /// </summary>
            /// <value>The password.</value>
            public string Password { get; set; }

            /// <summary>
            /// True when logged in
            /// </summary>
            /// <value><c>true</c> if logged in; otherwise, <c>false</c>.</value>
            public bool LoggedIn { get; set; }

            /// <summary>
            /// Person object representing this user's core personal data.
            /// </summary>
            /// <value>The person.</value>
            public Person Person;
            public string PersonJson { get; set; }

            private MobileUser( )
            {
                Person = new Person();
            }

            public string PreferredName( )
            {
                if( string.IsNullOrEmpty( Person.NickName ) == false )
                {
                    return Person.NickName;
                }
                else
                {
                    return Person.FirstName;
                }
            }

            public void Login( string username, string password, RockApi.RequestResult loginResult )
            {
                RockApi.Instance.Login( username, password, delegate(System.Net.HttpStatusCode statusCode, string statusDescription) 
                    {
                        // if we received Ok (nocontent), we're logged in.
                        if( statusCode == System.Net.HttpStatusCode.NoContent )
                        {
                            Username = username;
                            Password = password;

                            LoggedIn = true;

                            // save!
                            RockApi.Instance.SaveObjectsToDevice( );
                        }

                        // notify the caller
                        if( loginResult != null )
                        { 
                            loginResult( statusCode, statusDescription );
                        }
                    } );
            }

            public void Logout( )
            {
                // clear the person and take a blank copy
                Person = new Person();
                PersonJson = JsonConvert.SerializeObject( Person );

                LoggedIn = false;

                Username = "";
                Password = "";

                RockApi.Instance.Logout( );

                // save!
                RockApi.Instance.SaveObjectsToDevice( );
            }

            public void GetProfile( RockApi.RequestResult<Rock.Client.Person> profileResult )
            {
                RockApi.Instance.GetProfile( Username, delegate(System.Net.HttpStatusCode statusCode, string statusDescription, Rock.Client.Person model)
                    {
                        if( statusCode == System.Net.HttpStatusCode.NoContent || statusCode == System.Net.HttpStatusCode.OK )
                        {
                            // on retrieval, convert this version for dirty compares later
                            Person = model;
                            PersonJson = JsonConvert.SerializeObject( Person );

                            // save!
                            RockApi.Instance.SaveObjectsToDevice( );
                        }

                        // notify the caller
                        if( profileResult != null )
                        {
                            profileResult( statusCode, statusDescription, model );
                        }
                    });
            }

            public void UpdateProfile( RockApi.RequestResult profileResult )
            {
                RockApi.Instance.UpdateProfile( Person, delegate(System.Net.HttpStatusCode statusCode, string statusDescription)
                    {
                        if( statusCode == System.Net.HttpStatusCode.NoContent )
                        {
                            // if successful, update our json so we have a match and don't try to update again later.
                            PersonJson = JsonConvert.SerializeObject( Person );
                        }

                        // whether we succeeded in updating with the server or not, save to disk.
                        RockApi.Instance.SaveObjectsToDevice( );

                        if( profileResult != null )
                        {
                            profileResult( statusCode, statusDescription );
                        }
                    });
            }

            public void SyncDirtyObjects( )
            {
                // check to see if our person object changed. If our original json
                // created at a point when we know we were sync'd with the server
                // no longer matches our object, we should update it.
                string currPersonJson = JsonConvert.SerializeObject( Person );
                if( string.Compare( PersonJson, currPersonJson ) != 0 )
                {
                    UpdateProfile( null );
                }
            }

            public string Serialize( )
            {
                return JsonConvert.SerializeObject( this );
            }

            public void Deserialize( string json )
            {
                _Instance = JsonConvert.DeserializeObject<MobileUser>( json ) as MobileUser;
            }
        }
    }
}
