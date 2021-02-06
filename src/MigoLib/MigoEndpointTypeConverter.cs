using System;
using System.ComponentModel;
using System.Globalization;
using System.Net;

namespace MigoLib
{
    public class MigoEndpointTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) 
            => sourceType == typeof(string)  || base.CanConvertFrom(context, sourceType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var source = (string)value;
            var parts = source.Split(':');

            if (parts.Length != 2)
            {
                throw new ArgumentException("Can't convert");
            }

            var ip = IPAddress.Parse(parts[0]);
            var port = ushort.Parse(parts[1]);
            var result = new MigoEndpoint(ip, port);
            
            return result;
        }
    }
}