#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSPImporter;

public class BSPImporterEditor : EditorWindow {
	private string path = "path";
	private string doomMapName = "map name (Doom only)";
	private string texturePath = "Textures/";
	private bool loadIntoScene = true;
	private bool saveAsPrefab = true;
	private bool combineMeshes = true;
	
	private static Dictionary<string, Texture2D> textureDict = new Dictionary<string, Texture2D>();
	private static Dictionary<string, Material> materialDict = new Dictionary<string, Material>();
	
	[MenuItem ("Window/BSP Import")]
	public static void ShowWindow() {
		BSPImporterEditor main = (BSPImporterEditor)EditorWindow.GetWindow(typeof(BSPImporterEditor));
		main.autoRepaintOnSceneChange = true;
		UnityEngine.Object.DontDestroyOnLoad(main);
		main.Start();
	}
	
	public void Start() {
		
	}
	
	public void OnGUI() {
		EditorGUILayout.BeginVertical(); {
			path = EditorGUILayout.TextField(new GUIContent("Path", "Path to the BSP, starting from Assets"), path);
			doomMapName = EditorGUILayout.TextField(new GUIContent("Map", "Map name within a WAD, for Doom/Doom2/Heretic/Hexen only"), doomMapName);
			texturePath = EditorGUILayout.TextField(new GUIContent("Texture path", "Path to textures, starting from Assets."), texturePath);
			loadIntoScene = EditorGUILayout.Toggle(new GUIContent("Load into scene", "Load the BSP directly into the current scene"), loadIntoScene);
			saveAsPrefab = EditorGUILayout.Toggle(new GUIContent("Save as prefab", "Save the BSP as a prefab"), saveAsPrefab);
			combineMeshes = EditorGUILayout.Toggle(new GUIContent("Combine all meshes in an entity if possible"), combineMeshes);
			if(GUILayout.Button("Import")) {
				ReadBSP(path);
			}
		} EditorGUILayout.EndVertical();
	}
	
	public void ReadBSP(string path) {
		if(File.Exists(path)) {
			BSPReader reader = new BSPReader(path, mapType.TYPE_UNDEFINED);
			reader.readBSP();
			BSP bspObject = reader.BSPData;
			ImportBSP(bspObject);
			textureDict = new Dictionary<string, Texture2D>();
			materialDict = new Dictionary<string, Material>();
		} else {
			Debug.LogError("File " + path + " not found!");
		}
	}
	
