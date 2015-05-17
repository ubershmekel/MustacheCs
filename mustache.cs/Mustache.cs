using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mustache.Properties;

namespace Mustache
{
    /// <summary>
    /// Parses a format string and returns a text generator.
    /// </summary>
    public sealed class Mustache
    {
        public class Token
        {
            public string type { get; set; }
            public string value { get; set; }
            public long start { get; set; }
            public long end { get; set; }
            public List<Token> subTokens { get; set; }
            public long endSection { get; set; }
        }

        public class Tags
        {
            public string opener { get; set; }
            public string closer { get; set; }
        }

        private static Regex whiteRe = new Regex(@"\s*");
        private static Regex spaceRe = new Regex(@"\s+");
        private static Regex equalsRe = new Regex(@"\s*=");
        private static Regex curlyRe = new Regex(@"\s*\}");
        private static Regex tagRe = new Regex(@"#|\^|\/|>|\{|&|=|!");


        public const string name = "mustache.cs";
        public const string version = "2.0.0";
        public static readonly Tags mustacheTags = new Tags{opener="{{", closer="}}"};
        
        /// <summary>
        /// Initializes a new instance of a Mustache.
        /// </summary>
        public Mustache()
        {
        }

        private static bool isArray(Object obj) {
            return obj is System.Collections.IList;
        }

        private static bool isEmptyArray(Object obj) {
            if (obj is ICollection<Object>)
                return ((ICollection<Object>)obj).Any();
            return false;
        }

        private static bool isFunction(Object obj){
            // TODO:
            return false;
        }

        private static string partialsInvoke(Object obj, string param) {
            // TODO
            return "Asdf";
        }


        /**
        * A simple string scanner that is used by the template parser to find
        * tokens in template strings.
        */
        private class Scanner {
            public string _text;
            public string _tail;
            public long _pos;

            public Scanner(string text) {
                _text = text;
                _tail = text;
                _pos = 0;
            }

            /**
            * Returns `true` if the tail is empty (end of string).
            */
            public bool eos() {
                return _tail.Count() == 0;
            }

            /**
            * Tries to match the given regular expression at the current position.
            * Returns the matched text if it can match, the empty string otherwise.
            */
            public string scan(Regex re) {
                var match = re.Match(_tail);
                if (!match.Success || match.Index != 0)
                    return "";

                string res = match.Groups[0].Value;

                _tail = _tail.Substring(res.Count());
                _pos += res.Count();

                return res;
            }

            /**
            * Skips all text until the given regular expression can be matched. Returns
            * the skipped string, which is the entire tail if no match can be made.
            */
            public string scanUntil(Regex re) {
                var match = re.Match(_tail);
                string res;

                if (!match.Success) {
                    res = _tail;
                    _tail = "";
                }
                else if(match.Index == 0) {
                    res = "";
                }
                else {
                    res = _tail.Substring(0, match.Index);
                    _tail = _tail.Substring(match.Index);
                }

                _pos += res.Count();

                return res;
            }
        }

        /*public static string render(string template, Object obj)
        {
            return "bla";
        }*/

        private static bool isWhitespace(string str)
        {
            return spaceRe.IsMatch(str);
        }



