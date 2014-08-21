using System;
using System.Drawing;

namespace RockMobile
{
    namespace Math
    {
        class Util
        {
            public static float DegToRad = 0.0174532925f;

            public static float DotProduct( PointF v1, PointF v2 )
            {
                return ( (v1.X * v2.X) + (v1.Y * v2.Y) );
            }

            public static float MagnitudeSquared( PointF v )
            {
                return DotProduct( v, v );
            }

            public static float Magnitude( PointF v )
            {
                return (float) System.Math.Sqrt( MagnitudeSquared( v ) );
            }
        }
    }
}

