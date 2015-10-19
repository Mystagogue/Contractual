using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serve.Shared.Globalization;
using System.Globalization;
//using System.ServiceModel;
//using System.ServiceModel.Description;
using Serve.Shared.Globalization.Test;
using System.Runtime.Serialization;
using System.Threading;
using Contractual.DomainModel;

namespace Serve.Shared.Globalization.Test
{

    [TestClass]
    public class CurrencyFormatterTest
    {
        [TestMethod]
        [TestCategory("UnitTest")]
        public void CurrencyFormatting_MixAndMatchDecimalAndMoney_NoExceptions()
        {
            Money dough = new Money(8124.348m, "978");
            decimal cash = 3124.728m;

            string result;

            //Simple EURO currency parameters, with basic U.S. formatting
            result = cash.ToString("C", new MoneyFormatInfo("978"));
            Assert.AreEqual(result, "€3,124.73");

            //ILLEGAL.  The exception will let you know.  Only "Money" type can take 'M' format string.
            //result = cash.ToString("M", new MoneyFormatInfo("978"));  //EXCEPTION WILL BE THROWN!
            //Assert.AreEqual(result, "EUR 3,124.73");

            //Construct money formatter to default to 3 digit code.
            //This is the correct way to make a decimal type format with a 3-digit currency code.
            result = cash.ToString("C", new MoneyFormatInfo("978", true));
            Assert.AreEqual(result, "EUR 3,124.73");

            //Ambient culture also affects the outcome.
            var temp = Thread.CurrentThread.CurrentCulture;
            //Swiss francs...
            var Switzerland = new CultureInfo("fr-CH");
            Thread.CurrentThread.CurrentCulture = Switzerland;

            result = cash.ToString("C");
            Assert.AreEqual(result, "fr. 3'124.73");
            result = dough.ToString("M");
            Assert.AreEqual(result, "EUR 8'124.35");
            result = String.Format("the cash: {0:C} and the money: {1:M}", cash, dough);
            Assert.AreEqual(result, "the cash: fr. 3'124.73 and the money: EUR 8'124.35");
            Thread.CurrentThread.CurrentCulture = temp;

            result = String.Format(new MoneyFormatInfo("978"), "the cash: {0:C} and the money: {1:M}", cash, dough);
            Assert.AreEqual(result, "the cash: €3,124.73 and the money: EUR 8,124.35");

            result = String.Format(new MoneyFormatInfo("978", true), "the cash: {0:C} and the money: {1:C}", cash, dough);
            Assert.AreEqual(result, "the cash: EUR 3,124.73 and the money: €8,124.35");


            //JAPANESE and EURO currencies intermingled, both with basic U.S. formatting.
            result = String.Format(new MoneyFormatInfo("392"), "the cash: {0:C} and the money: {1:M}", cash, dough);
            Assert.AreEqual(result, "the cash: ¥3,125 and the money: EUR 8,124.35");

            result = String.Format(new MoneyFormatInfo("392", true), "the cash: {0:C} and the money: {1:C}", cash, dough);
            Assert.AreEqual(result, "the cash: JPY 3,125 and the money: €8,124.35");

            //EURO currency parameters, with basic French formatting
            var french = new CultureInfo("fr-FR");
            result = String.Format(french, "the money: {0:M}", dough);
            Assert.AreEqual(result, "the money: 8 124,35 EUR");

            //JAPANESE YEN, with basic French formatting.
            var frenchmen = new MoneyFormatInfo("392", new CultureInfo("fr-FR"));
            result = String.Format(frenchmen, "the cash: {0:C}", cash);
            Assert.AreEqual(result, "the cash: 3 125 ¥");

            //No currency formatting
            result = String.Format("numeric cash: {0} and numeric Money: {1}", cash, dough);
            Assert.AreEqual(result, "numeric cash: 3124.728 and numeric Money: 8124.348");

            result = String.Format(new MoneyFormatInfo("978"), "numeric cash: {0} and numeric Money: {1}", cash, dough);
            Assert.AreEqual(result, "numeric cash: 3124.728 and numeric Money: 8124.348");
        }


