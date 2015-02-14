using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class BSPLoader {

	public static string path = @"E:\Games\EA Games\NightFire\bond\maps\ctf_romania.bsp";
	public static string texturePath = @"E:\Games\EA Games\NightFire\bond\textures";

	private static Dictionary<string, Texture2D> textureDict = new Dictionary<string, Texture2D>();
	private static Dictionary<string, Material> materialDict = new Dictionary<string, Material>();

	public static BSP ReadBSP() {
		if(File.Exists(path)) {
			BSPReader reader = new BSPReader(path, mapType.TYPE_UNDEFINED);
			reader.debugLog = true;
			reader.readBSP();
			BSP bspObject = reader.BSPData;
			LoadBSP(bspObject);
			return bspObject;
		} else {
			Debug.LogError("File " + path + " not found!");
			return null;
		}
	}

	public static void LoadBSP(BSP bspObject) {
		RenderSettings.fog = false;
		GameObject bspGameObject = new GameObject(bspObject.MapNameNoExtension);

		LoadTextures(bspObject);

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
					if(BSPUtils.numVertices < 65535) { // If we can combine all the faces into one mesh and use a single game object
						Mesh entityMesh = BSPUtils.CombineAllMeshes(textureMeshes.Values.ToArray<Mesh>(), entityGameObject.transform, false, false);
						entityGameObject.AddComponent<MeshFilter>().mesh = entityMesh;
						Material[] sharedMaterials = new Material[textureMeshes.Count];
						for(int i = 0; i < textureMeshes.Count; i++) {
							sharedMaterials[i] = materialDict[textureMeshes.Keys.ToArray<string>()[i]];
						}
						entityGameObject.AddComponent<MeshRenderer>().sharedMaterials = sharedMaterials;
						entityGameObject.AddComponent<MeshCollider>();
					} else { // If there's too many vertices, we must treat each face as its own mesh
						foreach(string key in textureMeshes.Keys) {
							GameObject textureMeshGO = new GameObject(key);
							textureMeshGO.transform.parent = entityGameObject.transform;
							textureMeshGO.AddComponent<MeshFilter>().mesh = textureMeshes[key];
							textureMeshGO.AddComponent<MeshRenderer>().sharedMaterial = materialDict[key];
							textureMeshGO.AddComponent<MeshCollider>();
						}
					}
				}
			} else {
				entityGameObject.transform.position = origin;
				entityGameObject.transform.eulerAngles = eulerangles;
				BSPUtils.ParseEntity(entity, bspGameObject, entityGameObject);
			}
		}
	}

	public static void LoadTextures(BSP bspObject) {
		Shader def = Shader.Find("Transparent/Cutout/Diffuse");
		foreach(Texturedef texture in bspObject.Textures) {
			string globalTexturePath;
			if(texturePath.Contains(":")) {
				globalTexturePath = texturePath + "/" + texture.Name;
			} else {
				globalTexturePath = Application.dataPath + "/" + texturePath + "/" + texture.Name;
			}
			if(!File.Exists(globalTexturePath + ".png")) {
				if(!File.Exists(globalTexturePath + ".jpg")) {
					if(!File.Exists(globalTexturePath + ".tga")) {
						Debug.Log(globalTexturePath + " does not exist or is not in JPG, PNG or TGA format!");
					} else {
						textureDict[texture.Name] = Paloma.TargaImage.LoadTargaImage(globalTexturePath + ".tga");
					}
				} else {
					textureDict[texture.Name] = new Texture2D(0, 0);
					textureDict[texture.Name].LoadImage(File.ReadAllBytes(globalTexturePath + ".jpg"));
				}
			} else {
				textureDict[texture.Name] = new Texture2D(0, 0);
				textureDict[texture.Name].LoadImage(File.ReadAllBytes(globalTexturePath + ".png"));
			}
			if(!textureDict.ContainsKey(texture.Name) || textureDict[texture.Name] == null) {
				Debug.LogWarning("Texture " + texture.Name + " not found! Texture scaling will probably be wrong if imported later.");
			}
			materialDict[texture.Name] = new Material(def);
			if(textureDict.ContainsKey(texture.Name) && textureDict[texture.Name] != null) {
				materialDict[texture.Name].mainTexture = textureDict[texture.Name];
			}
		}
	}

}
