using CSharp.FgdbApi.Utilities;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace CSharp.FgdbApi.Schema
{
    partial class Field
    {
        [XmlAttribute(Namespace = XmlSchema.InstanceNamespace)]
        public string type = Constants.Esri + Constants.Colon + typeof(Field).Name;                    
    }    
    partial class GeometryDef
    {
        [XmlAttribute(Namespace = XmlSchema.InstanceNamespace)]
        public string type = Constants.Esri + Constants.Colon + typeof(GeometryDef).Name;
    }   
    partial class Fields
    {        
        [XmlAttribute(Namespace = XmlSchema.InstanceNamespace)]
        public string type = Constants.Esri + Constants.Colon + typeof(Fields).Name;
        
        [XmlElement(Form = XmlSchemaForm.Unqualified, ElementName = Constants.FieldArray, IsNullable = false)]        
        public FieldArray FieldArrayProperty { get; set; }
    }    
    public class FieldArray : ArrayOfField
    {        
        [XmlAttribute(Namespace = XmlSchema.InstanceNamespace)]
        public string type = Constants.Esri + Constants.Colon + typeof(ArrayOfField).Name;
    }
    partial class Index
    {
        [XmlAttribute(Namespace = XmlSchema.InstanceNamespace)]
        public string type = Constants.Esri + Constants.Colon + typeof(Index).Name;
    }
    partial class Indexes
    {
        [XmlAttribute(Namespace = XmlSchema.InstanceNamespace)]
        public string type = Constants.Esri + Constants.Colon + typeof(Indexes).Name;

        [XmlElement(Form = XmlSchemaForm.Unqualified, ElementName = Constants.IndexArray, IsNullable = false)]
        public IndexArray IndexArrayProperty { get; set; }
    }
    public class IndexArray : ArrayOfIndex
    {
        [XmlAttribute(Namespace = XmlSchema.InstanceNamespace)]
        public string type = Constants.Esri + Constants.Colon + typeof(ArrayOfIndex).Name;
    }
    
    partial class DETable
    {        
        [XmlElement(Form = XmlSchemaForm.Unqualified, ElementName = Constants.RelationshipClassNames, IsNullable = false, Order = 26)]
        public ArrayOfName RelationshipClassNamesProperty { get; set; }

        [XmlElement(Form = XmlSchemaForm.Unqualified, ElementName = Constants.ExtensionProperties, IsNullable = false, Order = 27)]
        public ArrayOfPropertySet ExtensionPropertiesProperty { get; set; }

        [XmlElement(Form = XmlSchemaForm.Unqualified, ElementName = Constants.ControllerMemberships, IsNullable = false, Order = 28)]
        public ArrayOfController ControllerMembershipsProperty { get; set; }
    }

    public class ArrayOfName
    {
        [XmlAttribute(Namespace = XmlSchema.InstanceNamespace)]
        public string type = Constants.Esri + Constants.Colon + typeof(Names).Name;

        private List<string> nameField;

        public ArrayOfName()
        {
            this.nameField = new List<string>();
        }

        [XmlElement("Name", Form = XmlSchemaForm.Unqualified)]
        public List<string> Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }
    }
    public class ArrayOfPropertySet : PropertySet
    {
        [XmlAttribute(Namespace = XmlSchema.InstanceNamespace)]
        public string type = Constants.Esri + Constants.Colon + typeof(PropertySet).Name;

        private PropertyArray arrayOfPropertySetField;

        public ArrayOfPropertySet()
        {
            this.arrayOfPropertySetField = new PropertyArray();
        }

        [XmlElement(Form = XmlSchemaForm.Unqualified, ElementName = Constants.PropertyArray, IsNullable = false)]
        public PropertyArray ArrayOfPropertyArray
        {
            get
            {
                return this.arrayOfPropertySetField;
            }
            set
            {
                this.arrayOfPropertySetField = value;
            }
        }
    }
    public class PropertyArray : ArrayOfPropertySetProperty
    {
        [XmlAttribute(Namespace = XmlSchema.InstanceNamespace)]
        public string type = Constants.Esri + Constants.Colon + typeof(ArrayOfPropertySetProperty).Name;
    }

    public class ArrayOfController : ArrayOfControllerMembership
    {
        [XmlAttribute(Namespace = XmlSchema.InstanceNamespace)]
        public string type = Constants.Esri + Constants.Colon + typeof(ArrayOfControllerMembership).Name;
    }
}
