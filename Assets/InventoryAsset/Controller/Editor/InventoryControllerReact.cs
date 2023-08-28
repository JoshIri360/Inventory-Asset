using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InventoryController))]
internal class InventoryControllerReact : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // for other non-HideInInspector fields

        InventoryController script = (InventoryController)target;

        if (GUILayout.Button("Initialize Inventories/Update Inventories"))
        {

            EditorApplication.delayCall += script.InitializeInventories;
        }
        else if (GUILayout.Button("Delete All Instantiated Inventories"))
        {
            EditorApplication.delayCall += script.ResetInventory;
        }
    }
}
