using System;
using System.Collections.Generic;

namespace Keyknox
{
    public class MetaDate
    {
        const string format = "MMM ddd d HH:mm yyyy";
        const string modificationDateKey = "keyknox_upd";
        const string creationDateKey = "keyknox_crd";

        public static DateTime ExtractModificationDateFrom(Dictionary<string, string> meta)
        {
            return DateTime.ParseExact(meta[modificationDateKey], format, null);
        }

        public static DateTime ExtractCreationDateFrom(Dictionary<string, string> meta)
        {
            return DateTime.ParseExact(meta[creationDateKey], format, null);
        }

        public static Dictionary<string, string> CopyAndAppendModificationDateTo(Dictionary<string, string> originalMeta, DateTime date)
        {
            var meta = new Dictionary<string, string>(originalMeta);
            meta.Add(modificationDateKey, date.ToString(format));
            return meta;
        }

        public static Dictionary<string, string> CopyAndAppendCreationDateTo(Dictionary<string, string> originalMeta, DateTime date)
        {
            var meta = new Dictionary<string, string>(originalMeta);
            meta.Add(creationDateKey, date.ToString(format));
            return meta;
        }
    }
}