        /**
        * Breaks up the given `template` string into a tree of tokens. If the `tags`
        * argument is given here it must be an array with two string values: the
        * opening and closing tags used in the template (e.g. [ "<%", "%>" ]). Of
        * course, the default is to use mustaches (i.e. mustache.tags).
        *
        * A token is an array with at least 4 elements. The first element is the
        * mustache symbol that was used inside the tag, e.g. "#" or "&". If the tag
        * did not contain a symbol (i.e. {{myValue}}) this element is "name". For
        * all text that appears outside a symbol this element is "text".
        *
        * The second element of a token is its "value". For mustache tags this is
        * whatever else was inside the tag besides the opening symbol. For text tokens
        * this is the text itself.
        *
        * The third and fourth elements of the token are the start and end indices,
        * respectively, of the token in the original template.
        *
        * Tokens that are the root node of a subtree contain two more elements: 1) an
        * array of tokens in the subtree and 2) the index in the original template at
        * which the closing tag for that section begins.
        */
        private static List<Token> parseTemplate (string template, Tags tags=null) {
            if (template.Count() == 0)
                return new List<Token>();

            var sections = new Stack<Token>();     // Stack to hold section tokens
            var tokens = new List<Token>();       // Buffer to hold the tokens
            var spaces = new Stack<int>();       // Indices of whitespace tokens on the current line
            var hasTag = false;    // Is there a {{tag}} on the current line?
            var nonSpace = false;  // Is there a non-space char on the current line?

            // Strips all whitespace tokens array for the current line
            // if there was a {{#tag}} on it and otherwise only space.
            Action stripSpace = delegate() {
                if (hasTag && !nonSpace) {
                    while (spaces.Count() > 0)
                        tokens.RemoveAt(spaces.Pop());
                } else {
                    spaces.Clear();
                }

                hasTag = false;
                nonSpace = false;
            };        

            // TODO: this `= null` is to avoid "Use of unassigned local variable" C# compiler error.
            Regex openingTagRe = null;
            Regex closingTagRe = null;
            Regex closingCurlyRe = null;
            Action<Tags> compileTags = delegate(Tags tagsToCompile) {
                openingTagRe = new Regex(Regex.Escape(tagsToCompile.opener) + "\\s*");
                closingTagRe = new Regex("\\s*" + Regex.Escape(tagsToCompile.closer));
                closingCurlyRe = new Regex("\\s*" + Regex.Escape('}' + tagsToCompile.closer));
            };

            if (tags == null)
                compileTags(mustacheTags);
            else
                compileTags(tags);

            long start = 0;
            //var start, type, value, chr, token, openSection;
            var scanner = new Scanner(template);
            Token openSection = null;
            while (!scanner.eos()) {
                var value = scanner.scanUntil(openingTagRe);
                var valueLength = value.Count();
                if (valueLength > 0) {
                    for (var i = 0; i < valueLength; ++i) {
                        string chr = "" + value[i];

                        if (isWhitespace(chr)) {
                            spaces.Push(tokens.Count());
                        } else {
                            nonSpace = true;
                        }

                        tokens.Add(new Token{type="text", value=chr, start=start, end=start + 1});
                        start += 1;

                        // Check for whitespace on the current line.
                        if (chr == "\n")
                            stripSpace();
                     }
                }

                // Match the opening tag.
                if (scanner.scan(openingTagRe).Count() == 0)
                    break;

                hasTag = true;

                // Get the tag type.
                var scanTag = scanner.scan(tagRe);
                string type;
                if(scanTag.Count() == 0)
                    type =  "name";
                else
                    type = scanTag;
            
                scanner.scan(whiteRe);

                // Get the tag value.
                if (type == "=") {
                    value = scanner.scanUntil(equalsRe);
                    scanner.scan(equalsRe);
                    scanner.scanUntil(closingTagRe);
                } else if (type == "{") {
                    value = scanner.scanUntil(closingCurlyRe);
                    scanner.scan(curlyRe);
                    scanner.scanUntil(closingTagRe);
                    type = "&";
                } else {
                    value = scanner.scanUntil(closingTagRe);
                }

                // Match the closing tag.
                if (scanner.scan(closingTagRe).Count() == 0)
                    throw new Exception("Unclosed tag at " + scanner._pos);

                var token = new Token{type=type, value=value, start=start, end=scanner._pos};
                tokens.Add(token);
                if (type == "#" || type == "^") {
                   sections.Push(token);
                } else if (type == "/") {
                    // Check section nesting.
                    openSection = sections.Pop();

                    if (openSection == null)
                        throw new Exception("Unopened section \"" + value + "\" at " + start);

                    if (openSection.value != value)
                        throw new Exception("Unclosed section \"" + openSection.value + "\" at " + start);
                } else if (type == "name" || type == "{" || type == "&") {
                    nonSpace = true;
                } else if (type == "=") {
                    // Set the tags for the next time around.
                    var newTags = spaceRe.Split(value, 2);
                    compileTags(new Tags{opener=newTags[0], closer=newTags[1]});
                }
            }

            // Make sure there are no open sections when we're done.
            if (sections.Count() > 0) {
                openSection = sections.Pop();
                throw new Exception("Unclosed section \"" + openSection.value + "\" at " + scanner._pos);
            }

            return nestTokens(squashTokens(tokens));
        }

