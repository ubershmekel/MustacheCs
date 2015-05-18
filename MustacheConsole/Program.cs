using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MustacheCs;

namespace MustacheConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Mustache.render("asdf{{!ignoredcomment...}} me", null));
        }
    }
}
