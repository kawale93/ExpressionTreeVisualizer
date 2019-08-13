﻿using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;
using static ExpressionToString.FormatterNames;
using Pather.CSharp;
using ExpressionToString.Util;
using System;
using static ExpressionToString.Tests.Functions;
using static System.Environment;

namespace ExpressionToString.Tests {
    public static class Runner {
        // TODO How can we make this happen automatically, once the assembly is loaded?
        static Runner() => RegisterTestObjectContainer(typeof(Objects.VBCompiler));

        [Obsolete]
        public static void RunTest(object o, string csharp, string vb, string factoryMethods) {
            string testCSharpCode = "";
            Dictionary<string, (int start, int length)> csharpPathSpans = null;
            string testVBCode = "";
            Dictionary<string, (int start, int length)> vbPathSpans = null;
            string testFactoryMethods = "";
            Dictionary<string, (int start, int length)> factoryMethodsPathSpans = null;

            switch (o) {
                case Expression expr:
                    testCSharpCode = expr.ToString(CSharp, out csharpPathSpans);
                    testVBCode = expr.ToString(VisualBasic, out vbPathSpans);
                    testFactoryMethods = expr.ToString(FactoryMethods, out factoryMethodsPathSpans);
                    break;
                case MemberBinding mbind:
                    testCSharpCode = mbind.ToString(CSharp, out csharpPathSpans);
                    testVBCode = mbind.ToString(VisualBasic, out vbPathSpans);
                    testFactoryMethods = mbind.ToString(FactoryMethods, out factoryMethodsPathSpans);
                    break;
                case ElementInit init:
                    testCSharpCode = init.ToString(CSharp, out csharpPathSpans);
                    testVBCode = init.ToString(VisualBasic, out vbPathSpans);
                    testFactoryMethods = init.ToString(FactoryMethods, out factoryMethodsPathSpans);
                    break;
                case SwitchCase switchCase:
                    testCSharpCode = switchCase.ToString(CSharp, out csharpPathSpans);
                    testVBCode = switchCase.ToString(VisualBasic, out vbPathSpans);
                    testFactoryMethods = switchCase.ToString(FactoryMethods, out factoryMethodsPathSpans);
                    break;
                case CatchBlock catchBlock:
                    testCSharpCode = catchBlock.ToString(CSharp, out csharpPathSpans);
                    testVBCode = catchBlock.ToString(VisualBasic, out vbPathSpans);
                    testFactoryMethods = catchBlock.ToString(FactoryMethods, out factoryMethodsPathSpans);
                    break;
                case LabelTarget labelTarget:
                    testCSharpCode = labelTarget.ToString(CSharp, out csharpPathSpans);
                    testVBCode = labelTarget.ToString(VisualBasic, out vbPathSpans);
                    testFactoryMethods = labelTarget.ToString(FactoryMethods, out factoryMethodsPathSpans);
                    break;
            }

            // check that the string results are equivalent, for both C# and VB code
            Assert.Equal(csharp, testCSharpCode);
            Assert.Equal(vb, testVBCode);
            factoryMethods = FactoryMethodsFormatter.CSharpUsing + NewLine + NewLine + factoryMethods;
            Assert.Equal(factoryMethods, testFactoryMethods);

            // using paths from factory methods formatter as a reference; other formatters may skip paths or introduce new onee
            var paths = factoryMethodsPathSpans.Keys.ToHashSet();

            // check that all the reference paths can resolve against the original object
            var resolver = new Resolver();
            Assert.All(paths, path => Assert.NotNull(resolver.Resolve(o, path)));

            // path segments for introduced paths (i.e. paths that don't actually exist on the object) follow a specific pattern - $"{name}_{index}"
            // remove all such variations from the distinct paths
            // TODO this should probably be a bit more robust, using regular expressions
            var csharpPaths = csharpPathSpans.Keys.Select(x => x.Replace("_0", "")).ToHashSet();
            var vbPaths = vbPathSpans.Keys.Select(x => x.Replace("_0", "")).ToHashSet();
            Assert.True(paths.IsSupersetOf(csharpPaths));
            Assert.True(paths.IsSupersetOf(vbPaths));
        }

        public static readonly string[] Formatters = new[] { CSharp, VisualBasic, FactoryMethods, ObjectNotation };

        public static void RunTest(object o, string objectName, ExpectedDataFixture allExpected) {
            var actual = Formatters.Select(formatter => {
                string singleResult;
                Dictionary<string, (int start, int length)> pathSpans;
                switch (o) {
                    case Expression expr:
                        singleResult = expr.ToString(formatter, out pathSpans);
                        break;
                    case MemberBinding mbind:
                        singleResult = mbind.ToString(formatter, out pathSpans);
                        break;
                    case ElementInit init:
                        singleResult = init.ToString(formatter, out pathSpans);
                        break;
                    case SwitchCase switchCase:
                        singleResult = switchCase.ToString(formatter, out pathSpans);
                        break;
                    case CatchBlock catchBlock:
                        singleResult = catchBlock.ToString(formatter, out pathSpans);
                        break;
                    case LabelTarget labelTarget:
                        singleResult = labelTarget.ToString(formatter, out pathSpans);
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                var selector =
                    formatter == ObjectNotation ? x => x :
                    (Func<string,string>)(x => x.Replace("_0", ""));

                var paths = pathSpans.Keys.Select(selector).ToHashSet();

                return (formatter, (singleResult, paths));
            }).ToDictionary();

            var expectedPaths = actual[ObjectNotation].paths;

            // check that all the expected paths can resolve against the original object
            var resolver = new Resolver();
            Assert.All(expectedPaths, path => Assert.NotNull(resolver.Resolve(o, path)));

            foreach (var formatter in Formatters) {
                var expected = allExpected[(formatter, objectName)];
                var actualSingle = actual[formatter].singleResult;

                // check that the actual matches the expected
                Assert.Equal(expected, actualSingle);

                if (formatter != FactoryMethods) { // we're using the paths of the FactoryMethodsFormatter as reference paths
                    var actualPaths = actual[formatter].paths;
                    Assert.True(expectedPaths.IsSupersetOf(actualPaths));
                }
            }
        }
    }
}