        /**
        * Combines the values of consecutive text tokens in the given `tokens` array
        * to a single token.
        */
        public static List<Token> squashTokens(List<Token> tokens) {
            var squashedTokens = new List<Token>();

            //var token, lastToken;
            var numTokens = tokens.Count();
            Token lastToken = null;
            for (var i = 0; i < numTokens; ++i) {
                var token = tokens[i];

                if (token.type == "text" && lastToken != null && lastToken.type == "text") {
                    lastToken.value += token.value;
                    lastToken.end = token.end;
                } else {
                    squashedTokens.Add(token);
                    lastToken = token;
                }
            }

            return squashedTokens;
        }

        /**
        * Forms the given array of `tokens` into a nested tree structure where
        * tokens that represent a section have two additional items: 1) an array of
        * all tokens that appear in that section and 2) the index in the original
        * template that represents the end of that section.
        */
        public static List<Token> nestTokens (List<Token> tokens) {
            List<Token> nestedTokens = new List<Token>();
            var collector = nestedTokens;
            var sections = new Stack<Token>();

            Token section;
            var numTokens = tokens.Count();
            for (var i = 0; i < numTokens; ++i) {
                var token = tokens[i];

                switch (token.type) {
                case "#":
                case "^":
                    collector.Add(token);
                    sections.Push(token);
                    token.subTokens = new List<Token>();
                    collector = token.subTokens;
                break;
                case "/":
                    section = sections.Pop();
                    section.endSection = token.start;
                    if (sections.Count() > 0)
                        collector = sections.Peek().subTokens;
                    else
                        collector = nestedTokens;
                break;
                default:
                    collector.Add(token);
                break;
                }
            }

            return nestedTokens;
        }





        public static object GetGenericValue(object obj, string keyName) {
            // obj.mykey()
            var type = obj.GetType();
            var method = type.GetMethod(keyName);
            if (method != null)
                return method.Invoke(obj, null);

            // obj.mykey (getter)
            var prop = type.GetProperty(keyName);
            if (prop != null)
                return prop.GetValue(obj, null);

            // obj.mykey (member)
            var field = type.GetField(keyName);
            if (field != null)
                return field.GetValue(obj);

            // dict["mykey"]
            //var dict = obj as IDictionary<string, Object>;
            //if (dict != null)
            //    return dict.Get[keyName];
            method = type.GetMethod("ContainsKey");
            if (method != null) {
                object[] parametersArray = new object[] { keyName };
                var containsKey = (bool)method.Invoke(obj, parametersArray);
                if (containsKey) {
                    method = type.GetMethod("get_Item");
                    if (method != null)
                    {
                        return method.Invoke(obj, parametersArray);
                    }
                }
            }
            return null;
        }


        /**
        * Represents a rendering context by wrapping a view object and
        * maintaining a reference to the parent context.
        */
        private class Context{
            Object _view;
            Dictionary<string, Object> _cache;
            Context _parent;

            public Context(Object view, Context parentContext=null) {
                _view = view;
                _cache = new Dictionary<string, Object>{ {".", _view} };
                _parent = parentContext;
            }


            /**
            * Creates a new context using the given view with this context
            * as the parent.
            */
            public Context push(Object view) {
                return new Context(view, this);
            }

