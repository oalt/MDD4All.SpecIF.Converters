using MDD4All.SpecIF.DataModels;
using MDD4All.SpecIF.DataProvider.Contracts;
using System;
using System.Collections.Generic;

namespace MDD4All.SpecIF.Converters
{
    public class HierarchyExporter
    {
        private ISpecIfMetadataReader _metadataReader;
        private ISpecIfDataReader _dataReader;

        public HierarchyExporter(ISpecIfMetadataReader metadataReader,
                                 ISpecIfDataReader dataReader)
        {
            _metadataReader = metadataReader;
            _dataReader = dataReader;
        }

        private Node Hierarchy { get; set; } = null;
        private Dictionary<Key, Resource> Resources { get; set; } = new Dictionary<Key, Resource>();
        private Dictionary<Key, Statement> Statements { get; set; } = new Dictionary<Key,Statement>();
        public Dictionary<Key, ResourceClass> ResouceClasses { get; set; } = new Dictionary<Key, ResourceClass>();
        public Dictionary<Key, StatementClass> StatementClasses { get; set; } = new Dictionary<Key, StatementClass>();
        public Dictionary<Key, PropertyClass> PropertyClasses { get; set; } = new Dictionary<Key, PropertyClass>();
        public Dictionary<Key, DataType> DataTypes { get; set; } = new Dictionary<Key,DataType>();

        private bool _includeMetadata = false;
        private bool _includeStatements = false;
        private bool _includeRevisions = false;


        public DataModels.SpecIF ExportHierarchy(Key hierarchyKey, 
                                                 bool includeMetadata,
                                                 bool includeStatements,
                                                 bool includeRevisions)
        {
            DataModels.SpecIF result = new DataModels.SpecIF();

            _includeMetadata = includeMetadata;
            _includeStatements = includeStatements;
            _includeRevisions = includeRevisions;

            ClearDictionaries();

            Hierarchy = _dataReader.GetHierarchyByKey(hierarchyKey);

            result.ID = Hierarchy.ProjectID;
            result.Generator = "SpecIFicator";

            AddNodeResourcesRecursively(Hierarchy);

            result.Hierarchies.Add(Hierarchy);

            foreach(KeyValuePair<Key, Resource> keyValuePair in Resources)
            {
                result.Resources.Add(keyValuePair.Value);
            }

            foreach(KeyValuePair<Key, Statement> keyValuePair in Statements)
            {
                result.Statements.Add(keyValuePair.Value);
            }

            foreach (KeyValuePair<Key, ResourceClass> keyValuePair in ResouceClasses)
            {
                result.ResourceClasses.Add(keyValuePair.Value);
            }

            foreach (KeyValuePair<Key, StatementClass> keyValuePair in StatementClasses)
            {
                result.StatementClasses.Add(keyValuePair.Value);
            }

            foreach (KeyValuePair<Key, PropertyClass> keyValuePair in PropertyClasses)
            {
                result.PropertyClasses.Add(keyValuePair.Value);
            }

            foreach (KeyValuePair<Key, DataType> keyValuePair in DataTypes)
            {
                result.DataTypes.Add(keyValuePair.Value);
            }

            return result;
        }

        private void ClearDictionaries()
        {
            Hierarchy = null;
            Resources.Clear();
            Statements.Clear();
            ResouceClasses.Clear();
            StatementClasses.Clear();
            PropertyClasses.Clear();
            DataTypes.Clear();
        }

        private void AddNodeResourcesRecursively(Node currentNode)
        {
            if(currentNode.ResourceReference != null)
            {
                if(!Resources.ContainsKey(currentNode.ResourceReference))
                {
                    List<Resource> resources = new List<Resource>();

                    if (_includeRevisions)
                    {
                        resources = _dataReader.GetAllResourceRevisions(currentNode.ResourceReference.ID);
                    }
                    else
                    {
                        Resource resource = _dataReader.GetResourceByKey(currentNode.ResourceReference);
                        resources.Add(resource);
                    }

                    if (resources != null)
                    {
                        foreach (Resource resource in resources)
                        {
                            if (resource != null)
                            {
                                Key resourceKey = new Key(resource.ID, resource.Revision);
                                Resources.Add(resourceKey, resource);

                                if (_includeMetadata)
                                {
                                    AddMetadataForResource(resource);
                                }
                                if(_includeStatements)
                                {
                                    AddStatementsForResource(resource);
                                }
                            }
                        }
                    }
                }
            }

            foreach(Node node in currentNode.Nodes)
            {
                AddNodeResourcesRecursively(node);
            }
        }

        private void AddMetadataForResource(Resource resource)
        {
            if (!ResouceClasses.ContainsKey(resource.Class))
            {
                ResourceClass resourceClass = _metadataReader.GetResourceClassByKey(resource.Class);

                if(resourceClass != null)
                {
                    ResouceClasses.Add(resource.Class, resourceClass);

                    foreach(Key propertyClassKey in resourceClass.PropertyClasses)
                    {
                        if(!PropertyClasses.ContainsKey(propertyClassKey))
                        {
                            PropertyClass propertyClass = _metadataReader.GetPropertyClassByKey(propertyClassKey);
                            if(propertyClass != null)
                            {
                                PropertyClasses.Add(propertyClassKey, propertyClass);

                                if(!DataTypes.ContainsKey(propertyClass.DataType))
                                {
                                    DataType dataType = _metadataReader.GetDataTypeByKey(propertyClass.DataType);
                                    if(dataType != null)
                                    {
                                        DataTypes.Add(propertyClass.DataType, dataType);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AddStatementsForResource(Resource resource)
        {
            List<Statement> statements = _dataReader.GetAllStatementsForResource(new Key(resource.ID, resource.Revision));

            foreach(Statement statement in statements)
            {
                Key statementKey = new Key(statement.ID, statement.Revision);
                if(!Statements.ContainsKey(statementKey))
                {
                    Statements.Add(statementKey, statement);

                    if(_includeMetadata)
                    {
                        AddMetadataForResource(statement);
                    }
                }
            }
        }

        private void AddMetadataForResource(Statement statement)
        {
            if (!StatementClasses.ContainsKey(statement.Class))
            {
                StatementClass statementClass = _metadataReader.GetStatementClassByKey(statement.Class);

                if (statementClass != null)
                {
                    StatementClasses.Add(statement.Class, statementClass);

                    if (statementClass.PropertyClasses != null)
                    {
                        foreach (Key propertyClassKey in statementClass.PropertyClasses)
                        {
                            if (!PropertyClasses.ContainsKey(propertyClassKey))
                            {
                                PropertyClass propertyClass = _metadataReader.GetPropertyClassByKey(propertyClassKey);
                                if (propertyClass != null)
                                {
                                    PropertyClasses.Add(propertyClassKey, propertyClass);

                                    if (!DataTypes.ContainsKey(propertyClass.DataType))
                                    {
                                        DataType dataType = _metadataReader.GetDataTypeByKey(propertyClass.DataType);
                                        if (dataType != null)
                                        {
                                            DataTypes.Add(propertyClass.DataType, dataType);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

    }
}
