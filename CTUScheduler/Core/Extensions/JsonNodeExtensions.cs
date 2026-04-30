using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CTUScheduler.Core.Extensions;

public static class JsonNodeExtensions
{
    public static bool HasFields<T>(this JsonNode? node, params Expression<Func<T, object?>>[] propertySelectors)
    {
        if (node is not JsonObject jsonObj) return false;

        foreach (var selector in propertySelectors)
        {
            var memberInfo = GetMemberInfo(selector);
            if (memberInfo is null) return false;

            var jsonAttr = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
            string jsonKey = jsonAttr?.Name ?? memberInfo.Name;

            if (!jsonObj.ContainsKey(jsonKey)) return false;
        }

        return true;
    }

    private static MemberInfo? GetMemberInfo<T>(Expression<Func<T, object?>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
            return memberExpression.Member;

        if (expression.Body is UnaryExpression { Operand: MemberExpression unaryMember })
            return unaryMember.Member;

        return null;
    }
}