            /**
            * Returns the value of the given name in this context, traversing
            * up the context hierarchy if the value is absent in this context's view.
            */
            public Object lookup(string name) {
                Object value = null;
                if (_cache.ContainsKey(name)) {
                    value = _cache[name];
                } else {
                    var context = this;
                    //, names, index
                    string[] names;
                    var lookupHit = false;

                    while (context != null) {
                        if (name.Contains('.')) {
                            value = context._view;
                            names = name.Split('.');
                            var index = 0;

                            /**
                            * Using the dot notion path in `name`, we descend through the
                            * nested objects.
                            *
                            * To be certain that the lookup has been successful, we have to
                            * check if the last object in the path actually has the property
                            * we are looking for. We store the result in `lookupHit`.
                            *
                            * This is specially necessary for when the value has been set to
                            * `undefined` and we want to avoid looking up parent contexts.
                            **/
                            while (value != null && index < names.Count()) {
                                /*var type = value.GetType();
                                if(type.IsGenericType ) {
                                    var key = type.GetProperty("Key");
                                    var val = type.GetProperty("Value");
                                    var keyObj = key.GetValue(value, null);
                                    valueObj = val.GetValue(value, null);
                                } else {
                                    throw new Exception("Not lookupable object: " + value);
                                }*/
                                value = GetGenericValue(value, names[index]);
                                if (index == names.Count() - 1 && value != null) {
                                    // after the last dot
                                    //if (valueObj != null) {
                                    //if(value != null)
                                    lookupHit = true;
                                    //}
                                }
                                //value = value[names[index]];
                                //value = valueObj;
                                index++;
                            }
                        } else if (context._view != null) {
                            value = GetGenericValue(context._view, name);
                            //lookupHit = context.view.hasOwnProperty(name);
                            lookupHit = value != null;
                        }

                        if (lookupHit)
                            break;

                        context = context._parent;
                    }

                    _cache[name] = value;
                }

                //if (isFunction(value))
                // TODO
                //if(value.GetType().ToString() == "invoke")
                //    value = "";//Invoke(value.call(this.view);

                return value;
            }
        }

        /**
        * A Writer knows how to take a stream of tokens and render them to a
        * string, given a context. It also maintains a cache of templates to
        * avoid the need to parse the same template twice.
        */
        public class Writer {
            Dictionary<string, List<Token>> _cache = new Dictionary<string, List<Token>>();

            /**
            * Clears all cached templates in this writer.
            */
            public void clearCache () {
                _cache.Clear();
            }

            /**
            * Parses and caches the given `template` and returns the array of tokens
            * that is generated from the parse.
            */
            public List<Token> parse (string template, Tags tags=null) {
                if (_cache.ContainsKey(template)) {
                    return _cache[template];
                } else {
                    var tokens = parseTemplate(template, tags);
                    _cache[template] = tokens;
                    return tokens;
                }
            }

            /**
            * High-level method that is used to render the given `template` with
            * the given `view`.
            *
            * The optional `partials` argument may be an object that contains the
            * names and templates of partials that are used in the template. It may
            * also be a function that is used to load partial templates on the fly
            * that takes a single argument: the name of the partial.
            */
            public string render(string template, Object view=null, Object partials=null) {
                var tokens = parse(template);
                Context context;
                //view instanceof Context
                // TODO
                if(view is Context)
                    context = (Context)view;
                else
                    context = new Context(view);
                return renderTokens(tokens, context, partials, template);
            }

            /**
            * Low-level method that renders the given array of `tokens` using
            * the given `context` and `partials`.
            *
            * Note: The `originalTemplate` is only ever used to extract the portion
            * of the original template that was contained in a higher-order section.
            * If the template doesn't use higher-order sections, this argument may
            * be omitted.
            */
            string renderTokens (List<Token> tokens, Context context, Object partials, string originalTemplate) {
                var buffer = "";

                Token token;
                string symbol;
                object value;
                var numTokens = tokens.Count();
                for (var i = 0; i < numTokens; ++i) {
                    value = null;
                    token = tokens[i];
                    symbol = token.type;

                    if (symbol == "#") value = renderSection(token, context, partials, originalTemplate);
                    else if (symbol == "^") value = renderInverted(token, context, partials, originalTemplate);
                    // JS VERSION BUG - else if (symbol == ">") value = renderPartial(token, context, partials, originalTemplate);
                    else if (symbol == ">") value = renderPartial(token, context, partials);
                    else if (symbol == "&") value = unescapedValue(token, context);
                    else if (symbol == "name") value = escapedValue(token, context);
                    else if (symbol == "text") value = rawValue(token);

                    if (value != null)
                        buffer += value;
                }

                return buffer;
            }

