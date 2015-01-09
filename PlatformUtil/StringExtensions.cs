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
    }
}

