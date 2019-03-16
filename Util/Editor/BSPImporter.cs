#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSPImporter;
using LibBSP;

public class BSPImporterEditor : EditorWindow {
	private string path = "path";
	private string texturePath = "Textures/";
	private bool loadIntoScene = true;
	private bool saveAsPrefab = true;
	private bool combineMeshes = true;
	public static int tesselationLevel = 12;

	public delegate void EntityGameObjectCreatedAction(GameObject gameObject, Entity entity);
	public EntityGameObjectCreatedAction EntityGameObjectCreated;
	
	private static Dictionary<string, Texture2D> textureDict = new Dictionary<string, Texture2D>();
	private static Dictionary<string, Material> materialDict = new Dictionary<string, Material>();
	private static Dictionary<string, GameObject> namedEntities = new Dictionary<string, GameObject>();
	private static Dictionary<GameObject, string> children = new Dictionary<GameObject, string>();
	
	[MenuItem ("Window/BSP Import")]
	public static void ShowWindow() {
		BSPImporterEditor main = (BSPImporterEditor)EditorWindow.GetWindow(typeof(BSPImporterEditor));
		main.autoRepaintOnSceneChange = true;
		UnityEngine.Object.DontDestroyOnLoad(main);
	}
	
	public virtual void OnGUI() {
		EditorGUILayout.BeginVertical(); {
			path = EditorGUILayout.TextField(new GUIContent("Path", "Path to the BSP"), path);
			texturePath = EditorGUILayout.TextField(new GUIContent("Texture path", "Path to textures, starting from Assets."), texturePath);
			loadIntoScene = EditorGUILayout.Toggle(new GUIContent("Load into scene", "Load the BSP directly into the current scene"), loadIntoScene);
			saveAsPrefab = EditorGUILayout.Toggle(new GUIContent("Save as prefab", "Save the BSP as a prefab"), saveAsPrefab);
			combineMeshes = EditorGUILayout.Toggle(new GUIContent("One mesh per entity", "If unchecked, there will be one mesh for each texture used. Uncheck this if you get warnings about too many verts."), combineMeshes);
			tesselationLevel = EditorGUILayout.IntSlider(new GUIContent("Curve detail", "Number of tesselations for curves. 12 is reasonable. Use higher values for smoother curves."), tesselationLevel, 1, 50);
			if(GUILayout.Button("Import")) {
				ReadBSP(path);
			}
		} EditorGUILayout.EndVertical();
	}
	
