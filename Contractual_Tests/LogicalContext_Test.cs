using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Contractual.DomainModel;

namespace Contractual_Tests
{
    public class TraceStack : LogicalContextStack<KeyValuePair<string, string>> { }

    [TestClass]
    public class LogicalContext_Test
    {


        [TestMethod]
        public void LogicalContext_PushPopOnOneThread_ValueAccessible()
        {
            var tracing = new TraceStack();
            var pair = new KeyValuePair<string, string>("TraceID", "1234");
            using (tracing.Push(pair))
            {
                var showMe = tracing.CurrentStack;
            }
        }
    }
}