        //A major section of these tests are now offline (see region below) but are not deleted
        //because future development likely will need to reference the unit tests as part
        //of evaluating design decisions.  It is too inaccessible to presume a developer
        //will merely go through TFS to the correct change set to retrieve these.

        #region Support 'M' format string for decimal type
        //[TestMethod]
        //public void CurrencyFormat_NoFormatting_Test_ExpectSuccess()
        //{

        //    Money dough = new Money(8124.348m, "978");

        //    decimal cash = 3124.728m;

        //    string result;
        //    //No currency formatting
        //    result = String.Format("numeric cash: {0} and numeric Money: {1}", cash, dough);
        //    Console.WriteLine(result);
        //    Assert.AreEqual(result, "numeric cash: 3124.728 and numeric Money: 8124.348");

        //}

        //[TestMethod]
        //public void CurrencyFormat_WithCustomFormatter_NoFormatting_Test_ExpectSuccess()
        //{
        //    Money dough = new Money(8124.348m, "978");
        //    decimal cash = 3124.728m;

        //    string result;
        //    //No currency formatting

        //    result = String.Format(new MoneyFormatInfo("978"), "numeric cash: {0} and numeric Money: {1}", cash, dough);
        //    Console.WriteLine(result);
        //    Assert.AreEqual(result, "numeric cash: 3124.728 and numeric Money: 8124.348");

        //}

        //[TestMethod]
        //public void CurrencyFormat_WithCustomFormatter_EURO_WithUSFormatt_Test_ExpectSuccess()
        //{
        //    Money dough = new Money(8124.348m, "978");
        //    decimal cash = 3124.728m;

        //    string result;

        //    //Simple EURO currency parameters, with basic U.S. formatting
        //    result = cash.ToString("C", new MoneyFormatInfo("978"));
        //    Console.WriteLine(result);
        //    Assert.AreEqual(result, "€3,124.73");

        //    result = String.Format(new MoneyFormatInfo("978"), "the cash: {0:C} and the money: {1:M}", cash, dough);
        //    Console.WriteLine(result);
        //    Assert.AreEqual(result, "the cash: €3,124.73 and the money: EUR 8,124.35");

        //    result = String.Format(new MoneyFormatInfo("978"), "the cash: {0:M} and the money: {1:C}", cash, dough);
        //    Console.WriteLine(result);
        //    Assert.AreEqual(result, "the cash: EUR 3,124.73 and the money: €8,124.35");

        //}

        //[TestMethod]
        //public void CurrencyFormat_WithCustomFormatter_Format_Intermingled_Test_ExpectSuccess()
        //{

        //    Money dough = new Money(8124.348m, "978");
        //    decimal cash = 3124.728m;

        //    string result;

        //    //JAPANESE and EURO currencies intermingled, both with basic U.S. formatting.
        //    result = String.Format(new MoneyFormatInfo("392"), "the cash: {0:C} and the money: {1:M}", cash, dough);
        //    Console.WriteLine(result);
        //    Assert.AreEqual(result, "the cash: ¥3,124.73 and the money: EUR 8,124.35");

        //    result = String.Format(new MoneyFormatInfo("392"), "the cash: {0:M} and the money: {1:C}", cash, dough);
        //    Console.WriteLine(result);
        //    Assert.AreEqual(result, "the cash: JPY 3,124.73 and the money: €8,124.35");
        //}

        //[TestMethod]
        //public void CurrencyFormat_WithCustomFormatter_CustomCulture_Test_ExpectSuccess()
        //{
        //    Money dough = new Money(8124.348m, "978");
        //    decimal cash = 3124.728m;

        //    string result;

        //    //EURO currency parameters, with basic French formatting
        //    var french = new CultureInfo("fr-FR");
        //    result = String.Format(french, "the money: {0:M}", dough);
        //    Console.WriteLine(result);
        //    Assert.AreEqual(result, "the money: 8 124,35 EUR");

