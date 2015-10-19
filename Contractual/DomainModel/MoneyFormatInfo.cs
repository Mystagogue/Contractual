using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Contractual.DomainModel
{
    public class MoneyFormatInfo : IFormatProvider, ICustomFormatter
    {
        internal IFormatProvider Provider;
        private string currencyCode;
        private bool useIsoCurrencyCode;

        //The number of digits to use with a foreign currency pertains to the type of foreign currency, not the users culture.
        //So a map is needed of Iso currency symbol to decimal digit count.
        static Dictionary<string, Meta> MetaMap = (from c in CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                                                   group c by new RegionInfo(c.LCID).ISOCurrencySymbol into ri
                                                   select new { Symbol = ri.Key, Info = ri.First().NumberFormat })
                        .ToDictionary(p => p.Symbol, p => new Meta { Digits = p.Info.CurrencyDecimalDigits, Symbol = p.Info.CurrencySymbol });

        #region ISO 4217 Currency Code Map
        /// <summary>
        /// A full map of ISO 4217 currency codes to three digit alpha currency symbol.
        /// </summary>
        private static Dictionary<string, string> CodeMap = new Dictionary<string, string>
        {
          {"784", "AED" } ,
          {"971", "AFN" } ,
          {"008", "ALL" } ,
          {"051", "AMD" } ,
          {"032", "ARS" } ,
          {"036", "AUD" } ,
          {"944", "AZN" } ,
          {"977", "BAM" } ,
          {"050", "BDT" } ,
          {"975", "BGN" } ,
          { "48", "BHD" },
          { "96", "BND" },
          { "68", "BOB" },
          {"986", "BRL" } ,
          {"974", "BYR" } ,
          {"084", "BZD" } ,
          {"124", "CAD" } ,
          {"756", "CHF" } ,
          {"152", "CLP" } ,
          {"156", "CNY" } ,
          {"170", "COP" } ,
          {"188", "CRC" } ,
          {"203", "CZK" } ,
          {"208", "DKK" } ,
          {"214", "DOP" } ,
          {"012", "DZD" } ,
          {"818", "EGP" } ,
          {"230", "ETB" } ,
          {"978", "EUR" } ,
          {"826", "GBP" } ,
          {"981", "GEL" } ,
          {"320", "GTQ" } ,
          {"344", "HKD" } ,
          {"340", "HNL" } ,
          {"191", "HRK" } ,
          {"348", "HUF" } ,
          {"360", "IDR" } ,
          {"376", "ILS" } ,
          {"356", "INR" } ,
          {"368", "IQD" } ,
          {"364", "IRR" } ,
          {"352", "ISK" } ,
          {"388", "JMD" } ,
          {"400", "JOD" } ,
          {"392", "JPY" } ,
          {"404", "KES" } ,
          {"417", "KGS" } ,
          {"116", "KHR" } ,
          {"410", "KRW" } ,
          {"414", "KWD" } ,
          {"398", "KZT" } ,
          {"418", "LAK" } ,
          {"422", "LBP" } ,
          {"144", "LKR" } ,
          {"440", "LTL" } ,
          {"428", "LVL" } ,  //why commented?
          {"434", "LYD" } ,
          {"504", "MAD" } ,
          {"807", "MKD" } ,
          {"496", "MNT" } ,
          {"446", "MOP" } ,
          {"462", "MVR" } ,
          {"484", "MXN" } ,
          {"458", "MYR" } ,
          {"558", "NIO" } ,
          {"578", "NOK" } ,
          {"524", "NPR" } ,
          {"554", "NZD" } ,
          {"512", "OMR" } ,
          {"590", "PAB" } ,
          {"604", "PEN" } ,
          {"608", "PHP" } ,
          {"586", "PKR" } ,
          {"985", "PLN" } ,
          {"600", "PYG" } ,
          {"634", "QAR" } ,
          {"946", "RON" } ,
          {"941", "RSD" } ,
          {"643", "RUB" } ,
          {"646", "RWF" } ,
          {"682", "SAR" } ,
          {"752", "SEK" } ,
          {"702", "SGD" } ,
          {"760", "SYP" } ,
          {"764", "THB" } ,
          {"972", "TJS" } ,
          {"788", "TND" } ,
          {"949", "TRY" } ,
          {"780", "TTD" } ,
          {"901", "TWD" } ,
          {"980", "UAH" } ,
          {"840", "USD" } ,
          {"858", "UYU" } ,
          {"860", "UZS" } ,
          {"937", "VEF" } ,
          {"704", "VND" } ,
          {"952", "XOF" } ,
          {"886", "YER" } ,
          {"710", "ZAR" }
        };
        #endregion

        public static string GetIsoField(string currencyCode)
        {
            string currSymbol = string.Empty;

            if (currencyCode != null && CodeMap.TryGetValue(currencyCode, out currSymbol))
                return currSymbol;
            else
                throw new ArgumentException("CurrencyCode not found", "CurrencyCode");
        }

        public string IsoField
        {
            get
            {
                string currSymbol = string.Empty;

                if (currencyCode != null && CodeMap.TryGetValue(currencyCode, out currSymbol))
                    return currSymbol;
                else
                    throw new ArgumentException("CurrencyCode not found", "CurrencyCode");
            }
        }

        public MoneyFormatInfo(string currencyCode, IFormatProvider provider = null)
        {
            this.Provider = provider ?? CultureInfo.CurrentCulture;
            this.currencyCode = currencyCode;
        }

        public MoneyFormatInfo(string currencyCode, bool useIsoCurrencyCode, IFormatProvider provider = null)
        {
            this.Provider = provider ?? CultureInfo.CurrentCulture;
            this.currencyCode = currencyCode;
            this.useIsoCurrencyCode = useIsoCurrencyCode;
        }

        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
            {
                return this;
            }

            NumberFormatInfo result = Provider.GetFormat(typeof(NumberFormatInfo)) as NumberFormatInfo;
            if (result == null)
            {
                //Follow the pattern seen in reflected code from the BCL.
                return null;
            }

            result = (NumberFormatInfo)result.Clone();

            //The number of decimal digits shown in a currency is related to its "strength" relative to the
            //dollar.  So for example:
            // * The Kuwait "Dinar" is worth $3.43 U.S. dollars, and thus the Dinar uses three decimal places.
            // * The Japanese "Yen" is worth $0.0085 U.S. dollars, and thus the Yen uses no decimal places at all.
            //To properly respect these currency valuations, it is necessary to use this command:
            result.CurrencyDecimalDigits = MetaMap[IsoField].Digits;

            if (useIsoCurrencyCode)
            {
                result.CurrencySymbol = IsoField;
                //Make sure there is a space between the amount and the ISO 3 digit symbol.
                result.CurrencyPositivePattern = result.CurrencyPositivePattern < 2 ? result.CurrencyPositivePattern + 2 : result.CurrencyPositivePattern;
            }
            else
            {
                result.CurrencySymbol = MetaMap[IsoField].Symbol;
            }

            return result;
        }

        private class Meta
        {
            public int Digits;
            public string Symbol;
        }


        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            var argType = arg.GetType();
            var moneyProvider = (MoneyFormatInfo)formatProvider;

            string result = null;

            format = String.IsNullOrWhiteSpace(format) ? "g" : format;

            if (argType == typeof(decimal))
            {
                //The "re-align" capability is designed to support a decimal type
                //working with either the 'C' or 'M' format strings.  However, we
                //are currently backing-away from using the 'M' format string with
                //the decimal type, and so the re-align call is commented.
                //formatProvider = RealignFormat(format, moneyProvider);
                format = format.ToLower() == "m" ? "c" : format;
                result = ((decimal)arg).ToString(format, formatProvider);
            }
            else if (argType == typeof(Money))
            {
                result = ((Money)arg).ToString(format, moneyProvider.Provider);
            }

            return result;
        }

        private IFormatProvider RealignFormat(string format, MoneyFormatInfo formatProvider)
        {
            if (!String.IsNullOrWhiteSpace(format))
            {
                if (format.ToLower() == "m" && !formatProvider.useIsoCurrencyCode)
                {
                    formatProvider = new MoneyFormatInfo(currencyCode, true, formatProvider.Provider);
                }
                else if (format.ToLower() == "c" && formatProvider.useIsoCurrencyCode)
                {
                    formatProvider = new MoneyFormatInfo(currencyCode, false, formatProvider.Provider);
                }
            }
            return formatProvider;
        }
    }

}
