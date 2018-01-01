using Esri.FileGDB;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CSharp.FgdbApi.Utilities.Tests
{
    [TestClass()]
    public class FgdbHelperTests
    {
        [TestMethod()]
        public void LoadFeatureComponentsFromShapefileTest()
        {
            string shapeFilePath = "../../samples/data/City/Cities.shp";

            FgdbHelper.FeatureComponents featureComponents = FgdbHelper.LoadFeatureComponentsFromShapefile(shapeFilePath).Result;
            Assert.IsNotNull(featureComponents.FeatureLayer);
            Assert.IsNotNull(featureComponents.FeatureSet);
            Assert.IsNotNull(featureComponents.ShapeFileTable);

            string displayName = featureComponents.ShapeFileTable.Name;
            Assert.AreEqual(displayName, "Cities");
        }

        [TestMethod()]
        public void DeleteGeodatabaseTest()
        {
            string fileGeoDBPath = "../../samples/data/Cities.gdb";
            FgdbHelper.DeleteGeodatabase(fileGeoDBPath);
            Geodatabase geodatabaseCreate = Geodatabase.Create(fileGeoDBPath);
            geodatabaseCreate.Close();

            bool isDeleted = FgdbHelper.DeleteGeodatabase(fileGeoDBPath);
            Assert.IsTrue(isDeleted);
        }

        [TestMethod()]
        public void GetDEFeatureDatasetTest()
        {
            string shapeFilePath = "../../samples/data/City/Cities.shp";
            string fileGeoDBPath = "../../samples/data/Cities.gdb";

            FgdbHelper.FeatureComponents featureComponents = FgdbHelper.LoadFeatureComponentsFromShapefile(shapeFilePath).Result;
                       
            string displayName = featureComponents.ShapeFileTable.Name;

            // Delete it, if found.
            FgdbHelper.DeleteGeodatabase(fileGeoDBPath);

            // Create it again for testing.
            Geodatabase geodatabaseCreate = Geodatabase.Create(fileGeoDBPath);
            geodatabaseCreate.Close();

            // Open the geodatabase.
            Geodatabase geodatabase = Geodatabase.Open(fileGeoDBPath);

            // Create new Feature Set.                        
            bool omitXmlDeclation = true;
            string xmlSchema = FgdbHelper.GetDEFeatureDataset(featureComponents.FeatureSet, displayName).Serialize(omitXmlDeclation);
            geodatabase.CreateFeatureDataset(xmlSchema);

            geodatabase.Close();

            // Delete it, if found.
            FgdbHelper.DeleteGeodatabase(fileGeoDBPath);

            Assert.IsNotNull(xmlSchema);

        }

        [TestMethod()]
        public void GetDEFeatureClassTest()
        {
            string shapeFilePath = "../../samples/data/City/Cities.shp";
            string fileGeoDBPath = "../../samples/data/Cities.gdb";

            FgdbHelper.FeatureComponents featureComponents = FgdbHelper.LoadFeatureComponentsFromShapefile(shapeFilePath).Result;
                        
            string displayName = featureComponents.ShapeFileTable.Name;

            // Delete it, if found.
            FgdbHelper.DeleteGeodatabase(fileGeoDBPath);

            // Create it again for testing.
            Geodatabase geodatabaseCreate = Geodatabase.Create(fileGeoDBPath);
            geodatabaseCreate.Close();

            // Open the geodatabase.
            Geodatabase geodatabase = Geodatabase.Open(fileGeoDBPath);

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

            Assert.IsNotNull(table);

            table.Close();
            geodatabase.Close();

            // Delete it, if found.
            FgdbHelper.DeleteGeodatabase(fileGeoDBPath);
        }

        [TestMethod()]
        public void CreateGeoDBFromShapefileTest()
        {
            string shapeFilePath = "../../samples/data/City/Cities.shp";
            string fileGeoDBPath = "../../samples/data/Cities.gdb";

            /*
             * Create a Geodatabase from a Shapefile.
             */
            FgdbHelper.FeatureComponents featureComponents = FgdbHelper.CreateGeoDBFromShapefile(shapeFilePath, fileGeoDBPath);

            Assert.IsNotNull(featureComponents.FeatureLayer);
            Assert.IsNotNull(featureComponents.FeatureSet);
            Assert.IsNotNull(featureComponents.ShapeFileTable);

            string displayName = featureComponents.ShapeFileTable.Name;
            string tableName = displayName;
            Assert.AreEqual(displayName, "Cities");

            // Open the geodatabase.
            Geodatabase geodatabase = Geodatabase.Open(fileGeoDBPath);

            Table table = geodatabase.OpenTable(tableName);
            Assert.IsNotNull(table);

            Envelope env = table.Extent;
            if (!env.IsEmpty)
            {
                var sVal = env.xMax;
            }
            FieldInfo fInfo = table.FieldInformation;
            Assert.IsNotNull(fInfo);

            RowCollection tRows = table.Search("*", "", RowInstance.Recycle);
            Assert.IsNotNull(tRows);
            
            if (tRows.Count() > 0)
            {
                foreach (Row r in tRows)
                {
                    for (int c = 0; c < r.FieldInformation.Count; c++)
                    {
                        string field = r.FieldInformation.GetFieldName(c);
                        Assert.IsNotNull(field);

                        var value = r[field];
                        Assert.IsNotNull(value);

                        Type cType = r[field].GetType();
                        Assert.IsNotNull(cType);
                    }
                }
            }

            table.Close();
            geodatabase.Close();

        }
    }
}