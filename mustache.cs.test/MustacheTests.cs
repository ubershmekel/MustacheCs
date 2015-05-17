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
        private class SubObj
        {
            public string whatever = "life is 42";
        }

        private class MyView
        {
            public bool person = false;
            public string title { get; set; }
            public string calc()
            {
                return (2 + 4).ToString();
            }
            public SubObj subber = new SubObj();
        }

        [TestMethod]
        public void Tutorial()
        {
            var view = new MyView()
            {
                title = "Joe",
            };

            var output = Mustache.render("{{title}} spends {{calc}}", view);
            Assert.AreEqual("Joe spends 6", output);
        }

        [TestMethod]
        public void Unescape()
        {
            var expected = @"* Chris
* 
* &lt;b&gt;GitHub&lt;/b&gt;
* <b>GitHub</b>
* <b>GitHub</b>";
            var template = @"* {{name}}
* {{age}}
* {{company}}
* {{{company}}}
* {{&company}}";
            var view = new Dictionary<string, string> {
                {"name", "Chris"},
                {"company", "<b>GitHub</b>"}
            };

            var output = Mustache.render(template, view);
            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void DotNotation()
        {
            var view = new MyView()
            {
                title = "Joe",
            };
            var output = Mustache.render("{{title}} believes {{subber.whatever}}", view);
            Assert.AreEqual("Joe believes life is 42", output);
        }

        [TestMethod]
        public void Sections()
        {
            var view = new MyView()
            {
                title = "Joe",
            };
            var output = Mustache.render("Shown.{{#person}}Never shown!{{/person}}", view);
            Assert.AreEqual("Shown.", output);
        }
    }
}
