#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Linq;
using NWH.Common.Vehicles;
using UnityEditorInternal;

namespace NWH.VehiclePhysics2
{
    public class OptionalPackageWizard : EditorWindow
    {

        public static string basePath = "Assets/NWH/Vehicle Physics 2/_OptionalPackages/";
        public static string[] assetPaths = new string[]
                                     {
                                         "Input/SteeringWheelInput",
                                         "Input/Rewired",
                                         "Multiplayer/Mirror",
                                         "Multiplayer/PUN2"
                                     };

        private int dropdownSelection;


        [MenuItem("Tools/NWH/Optional Package Wizard", priority = 12)]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            OptionalPackageWizard window = (OptionalPackageWizard)EditorWindow.GetWindow(typeof(OptionalPackageWizard));
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Individual Package Import");
            dropdownSelection = EditorGUILayout.Popup(dropdownSelection, assetPaths);
            if (GUILayout.Button("Import Selected Package"))
            {
                ImportPackage(assetPaths[dropdownSelection]);
            }
            if (GUILayout.Button("Remove Selected Package Folder"))
            {
                RemovePackageFolder(assetPaths[dropdownSelection]);
            }

            GUILayout.Space(20);
            GUILayout.Label("Batch Actions");
            if (GUILayout.Button("Import All Packages"))
            {
                ImportAllPackages();
            }

            if (GUILayout.Button("Export All Packages"))
            {
                ExportAllPackages();
            }

            if (GUILayout.Button("Remove All Imported Package Folders"))
            {
                RemoveImportedPackageFolders();
            }
        }


        private static void ImportAllPackages()
        {
            foreach (string assetPath in assetPaths)
            {
                ImportPackage(assetPath);
            }
        }


        private static void ImportPackage(string assetPath)
        {
            string fullPath = basePath + assetPath + ".unitypackage";
            Debug.Log($"Import '{fullPath}'.");
            AssetDatabase.ImportPackage(fullPath, false);
            AssetDatabase.Refresh();
        }


        private static void ExportAllPackages()
        {
            foreach (string assetPath in assetPaths)
            {
                ExportPackage(assetPath);
            }
        }


        private static void ExportPackage(string assetPath)
        {
            string folderPath = basePath + assetPath;
            string filename = folderPath + ".unitypackage";
            Debug.Log($"Export '{folderPath}' to '{filename}'.");
            AssetDatabase.ExportPackage(folderPath, filename, ExportPackageOptions.Recurse);
            AssetDatabase.Refresh();
        }


        private static void RemoveImportedPackageFolders()
        {
            foreach (string assetPath in assetPaths)
            {
                RemovePackageFolder(assetPath);
            }
        }


        public static void RemovePackageFolder(string assetPath)
        {
            string folderPath = basePath + assetPath;
            FileUtil.DeleteFileOrDirectory(folderPath);
            Debug.Log($"Delete '{folderPath}'.");
            AssetDatabase.Refresh();
        }
    }
}

#endif
