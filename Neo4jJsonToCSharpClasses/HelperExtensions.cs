﻿namespace Neo4jJsonToCSharpClasses;

public static class HelperExtensions
{
    public static RelationshipNormalized ToNormalized<T>(this IRelationship<T> relationship) 
        where T:IProperty
    {
        var normalized = new RelationshipNormalized { Type = relationship.Type };
        normalized.AddSourceTargetsAndProperties(relationship.SourceNode!, relationship.TargetNode!, relationship.Properties!.Cast<IProperty>());
        return normalized;
    }

    public static string ToCSharpType(this string javaType)
    {
        var type = javaType.ToLowerInvariant();
        var isArray = type.ToLowerInvariant().Contains("array");

        type = type.Replace("array", string.Empty);

        return type.ToLowerInvariant() switch
        {
            "string" => "string".AddArray(isArray),
            "integer" => "int".AddArray(isArray),
            "float" => "float".AddArray(isArray),
            "boolean" => "bool".AddArray(isArray),
            "date" => "DateTime".AddArray(isArray),
            "datetime" => "DateTime".AddArray(isArray),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown type '{type}' to parse.")
        };
    }

    private static string AddArray(this string type, bool isArray)
    {
        return $"{type}{(isArray ? "[]" : string.Empty)}";
    }
}