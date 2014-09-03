using System;
using Rock.Client;

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
            public string Username { get; private set; }

            /// <summary>
            /// Account - Password
            /// </summary>
            /// <value>The password.</value>
            public string Password { get; private set; }

            /// <summary>
            /// True when logged in
            /// </summary>
            /// <value><c>true</c> if logged in; otherwise, <c>false</c>.</value>
            public bool LoggedIn { get; private set; }

            public Person Person { get; set; }

            private MobileUser( )
            {
                Person = new Person();
            }

            public void Login( string username, string password )
            {
                Username = username;
                Password = password;

                LoggedIn = true;
            }

            public void Logout( )
            {
                //Todo: Any wrap up we should do before logging out?

                LoggedIn = false;

                Username = "";
                Password = "";
            }



        }
    }
}

