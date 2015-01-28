#if __IOS__
using System;
using Foundation;
using CoreGraphics;

namespace Rock.Mobile.PlatformSpecific.Util
{
    public static class DateTimeExtensions
    {
        public static DateTime NSDateToDateTime(this NSDate date)
        {
            DateTime reference = TimeZone.CurrentTimeZone.ToLocalTime( 
                new DateTime(2001, 1, 1, 0, 0, 0) );
            return reference.AddSeconds(date.SecondsSinceReferenceDate);
        }

        public static NSDate DateTimeToNSDate(this DateTime date)
        {
            DateTime reference = TimeZone.CurrentTimeZone.ToLocalTime(
                new DateTime(2001, 1, 1, 0, 0, 0) );
            return NSDate.FromTimeIntervalSinceReferenceDate(
                (date - reference).TotalSeconds);
        }
    }

    public static class CGObjectExtensions
    {
        public static System.Drawing.PointF ToPointF( this CGPoint point )
        {
            return new System.Drawing.PointF( (float) point.X, (float) point.Y );
        }

        public static System.Drawing.SizeF ToSizeF( this CGSize size )
        {
            return new System.Drawing.SizeF( (float)size.Width, (float)size.Height );
        }

        public static System.Drawing.RectangleF ToRectF( this CGRect rect )
        {
            return new System.Drawing.RectangleF( (float) rect.X, (float) rect.Y, (float) rect.Width, (float) rect.Height );
        }
    }
}
#endif
