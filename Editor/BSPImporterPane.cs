#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class BSPImporterPane : EditorWindow {
	private string path = "path";
	private string doomMapName = "map name (Doom only)";
	
	private Dictionary<string, Texture2D> textureDict = new Dictionary<string, Texture2D>();
	private Dictionary<string, Material> materialDict = new Dictionary<string, Material>();
	
	[MenuItem ("Window/BSP Import")]
	public static void ShowWindow() {
		BSPImporterPane main = (BSPImporterPane)EditorWindow.GetWindow(typeof(BSPImporterPane));
		main.autoRepaintOnSceneChange = true;
		UnityEngine.Object.DontDestroyOnLoad(main);
		main.Start();
	}
	
	public void Start() {
		
	}
	
	public void OnGUI() {
		EditorGUILayout.BeginVertical(); {
			path = EditorGUILayout.TextField(path);
			doomMapName = EditorGUILayout.TextField(doomMapName);
			if(GUILayout.Button("Import")) {
				ReadBSP(path);
			}
		} EditorGUILayout.EndVertical();
	}
	
	public void ReadBSP(string path) {
		if(File.Exists(Application.dataPath + "/" + path)) {
			BSPReader reader = new BSPReader(Application.dataPath + "/" + path, mapType.TYPE_UNDEFINED);
			reader.readBSP();
			BSP bspObject = reader.BSPData;
			LoadBSP42(bspObject);
		} else {
			Debug.LogError("File "+ Application.dataPath + "/" + path +" not found!");
		}
	}
	
	public void LoadBSP42(BSP bspObject) {
		Shader def = Shader.Find("Diffuse");
		GameObject bspGameObject = new GameObject(bspObject.MapNameNoExtension);
		
		foreach(Texture texture in bspObject.Textures) {
			string texturePath = "C:/Games/EA Games/NightFire/bond/textures/"+texture.Name+".png";
			if(!File.Exists(texturePath)) {
				Debug.Log(texturePath+" does not exist!");
			}
			textureDict[texture.Name] = Resources.Load("nightfire/textures/"+texture.Name, typeof(Texture2D)) as Texture2D;
			materialDict[texture.Name] = new Material(def);
			materialDict[texture.Name].mainTexture = textureDict[texture.Name];
			
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
				int numBrushes = 0;
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
							GameObject brushGameObject = new GameObject("Brush "+(numBrushes++));
							brushGameObject.transform.parent = entityGameObject.transform;
							List<MeshFilter> facemeshfilters = new List<MeshFilter>();
							int firstSide = brush.FirstSide;
							int numSides = brush.NumSides;
							for(int k = 0;k < numSides; k++) {
								BrushSide side = bspObject.BrushSides[firstSide+k];
								Face face = bspObject.Faces[side.Face];
								if(face.NumVertices > 0) {
									GameObject faceGameObject = new GameObject("Side "+k);
									faceGameObject.transform.parent = brushGameObject.transform;
									MeshFilter filter = faceGameObject.AddComponent<MeshFilter>();
									MeshRenderer renderer = faceGameObject.AddComponent<MeshRenderer>();
									Mesh mesh = filter.mesh;
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
									for(int l=0;l<vertices.Length;l++) {
										vertices[l] = SwapYZ(bspObject.Vertices[face.FirstVertex + l].Vector * 0.0254f) + origin;
										Vector3 textureCoord = texmatinverse.MultiplyPoint3x4(vertices[l]);
										if(textureDict[bspObject.Textures[face.Texture].Name] != null) {
											uvs[l] = new Vector2((sAxis.sqrMagnitude*textureCoord[0]+texinfo.SShift)/textureDict[bspObject.Textures[face.Texture].Name].width, -(tAxis.sqrMagnitude*textureCoord[1]+texinfo.TShift)/textureDict[bspObject.Textures[face.Texture].Name].height);
										} else {
											Debug.LogWarning("Texture "+bspObject.Textures[face.Texture].Name + " not found!");
											uvs[l] = new Vector2((sAxis.sqrMagnitude*textureCoord[0]+texinfo.SShift)/64, -(tAxis.sqrMagnitude*textureCoord[1]+texinfo.TShift)/64);
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
									renderer.material = materialDict[bspObject.Textures[face.Texture].Name];
									facemeshfilters.Add(filter);
								}
							}
							if(facemeshfilters.Count > 0) {
								MeshFilter[] meshFilters = Enumerable.ToArray(facemeshfilters);
								CombineInstance[] combine = new CombineInstance[meshFilters.Length];
								brushGameObject.AddComponent<MeshRenderer>();
								brushGameObject.renderer.materials = new Material[meshFilters.Length];
								for(int l=0;l<meshFilters.Length;l++) {
									brushGameObject.renderer.materials[l] = meshFilters[l].renderer.material;
									brushGameObject.renderer.materials[l].mainTexture = meshFilters[l].renderer.material.mainTexture;
									combine[l].mesh = meshFilters[l].sharedMesh;
									combine[l].transform = meshFilters[l].transform.localToWorldMatrix;
									//GameObject.Destroy(meshFilters[l].gameObject);
									meshFilters[l].gameObject.SetActive(false);
								}
								Mesh brushmesh = new Mesh();
								brushmesh.CombineMeshes(combine, false);
								AssetDatabase.CreateAsset(brushmesh, "Assets/Objects/"+bspObject.MapNameNoExtension+"/brush"+(bspObject.MarkBrushes[firstMarkBrush + j])+".asset");
							} else {
								GameObject.Destroy(brushGameObject);
							}
						}
					}
				}
			} else {
				entityGameObject.transform.position = origin;
				entityGameObject.transform.eulerAngles = eulerangles;
				if(entity["classname"]=="light") {
					Light light = entityGameObject.AddComponent<Light>();
					light.type = LightType.Point;
					ParseLight(entity, light);
				} else if(entity["classname"]=="light_environment") {
					Light light = entityGameObject.AddComponent<Light>();
					light.type = LightType.Directional;
					light.shadows = LightShadows.Soft;
					light.shadowBias = 0.04f;
					ParseLight(entity, light);
				} else if(entity["classname"]=="light_spot") {
					Light light = entityGameObject.AddComponent<Light>();
					light.type = LightType.Spot;
					ParseLight(entity, light);
				} else if(entity["classname"]=="info_player_start") {
					entityGameObject.tag = "Respawn";
				}
			}
		}
	}
	
	public void LoadSourceBSP(BSP bspObject) {
		Shader def = Shader.Find("Diffuse");
		GameObject bspGameObject = new GameObject(bspObject.MapNameNoExtension);
		
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
			int modelNumber = entity.ModelNumber;
			if(modelNumber > -1) {
				int numBrushes = 0;
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
							GameObject brushGameObject = new GameObject("Brush "+(numBrushes++));
							brushGameObject.transform.parent = entityGameObject.transform;
							List<MeshFilter> facemeshfilters = new List<MeshFilter>();
							int firstSide = brush.FirstSide;
							int numSides = brush.NumSides;
							for(int k = 0;k < numSides; k++) {
								BrushSide side = bspObject.BrushSides[firstSide+k];
								Face face = bspObject.Faces[side.Face];
								if(face.NumVertices > 0) {
									GameObject faceGameObject = new GameObject("Side "+k);
									faceGameObject.transform.parent = brushGameObject.transform;
									MeshFilter filter = faceGameObject.AddComponent<MeshFilter>();
									MeshRenderer renderer = faceGameObject.AddComponent<MeshRenderer>();
									Mesh mesh = filter.mesh;
									mesh.Clear();
									TexInfo texinfo = bspObject.TexInfo[face.TextureScale];
									Vector3[] vertices = new Vector3[face.NumVertices];
									Vector2[] uvs = new Vector2[vertices.Length];
									Vector3 sAxis = SwapYZ(texinfo.SAxis / 0.0254f);
									Vector3 tAxis = SwapYZ(texinfo.TAxis / 0.0254f);
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
									for(int l=0;l<vertices.Length;l++) {
										vertices[l] = bspObject.Vertices[face.FirstVertex + l].Vector + origin;
										Vector3 textureCoord = texmatinverse.MultiplyPoint3x4(vertices[l]);
										if(textureDict[bspObject.Textures[face.Texture].Name] != null) {
											uvs[l] = new Vector2((sAxis.sqrMagnitude*textureCoord[0]+texinfo.SShift)/textureDict[bspObject.Textures[face.Texture].Name].width, -(tAxis.sqrMagnitude*textureCoord[1]+texinfo.TShift)/textureDict[bspObject.Textures[face.Texture].Name].height);
										} else {
											Debug.LogWarning("Texture "+bspObject.Textures[face.Texture].Name + " not found!");
											uvs[l] = new Vector2((sAxis.sqrMagnitude*textureCoord[0]+texinfo.SShift)/64, -(tAxis.sqrMagnitude*textureCoord[1]+texinfo.TShift)/64);
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
									renderer.material = materialDict[bspObject.Textures[face.Texture].Name];
									facemeshfilters.Add(filter);
								}
							}
							if(facemeshfilters.Count > 0) {
								MeshFilter[] meshFilters = Enumerable.ToArray(facemeshfilters);
								CombineInstance[] combine = new CombineInstance[meshFilters.Length];
								brushGameObject.AddComponent<MeshRenderer>();
								brushGameObject.renderer.materials = new Material[meshFilters.Length];
								for(int l=0;l<meshFilters.Length;l++) {
									brushGameObject.renderer.materials[l] = meshFilters[l].renderer.material;
									brushGameObject.renderer.materials[l].mainTexture = meshFilters[l].renderer.material.mainTexture;
									combine[l].mesh = meshFilters[l].sharedMesh;
									combine[l].transform = meshFilters[l].transform.localToWorldMatrix;
									//GameObject.Destroy(meshFilters[l].gameObject);
									meshFilters[l].gameObject.SetActive(false);
								}
								MeshFilter brushfilter = brushGameObject.AddComponent<MeshFilter>();
								brushfilter.mesh = new Mesh();
								brushfilter.mesh.CombineMeshes(combine, false);
								if(entity["classname"]=="worldspawn") {
									brushGameObject.AddComponent<MeshCollider>();
								}
							} else {
								GameObject.Destroy(brushGameObject);
							}
						}
					}
				}
			} else {
				entityGameObject.transform.position = origin;
				entityGameObject.transform.eulerAngles = eulerangles;
				if(entity["classname"]=="light") {
					Light light = entityGameObject.AddComponent<Light>();
					light.type = LightType.Point;
					ParseLight(entity, light);
				} else if(entity["classname"]=="light_environment") {
					Light light = entityGameObject.AddComponent<Light>();
					light.type = LightType.Directional;
					light.shadows = LightShadows.Soft;
					light.shadowBias = 0.04f;
					ParseLight(entity, light);
				} else if(entity["classname"]=="light_spot") {
					Light light = entityGameObject.AddComponent<Light>();
					light.type = LightType.Spot;
					ParseLight(entity, light);
				} else if(entity["classname"]=="info_player_start") {
					entityGameObject.tag = "Respawn";
				}
			}
		}
	}
	
	public void ParseLight(Entity ent, Light light) {
		if(ent.hasAttribute("_light")) {
			float[] colorAsNums = new float[3];
			string[] _light = ent["_light"].Split(' ');
			for (int i = 0; i < 3 && i < _light.Length; i++) {
				try {
					colorAsNums[i] = System.Single.Parse(_light[i]);
				} catch { ; }
			}
			float intensity = 0;
			if(_light.Length >= 4) {
				try {
					intensity = System.Single.Parse(_light[3]);
				} catch { ; }
			}
			light.color = new Color(colorAsNums[0] / 255.0f, colorAsNums[1] / 255.0f, colorAsNums[2] / 255.0f);
			light.intensity = intensity / 255.0f;
		}
	}
	
	public Vector3 SwapYZ(Vector3 v) {
		return new Vector3(v.x, v.z, v.y);
	}
	
}
#endif