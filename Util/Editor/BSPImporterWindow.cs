#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSPImporter;
using LibBSP;

/// <summary>
/// Editor window for importing BSPs, a simple example of how to provide a GUI
/// for importing a BSP. This class can be deleted without causing any problems.
/// </summary>
public class BSPImporterWindow : EditorWindow {

	protected BSPLoader.Settings settings;

	/// <summary>
	/// Shows this window.
	/// </summary>
	[MenuItem("BSP Importer/Import BSP")]
	public static void ShowWindow() {
		BSPImporterWindow window = GetWindow<BSPImporterWindow>();
#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3_OR_NEWER
		window.titleContent = new GUIContent("Example BSP Importer GUI");
#else
		window.title = "Example BSP Importer GUI";
#endif
		window.autoRepaintOnSceneChange = true;
		DontDestroyOnLoad(window);
	}

	/// <summary>
	/// GUI for this window.
	/// </summary>
	protected virtual void OnGUI() {
		EditorGUILayout.BeginVertical(); {
			DrawImportOptions();
			DrawImportButton();
		} EditorGUILayout.EndVertical();
	}

	/// <summary>
	/// Draws GUI elements for BSP Importer settings.
	/// </summary>
	protected virtual void DrawImportOptions() {
		if (settings.path == null) {
			settings.path = "";
			settings.meshCombineOptions = BSPLoader.MeshCombineOptions.PerEntity;
			settings.curveTessellationLevel = 9;
		}
		EditorGUILayout.BeginHorizontal(); {
			settings.path = EditorGUILayout.TextField(new GUIContent("Import BSP file", "The path to a BSP file on the hard drive."), settings.path);
			if (GUILayout.Button("Browse...", GUILayout.MaxWidth(100))) {
				string dir = string.IsNullOrEmpty(settings.path) ? "." : Path.GetDirectoryName(settings.path);
#if UNITY_5_2 || UNITY_5_3_OR_NEWER
				string[] filters = {
					"BSP Files", "BSP",
					"D3DBSP Files", "D3DBSP",
					"All Files", "*",
				};

				settings.path = EditorUtility.OpenFilePanelWithFilters("Select BSP file", dir, filters);
#else
				settings.path = EditorUtility.OpenFilePanel("Select BSP file", dir, "*BSP");
#endif
			}
		} EditorGUILayout.EndHorizontal();

		if (settings.texturePath == null) {
			settings.texturePath = "";
		}
		EditorGUILayout.BeginHorizontal(); {
			settings.texturePath = EditorGUILayout.TextField(new GUIContent("Import Texture path", "Path to textures to use, either relative to /Assets/ or anywhere on the hard drive."), settings.texturePath);
			if (GUILayout.Button("Browse...", GUILayout.MaxWidth(100))) {
				settings.texturePath = EditorUtility.OpenFolderPanel("Find texture path", settings.texturePath, "textures");
			}
		} EditorGUILayout.EndHorizontal();

		if (settings.materialPath == null) {
			settings.materialPath = "";
		}
		EditorGUILayout.BeginHorizontal(); {
			settings.materialPath = EditorGUILayout.TextField(new GUIContent("Unity Material save path", "Path to save/load materials, relative to /Assets/"), settings.materialPath);
			if (GUILayout.Button("Browse...", GUILayout.MaxWidth(100))) {
				settings.materialPath = EditorUtility.OpenFolderPanel("Find material path", settings.materialPath, "materials");
			}
		} EditorGUILayout.EndHorizontal();

		if (settings.meshPath == null) {
			settings.meshPath = "";
		}
		EditorGUILayout.BeginHorizontal(); {
			settings.meshPath = EditorGUILayout.TextField(new GUIContent("Unity Mesh save path", "Path to save meshes, relative to /Assets/"), settings.meshPath);
			if (GUILayout.Button("Browse...", GUILayout.MaxWidth(100))) {
				settings.meshPath = EditorUtility.OpenFolderPanel("Find mesh path", settings.meshPath, "models");
			}
		} EditorGUILayout.EndHorizontal();
			
		settings.meshCombineOptions = (BSPLoader.MeshCombineOptions)EditorGUILayout.EnumPopup(new GUIContent("Mesh combining", "Options for combining meshes. Per entity gives the cleanest hierarchy but may corrupt meshes with too many vertices."), settings.meshCombineOptions);
		settings.assetSavingOptions = (BSPLoader.AssetSavingOptions)EditorGUILayout.EnumPopup(new GUIContent("Assets to save", "Which assets to save into the project, at edit-time only."), settings.assetSavingOptions);
		settings.curveTessellationLevel = EditorGUILayout.IntSlider(new GUIContent("Curve detail", "Number of triangles used to tessellate curves. Higher values give smoother curves with exponentially more vertices."), settings.curveTessellationLevel, 1, 50);
	}

	/// <summary>
	/// Draws a button to start the import process.
	/// </summary>
	protected virtual void DrawImportButton() {
		if (GUILayout.Button("Import")) {
			BSPLoader loader = new BSPLoader() {
				settings = settings
			};
			loader.LoadBSP();
		}
	}

}
#endif
