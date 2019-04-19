using System;
namespace Keyknox
{
    public static class DateTimeExtension
    {
        public static DateTime RoundTicks(this DateTime dateTime){
            var cutTicks = dateTime.Ticks % TimeSpan.TicksPerSecond;
            return dateTime.AddTicks(-cutTicks);
        }
    }
}
