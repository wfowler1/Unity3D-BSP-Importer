#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class BSPImporter : EditorWindow {
	private string path = "path";
	private string doomMapName = "map name (Doom only)";
	private string texturePath = "Textures/Resources";
	private bool loadIntoScene = true;
	private bool saveAsPrefab = true;
	private bool combineMeshes = true;
	
	private Dictionary<string, Texture2D> textureDict = new Dictionary<string, Texture2D>();
	private Dictionary<string, Material> materialDict = new Dictionary<string, Material>();
	
	[MenuItem ("Window/BSP Import")]
	public static void ShowWindow() {
		BSPImporter main = (BSPImporter)EditorWindow.GetWindow(typeof(BSPImporter));
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
			texturePath = EditorGUILayout.TextField(new GUIContent("Texture path", "Path to textures, starting from Assets. MUST be in a Resources folder."), texturePath);
			loadIntoScene = EditorGUILayout.Toggle(new GUIContent("Load into scene", "Load the BSP directly into the current scene"), loadIntoScene);
			saveAsPrefab = EditorGUILayout.Toggle(new GUIContent("Save as prefab", "Save the BSP as a prefab"), saveAsPrefab);
			combineMeshes = EditorGUILayout.Toggle(new GUIContent("Combine Meshes", "Disable this if you get a \"count <= std::numeric_limits<UInt16>::max()\" error"), combineMeshes);
			if(GUILayout.Button("Import")) {
				ReadBSP(path);
			}
		} EditorGUILayout.EndVertical();
	}
	
	public void ReadBSP(string path) {
		if(File.Exists(Application.dataPath + "/" + path)) {
			if(!texturePath.Contains("Resources")) {
				Debug.LogWarning("WARNING: Texture path does not contain a Resources folder! Won't be able to load any textures!");
			}
			BSPReader reader = new BSPReader(Application.dataPath + "/" + path, mapType.TYPE_UNDEFINED);
			reader.readBSP();
			BSP bspObject = reader.BSPData;
			LoadBSP42(bspObject);
		} else {
			Debug.LogError("File "+ Application.dataPath + "/" + path +" not found!");
		}
	}
	
	public void LoadBSP42(BSP bspObject) {
		RenderSettings.fog = false;
		Directory.CreateDirectory(Application.dataPath+"/Models/"+bspObject.MapNameNoExtension);
		Shader def = Shader.Find("Diffuse");
		//Shader transparent = Shader.Find("Transparent/Cutout/Diffuse");
		GameObject bspGameObject = new GameObject(bspObject.MapNameNoExtension);
		
		foreach(Texture texture in bspObject.Textures) {
			string globalTexturePath = Application.dataPath+"/"+texturePath+"/"+texture.Name+".png";
			if(!File.Exists(globalTexturePath)) {
				Debug.Log(globalTexturePath+" does not exist!");
			}
			string resourcesPath = globalTexturePath.Substring(globalTexturePath.IndexOf("Resources")+9);
			if(resourcesPath[0] == '/' || resourcesPath[0] == '\\') {
				resourcesPath = resourcesPath.Substring(1);
			}
			textureDict[texture.Name] = Resources.Load(resourcesPath.Substring(0,resourcesPath.Length-4), typeof(Texture2D)) as Texture2D;
			if(textureDict[texture.Name] == null) {
				Debug.LogWarning("Texture "+texture.Name + " not found! Texture scaling will probably be wrong if imported later.");
			}
			materialDict[texture.Name] = new Material(def);
			materialDict[texture.Name].mainTexture = textureDict[texture.Name];
			Directory.CreateDirectory(Application.dataPath+"/Materials/Resources/nightfire/"+texture.Name.Substring(0,texture.Name.IndexOf('/')));
			if(!File.Exists(Application.dataPath+"/Materials/Resources/nightfire/"+texture.Name+".mat")) {
				AssetDatabase.CreateAsset(materialDict[texture.Name], "Assets/Materials/Resources/nightfire/"+texture.Name+".mat");
			}
		}
		
		foreach(Entity entity in bspObject.Entities) {
			GameObject entityGameObject;
			if(entity.hasAttribute("targetname")) {
				entityGameObject = new GameObject(entity["classname"]+" "+entity["targetname"]);
			} else {
				entityGameObject = new GameObject(entity["classname"]);
			}
			entityGameObject.transform.parent = bspGameObject.transform;
			Vector3 origin = SwapYZ(entity.Origin * 0.0254f);
			Vector3 eulerangles = entity.Angles;
			eulerangles.x = -eulerangles.x;
			int modelNumber = entity.ModelNumber;
			if(modelNumber > -1) {
				Dictionary<string, List<MeshFilter>> facemeshfilters = new Dictionary<string, List<MeshFilter>>();
				Model model = bspObject.Models[modelNumber];
				int firstLeaf = model.FirstLeaf;
				int numLeaves = model.NumLeaves;
				bool[] brushesUsed = new bool[bspObject.Brushes.Count];
				for(int i = 0;i < numLeaves; i++) {
					Leaf leaf = bspObject.Leaves[firstLeaf+i];
					int firstMarkBrush = leaf.FirstMarkBrush;
					int numMarkBrushes = leaf.NumMarkBrushes;
					for(int j=0;j < numMarkBrushes; j++) {
						if(!brushesUsed[bspObject.MarkBrushes[firstMarkBrush + j]]) {
							brushesUsed[bspObject.MarkBrushes[firstMarkBrush + j]] = true;
							Brush brush = bspObject.Brushes[(int)bspObject.MarkBrushes[firstMarkBrush + j]];
							int firstSide = brush.FirstSide;
							int numSides = brush.NumSides;
							for(int k = 0;k < numSides; k++) {
								BrushSide side = bspObject.BrushSides[firstSide+k];
								Face face = bspObject.Faces[side.Face];
								if(face.NumVertices > 0) {
									GameObject faceGameObject = new GameObject("Side "+k);
									faceGameObject.transform.parent = entityGameObject.transform;
									MeshFilter filter = faceGameObject.AddComponent<MeshFilter>();
									MeshRenderer renderer = faceGameObject.AddComponent<MeshRenderer>();
									//MeshFilter filter = new MeshFilter();
									//MeshRenderer renderer = new MeshRenderer();
									filter.sharedMesh = new Mesh();
									Mesh mesh = filter.sharedMesh;
									mesh.Clear();
									TexInfo texinfo = bspObject.TexInfo[face.TextureScale];
									Vector3[] vertices = new Vector3[face.NumVertices];
									Vector2[] uvs = new Vector2[vertices.Length];
									Vector3 sAxis = SwapYZ(texinfo.SAxis / 0.0254f); // No, this is NOT a typo
									Vector3 tAxis = SwapYZ(texinfo.TAxis / 0.0254f); // The texture axis vectors need to be divided by the conversion.
									Vector3 STNormal = Vector3.Cross(sAxis, tAxis);
									Matrix4x4 texmatrix = Matrix4x4.identity;
									texmatrix[0,0] = sAxis[0];
									texmatrix[0,1] = tAxis[0];
									texmatrix[0,2] = STNormal[0];
									texmatrix[1,0] = sAxis[1];
									texmatrix[1,1] = tAxis[1];
									texmatrix[1,2] = STNormal[1];
									texmatrix[2,0] = sAxis[2];
									texmatrix[2,1] = tAxis[2];
									texmatrix[2,2] = STNormal[2];
									Matrix4x4 texmatinverse = texmatrix.inverse;
									Vector2 originShifts = new Vector2(Vector3.Dot(entity.Origin, texinfo.SAxis.normalized) * texinfo.SAxis.magnitude, Vector3.Dot(entity.Origin, texinfo.TAxis.normalized) * texinfo.TAxis.magnitude);
									for(int l=0;l<vertices.Length;l++) {
										vertices[l] = SwapYZ(bspObject.Vertices[face.FirstVertex + l].Vector * 0.0254f) + origin;
										Vector3 textureCoord = texmatinverse.MultiplyPoint3x4(vertices[l]);
										if(textureDict[bspObject.Textures[face.Texture].Name] != null) {
											uvs[l] = new Vector2((sAxis.sqrMagnitude*textureCoord[0]+texinfo.SShift-originShifts[0])/textureDict[bspObject.Textures[face.Texture].Name].width, -(tAxis.sqrMagnitude*textureCoord[1]+texinfo.TShift-originShifts[1])/textureDict[bspObject.Textures[face.Texture].Name].height);
										} else {
											uvs[l] = new Vector2((sAxis.sqrMagnitude*textureCoord[0]+texinfo.SShift-originShifts[0])/64, -(tAxis.sqrMagnitude*textureCoord[1]+texinfo.TShift-originShifts[1])/64);
										}
									}
									mesh.vertices = vertices;
									mesh.uv = uvs;
									int[] triangles = new int[face.NumIndices];
									for(int l=0;l<triangles.Length;l++) {
										triangles[l] = (int)bspObject.Indices[face.FirstIndex + l];
									}
									mesh.triangles = triangles;
									mesh.RecalculateNormals();
									renderer.sharedMaterial = Resources.Load("nightfire/"+bspObject.Textures[face.Texture].Name, typeof(Material)) as Material;
									//renderer.sharedMaterial = materialDict[bspObject.Textures[face.Texture].Name];
									if(!facemeshfilters.ContainsKey(bspObject.Textures[face.Texture].Name)) {
										facemeshfilters[bspObject.Textures[face.Texture].Name] = new List<MeshFilter>();
									}
									facemeshfilters[bspObject.Textures[face.Texture].Name].Add(filter);
								}
							}
						}
					}
				}
				List<MeshFilter> bmodelMeshFilters = new List<MeshFilter>();
				List<string> meshFilterTextureNames = new List<string>();
				// Combine faces using the same texture into a single mesh.
				// This saves tens of thousands of draw calls.
				foreach(Texture texture in bspObject.Textures) {
					if(facemeshfilters.ContainsKey(texture.Name)) {
						if(facemeshfilters[texture.Name].Count > 0) {
							MeshFilter[] meshFilters = Enumerable.ToArray(facemeshfilters[texture.Name]);
							CombineInstance[] combine = new CombineInstance[meshFilters.Length];
							GameObject meshGameObject = new GameObject(texture.Name);
							meshGameObject.transform.parent = entityGameObject.transform;
							MeshFilter filter = meshGameObject.AddComponent<MeshFilter>();
							meshGameObject.AddComponent<MeshRenderer>();
							for(int l=0;l<meshFilters.Length;l++) {
								combine[l].mesh = meshFilters[l].sharedMesh;
								combine[l].transform = meshFilters[l].transform.localToWorldMatrix;
								GameObject.DestroyImmediate(meshFilters[l].gameObject);
							}
							Mesh modelmesh = new Mesh();
							modelmesh.CombineMeshes(combine, true, false);
							filter.sharedMesh = modelmesh;
							meshFilterTextureNames.Add(texture.Name);
							if(!combineMeshes) {
								meshGameObject.renderer.material = Resources.Load("nightfire/"+texture.Name, typeof(Material)) as Material;
								modelmesh.Optimize();
								if(entity["classname"] == "worldspawn" || entity["classname"] == "func_wall") {
									meshGameObject.AddComponent<MeshCollider>();
								}
								AssetDatabase.CreateAsset(filter.sharedMesh, "Assets/Models/"+bspObject.MapNameNoExtension+"/mesh"+(filter.sharedMesh.GetHashCode())+".asset");
							} else {
								bmodelMeshFilters.Add(filter);
							}
							//AssetDatabase.CreateAsset(modelmesh, "Assets/Objects/"+bspObject.MapNameNoExtension+"/entity"+(currEnt++)+".asset");
						}/* else {
							GameObject.Destroy(brushGameObject);
						}*/
					}
				}
				// Then combine these texture meshes into one mesh, as submeshes.
				// This doesn't save draw calls, it just cleans up the scene a bit.
				// This is optional, though, because some larger maps may overflow
				// an int16, which (for some stupid reason) breaks Mesh.CombineMeshes().
				if(combineMeshes) {
					MeshFilter filter = null;
					if(bmodelMeshFilters.Count > 1) {
						MeshFilter[] meshFilters = Enumerable.ToArray(bmodelMeshFilters);
						CombineInstance[] combine = new CombineInstance[meshFilters.Length];
						filter = entityGameObject.AddComponent<MeshFilter>();
						entityGameObject.AddComponent<MeshRenderer>();
						Material[] materials = new Material[meshFilters.Length];
						for(int l=0;l<meshFilters.Length;l++) {
							combine[l].mesh = meshFilters[l].sharedMesh;
							combine[l].transform = meshFilters[l].transform.localToWorldMatrix;
							materials[l] = Resources.Load("nightfire/"+meshFilterTextureNames[l], typeof(Material)) as Material;
							GameObject.DestroyImmediate(meshFilters[l].gameObject);
						}
						Mesh bmodelmesh = new Mesh();
						bmodelmesh.CombineMeshes(combine, false, false);
						entityGameObject.renderer.sharedMaterials = materials;
						bmodelmesh.Optimize();
						filter.sharedMesh = bmodelmesh;
					} else if(bmodelMeshFilters.Count == 1) {
						bmodelMeshFilters[0].gameObject.name = entityGameObject.name;
						bmodelMeshFilters[0].gameObject.transform.parent = bspGameObject.transform;
						GameObject.DestroyImmediate(entityGameObject);
						entityGameObject = bmodelMeshFilters[0].gameObject;
						entityGameObject.renderer.sharedMaterial = Resources.Load("nightfire/"+meshFilterTextureNames[0], typeof(Material)) as Material;
						filter = entityGameObject.GetComponent<MeshFilter>();
					}
					if(entity["classname"] == "worldspawn" || entity["classname"] == "func_wall") {
						entityGameObject.AddComponent<MeshCollider>();
					}
					if(filter != null) {
						AssetDatabase.CreateAsset(filter.sharedMesh, "Assets/Models/"+bspObject.MapNameNoExtension+"/mesh"+(filter.sharedMesh.GetHashCode())+".asset");
					}
				}
			} else {
				entityGameObject.transform.position = origin;
				entityGameObject.transform.eulerAngles = eulerangles;
				switch(entity["classname"]) {
					case "light":
						Light light = entityGameObject.AddComponent<Light>();
						light.type = LightType.Point;
						NormalizeParseLight(entity, light);
						break;
					case "light_environment":
						Light envlight = entityGameObject.AddComponent<Light>();
						envlight.type = LightType.Directional;
						envlight.shadows = LightShadows.Soft;
						envlight.shadowBias = 0.04f;
						NormalizeParseLight(entity, envlight);
						break;
					case "light_spot":
						Light spotlight = entityGameObject.AddComponent<Light>();
						spotlight.type = LightType.Spot;
						NormalizeParseLight(entity, spotlight);
						break;
					case "info_player_start":
						entityGameObject.tag = "Respawn";
						break;
					case "env_fog":
						RenderSettings.fog = true;
						RenderSettings.fogMode = FogMode.Linear;
						try {
							RenderSettings.fogStartDistance = System.Single.Parse(entity["fogstart"]) * 0.0254f;
						} catch {
							RenderSettings.fogStartDistance = 0.0f;
						}
						try {
							RenderSettings.fogEndDistance = System.Single.Parse(entity["fogend"]) * 0.0254f;
						} catch {
							RenderSettings.fogEndDistance = 0.0f;
						}
						RenderSettings.fogColor = NormalizeParseColor(entity["rendercolor"]);
						try {
							RenderSettings.fogDensity = System.Single.Parse(entity["density"]) * 0.0254f;
						} catch {
							RenderSettings.fogDensity = 0.01f;
						}
						break;
				}
			}
		}
		if(saveAsPrefab) {
			PrefabUtility.CreatePrefab("Assets/Objects/"+bspObject.MapNameNoExtension+".prefab", bspGameObject);
		}
		if(!loadIntoScene) {
			GameObject.DestroyImmediate(bspGameObject);
		}
	}
	
	// Takes in a light entity with color values between 0 and 255, and converts it to Unity lighting
	public void NormalizeParseLight(Entity ent, Light light) {
		if(ent.hasAttribute("_light")) {
			Color color = NormalizeParseColor(ent["_light"]);
			light.color = color;
			light.intensity = color.a;
		}
	}
	
	// Takes in a string or array of strings representing color values between 0 and 255, and converts it to a Unity Color
	public Color NormalizeParseColor(string col) { return NormalizeParseColor(col.Split(' ')); }
	public Color NormalizeParseColor(string[] col) {
		float[] colorAsNums = new float[4];
		for (int i = 0; i < 4 && i < col.Length; i++) {
			try {
				colorAsNums[i] = System.Single.Parse(col[i]);
			} catch { ; }
		}
		return new Color(colorAsNums[0] / 255.0f, colorAsNums[1] / 255.0f, colorAsNums[2] / 255.0f, colorAsNums[3] / 255.0f);
	}
	
	public Vector3 SwapYZ(Vector3 v) {
		return new Vector3(v.x, v.z, v.y);
	}
	
}
#endif