	public void ImportBSP(BSP bspObject) {
		RenderSettings.fog = false;
		Directory.CreateDirectory(Application.dataPath + "/Models/" + bspObject.MapNameNoExtension);
		GameObject bspGameObject = new GameObject(bspObject.MapNameNoExtension);

		LoadTextures(bspObject, texturePath);

		foreach(Entity entity in bspObject.Entities) {
			GameObject entityGameObject;
			if(entity.hasAttribute("targetname")) {
				entityGameObject = new GameObject(entity["classname"] + " " + entity["targetname"]);
			} else {
				entityGameObject = new GameObject(entity["classname"]);
			}
			entityGameObject.transform.parent = bspGameObject.transform;
			Vector3 origin = BSPUtils.SwapYZ(entity.Origin * BSPUtils.inch2meterScale);
			Vector3 eulerangles = entity.Angles;
			eulerangles.x = -eulerangles.x;
			int modelNumber = entity.ModelNumber;
			if(modelNumber > -1) {
				BSPUtils.numVertices = 0;
				List<Face> faces = BSPUtils.GetFacesInModel(bspObject, bspObject.Models[modelNumber]);
				if(faces != null && faces.Count > 0) {
					Dictionary<string, List<Mesh>> faceMeshes = new Dictionary<string, List<Mesh>>();
					Dictionary<string, Mesh> textureMeshes = new Dictionary<string, Mesh>();
					foreach(Face currentFace in faces) {
						if(currentFace.NumVertices > 0 || currentFace.NumEdges > 0) {
							TexInfo texinfo = null;
							int textureIndex = BSPUtils.GetTextureIndex(bspObject, currentFace);
							if(bspObject.Textures[textureIndex].TexAxes != null) {
								texinfo = bspObject.Textures[textureIndex].TexAxes;
							} else {
								if(currentFace.TextureScale > -1) {
									texinfo = bspObject.TexInfo[currentFace.TextureScale];
								} else {
									if(currentFace.Plane >= 0) { // If not we've hit a Q3 wall. Never mind that, Q3 stores UVs directly.
										Vector3[] axes = TexInfo.textureAxisFromPlane(bspObject.Planes[currentFace.Plane]);
										texinfo = new TexInfo(axes[0], 0, axes[1], 0, 0, bspObject.findTexDataWithTexture("tools/toolsclip"));
									}
								}
							}
							int[] triangles = null;
							UIVertex[] vertices = BSPUtils.GetVertices(bspObject, currentFace, origin, ref triangles);
							Vector2[] uvs = new Vector2[vertices.Length];
							if(currentFace.NumIndices > 0) {
								triangles = new int[currentFace.NumIndices];
								for(int k = 0; k < triangles.Length; k++) {
									triangles[k] = (int)bspObject.Indices[currentFace.FirstIndex + k];
								}
							}
							Vector3[] meshCorners = new Vector3[vertices.Length];
							for(int k = 0; k < vertices.Length; k++) {
								meshCorners[k] = vertices[k].position;
								uvs[k] = vertices[k].uv0; // On anything but Q3-based engines these will all be zeroes
							}
							Mesh faceMesh;
							if(bspObject.Version == mapType.TYPE_COD ||
								bspObject.Version == mapType.TYPE_COD2 ||
								bspObject.Version == mapType.TYPE_COD4 ||
								bspObject.Version == mapType.TYPE_FAKK ||
								bspObject.Version == mapType.TYPE_MOHAA ||
								bspObject.Version == mapType.TYPE_QUAKE3 ||
								bspObject.Version == mapType.TYPE_RAVEN ||
								bspObject.Version == mapType.TYPE_STEF2 ||
								bspObject.Version == mapType.TYPE_STEF2DEMO) {
								faceMesh = BSPUtils.Q3BuildFaceMesh(meshCorners, triangles, uvs, entity.Origin, null);
							} else {
								if(textureDict.ContainsKey(bspObject.Textures[textureIndex].Name)) {
									faceMesh = BSPUtils.LegacyBuildFaceMesh(meshCorners, triangles, texinfo, entity.Origin, textureDict[bspObject.Textures[textureIndex].Name]);
								} else {
									faceMesh = BSPUtils.LegacyBuildFaceMesh(meshCorners, triangles, texinfo, entity.Origin, null);
								}
							}
							if(!faceMeshes.ContainsKey(bspObject.Textures[textureIndex].Name)) {
								faceMeshes.Add(bspObject.Textures[textureIndex].Name, new List<Mesh>());
							}
							faceMeshes[bspObject.Textures[textureIndex].Name].Add(faceMesh);
						}
					}
					foreach(string key in faceMeshes.Keys) {
						textureMeshes.Add(key, BSPUtils.CombineAllMeshes(faceMeshes[key].ToArray<Mesh>(), entityGameObject.transform, true, false));
					}
					if(BSPUtils.numVertices < 65535 && combineMeshes) { // If we can combine all the faces into one mesh and use a single game object
						Mesh entityMesh = BSPUtils.CombineAllMeshes(textureMeshes.Values.ToArray<Mesh>(), entityGameObject.transform, false, false);
						MeshFilter filter = entityGameObject.AddComponent<MeshFilter>();
						filter.mesh = entityMesh;
						Material[] sharedMaterials = new Material[textureMeshes.Count];
						for(int i = 0; i < textureMeshes.Count; i++) {
							sharedMaterials[i] = materialDict[textureMeshes.Keys.ToArray<string>()[i]];
						}
						entityGameObject.AddComponent<MeshRenderer>().sharedMaterials = sharedMaterials;
						entityGameObject.AddComponent<MeshCollider>();
						AssetDatabase.CreateAsset(filter.sharedMesh, "Assets/Models/" + bspObject.MapNameNoExtension + "/mesh" + (filter.sharedMesh.GetHashCode()) + ".asset");
					} else { // If there's too many vertices, we must treat each face as its own mesh
						foreach(string key in textureMeshes.Keys) {
							GameObject textureMeshGO = new GameObject(key);
							textureMeshGO.transform.parent = entityGameObject.transform;
							MeshFilter filter = textureMeshGO.AddComponent<MeshFilter>();
							filter.mesh = textureMeshes[key];
							textureMeshGO.AddComponent<MeshRenderer>().sharedMaterial = materialDict[key];
							textureMeshGO.AddComponent<MeshCollider>();
							AssetDatabase.CreateAsset(filter.sharedMesh, "Assets/Models/" + bspObject.MapNameNoExtension + "/mesh" + (filter.sharedMesh.GetHashCode()) + ".asset");
						}
					}
				}
			} else {
				entityGameObject.transform.position = origin;
				entityGameObject.transform.eulerAngles = eulerangles;
				BSPUtils.ParseEntity(entity, bspGameObject, entityGameObject);
			}
		}
		if(saveAsPrefab) {
			PrefabUtility.CreatePrefab("Assets/Objects/" + bspObject.MapNameNoExtension + ".prefab", bspGameObject);
		}
		if(!loadIntoScene) {
			GameObject.DestroyImmediate(bspGameObject);
		}
	}

