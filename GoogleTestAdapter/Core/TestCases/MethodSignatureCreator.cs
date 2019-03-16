// This file has been modified by Microsoft on 8/2017.

using System;
using System.Collections.Generic;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestCases
{

    internal class MethodSignatureCreator
    {

        internal IEnumerable<string> GetTestMethodSignatures(TestCase testcase)
        {
            switch (testcase.TestType)
            {
                case TestCase.TestTypes.TypeParameterized:
                    return GetTypedTestMethodSignatures(testcase);
                case TestCase.TestTypes.Parameterized:
                    return GetParameterizedTestMethodSignature(testcase).Yield();
                case TestCase.TestTypes.Simple:
                    return GetTestMethodSignature(testcase.Suite, testcase.Name).Yield();
                default:
                    throw new InvalidOperationException(String.Format(Resources.UnknownLiteral, testcase.TestType));
            }
        }

        private IEnumerable<string> GetTypedTestMethodSignatures(TestCase testCase)
        {
            var result = new List<string>();

            // remove instance number
            string suite = testCase.Suite.Substring(0, testCase.Suite.LastIndexOf("/", StringComparison.Ordinal));

            // remove prefix
            if (suite.Contains("/"))
            {
                int index = suite.IndexOf("/", StringComparison.Ordinal);
                suite = suite.Substring(index + 1, suite.Length - index - 1);
            }

            string typeParam = "<.+>";

            // <testcase name>_<test name>_Test<type param value>::TestBody
            result.Add(GetTestMethodSignature(suite, testCase.Name, typeParam));

            // gtest_case_<testcase name>_::<test name><type param value>::TestBody
            string signature =
                $"gtest_case_{suite}_::{testCase.Name}{typeParam}{GoogleTestConstants.TestBodySignature}";
            result.Add(signature);

            return result;
        }

        private string GetParameterizedTestMethodSignature(TestCase testcase)
        {
            // remove instance number
            int index = testcase.Suite.IndexOf('/');
            string suite = index < 0 ? testcase.Suite : testcase.Suite.Substring(index + 1);

            index = testcase.Name.IndexOf('/');
            string testName = index < 0 ? testcase.Name : testcase.Name.Substring(0, index);

            return GetTestMethodSignature(suite, testName);
        }

        private string GetTestMethodSignature(string suite, string testCase, string typeParam = "")
        {
            return suite + "_" + testCase + "_Test" + typeParam + GoogleTestConstants.TestBodySignature;
        }

    }

}