using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace BtmI2p.MiscUtils
{
    public static partial class MyExtensionMethods
    {
        public static List<string> MyNameOfProperties<T1>(
            this T1 obj,
            params Expression<Func<T1, object>>[] expressions
        )
        {
            var result = expressions.Select(MyNameof<T1>.Property).ToList();
            Assert.Equal(result,result.Distinct());
            return result;
        }

        public static string MyNameOfProperty<T1, TProp>(
            this T1 obj,
            Expression<Func<T1, TProp>> expression)
        {
            return MyNameof<T1>.Property(expression);
        }

        public static string MyNameOfMethod<T1>(
            this T1 obj,
            Expression<Action<T1>> expression
            )
        {
            return MyNameof<T1>.MethodName(expression);
        }
    }
    public class MyNameof
    {
        public static string GetLocalVarName<T>(
            Expression<Func<T>> memberExpression
        )
        {
            var expressionBody
                = (MemberExpression)memberExpression.Body;
            return expressionBody.Member.Name;
        }

        public static string GetMethodName(
            Expression<Action> expression
        )
        {
            var body = expression.Body as MethodCallExpression;
            if (body == null)
                throw new ArgumentException(
                    string.Format(
                        "'{0}' should be a member expression",
                        GetLocalVarName(() => expression)
                    )
                );
            return body.Method.Name;
        }
    }
    public class MyNameof<T>
    {
        public static string MethodName(
            Expression<Action<T>> expression
        )
        {
            var body = expression.Body as MethodCallExpression;
            if (body == null)
                throw new ArgumentException(
                    string.Format(
                        "'{0}' should be a member expression",
                        MyNameof.GetLocalVarName(() => expression)
                    )
                );
            return body.Method.Name;
        }

        public static string Property<TProp>(
            Expression<Func<T, TProp>> expression
        )
        {
            var bodyExpr = expression.Body;
            if (
                typeof (TProp) == typeof (object)
                && bodyExpr.NodeType == ExpressionType.Convert
            )
            {
                var body = bodyExpr as UnaryExpression;
                Assert.NotNull(body);
                Assert.Equal(
                    body.Operand.NodeType,
                    ExpressionType.MemberAccess
                );
                var memberBody = body.Operand as MemberExpression;
                Assert.NotNull(memberBody);
                return memberBody.Member.Name;
            }
            if (
                bodyExpr.NodeType == ExpressionType.MemberAccess
            )
            {
                var memberBody = bodyExpr as MemberExpression;
                Assert.NotNull(memberBody);
                return memberBody.Member.Name;
            }
            throw new ArgumentException(
                string.Format(
                    "'{0}' should be a member expression",
                    MyNameof.GetLocalVarName(() => expression)
                )
            );
        }
    }
}