	public static void LoadTextures(BSP bspObject, string texturePath) {
		Shader def = Shader.Find("Transparent/Cutout/Diffuse");
		foreach(Texturedef texture in bspObject.Textures) {
			string globalTexturePath = Application.dataPath + "/" + texturePath + "/" + texture.Name;
			if(!File.Exists(globalTexturePath + ".png")) {
				if(!File.Exists(globalTexturePath + ".jpg")) {
					if(!File.Exists(globalTexturePath + ".tga")) {
						Debug.Log(globalTexturePath + " does not exist or is not in JPG, PNG or TGA format!");
					} else {
						textureDict[texture.Name] = AssetDatabase.LoadAssetAtPath("Assets/" + texturePath + "/" + texture.Name + ".tga", typeof(Texture2D)) as Texture2D;
					}
				} else {
					textureDict[texture.Name] = AssetDatabase.LoadAssetAtPath("Assets/" + texturePath + "/" + texture.Name + ".jpg", typeof(Texture2D)) as Texture2D;
				}
			} else {
				textureDict[texture.Name] = AssetDatabase.LoadAssetAtPath("Assets/" + texturePath + "/" + texture.Name + ".png", typeof(Texture2D)) as Texture2D;
			}

			materialDict[texture.Name] = new Material(def);

			if(!textureDict.ContainsKey(texture.Name) || textureDict[texture.Name] == null) {
				Debug.LogWarning("Texture " + texture.Name + " not found! Texture scaling will probably be wrong if imported later.");
			} else {
				materialDict[texture.Name].mainTexture = textureDict[texture.Name];
			}

			if(texture.Name.LastIndexOf('/') > 0) {
				Directory.CreateDirectory(Application.dataPath + "/Materials/" + texturePath + "/" + texture.Name.Substring(0, texture.Name.LastIndexOf('/')));
			}
			if(!File.Exists(Application.dataPath + "/Materials/" + texturePath + "/" + texture.Name + ".mat")) {
				AssetDatabase.CreateAsset(materialDict[texture.Name], "Assets/Materials/" + texturePath + "/" + texture.Name + ".mat");
			}
		}

		AssetDatabase.Refresh();
	}
	
}
#endif