## En detalle:
`FamilyParameter.IsShared` returns a true or false value to indicate if the given family parameter is a shared parameter.

In the example below, the current Revit document (a door family from the sample model) is open. The parameters in the family are returned, along with a value representing if it is shared. `List Create` and `List.Transpose` are used to combine the values in sublists.
___
## Archivo de ejemplo

![FamilyParameter.IsShared](./Revit.Elements.FamilyParameter.IsShared_img.jpg)