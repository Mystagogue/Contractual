using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.Generic;
using Contractual;

namespace Contractual_Tests
{
    public class Foo
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class MyQuerySpec : IQuerySpec<List<Foo>>
    {
        public string Weight { get; set; }
        public int Size { get; set; }
        public Sortation<MyQuerySpec> Sortation { get; set; }
    }

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var myQuery = new MyQuerySpec();
            //var foofy = Sortation<MyQuerySpec>.OrderBy(x => x.Size).ThenBy(x => x.Sortation);

            var woops = new Foo { Name = "hey" };
            myQuery.OrderBy(x => x.Size).ThenBy(x => x.Weight).ToList();

            //myQuery.OrderBy(f => f.Size).
            //var stuff = myQuery.OrderBy<MyQuerySpec,List<Foo>>(x => x.Weight);
            //var stuff = myQuery.OrderBy(x => x.Size).OrderBy()
            //List<Foo> foo = null;
            //var yes = foo.OrderBy(x => x.Name);

        }
    }
}
