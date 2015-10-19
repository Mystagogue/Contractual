using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Globalization;

namespace Contractual.DomainModel
{
    [DataContract]
    public class Money : IFormattable
    {
        [DataMember]
        public decimal Amount { get; set; }

        [DataMember]
        public string CurrencyNumeric3
        {
            get;
            private set;
        }

        public readonly string CurrencyAlpha3;

        public Money(decimal amount, string currencyNumber)
        {
            Amount = amount;
            CurrencyNumeric3 = currencyNumber;
            CurrencyAlpha3 = MoneyFormatInfo.GetIsoField(currencyNumber);
        }

        public override string ToString()
        {
            return this.ToString("G", null);
        }

        public string ToString(string format)
        {
            return this.ToString(format, null);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            var check = formatProvider as MoneyFormatInfo;
            if (check != null && check.IsoField != CurrencyAlpha3)
            {
                //Force an override of the general Iso currency code request to instead use this object's specific code.
                formatProvider = check.Provider;
            }
            format = !String.IsNullOrWhiteSpace(format) ? format.ToLower() : "g";

            bool useIsoSymbol = format == "m";
            format = useIsoSymbol ? "c" : format;

            formatProvider = format == "c" ?
                new MoneyFormatInfo(CurrencyNumeric3, useIsoSymbol, formatProvider) :
                formatProvider ?? CultureInfo.CurrentCulture;

            string result = Amount.ToString(format, formatProvider);

            return result;
        }

    }


}