	public void ReadBSP(string path) {
		if(File.Exists(path)) {
			BSP bsp = new BSP(path);
			ImportBSP(bsp);
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

		foreach(Entity entity in bspObject.entities) {
			GameObject entityGameObject;
			if(entity.ContainsKey("targetname")) {
				entityGameObject = new GameObject(entity["classname"] + " " + entity["targetname"]);
			} else {
				entityGameObject = new GameObject(entity["classname"]);
			}
			children.Add(entityGameObject, entity["parentname"]);
			if (!string.IsNullOrEmpty(entity.name)) {
				namedEntities[entity.name] = entityGameObject;
			}
			entityGameObject.transform.parent = bspGameObject.transform;
			Vector3 origin = BSPUtils.Swizzle(entity.origin * BSPUtils.inch2meterScale);
			Vector3 eulerangles = entity.angles;
			eulerangles.x = -eulerangles.x;
			int modelNumber = entity.modelNumber;
			if(modelNumber > -1) {
			//if (modelNumber == 0) {
				BSPUtils.numVertices = 0;
				List<Face> faces = BSPUtils.GetFacesInModel(bspObject, bspObject.models[modelNumber]);
				if(faces != null && faces.Count > 0) {
					Dictionary<string, List<Mesh>> faceMeshes = new Dictionary<string, List<Mesh>>();
					Dictionary<string, Mesh> textureMeshes = new Dictionary<string, Mesh>();
					foreach(Face currentFace in faces) {
						if(currentFace.numVertices > 0 || currentFace.numEdges > 0) {
						//if ((currentFace.numVertices > 0 || currentFace.numEdges > 0) && currentFace.displacement >= 0) {
							TextureInfo texinfo = null;
							int textureIndex = BSPUtils.GetTextureIndex(bspObject, currentFace);
							if(bspObject.textures[textureIndex].texAxes != null) {
								texinfo = bspObject.textures[textureIndex].texAxes;
							} else {
								if(currentFace.textureInfo > -1) {
									texinfo = bspObject.texInfo[currentFace.textureInfo];
								} else {
									if(currentFace.plane >= 0) { // If not we've hit a Q3 wall. Never mind that, Q3 stores UVs directly.
										Vector3[] axes = TextureInfo.TextureAxisFromPlane(bspObject.planes[currentFace.plane]);
										texinfo = new TextureInfo(axes[0], axes[1], Vector2.zero, Vector2.one, 0, bspObject.FindTexDataWithTexture("tools/toolsclip"), 0);
									}
								}
							}
							int[] triangles = null;
							UIVertex[] vertices = BSPUtils.GetVertices(bspObject, currentFace, origin, ref triangles);
							Vector2[] uvs = new Vector2[vertices.Length];
							if(currentFace.numIndices > 0) {
								triangles = new int[currentFace.numIndices];
								for(int k = 0; k < triangles.Length; k++) {
									triangles[k] = (int)bspObject.indices[currentFace.firstIndex + k];
								}
							}
							Vector3[] meshCorners = new Vector3[vertices.Length];
							for(int k = 0; k < vertices.Length; k++) {
								meshCorners[k] = vertices[k].position;
								uvs[k] = vertices[k].uv0; // On anything but Q3-based engines these will all be zeroes
							}
							Mesh faceMesh = null;
							if(bspObject.version == MapType.CoD ||
								bspObject.version == MapType.CoD2 ||
								bspObject.version == MapType.CoD4 ||
								bspObject.version == MapType.FAKK ||
								bspObject.version == MapType.MOHAA ||
								bspObject.version == MapType.Quake3 ||
								bspObject.version == MapType.Raven ||
								bspObject.version == MapType.STEF2 ||
								bspObject.version == MapType.STEF2Demo) {
								if(currentFace.flags == 2) {
									GameObject patchGO = new GameObject("Bezier patch");
									patchGO.transform.parent = entityGameObject.transform;
									BezierPatch patch = patchGO.AddComponent<BezierPatch>();
									patch.controls = vertices;
									patch.size = currentFace.patchSize;
									patch.CreatePatchMesh(tesselationLevel, materialDict[bspObject.textures[textureIndex].name]);
								} else {
									faceMesh = BSPUtils.Q3BuildFaceMesh(meshCorners, triangles, uvs, entity.origin);
								}
							} else {
								if (currentFace.displacement >= 0) {
									if (textureDict.ContainsKey(bspObject.textures[textureIndex].name)) {
										faceMesh = BSPUtils.BuildDisplacementMesh(meshCorners, triangles, texinfo, bspObject, bspObject.dispInfos[currentFace.displacement], textureDict[bspObject.textures[textureIndex].name]);
									} else {
										faceMesh = BSPUtils.BuildDisplacementMesh(meshCorners, triangles, texinfo, bspObject, bspObject.dispInfos[currentFace.displacement], null);
									}
								} else {
									if (textureDict.ContainsKey(bspObject.textures[textureIndex].name)) {
										faceMesh = BSPUtils.LegacyBuildFaceMesh(meshCorners, triangles, texinfo, entity.origin, textureDict[bspObject.textures[textureIndex].name]);
									} else {
										faceMesh = BSPUtils.LegacyBuildFaceMesh(meshCorners, triangles, texinfo, entity.origin, null);
									}
								}
							}
							if (faceMesh != null) {
								if (!faceMeshes.ContainsKey(bspObject.textures[textureIndex].name)) {
									faceMeshes.Add(bspObject.textures[textureIndex].name, new List<Mesh>());
								}
								faceMeshes[bspObject.textures[textureIndex].name].Add(faceMesh);
							}
						}
					}
					foreach(string key in faceMeshes.Keys) {
						textureMeshes.Add(key, BSPUtils.CombineAllMeshes(faceMeshes[key].ToArray<Mesh>(), entityGameObject.transform, true, false));
					}
					/**/if(BSPUtils.numVertices < 65535 && combineMeshes) { // If we can combine all the faces into one mesh and use a single game object
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
					} else {//*/ // If there's too many vertices, we must treat each face as its own mesh
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
				entityGameObject.transform.eulerAngles = eulerangles;
				BSPUtils.ParseEntity(entity, bspGameObject, entityGameObject);
			}

			entityGameObject.transform.position = origin;

			foreach (var pair in children) {
				if (namedEntities.ContainsKey(pair.Value)) {
					try {
						if (pair.Key == null) {
							Debug.LogError("Child GameObject needs to be parented but it's null?");
							continue;
						}
						if (namedEntities[pair.Value] == null) {
							Debug.LogError("Orphaned GameObject names a parent but none exists!");
							continue;
						}
						pair.Key.transform.parent = namedEntities[pair.Value].transform;
					} catch (System.Exception e) {
						Debug.LogError("Couldn't parent child to parent because " + e);
					}
				}
			}

			if (EntityGameObjectCreated != null) {
				EntityGameObjectCreated(entityGameObject, entity);
			}
		}
		if(saveAsPrefab) {
			Directory.CreateDirectory(Path.Combine(Application.dataPath, "Objects"));
			PrefabUtility.CreatePrefab("Assets/Objects/" + bspObject.MapNameNoExtension + ".prefab", bspGameObject);
		}
		if(!loadIntoScene) {
			GameObject.DestroyImmediate(bspGameObject);
		}
	}

	public static void LoadTextures(BSP bspObject, string texturePath) {
		Shader def = Shader.Find("Standard");
		foreach(LibBSP.Texture texture in bspObject.textures) {
			string relativeTexturePath = Path.Combine("Assets", texturePath, texture.name);
			if (File.Exists(relativeTexturePath + ".png")) {
				textureDict[texture.name] = AssetDatabase.LoadAssetAtPath(relativeTexturePath + ".png", typeof(Texture2D)) as Texture2D;
			}
			else if(File.Exists(relativeTexturePath + ".jpg")) {
				textureDict[texture.name] = AssetDatabase.LoadAssetAtPath(relativeTexturePath + ".jpg", typeof(Texture2D)) as Texture2D;
			}
			else if (File.Exists(relativeTexturePath + ".tga")) {
				textureDict[texture.name] = AssetDatabase.LoadAssetAtPath(relativeTexturePath + ".tga", typeof(Texture2D)) as Texture2D;
			}

			materialDict[texture.name] = new Material(def);

			if(!textureDict.ContainsKey(texture.name) || textureDict[texture.name] == null) {
				Debug.LogWarning("Texture " + relativeTexturePath + " not found! Texture UVs will probably be wrong if imported later.");
			} else {
				materialDict[texture.name].mainTexture = textureDict[texture.name];
			}

			if(texture.name.LastIndexOf('/') > 0) {
				Directory.CreateDirectory(Application.dataPath + "/Materials/" + texturePath + "/" + texture.name.Substring(0, texture.name.LastIndexOf('/')));
			}
			if(!File.Exists(Application.dataPath + "/Materials/" + texturePath + "/" + texture.name + ".mat")) {
				AssetDatabase.CreateAsset(materialDict[texture.name], "Assets/Materials/" + texturePath + "/" + texture.name + ".mat");
			}
		}

		AssetDatabase.Refresh();
	}
	
}
#endif
