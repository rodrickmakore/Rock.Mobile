using System;

namespace Rock.Mobile.Util.Strings
{
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

