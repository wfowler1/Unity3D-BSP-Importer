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
/// Editor window for importing BSPs. Can be inheretited for more control.
/// </summary>
public class BSPImporterWindow : EditorWindow {

	protected BSPLoader.Settings settings;

	/// <summary>
	/// Shows this window.
	/// </summary>
	[MenuItem("BSP Importer/Import BSP")]
	public static void ShowWindow() {
		BSPImporterWindow window = GetWindow<BSPImporterWindow>();
#if UNITY_5 || UNITY_5_3_OR_NEWER
		window.titleContent = new GUIContent("BSP Importer");
#else
		window.title = "BSP Importer";
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
		}
		EditorGUILayout.BeginHorizontal(); {
			settings.path = EditorGUILayout.TextField(new GUIContent("BSP file", "The path to a BSP file on the hard drive."), settings.path);
			if (GUILayout.Button("Browse...", GUILayout.MaxWidth(100))) {
				settings.path = EditorUtility.OpenFilePanel("Select BSP file", (string.IsNullOrEmpty(settings.path) ? "." : Path.GetDirectoryName(settings.path)), "BSP Files;*.BSP;*.D3DBSP");
			}
		} EditorGUILayout.EndHorizontal();

		if (settings.texturePath == null) {
			settings.texturePath = "";
		}
		EditorGUILayout.BeginHorizontal(); {
			settings.texturePath = EditorGUILayout.TextField(new GUIContent("Texture path", "Path to textures to use, either relative to /Assets/ or anywhere on the hard drive."), settings.texturePath);
			if (GUILayout.Button("Browse...", GUILayout.MaxWidth(100))) {
				settings.texturePath = EditorUtility.OpenFolderPanel("Find texture path", settings.texturePath, "textures");
			}
		} EditorGUILayout.EndHorizontal();

		if (settings.materialPath == null) {
			settings.materialPath = "";
		}
		EditorGUILayout.BeginHorizontal(); {
			settings.materialPath = EditorGUILayout.TextField(new GUIContent("Materials path", "Path to save/load materials, relative to /Assets/"), settings.materialPath);
			if (GUILayout.Button("Browse...", GUILayout.MaxWidth(100))) {
				settings.materialPath = EditorUtility.OpenFolderPanel("Find material path", settings.materialPath, "materials");
			}
		} EditorGUILayout.EndHorizontal();

		if (settings.meshPath == null) {
			settings.meshPath = "";
		}
		EditorGUILayout.BeginHorizontal(); {
			settings.meshPath = EditorGUILayout.TextField(new GUIContent("Mesh path", "Path to save meshes, relative to /Assets/"), settings.meshPath);
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
