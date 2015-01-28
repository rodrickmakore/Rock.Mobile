using System;

namespace Rock.Mobile.Util.Strings
{
    public static class Parsers
    {
        /// <summary>
        /// Given an address formatted "street, city, state, zip" (commas optional)
        /// The broken out components will be returned.
        /// </summary>
        /// <returns><c>true</c>, if address was parsed, <c>false</c> otherwise.</returns>
        public static bool ParseAddress( string address, ref string street, ref string city, ref string state, ref string zip )
        {
            bool result = false;

            // we parse by working backwards.
            do
            {
                // first parse off the zip code, which comes last.
                int zipCodeIndex = address.LastIndexOf( ' ' );
                if( zipCodeIndex == -1 ) break;
                string workingStr = address.Substring( zipCodeIndex );

                if( workingStr == null ) break;
                zip = workingStr.Trim( new char[] { ',' } );

                // make sure it contains at least 1 digit, or it's obviously not a zip code.
                if( zip.AsNumeric( ).Length == 0 ) break;

                // truncate at the zipcode
                address = address.Remove( zipCodeIndex );


                // next comes the state
                int stateIndex = address.LastIndexOf( ' ' );
                if( stateIndex == -1 ) break;

                state = address.Substring( stateIndex );
                if( state == null ) break;

                state = state.Trim( new char[] { ',' } );

                // truncate at the state
                address = address.Remove( stateIndex );


                // city
                int cityIndex = address.LastIndexOf( ' ' );
                if( cityIndex == -1 ) break;

                city = address.Substring( cityIndex );
                if( city == null ) break;

                city = city.Trim( new char[] { ',' } );

                // truncate at the city
                address = address.Remove( cityIndex );


                // street is the remaning string
                if( address == null ) break;
                street = address.Trim( new char[] { ',' } );

                result = true;
            }
            while( 0 != 1 );

            return result;
        }
    }

    public static class StringExtensions
    {
        /// <summary>
        /// Returns a new string that contains only digits
        /// </summary>
        /// <returns>The non numeric.</returns>
        /// <param name="source">Source.</param>
        public static string AsNumeric( this string source )
        {
            string numericString = "";

            for( int i = 0; i < source.Length; i++ )
            {
                if( source[ i ] >= '0' && source[ i ] <= '9' )
                {
                    numericString += source[ i ];
                }
            }

            return numericString;
        }

        /// <summary>
        /// Takes a string assumed to be only digits and formats it as a phone number.
        /// </summary>
        public static string AsPhoneNumber( this string number )
        {
            // nothing to do if it's less than four digits
            if ( number.Length < 4 
            )
            {
                return number;
            }
            // We know it has at least enough for a local exchange and subscriber number
            else if ( number.Length < 8 )
            {
                return number.Substring( 0, 3 ) + "-" + number.Substring( 3 );
            }
            else
            {
                // We know it has at least enough for an area code and local exchange
                // Area Code
                // Local Exchange
                // Subscriber Number
                string areaCode = number.Substring( 0, 3 );

                string localExchange = number.Substring( 3, 3 );

                // for the subscriber nubmer, take the remaining four digits, but no more.
                string subscriberNumber = number.Substring( 6, System.Math.Min( number.Length - 6, 4 ) ); 

                return "(" + areaCode + ")" + " " + localExchange + "-" + subscriberNumber;
            }
        }
    }
}

