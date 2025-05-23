﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;
using DSRevitNodesUI;
using Autodesk.Revit.DB;
using CoreNodeModels;
using RVT = Autodesk.Revit.DB;
using RevitServices.Persistence;
using RevitServices.Transactions;

using Dynamo.Utilities;
using Dynamo.Models;
using Dynamo.Graph.Nodes;
using ProtoCore.AST.AssociativeAST;

namespace DSRevitNodesUI
{

    /// <summary>
    /// Generic UI Dropdown node baseclass for Revit Elements.
    /// This class populates a dropdown with all Revit elements of the specified type.
    /// It uses a filtered element collector filtering by class.
    /// </summary>
    public abstract class CustomRevitElementDropDown : RevitDropDownBase
    {
        /// <summary>
        /// Generic Revit Element Class Dropdown Node
        /// </summary>
        /// <param name="name">Name of the Node</param>
        /// <param name="elementType">Type of Revit Element to display</param>
        public CustomRevitElementDropDown(string name, Type elementType) : base(name)
        {
            this.ElementType = elementType;
            PopulateDropDownItems();
        }

        [JsonConstructor]
        public CustomRevitElementDropDown(string name, Type elementType, IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) 
            : base(name, inPorts, outPorts)
        {
            this.ElementType = elementType;
            PopulateDropDownItems();
        }

        /// <summary>
        /// Type of Element
        /// </summary>
        private Type ElementType
        {
            get;
            set;
        }

        protected override CoreNodeModels.DSDropDownBase.SelectionState PopulateItemsCore(string currentSelection)
        {
            PopulateDropDownItems();
            return SelectionState.Done;
        }
        /// <summary>
        /// Populate the Dropdown menu
        /// </summary>
        public void PopulateDropDownItems()
        {
            if (this.ElementType != null)
            {
                // Clear the Items
                Items.Clear();

                // If the active doc is null, throw an exception
                if (DocumentManager.Instance.CurrentDBDocument == null)
                {
                    throw new Exception(Properties.Resources.NoActiveDocumentFound);
                }

                // Set up a new element collector using the Type field
                var fec = new RVT.FilteredElementCollector(DocumentManager.Instance.CurrentDBDocument).OfClass(ElementType);

                // If there is nothing in the collector add the missing Type message to the Dropdown menu.
                if (fec.ToElements().Count == 0)
                {
                    Items.Add(new CoreNodeModels.DynamoDropDownItem(Properties.Resources.NoTypesFound, null));
                    SelectedIndex = 0;
                    return;
                }

                // if the elementtype is a RebarHookType add an initial "None" value which does not come from the collector
                if (this.ElementType.FullName == "Autodesk.Revit.DB.Structure.RebarHookType") Items.Add(new CoreNodeModels.DynamoDropDownItem(Properties.Resources.None, null));

                // Walk through all elements in the collector and add them to the dropdown
                foreach (var ft in fec.ToElements())
                {
                    Items.Add(new CoreNodeModels.DynamoDropDownItem(ft.Name, ft));
                }

                Items = Items.OrderBy(x => x.Name).ToObservableCollection();
            }
        }

        /// <summary>
        /// Cast the selected element to a dynamo node
        /// </summary>
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // If there are no elements in the dropdown or the selected Index is invalid return a Null node.
            if(!CanBuildOutputAst(Properties.Resources.NoTypesFound,Properties.Resources.None))
               return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };

            // Cast the selected object to a Revit Element and get its Id
            Autodesk.Revit.DB.ElementId Id = ((Autodesk.Revit.DB.Element)Items[SelectedIndex].Item).Id;
            
            // Select the element using the elementIds Integer Value
            var node = AstFactory.BuildFunctionCall("Revit.Elements.ElementSelector", "ByElementId",
                new List<AssociativeNode> { AstFactory.BuildIntNode(Id.Value) });

