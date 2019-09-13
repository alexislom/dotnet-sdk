using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Kinvey.Tests
{
    public static class AssertExtensions
    {
        public static void StringEquals(this Assert assert, string expected, string actual)
        {
            if (expected == actual)
                return;

            throw new AssertFailedException(GetMessage(expected, actual));
        }

        private static string GetMessage(string expected, string actual)
        {
            var expectedFormat = ReplaceInvisibleCharacters(expected);
            var actualFormat = ReplaceInvisibleCharacters(actual);

            // Get the index of the first different character
            var index = expectedFormat.Zip(actualFormat, (c1, c2) => c1 == c2).TakeWhile(b => b).Count();
            var caret = new string(' ', index) + "^";
            // Do not use tabs to display error message correctly
            return $@"Strings are different.
Expect: <{expectedFormat}>
Actual: <{actualFormat}>
         {caret}";
        }

        private static string ReplaceInvisibleCharacters(string value)
        {
            return value
                .Replace(' ', '·')
                .Replace('\t', '→')
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}