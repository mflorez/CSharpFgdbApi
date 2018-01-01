using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using CSharp.FgdbApi.Schema;
using System.Collections.Generic;
using System.IO;
using Esri.ArcGISRuntime.Layers;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.FileGDB;
using Esri.ArcGISRuntime.Geometry;

namespace CSharp.FgdbApi.Utilities
{
    public static class Constants
    {
        public static readonly string Esri = "esri";
        public static readonly string Colon = ":";
        public static readonly string Xsi = "xsi";
        public static readonly string Xs = "xs";
        public const string FieldArray = "FieldArray";
        public const string IndexArray = "IndexArray";
        public const string FieldNameFid = "FID";
        public const string FieldNameObjectID = "OBJECTID";
        public const string Catalog = "Catalog";
        public const string FieldNameShape = "SHAPE";
        public const string FieldPartFdo = "FDO";
        public const string Names = "Names";
        public const string RelationshipClassNames = "RelationshipClassNames";
        public const string ExtensionProperties = "ExtensionProperties";
        public const string PropertyArray = "PropertyArray";
        public const string ArrayOfPropertySetProperty = "ArrayOfPropertySetProperty";
        public const string ControllerMemberships = "ControllerMemberships";
        public const string GeometryDef = "GeometryDef";
    }
    public static class DataElementSerializer
    {
        public static string Serialize(this DataElement dataElement, bool omitXmlDeclation)
        {
            XmlTypeAttribute dataElementTypeXmlAttribute = (XmlTypeAttribute)Attribute.GetCustomAttribute(typeof(DataElement), typeof(XmlTypeAttribute));
            string esriNSVal = dataElementTypeXmlAttribute.Namespace; //"http://www.esri.com/schemas/ArcGIS/10.3";
            string xsiNSVal = XmlSchema.InstanceNamespace; //"http://www.w3.org/2001/XMLSchema-instance";
            string xsNSVal = XmlSchema.Namespace; //"http://www.w3.org/2001/XMLSchema";
            XmlSerializerNamespaces xsNS = new XmlSerializerNamespaces();
            xsNS.Add(Constants.Esri, esriNSVal);
            xsNS.Add(Constants.Xsi, xsiNSVal);
            xsNS.Add(Constants.Xs, xsNSVal);
            
            XmlWriterSettings writerSettings = new XmlWriterSettings()
            {
                Indent = true,
                NewLineChars = "\n",
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = omitXmlDeclation
            };

            Utf8StringWriter xmlDataBuffer = new Utf8StringWriter();

            using (XmlWriter xmlWriter = XmlWriter.Create(xmlDataBuffer, writerSettings))
            {
                XmlRootAttribute root = new XmlRootAttribute();
                root.ElementName = typeof(DataElement).Name;
                root.Namespace = esriNSVal;

                var extraTypeList = (from lAssembly in AppDomain.CurrentDomain.GetAssemblies()
                                     from lType in lAssembly.GetTypes()
                                     where typeof(DataElement).IsAssignableFrom(lType)
                                     select lType).ToList();

                var extraTypes = extraTypeList.ToArray();

                XmlAttributeOverrides overrides = new XmlAttributeOverrides();
                XmlAttributes xAtt = new XmlAttributes();

                foreach (Type type in extraTypes)
                {
                    xAtt.XmlElements.Add(new XmlElementAttribute(type.Name, type));
                }

                overrides.Add(dataElement.GetType(), typeof(DataElement).Name, xAtt);

                /**
                 * FieldArray, IndexArray, RelationshipClassNames, ExtensionProperties,
                 * PropertyArray, ControllerMemberships implementation done inside FileGDBAPI.partial.cs.
                 * The original properties must be ignored.  This is needed to affect
                 * XML Serialization correct output.
                 * 
                 * */
                xAtt = new XmlAttributes();
                xAtt.XmlIgnore = true;
                overrides.Add(typeof(Fields), Constants.FieldArray, xAtt);

                xAtt = new XmlAttributes();
                xAtt.XmlIgnore = true;
                overrides.Add(typeof(Indexes), Constants.IndexArray, xAtt);

                xAtt = new XmlAttributes();
                xAtt.XmlIgnore = true;
                overrides.Add(typeof(DETable), Constants.RelationshipClassNames, xAtt);
                overrides.Add(typeof(DETable), Constants.ExtensionProperties, xAtt);
                overrides.Add(typeof(PropertySet), Constants.PropertyArray, xAtt);
                overrides.Add(typeof(DETable), Constants.ControllerMemberships, xAtt);

                var serializer = new XmlSerializer(dataElement.GetType().BaseType, overrides, extraTypes, root, esriNSVal);
                serializer.Serialize(xmlWriter, dataElement, xsNS);
            }


            return xmlDataBuffer.ToString();
        }
    }
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding
        {
            get
            {
                return new UTF8Encoding(false);
            }
        }
    }
    public class FgdbHelper
    {
        public static FeatureComponents CreateGeoDBFromShapefile(string shapeFilePath, string fileGeoDBPath)
        {            
            FeatureComponents featureComponents = FgdbHelper.LoadFeatureComponentsFromShapefile(shapeFilePath).Result;
            
            //string displayName = fLayer.DisplayName;
            string displayName = featureComponents.ShapeFileTable.Name;

            // Delete it, if found.
            FgdbHelper.DeleteGeodatabase(fileGeoDBPath);

            // Create it again for testing.
            Esri.FileGDB.Geodatabase geodatabaseCreate = Esri.FileGDB.Geodatabase.Create(fileGeoDBPath);
            geodatabaseCreate.Close();

            // Open the geodatabase.
            Esri.FileGDB.Geodatabase geodatabase = Esri.FileGDB.Geodatabase.Open(fileGeoDBPath);

            // Create new Feature Set.                        
            bool omitXmlDeclation = true;
            string xmlSchema = FgdbHelper.GetDEFeatureDataset(featureComponents.FeatureSet, displayName).Serialize(omitXmlDeclation);
            geodatabase.CreateFeatureDataset(xmlSchema);

            // Create table.
            omitXmlDeclation = false;
            string xmlSchemaDEFeatureClass = FgdbHelper.GetDEFeatureClass(featureComponents.FeatureSet, displayName).Serialize(omitXmlDeclation);
            Table table = geodatabase.CreateTable(xmlSchemaDEFeatureClass, "\\" + displayName + Constants.Catalog);

            // Load the table data from featureSet
            table = FgdbHelper.LoadShapefileDataIntoTable(featureComponents.FeatureSet, table);

            table.Close();
            geodatabase.Close();           

            return featureComponents;
        }
        public static async Task<ShapefileTable> LoadShapefileTableFromShapefile(string path)
        {
            ShapefileTable shapefileTable = null;

            try
            {
                // open shapefile table
                shapefileTable = await ShapefileTable.OpenAsync(path);                                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating shapefile table: " + ex.Message, "Sample Error");
            }

            return shapefileTable;
        }
        public static async Task<FeatureLayer> LoadFeatureLayerFromShapefile(string path)
        {
            FeatureLayer featureLayer = null;
            try
            {
                // open shapefile table
                var shapefileTable = await ShapefileTable.OpenAsync(path);
                // create feature layer based on the shapefile
                featureLayer = new FeatureLayer(shapefileTable)
                {
                    ID = path,
                    DisplayName = shapefileTable.Name
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating feature layer: " + ex.Message, "Sample Error");
            }
            return featureLayer;
        }
        public interface IFeatureComponents
        {
            ShapefileTable ShapeFileTable { get; set; }
            FeatureLayer FeatureLayer { get; set; }
            FeatureSet FeatureSet { get; set; }
        }
        public class FeatureComponents : IFeatureComponents
        {
            public ShapefileTable ShapeFileTable { get; set; }
            public FeatureLayer FeatureLayer { get; set; }
            public FeatureSet FeatureSet { get; set; }
        }
        public static async Task<FeatureComponents> LoadFeatureComponentsFromShapefile(string path)
        {
            FeatureComponents featureComponents = new FeatureComponents();
            featureComponents.ShapeFileTable = await LoadShapefileTableFromShapefile(path);
            featureComponents.FeatureLayer = new FeatureLayer(featureComponents.ShapeFileTable)
            {
                ID = path,
                DisplayName = featureComponents.ShapeFileTable.Name                                               
            };
            featureComponents.FeatureSet = LoadFeatureSetFromFeatureLayer(featureComponents.FeatureLayer);            
            return featureComponents;            
        }
        public static FeatureSet LoadFeatureSetFromFeatureLayer(FeatureLayer featureLayer)
        {
            if (!(featureLayer.FeatureTable is ShapefileTable)) { return null; }
            IEnumerable<Feature> features = (featureLayer.FeatureTable as ShapefileTable).QueryAsync(new QueryFilter { WhereClause = "1=1" }).Result;
            FeatureSet featureSet = new FeatureSet(features, featureLayer.FeatureTable.Schema.Fields);
            return featureSet;
        }
        public static bool DeleteGeodatabase(string path)
        {
            bool isDeleted = false;
            try
            {
                // Delete the geodatabase in the current directory, if it's already there.
                Esri.FileGDB.Geodatabase.Delete(path);
                isDeleted = true;
                Console.WriteLine("The geodatabase has been deleted");
            }
            catch (FileGDBException ex)
            {
                if (ex.ErrorCode == -2147024893)
                    Console.WriteLine("The geodatabase does not exist, no need to delete");
                else
                {
                    Console.WriteLine("{0} - {1}", ex.Message, ex.ErrorCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while deleting the geodatabase.  " + ex.Message);
                return false;
            }

            return isDeleted;
        }
        public static DEFeatureDataset GetDEFeatureDataset(FeatureSet featureSet, string displayName)
        {
            string wktText = featureSet.SpatialReference.WkText;
            int wkID = featureSet.SpatialReference.Wkid;

            // Create new Feature Set.            
            DEFeatureDataset de = new DEFeatureDataset();
            de.CatalogPath = "\\" + displayName + Constants.Catalog;
            de.Name = displayName + Constants.Catalog;
            de.ChildrenExpanded = false;
            de.DatasetType = esriDatasetType.esriDTFeatureDataset;
            de.Versioned = false;
            de.CanVersion = false;

            Schema.SpatialReference spatialRef = GetSpatialReference(featureSet);
            de.SpatialReference = spatialRef;

            de.ChildrenExpandedSpecified = true;
            de.ChildrenExpanded = false;
            de.VersionedSpecified = true;
            de.Versioned = false;
            de.CanVersionSpecified = true;
            de.CanVersion = false;
            de.Extent = null;

            de.Metadata = null;
            de.Children = null;

            return de;
        }
        public static DEFeatureClass GetDEFeatureClass(FeatureSet featureSet, string displayName)
        {
            // Create new Feature Set.            
            DEFeatureClass de = new DEFeatureClass();
            de.CatalogPath = "\\" + displayName + Constants.Catalog + "\\" + displayName;
            de.Name = displayName;
            de.ChildrenExpanded = false;
            de.DatasetType = esriDatasetType.esriDTFeatureClass;
            de.Versioned = false;
            de.CanVersion = false;

            de.ChildrenExpandedSpecified = true;
            de.ChildrenExpanded = false;
            de.VersionedSpecified = true;
            de.Versioned = false;
            de.CanVersionSpecified = true;
            de.CanVersion = false;

            de.HasOID = true;
            de.OIDFieldName = Constants.FieldNameObjectID;

            de.Metadata = null;
            de.Children = null;

            Fields flds = new Fields();
            FieldArray fldArray = new FieldArray();
            fldArray.Field = GetFields(featureSet);
            flds.FieldArrayProperty = fldArray;

            de.Fields = flds;

            Indexes indxs = new Indexes();
            IndexArray idxArray = new IndexArray();
            idxArray.Index = GetIndexes(featureSet);
            indxs.IndexArrayProperty = idxArray;

            de.Indexes = indxs;

            de.CLSID = GetSimpleClsidGuid();
            de.EXTCLSID = "";

            de.Subtypes = null;
            de.FilteredFieldNames = null;

            de.FeatureType = esriFeatureType.esriFTSimple;

            de.RelationshipClassNamesProperty = null;

            de.AliasName = displayName;
            de.HasGlobalIDSpecified = true;
            de.HasGlobalID = false;            

            ArrayOfPropertySet arrPropSet = new ArrayOfPropertySet();
            arrPropSet.ArrayOfPropertyArray = new PropertyArray();
            de.ExtensionPropertiesProperty = arrPropSet;

            ArrayOfController arrCtrlMembership = new ArrayOfController();
            de.ControllerMembershipsProperty = arrCtrlMembership;

            de.ShapeType = GetShapeType(featureSet.GeometryType);

            de.ShapeFieldName = Constants.FieldNameShape;

            de.HasMSpecified = true;
            de.HasM = featureSet.HasM;

            de.HasZSpecified = true;
            de.HasZ = featureSet.HasZ;

            de.HasSpatialIndexSpecified = true;
            de.HasSpatialIndex = true;            

            de.Extent = null;

            Schema.SpatialReference spatialRef = GetSpatialReference(featureSet);
            de.SpatialReference = spatialRef;

            return de;
        }
        private static esriGeometryType GetShapeType(Esri.ArcGISRuntime.Geometry.GeometryType geometryType)
        {
            esriGeometryType geoType = new esriGeometryType();

            switch (geometryType)
            {
                case Esri.ArcGISRuntime.Geometry.GeometryType.Point:
                    geoType = esriGeometryType.esriGeometryPoint;
                    break;
                case Esri.ArcGISRuntime.Geometry.GeometryType.Polygon:
                    geoType = esriGeometryType.esriGeometryPolygon;
                    break;
                case Esri.ArcGISRuntime.Geometry.GeometryType.Polyline:
                    geoType = esriGeometryType.esriGeometryPolyline;
                    break;
                case Esri.ArcGISRuntime.Geometry.GeometryType.Multipoint:
                    geoType = esriGeometryType.esriGeometryMultipoint;
                    break;
                default:
                    geoType = esriGeometryType.esriGeometryPoint;
                    break;
            }

            return geoType;
        }
        private static List<Index> GetIndexes(FeatureSet featureSet)
        {
            List<Index> indexes = new List<Index>();
            indexes.Add(CreateObjectIDFieldIndex());

            var shapeField = featureSet.Fields.Where(f => f.Name.Equals(Constants.FieldNameShape, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (shapeField != null)
            {
                indexes.Add(CreateShapeFieldIndex(shapeField, featureSet));
            }

            return indexes;
        }
        private static Index CreateObjectIDFieldIndex()
        {
            Index idx = new Index();

            string fieldName = Constants.FieldNameObjectID;
            idx.Name = Constants.FieldPartFdo + "_" + fieldName;

            idx.IsUnique = true;
            idx.IsAscending = true;

            Schema.Field fld = new Schema.Field();
            fld.Name = fieldName;
            fld.Type = esriFieldType.esriFieldTypeOID;

            fld.Length = 16;
            fld.Precision = 0;
            fld.Scale = 0;
            fld.Required = true;
            fld.EditableSpecified = true;
            fld.Editable = false;
            fld.GeometryDef = null;
            fld.RasterDef = null;

            fld.AliasName = fieldName;
            fld.ModelName = fieldName;

            fld.IsNullable = false;

            Fields flds = new Fields();
            FieldArray fldArray = new FieldArray();
            fldArray.Field = new List<Schema.Field>() { fld };
            flds.FieldArrayProperty = fldArray;

            idx.Fields = flds;

            fld.AliasName = fieldName;
            fld.ModelName = fieldName;
            return idx;
        }
        private static Index CreateShapeFieldIndex(Esri.ArcGISRuntime.Data.FieldInfo field, FeatureSet featureSet)
        {
            string fieldName = field.Name.ToUpperInvariant();
            Index idx = new Index();
            idx.Name = Constants.FieldPartFdo + "_" + fieldName;

            idx.IsUnique = false;
            idx.IsAscending = true;

            Schema.Field fld = new Schema.Field();
            fld.Name = fieldName;
            fld.Type = esriFieldType.esriFieldTypeGeometry;
            fld.IsNullable = true;
            fld.Length = field.Length ?? 0;
            fld.Precision = 0;
            fld.Scale = 0;
            fld.Required = true;

            Schema.GeometryDef geoDef = GetGeometryDef(field, featureSet);
            fld.GeometryDef = geoDef;

            Fields flds = new Fields();
            FieldArray fldArray = new FieldArray();
            fldArray.Field = new List<Schema.Field>() { fld };
            flds.FieldArrayProperty = fldArray;

            idx.Fields = flds;

            fld.AliasName = fieldName;
            fld.ModelName = fieldName;

            return idx;
        }
        private static Index GetIndexFromFieldInfo(Esri.ArcGISRuntime.Data.FieldInfo field, FeatureSet featureSet)
        {
            string fieldName = field.Name.ToUpperInvariant();

            Index idx = new Index();
            idx.Name = Constants.FieldPartFdo + "_" + fieldName;

            idx.IsUnique = false;
            idx.IsAscending = true;

            return idx;
        }
        public static Schema.GeometryDef GetGeometryDef(Esri.ArcGISRuntime.Data.FieldInfo field, FeatureSet featureSet)
        {
            CSharp.FgdbApi.Schema.GeometryDef geoDef = new CSharp.FgdbApi.Schema.GeometryDef();
            geoDef.AvgNumPoints = geoDef.AvgNumPoints;
            geoDef.GeometryType = GetGeometryDefType(field, featureSet.GeometryType);
            geoDef.HasM = featureSet.HasM;
            geoDef.HasZ = featureSet.HasZ;
            geoDef.SpatialReference = GetSpatialReference(featureSet);
            geoDef.GridSize0Specified = true;
            geoDef.GridSize0 = 0;

            return geoDef;
        }
        public static string GetSimpleClsidGuid()
        {
            return "{52353152-891A-11D0-BEC6-00805F7C4268}";
        }
        public static List<Schema.Field> GetFields(FeatureSet featureSet)
        {
            List<Schema.Field> fields = new List<Schema.Field>();
            var featureSetFields = featureSet.Fields.Where(f => CanProcessField(f));

            fields.Add(CreateObjectIDField());

            foreach (Esri.ArcGISRuntime.Data.FieldInfo field in featureSetFields)
            {
                if (IsFldNameFidOrObjectId(field.Name) == false)
                {
                    fields.Add(GetFieldFromFieldInfo(field, featureSet));
                }
            }

            return fields;
        }
        public static bool CanProcessField(Esri.ArcGISRuntime.Data.FieldInfo fieldInfo)
        {
            bool isOjbectID = fieldInfo.Name.Equals(Constants.FieldNameObjectID, StringComparison.InvariantCultureIgnoreCase);
            bool canProcess = !isOjbectID;
            return canProcess;
        }
        public static Schema.Field CreateObjectIDField()
        {
            string fieldName = Constants.FieldNameObjectID;
            Schema.Field fld = new Schema.Field();
            fld.Name = fieldName;
            fld.AliasName = fieldName;
            fld.ModelName = fieldName;

            fld.IsNullable = false;
            fld.EditableSpecified = true;
            fld.Type = esriFieldType.esriFieldTypeOID;
            fld.Length = 4;
            fld.Precision = 0;
            fld.Scale = 0;
            fld.EditableSpecified = true;
            fld.Editable = false;
            fld.GeometryDef = null;

            return fld;
        }
        public static Schema.Field GetFieldFromFieldInfo(Esri.ArcGISRuntime.Data.FieldInfo field, FeatureSet featureSet)
        {
            string fieldName = field.Name.ToUpperInvariant();
            Schema.Field fld = new Schema.Field();

            fld.Name = fieldName;
            fld.AliasName = fieldName;
            fld.ModelName = fieldName;

            fld.IsNullable = field.IsNullable;
            fld.EditableSpecified = true;
            fld.Editable = field.IsEditable;

            if (fieldName.Equals(Constants.FieldNameShape))
            {
                fld.Type = esriFieldType.esriFieldTypeGeometry;
                fld.Length = field.Length ?? 0;
                fld.Precision = 0;
                fld.Scale = 0;
                fld.Required = true;
                Schema.GeometryDef geoDef = GetGeometryDef(field, featureSet);
                fld.GeometryDef = geoDef;
            }
            else
            {
                fld.Type = GetType(field.Type);
                fld.Length = field.Length ?? 0;
                fld.GeometryDef = null;
            }

            return fld;
        }
        public static esriGeometryType GetGeometryDefType(Esri.ArcGISRuntime.Data.FieldInfo field, Esri.ArcGISRuntime.Geometry.GeometryType shapeGeomeryType)
        {
            esriGeometryType geoType = new esriGeometryType();
            switch (shapeGeomeryType)
            {
                case Esri.ArcGISRuntime.Geometry.GeometryType.Multipoint:
                    geoType = esriGeometryType.esriGeometryMultiPatch;
                    break;
                case Esri.ArcGISRuntime.Geometry.GeometryType.Point:
                    geoType = esriGeometryType.esriGeometryPoint;
                    break;
                case Esri.ArcGISRuntime.Geometry.GeometryType.Polygon:
                    geoType = esriGeometryType.esriGeometryPolygon;
                    break;
                case Esri.ArcGISRuntime.Geometry.GeometryType.Polyline:
                    geoType = esriGeometryType.esriGeometryPolyline;
                    break;
            }

            return geoType;
        }
        public static esriFieldType GetType(Esri.ArcGISRuntime.Data.FieldType type)
        {
            esriFieldType fldType = new esriFieldType();
            switch (type)
            {
                case Esri.ArcGISRuntime.Data.FieldType.Blob:
                    fldType = esriFieldType.esriFieldTypeBlob;
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Date:
                    fldType = esriFieldType.esriFieldTypeDate;
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Double:
                    fldType = esriFieldType.esriFieldTypeDouble;
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Geometry:
                    fldType = esriFieldType.esriFieldTypeGeometry;
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.GlobalID:
                    fldType = esriFieldType.esriFieldTypeGlobalID;
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Guid:
                    fldType = esriFieldType.esriFieldTypeGUID;
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Integer:
                    fldType = esriFieldType.esriFieldTypeInteger;
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Oid:
                    fldType = esriFieldType.esriFieldTypeOID;
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Raster:
                    fldType = esriFieldType.esriFieldTypeRaster;
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Single:
                    fldType = esriFieldType.esriFieldTypeSingle;
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.SmallInteger:
                    fldType = esriFieldType.esriFieldTypeSmallInteger;
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.String:
                    fldType = esriFieldType.esriFieldTypeString;
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Xml:
                    fldType = esriFieldType.esriFieldTypeXML;
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Unknown:
                    fldType = esriFieldType.esriFieldTypeString;
                    break;
            }

            return fldType;
        }
        public static Schema.SpatialReference GetSpatialReference(FeatureSet featureSet)
        {
            Schema.SpatialReference spatialRef;

            if (featureSet.SpatialReference.IsGeographic)
            {
                spatialRef = new GeographicCoordinateSystem();
            }
            else if (featureSet.SpatialReference.IsProjected)
            {
                spatialRef = new ProjectedCoordinateSystem();
            }
            else
            {
                spatialRef = new UnknownCoordinateSystem();
            }

            spatialRef.WKT = featureSet.SpatialReference.WkText;
            spatialRef.WKIDSpecified = true;
            spatialRef.WKID = featureSet.SpatialReference.Wkid;

            // Create the projected coordinate system for the feature.                      
            spatialRef.XOriginSpecified = true;
            spatialRef.YOriginSpecified = true;
            spatialRef.XYScaleSpecified = true;
            spatialRef.ZOriginSpecified = true;
            spatialRef.ZScaleSpecified = true;
            spatialRef.MOriginSpecified = true;
            spatialRef.MScaleSpecified = true;
            spatialRef.XYToleranceSpecified = true;
            spatialRef.ZToleranceSpecified = true;
            spatialRef.MToleranceSpecified = true;
            spatialRef.HighPrecisionSpecified = true;

            spatialRef.XOrigin = -5120900;
            spatialRef.YOrigin = -9998100;
            spatialRef.XYScale = 10000;
            spatialRef.ZOrigin = -100000;
            spatialRef.ZScale = 10000;
            spatialRef.MOrigin = -100000;
            spatialRef.MScale = 10000;
            spatialRef.XYTolerance = 0.001;
            spatialRef.ZTolerance = 0.001;
            spatialRef.MTolerance = 0.001;
            spatialRef.HighPrecision = true;

            return spatialRef;
        }
        public static bool IsFldNameFidOrObjectId(string field)
        {
            return (field.Equals(Constants.FieldNameFid, StringComparison.InvariantCultureIgnoreCase)
                || field.Equals(Constants.FieldNameObjectID, StringComparison.InvariantCultureIgnoreCase));
        }
        public static Row GetFeatureRowData(Row row, KeyValuePair<string, object> keyValPair, Esri.ArcGISRuntime.Data.FieldType fieldType)
        {
            string key = keyValPair.Key;
            object val = keyValPair.Value;            
            switch (fieldType)
            {
                case Esri.ArcGISRuntime.Data.FieldType.Blob:
                    ByteArray blob = val as ByteArray;
                    if (blob != null)
                    {
                        row.SetBinary(key, blob);
                    }
                    else
                    {
                        row.SetNull(key);
                    }
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Date:
                    DateTime? date = val as DateTime?;
                    if (date != null)
                    {
                        row.SetDate(key, date.GetValueOrDefault());
                    }
                    else
                    {
                        row.SetNull(key);
                    }
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Double:
                    double dbl = (double)val;
                    row.SetDouble(key, dbl);
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Geometry:
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.GlobalID:
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Guid:
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Integer:
                    int intVal = Convert.ToInt32(val);
                    row.SetInteger(key, intVal);
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Oid:
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Raster:
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Single:
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.SmallInteger:
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.String:
                    string value = val as string;
                    row.SetString(key, value);
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Unknown:
                    break;
                case Esri.ArcGISRuntime.Data.FieldType.Xml:
                    break;                                                                                
            }            
            return row;
        }        
        public static Table LoadShapefileDataIntoTable(FeatureSet featureSet, Table table)
        {
            IReadOnlyList<Esri.ArcGISRuntime.Data.FieldInfo> fldInfoList = featureSet.Fields;
            IEnumerable<Feature> features = featureSet.Features;

            foreach (Feature feature in features)
            {
                Row row = table.CreateRowObject();
                feature.Attributes.AsParallel().ForAll(keyValPair =>
                {
                    if (IsFldNameFidOrObjectId(keyValPair.Key) == false)
                    {
                        Esri.ArcGISRuntime.Data.FieldInfo fieldInfo = fldInfoList.Where(f => f.Name == keyValPair.Key).FirstOrDefault();
                        row = GetFeatureRowData(row, keyValPair, fieldInfo.Type);
                    }
                });

                row.SetGeometry(GetFeatureRowGeometry(feature.Geometry));

                table.Insert(row);
            }            

            return table;
        }
        public static PointShapeBuffer GetFeatureRowGeometry(Esri.ArcGISRuntime.Geometry.Geometry geometry)
        {
            PointShapeBuffer pointShapeBuffer = new PointShapeBuffer();
            ShapeType shpType = GetGeometryShapeType(geometry.GeometryType);
            pointShapeBuffer.Setup(shpType);

            MapPoint mp = geometry as MapPoint;
            double xValue = mp.X;
            double yValue = mp.Y;
            Esri.FileGDB.Point point = new Esri.FileGDB.Point(xValue, yValue);
            pointShapeBuffer.point = point;

            return pointShapeBuffer;
        }
        private static ShapeType GetGeometryShapeType(Esri.ArcGISRuntime.Geometry.GeometryType geometryType)
        {
            ShapeType shapeType = new ShapeType();

            switch (geometryType)
            {
                case Esri.ArcGISRuntime.Geometry.GeometryType.Point:
                    shapeType = ShapeType.Point;
                    break;
                case Esri.ArcGISRuntime.Geometry.GeometryType.Multipoint:
                    shapeType = ShapeType.Multipoint;
                    break;
                case Esri.ArcGISRuntime.Geometry.GeometryType.Polygon:
                    shapeType = ShapeType.Multipoint;
                    break;
                case Esri.ArcGISRuntime.Geometry.GeometryType.Polyline:
                    shapeType = ShapeType.Polygon;
                    break;
                case Esri.ArcGISRuntime.Geometry.GeometryType.Unknown:
                    shapeType = ShapeType.Null;
                    break;                   
            }

            return shapeType;
        }
    }

}
