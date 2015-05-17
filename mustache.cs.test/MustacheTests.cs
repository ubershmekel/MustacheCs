using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mustache.Test
{
    [TestClass]
    public class MustacheTests
    {
        private class View
        {
            public string title { get; set; }
            public string calc()
            {
                return (2 + 4).ToString();
            }
        }
        
        [TestMethod]
        public void ShouldReturnNullForNull()
        {
            var view = new View()
            {
                title = "Joe",
            };

            var output = Mustache.render("{{title}} spends {{calc}}", view);
            Assert.AreEqual("Joe spends 6", output);
        }
    }
}
