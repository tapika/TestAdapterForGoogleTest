// This file has been modified by Microsoft on 9/2017.

using GoogleTestAdapter.Model;
using System;
using System.Text.RegularExpressions;

namespace GoogleTestAdapter.TestCases
{

    public class StreamingListTestsParser
    {
        private static readonly Regex SuiteRegex = new Regex($@"([\w\/]*(?:\.[\w\/]+)*)(?:{Regex.Escape(GoogleTestConstants.TypedTestMarker)}(.*))?", RegexOptions.Compiled);
        private static readonly Regex NameRegex = new Regex($@"([\w\/]*)(?:{Regex.Escape(GoogleTestConstants.ParameterizedTestMarker)}(.*))?", RegexOptions.Compiled);
        private static readonly Regex IsParamRegex = new Regex(@"(\w+/)?\w+/\d+", RegexOptions.Compiled);

        private readonly string _testNameSeparator;

        private string _currentSuite = "";

        public StreamingListTestsParser(string testNameSeparator)
        {
            _testNameSeparator = testNameSeparator;
        }

        public class TestCaseCreatedEventArgs : EventArgs
        {
            public TestCase TestCase { get; set; }
        }

        public event EventHandler<TestCaseCreatedEventArgs> TestCaseCreated;


        public void ReportLine(string line)
        {
            string trimmedLine = line.Trim('.', '\n', '\r');
            if (trimmedLine.StartsWith("  ", StringComparison.Ordinal))
            {
                TestCase descriptor = CreateTestCase(_currentSuite, trimmedLine.Substring(2));
                TestCaseCreated?.Invoke(this, new TestCaseCreatedEventArgs {TestCase = descriptor});
            }
            else
            {
                _currentSuite = trimmedLine;
            }
        }

        private TestCase CreateTestCase(string suiteLine, string testCaseLine)
        {
            Match suiteMatch = SuiteRegex.Match(suiteLine);
            string suite = suiteMatch.Groups[1].Value;
            string typeParam = suiteMatch.Groups[2].Value
                .Replace("class ", "")
                .Replace("struct ", "");

            Match nameMatch = NameRegex.Match(testCaseLine);
            string name = nameMatch.Groups[1].Value;
            string param = nameMatch.Groups[2].Value;

            string fullyQualifiedName = $"{suite}.{name}";

            string displayName = GetDisplayName(fullyQualifiedName, typeParam, param);
            if (!string.IsNullOrEmpty(_testNameSeparator))
                displayName = displayName.Replace("/", _testNameSeparator);

            TestCase.TestTypes testType = TestCase.TestTypes.Simple;
            if (IsParamRegex.IsMatch(suite))
                testType = TestCase.TestTypes.TypeParameterized;
            else if (IsParamRegex.IsMatch(name))
                testType = TestCase.TestTypes.Parameterized;

            return new TestCase(suite, fullyQualifiedName, null, name, displayName) { TestType = testType };
        }

        private static string GetDisplayName(string fullyQalifiedName, string typeParam, string param)
        {
            string displayName = fullyQalifiedName;
            if (!string.IsNullOrEmpty(typeParam))
            {
                displayName += GetEnclosedTypeParam(typeParam);
            }
            if (!string.IsNullOrEmpty(param))
            {
                displayName += $" [{param}]";
            }

            return displayName;
        }

        private static string GetEnclosedTypeParam(string typeParam)
        {
            if (typeParam.EndsWith(">", StringComparison.Ordinal))
            {
                typeParam += " ";
            }
            return $"<{typeParam}>";
        }

    }

}