            string renderSection(Token token, Context context, Object partials, string originalTemplate) {
                var buffer = "";
                var value = context.lookup(token.value);

                // This function is used to render an arbitrary template
                // in the current context by higher-order sections.
                Func<string, string> subRender = delegate(string template) {
                    return render(template, context, partials);
                };

                if (value == null || value is bool && !(bool)value)
                    return ""; // this would have returned null/undefined instead of a string in JS version?

                //if (isArray(value)) {
                if(isArray(value)) {
                    foreach (var item in (System.Collections.IList)value) {
                        buffer += this.renderTokens(token.subTokens, context.push(item), partials, originalTemplate);
                    }
                //} else if (typeof value === 'object' || typeof value === 'string' || typeof value === 'number') {
                //    buffer += this.renderTokens(token[4], context.push(value), partials, originalTemplate);
                /* TODO
                 * } else if (isFunction(value)) {
                    if (typeof originalTemplate !== 'string')
                    throw new Error('Cannot use higher-order sections without the original template');

                    // Extract the portion of the original template that the section contains.
                    value = value.call(context.view, originalTemplate.slice(token[3], token[5]), subRender);

                    if (value != null)
                    buffer += value;
                 */
                } else {
                    buffer += this.renderTokens(token.subTokens, context, partials, originalTemplate);
                }
                return buffer;
            }

            string renderInverted (Token token, Context context, Object partials, string originalTemplate) {
                var value = context.lookup(token.value);

                // Use JavaScript's definition of falsy. Include empty arrays.
                // See https://github.com/janl/mustache.js/issues/186
                //if (!value || (isArray(value) && value.length === 0))
                if (value == null || isEmptyArray(value))
                    return this.renderTokens(token.subTokens, context, partials, originalTemplate);
                
                return null; // TODO?
            }

            string renderPartial (Token token, Context context, Object partials) {
                if (partials == null)
                    return null;

                var value = isFunction(partials) ? partialsInvoke(partials, token.value) : GetGenericValue(partials, token.value);
                if (value != null)
                    // TODO WHICH `parse`??
                    return renderTokens(parse((string)value), context, partials, (string)value);
                return null;
            }

            string unescapedValue (Token token, Context context) {
                var value = context.lookup(token.value);
                if (value != null)
                    return (string)value;
                return null;
            }

            string escapedValue (Token token, Context context) {
                var value = context.lookup(token.value);
                if (value != null)
                    return System.Security.SecurityElement.Escape((string)value);
                return null;
            }

            string rawValue (Token token) {
                return token.value;
            }

        }

        // All high-level mustache.* functions use this writer.
        static Writer defaultWriter = new Writer();

        /**
        * Clears all cached templates in the default writer.
        */
        void clearCache () {
            defaultWriter.clearCache();
        }


        /**
        * Parses and caches the given template in the default writer and returns the
        * array of tokens it contains. Doing this ahead of time avoids the need to
        * parse templates on the fly as they are rendered.
        */
        public List<Token> parse (string template, Tags tags) {
            return defaultWriter.parse(template, tags);
        }

        /**
        * Renders the `template` with the given `view` and `partials` using the
        * default writer.
        */
        public static string render (string template, Object view, Object partials=null) {
            return defaultWriter.render(template, view, partials);
        }

        // This is here for backwards compatibility with 0.4.x.,
        /*eslint-disable */ // eslint wants camel cased function name
        string to_html (string template, Object view, Object partials, Object send) {
            /*eslint-enable*/

            var result = render(template, view, partials);

            if (isFunction(send)) {
                //send(result);
                // TODO
                partialsInvoke(send, result);                
            } else {
                return result;
            }

            return null;
        }

        // Export the escaping function so that the user may override it.
        // See https://github.com/janl/mustache.js/issues/244
        // TODO
        //public Func<string, string> escape = System.Security.SecurityElement.Escape;

    }
}
