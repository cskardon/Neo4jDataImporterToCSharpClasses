﻿namespace Neo4jJsonToCSharpClasses;

using System.Text;
using Neo4jJsonToCSharpClasses.CypherWorkbench;
using Neo4jJsonToCSharpClasses.DataImporter;
using Newtonsoft.Json;

public static class Parser
{
    #region Relationsips
    private static IDictionary<string, RelationshipNormalized> GetNormalizedRelationships<TRelationship, TProperty>(IEnumerable<TRelationship> relationships) 
        where TProperty: IProperty 
        where TRelationship : IRelationship<TProperty>
    {
        var normalizedRelationships = new Dictionary<string, RelationshipNormalized>();
        foreach (var relationship in relationships)
        {
            if (!normalizedRelationships.ContainsKey(relationship.Type.ToLowerInvariant()))
            {
                normalizedRelationships.Add(relationship.Type.ToLowerInvariant(), relationship.ToNormalized());
                continue;
            }

            normalizedRelationships[relationship.Type.ToLowerInvariant()].AddSourceTargetsAndProperties(relationship);
        }

        return normalizedRelationships;
    }

    private static StringBuilder ParseNormalizedRelationships<TNode, TProperty>(IDictionary<string, RelationshipNormalized> normalizedRelationships, IDictionary<string, TNode> nodes, bool useUpperCamelCaseForProperties)
        where TProperty: IProperty
        where TNode : INode<TProperty>
    {
        var relationshipClasses = Generate.StartRelationshipsClassFile;
        foreach (var relationshipClass in
                 normalizedRelationships.Select(normalizedRelationship => Generate.OutputRelationship.Class<TNode, TProperty>(normalizedRelationship.Value, nodes, useUpperCamelCaseForProperties)))
            relationshipClasses.Append(relationshipClass);

        return relationshipClasses;
    }

    private static StringBuilder ParseNormalizedRelationshipsIntoConstClass<TNode, TProperty>(IDictionary<string, RelationshipNormalized> normalizedRelationships, IDictionary<string, TNode> nodes)
        where TProperty : IProperty
        where TNode : INode<TProperty>
    {
        return Generate.OutputRelationship.Consts<TNode, TProperty>(normalizedRelationships.Values, nodes);
    }

    private static StringBuilder ParseRelationships<TRelationship, TNode, TProperty>(IDictionary<string, TNode> nodes, IDictionary<string, TRelationship> relationships, bool useUpperCamelCaseForProperties, out StringBuilder constClass)
        where TProperty : IProperty
        where TNode : INode<TProperty>
        where TRelationship : IRelationship<TProperty>
    {
        var normalizedRelationships = GetNormalizedRelationships<TRelationship, TProperty>(relationships.Values);
        constClass = ParseNormalizedRelationshipsIntoConstClass<TNode, TProperty>(normalizedRelationships, nodes);
        return ParseNormalizedRelationships<TNode, TProperty>(normalizedRelationships, nodes, useUpperCamelCaseForProperties);
    }
    #endregion Relationsips

    #region Nodes
    private static StringBuilder ParseNodes<TNode, TProperty>(IDictionary<string, TNode> model, bool useUpperCamelCaseForProperties)
        where TProperty : IProperty
        where TNode : INode<TProperty>
    {
        var compressed = CompressNodes<TNode, TProperty>(model);

        var nodeClasses = Generate.StartNodeClassFile;
        foreach (var node in compressed)
        {
            var nodeClass = Generate.OutputNode.Class(node.Value, useUpperCamelCaseForProperties);
            nodeClasses.Append(nodeClass).AppendLine();
        }

        return nodeClasses;
    }

    private static IDictionary<string, NormalizedNode> CompressNodes<TNode, TProperty>(IDictionary<string, TNode> nodes)
        where TProperty : IProperty
        where TNode : INode<TProperty>
    {
        var output = new Dictionary<string, NormalizedNode>();

        foreach (var nodeWithKey in nodes)
        {
            var labelLower = nodeWithKey.Value.Label.ToLowerInvariant();
            if(!output.ContainsKey(labelLower))
                output.Add(labelLower, new NormalizedNode(nodeWithKey.Value.Label));

            output[labelLower].Merge<TNode, TProperty>(nodeWithKey.Key, nodeWithKey.Value);
        }

        return output;
    }

    #endregion Nodes

    public static class CypherWorkbench
    {
        private static Version VersionWorksWith = new(1, 3, 0);

        public static void Parse(string contentIn, bool useUpperCamelCaseForProperties, out StringBuilder nodeClasses, out StringBuilder relationshipClasses, out StringBuilder constClass)
        {
            var model = JsonConvert.DeserializeObject<Neo4jJsonToCSharpClasses.CypherWorkbench.CypherWorkbench>(contentIn);
            if (model == null)
                throw new InvalidDataException("CypherWorkbench: The file could not be parsed as a JSON file.");

            if (model.Metadata.Version != VersionWorksWith)
                Console.WriteLine($"This is set to work with {VersionWorksWith} of the Cypher Workbench JSON - double check results!");

            nodeClasses = ParseNodes<CypherWorkbenchNode, CypherWorkbenchProperty>(model.DataModel.Nodes, useUpperCamelCaseForProperties);
            relationshipClasses = ParseRelationships<CypherWorkbenchRelationship, CypherWorkbenchNode, CypherWorkbenchProperty>(model.DataModel.Nodes, model.DataModel.Relationships, useUpperCamelCaseForProperties, out constClass);
        }
    }

    public static class DataImporter
    {
        private static Version VersionWorksWith = new(0, 7, 0);

        public static void Parse(string contentIn, bool useUpperCamelCaseForProperties, out StringBuilder nodeClasses, out StringBuilder relationshipClasses, out StringBuilder constClass)
        {
            var model = JsonConvert.DeserializeObject<Neo4jImporter>(contentIn);
            if (model == null)
                throw new InvalidDataException("DataImporter: The file could not be parsed as a JSON file.");

            if (model.Version != VersionWorksWith)
                Console.WriteLine($"This is set to work with {VersionWorksWith} of the Cypher Workbench JSON - double check results!");

            nodeClasses = ParseNodes<DataImporterNode, DataImporterProperty>(model.DataModel.GraphModel.Nodes, useUpperCamelCaseForProperties);
            relationshipClasses = ParseRelationships<DataImporterRelationship, DataImporterNode, DataImporterProperty>(model.DataModel.GraphModel.Nodes, model.DataModel.GraphModel.Relationships, useUpperCamelCaseForProperties, out constClass);

        }
    }

    public static class Arrows
    {
        public static void Parse(string contentIn, bool useUpperCamelCaseForProperties, out StringBuilder nodeClasses, out StringBuilder relationshipClasses, out StringBuilder constClass)
        {
            var model = JsonConvert.DeserializeObject<ArrowsDocument>(contentIn);
            if (model == null)
                throw new InvalidDataException("Arrows: The file could not be parsed as a JSON file.");

            nodeClasses = ParseNodes<ArrowsNode, ArrowsProperty>(model.Nodes, useUpperCamelCaseForProperties);
            relationshipClasses = ParseRelationships<ArrowsRelationship, ArrowsNode, ArrowsProperty>(model.Nodes, model.Relationships, useUpperCamelCaseForProperties, out constClass);
        }
    }
}