            // Return the selected element
            return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), node) };
        }
    }

    /// <summary>
    /// Generic UI Dropdown node baseclass for Enumerations.
    /// This class populates a dropdown with all enumeration values of the specified type.
    /// </summary>
    public abstract class CustomGenericEnumerationDropDown : RevitDropDownBase
    {
        /// <summary>
        /// Generic Enumeration Dropdown
        /// </summary>
        /// <param name="name">Node Name</param>
        /// <param name="enumerationType">Type of Enumeration to Display</param>
        public CustomGenericEnumerationDropDown(string name, Type enumerationType) : base(name)
        {
            this.EnumerationType = enumerationType;
            PopulateDropDownItems();
        }

        [JsonConstructor]
        public CustomGenericEnumerationDropDown(string name, Type enumerationType, IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts) 
            : base(name, inPorts, outPorts)
        {
            this.EnumerationType = enumerationType;
            PopulateDropDownItems();
        }

        /// <summary>
        /// Type of Enumeration
        /// </summary>
        private Type EnumerationType
        {
            get;
            set;
        }

        protected override CoreNodeModels.DSDropDownBase.SelectionState PopulateItemsCore(string currentSelection)
        {
            PopulateDropDownItems();
            return SelectionState.Done;
        }

        /// <summary>
        /// Populate Items in Dropdown menu
        /// </summary>
        public void PopulateDropDownItems()
        {
            if (this.EnumerationType != null && this.EnumerationType.IsEnum)
            {
                // Clear the dropdown list
                Items.Clear();

                // Get all enumeration names and add them to the dropdown menu
                foreach (string name in Enum.GetNames(EnumerationType))
                {
                    Items.Add(new CoreNodeModels.DynamoDropDownItem(name, Enum.Parse(EnumerationType, name)));
                }

                Items = Items.OrderBy(x => x.Name).ToObservableCollection();
            }
        }

        /// <summary>
        /// Assign the selected Enumeration value to the output
        /// </summary>
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // If the dropdown is still empty try to populate it again          
            if (Items.Count == 0 || Items.Count == -1)
            {
                if (this.EnumerationType != null && Enum.GetNames(this.EnumerationType).Length > 0)
                {
                    PopulateItems();
                }
            }

            // If there are no elements in the dropdown or the selected Index is invalid return a Null node.
            if(!CanBuildOutputAst(Properties.Resources.NoTypesFound,Properties.Resources.None))
               return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };

            // get the selected items name
            var stringNode = AstFactory.BuildStringNode((string)Items[SelectedIndex].Name);

            // assign the selected name to an actual enumeration value
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), stringNode);

            // return the enumeration value
            return new List<AssociativeNode> { assign };
        }
    }

    public abstract class CustomForgeTypeIdDropDown : RevitDropDownBase
    {
        public CustomForgeTypeIdDropDown(string name, Type type) : base(name)
        {
            this.EnumerationType = type;
            PopulateDropDownItems();
        }

        [JsonConstructor]
        public CustomForgeTypeIdDropDown(string name, Type type, IEnumerable<PortModel> inPorts, IEnumerable<PortModel> outPorts)
            : base(name, inPorts, outPorts)
        {
            this.EnumerationType = type;
            PopulateDropDownItems();
        }

        /// <summary>
        /// Type of Class
        /// </summary>
        private Type EnumerationType
        {
            get;
            set;
        }

        protected override CoreNodeModels.DSDropDownBase.SelectionState PopulateItemsCore(string currentSelection)
        {
            PopulateDropDownItems();
            return SelectionState.Done;
        }

        private Dictionary<string, string> ShortForgeIdMap = new Dictionary<string, string>();

        /// <summary>
        /// Populate Items in Dropdown menu
        /// </summary>
        public void PopulateDropDownItems()
        {
            if (this.EnumerationType != null)
            {
                // Clear the dropdown list
                Items.Clear();
                ShortForgeIdMap.Clear();

                Func<ForgeTypeId,string> getLabelFunc = null;

                if (this.EnumerationType == typeof(SpecTypeId))
                {
                    getLabelFunc = LabelUtils.GetLabelForSpec;
                }
                else if (this.EnumerationType == typeof(GroupTypeId))
                {
                    getLabelFunc = LabelUtils.GetLabelForGroup;
                }
                //Future handlers can be added when other types are exposed in the Dynamo UI

                var properties = EnumerationType.GetProperties();
                foreach (var property in properties)
                {
                    var obj = property.GetValue(null, null);
                    if (obj is ForgeTypeId forgeTypeId && getLabelFunc != null)
                    {
                        try
                        {
                            // For SpecTypeId.Custom, it is "Unrecognized custom spec", and not be identified.
                            // It doesn't need to display in DropDown List.
                            if (forgeTypeId.Equals(SpecTypeId.Custom))
                            {
                                continue;
                            }
                            var name = getLabelFunc(forgeTypeId);
                            var shortTypeId = GetShortForgeTypeId(forgeTypeId.TypeId);
                            if (String.IsNullOrEmpty(shortTypeId))
                            {
                                continue;
                            }

                            ShortForgeIdMap[shortTypeId] = forgeTypeId.TypeId;
                            Items.Add(new CoreNodeModels.DynamoDropDownItem(name, shortTypeId));
                        }
                        catch
                        {
                            //Continue to next property
                        }
                        
                    }
                }

                var types = this.EnumerationType.GetNestedTypes();
                foreach (var type in types)
                {
                    var nestProperties = type.GetProperties();
                    foreach (var nestProperty in nestProperties)
                    {
                        var obj = nestProperty.GetValue(null, null);
                        if (obj is ForgeTypeId forgeTypeId && getLabelFunc != null)
                        {
                            try
                            {
                                var name = getLabelFunc(forgeTypeId);
                                var shortTypeId = GetShortForgeTypeId(forgeTypeId.TypeId);
                                if (String.IsNullOrEmpty(shortTypeId))
                                {
                                    continue;
                                }

                                ShortForgeIdMap[shortTypeId] = forgeTypeId.TypeId;
                                Items.Add(new CoreNodeModels.DynamoDropDownItem(name, shortTypeId));
                            }
                            catch
                            {
                                //Continue to next property
                            }
                        }
                    }
                }

                if (Items.Count <= 0)
                {
                    Items.Add(new CoreNodeModels.DynamoDropDownItem(Properties.Resources.NoTypesFound, null));
                    SelectedIndex = 0;
                    return;
                }
                Items = Items.OrderBy(x => x.Name).ToObservableCollection();
            }
        }

        private string GetShortForgeTypeId(string TypeId)
        {
            var strings = TypeId.Split('-');

            return strings.Length == 2 ? strings[0] : String.Empty;
        }

        /// <summary>
        /// Override the default behavior to serialize typeID 
        /// instead of ForgeTypeId name.
        /// </summary>
        /// <param name="item">Selected DynamoDropDownItem</param>
        /// <returns></returns>
        protected override string GetSelectedStringFromItem(DynamoDropDownItem item)
        {
            return item == null ? string.Empty : item.Item.ToString();
        }

        /// <summary>
        /// Assign the selected Enumeration value to the output
        /// </summary>
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // If the dropdown is still empty try to populate it again          
            if (Items.Count == 0 || Items.Count == -1)
            {
                if (this.EnumerationType != null)
                {
                    PopulateItems();
                }
            }

            // If there are no elements in the dropdown or the selected Index is invalid return a Null node.
            if (!CanBuildOutputAst(Properties.Resources.NoTypesFound, Properties.Resources.None))
                return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
            
            var args = new List<AssociativeNode>
            {
                AstFactory.BuildStringNode(ShortForgeIdMap[(string)Items[SelectedIndex].Item])
            };

            var functionCall = GetAstFunction(args);

            // assign the selected name to an actual enumeration value
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), functionCall);

            // return the enumeration value
            return new List<AssociativeNode> { assign };
        }

        public abstract AssociativeNode GetAstFunction(List<AssociativeNode> args);
        //{
        //    return AstFactory.BuildFunctionCall<String, Revit.Elements.ForgeType>(<<DerivedType>>.ByTypeId, args);
        //}
    }
}