        //    //JAPANESE YEN, with basic French formatting.
        //    var frenchmen = new MoneyFormatInfo("392", new CultureInfo("fr-FR"));
        //    result = String.Format(frenchmen, "the cash: {0:M}", cash);
        //    Console.WriteLine(result);
        //    Assert.AreEqual(result, "the cash: 3 124,73 JPY");

        //    result = dough.ToString("c", frenchmen);
        //    Assert.AreEqual(result, "8 124,35 €");

        //    //result = cash.ToString("m", frenchmen);
        //    //Assert.AreEqual(result, "3 124,73 JPY");


        //}

        //[TestMethod]
        //public void TestingSomething()
        //{
        //    Money dough = new Money(8124.348m, "978");
        //    decimal cash = 3124.728m;
        //    IFormatProvider foo;

        //    string result;

        //    //Simple EURO currency parameters, with basic U.S. formatting
        //    result = cash.ToString("C", new MoneyFormatInfo("978"));
        //    Assert.AreEqual(result, "€3,124.73");

        //    result = cash.ToString("M", new MoneyFormatInfo("978"));
        //    Assert.AreEqual(result, "EUR 3,124.73");

        //    result = String.Format(new MoneyFormatInfo("978"), "the cash: {0:C} and the money: {1:M}", cash, dough);
        //    Assert.AreEqual(result, "the cash: €3,124.73 and the money: EUR 8,124.35");

        //    result = String.Format(new MoneyFormatInfo("978"), "the cash: {0:M} and the money: {1:C}", cash, dough);
        //    Assert.AreEqual(result, "the cash: EUR 3,124.73 and the money: €8,124.35");


        //    //JAPANESE and EURO currencies intermingled, both with basic U.S. formatting.
        //    result = String.Format(new MoneyFormatInfo("392"), "the cash: {0:C} and the money: {1:M}", cash, dough);
        //    Assert.AreEqual(result, "the cash: ¥3,125 and the money: EUR 8,124.35");

        //    result = String.Format(new MoneyFormatInfo("392"), "the cash: {0:M} and the money: {1:C}", cash, dough);
        //    Assert.AreEqual(result, "the cash: JPY 3,125 and the money: €8,124.35");

        //    //EURO currency parameters, with basic French formatting
        //    var french = new CultureInfo("fr-FR");
        //    result = String.Format(french, "the money: {0:M}", dough);
        //    Assert.AreEqual(result, "the money: 8 124,35 EUR");

        //    //JAPANESE YEN, with basic French formatting.
        //    var frenchmen = new MoneyFormatInfo("392", new CultureInfo("fr-FR"));
        //    result = String.Format(frenchmen, "the cash: {0:M}", cash);
        //    Assert.AreEqual(result, "the cash: 3 125 JPY");

        //    //No currency formatting
        //    result = String.Format("numeric cash: {0} and numeric Money: {1}", cash, dough);
        //    Assert.AreEqual(result, "numeric cash: 3124.728 and numeric Money: 8124.348");

        //    result = String.Format(new MoneyFormatInfo("978"), "numeric cash: {0} and numeric Money: {1}", cash, dough);
        //    Assert.AreEqual(result, "numeric cash: 3124.728 and numeric Money: 8124.348");
        //}
        #endregion


        [TestMethod]
        [TestCategory("UnitTest")]
        public void CurrencyFormatting_ThrowException_WhenKeyNotFound()
        {
            try
            {
                Money dough = new Money(8124.348m, "1");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("CurrencyCode not found\r\nParameter name: CurrencyCode", ex.Message);
            }
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void CurrencyFormatting_ThrowException_WhenEmptyKey()
        {
            try
            {
                Money dough = new Money(8124.348m, "");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("CurrencyCode not found\r\nParameter name: CurrencyCode", ex.Message);
            }
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void CurrencyFormatting_ThrowException_WhenNullKey()
        {
            try
            {
                Money dough = new Money(8124.348m, null);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("CurrencyCode not found\r\nParameter name: CurrencyCode", ex.Message);
            }
        }
    }
}
