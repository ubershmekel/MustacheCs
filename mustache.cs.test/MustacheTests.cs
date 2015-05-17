using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

using RenderLambdaType = System.Func<string, System.Func<string, string>, string>;

namespace Mustache.Test
{
    [TestFixture]
    public class MustacheTests
    {
        private class SubObj
        {
            public string whatever = "life is 42";
            public string uppers()
            {
                return whatever.ToUpper();
            }
        }

        private class MyView
        {
            public bool person = false;
            public List<string> stooges = new List<string>();
            public string[] stoogesArray = {};
            public string title { get; set; }
            public string calc()
            {
                return (2 + 4).ToString();
            }
            public SubObj subber = new SubObj();
            public List<SubObj> subberList;
            public RenderLambdaType wrapped() {
                return (string text, Func<string, string> render) => "<b>" + render(text) + "</b>";
            }
            public int aNumber = 13;
        }

        [Test]
        public void Tutorial()
        {
            var view = new MyView()
            {
                title = "Joe",
            };

            var output = Mustache.render("{{title}} spends {{calc}}", view);
            Assert.AreEqual("Joe spends 6", output);
        }

        [Test]
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

        [Test]
        public void DotNotation()
        {
            var view = new MyView()
            {
                title = "Joe",
            };
            var output = Mustache.render("{{title}} believes {{subber.whatever}}", view);
            Assert.AreEqual("Joe believes life is 42", output);
        }

        [Test]
        public void Sections()
        {
            var view = new MyView()
            {
                title = "Joe",
            };
            var output = Mustache.render("Shown.{{#person}}Never shown!{{/person}}", view);
            Assert.AreEqual("Shown.", output);

            output = Mustache.render("Shown.{{#stooges}}Never shown!{{/stooges}}", view);
            Assert.AreEqual("Shown.", output);

            output = Mustache.render("Shown.{{#stoogesArray}}Never shown!{{/stoogesArray}}", view);
            Assert.AreEqual("Shown.", output);
        }

        [Test]
        public void NonEmptyLists()
        {
            var view = new MyView()
            {
                stooges = new List<string>{"Moe", "Larry", "Curly"},
            };
            var output = Mustache.render("Shown{{#stooges}} {{.}}{{/stooges}}", view);
            Assert.AreEqual("Shown Moe Larry Curly", output);

            var context = new MyView()
            {
                subberList = new List<SubObj> { new SubObj { whatever = "abc" }, new SubObj { whatever = "xyz" } }
            };
            
            output = Mustache.render("={{#subberList}}-{{whatever}}{{/subberList}}", context);
            Assert.AreEqual("=-abc-xyz", output);
            
            output = Mustache.render("={{#subberList}}-{{uppers}}{{/subberList}}", context);
            Assert.AreEqual("=-ABC-XYZ", output);
        }

        [Test]
        public void Lambdas()
        {
            var template = @"{{#wrapped}} {{title}} is awesome. {{/wrapped}}";
            var view = new MyView()
            {
                title = "Willy"
            };
            var output = Mustache.render(template, view);
            Assert.AreEqual("<b> Willy is awesome. </b>", output);
        }

        [Test]
        public void Numbers()
        {
            var view = new MyView();
            var output = Mustache.render("{{aNumber}} believes", view);
            Assert.AreEqual("13 believes", output);
        }
    }
}
