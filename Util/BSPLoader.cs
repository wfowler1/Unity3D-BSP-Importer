using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibBSP;

namespace BSPImporter {
	public static class BSPLoader {

		public static string path = "";
		public static string texturePath = "";
		public static int tesselationLevel = 12;

		public delegate void EntityGameObjectCreatedAction(GameObject gameObject, Entity entity);
		public static EntityGameObjectCreatedAction EntityGameObjectCreated;

		private static Dictionary<string, Texture2D> textureDict = new Dictionary<string, Texture2D>();
		private static Dictionary<string, Material> materialDict = new Dictionary<string, Material>();

		public static BSP ReadBSP() {
			if(File.Exists(path)) {
				BSP bsp = new BSP(path);
				LoadBSP(bsp);
				return bsp;
			} else {
				Debug.LogError("File " + path + " not found!");
				return null;
			}
		}

		public static void LoadBSP(BSP bspObject) {
			RenderSettings.fog = false;
			GameObject bspGameObject = new GameObject(bspObject.MapNameNoExtension);

			LoadTextures(bspObject);

			foreach(Entity entity in bspObject.entities) {
				GameObject entityGameObject;
				if(entity.ContainsKey("targetname")) {
					entityGameObject = new GameObject(entity["classname"] + " " + entity["targetname"]);
				} else {
					entityGameObject = new GameObject(entity["classname"]);
				}
				entityGameObject.transform.parent = bspGameObject.transform;
				Vector3 origin = BSPUtils.Swizzle(entity.origin * BSPUtils.inch2meterScale);
				Vector3 eulerangles = entity.angles;
				eulerangles.x = -eulerangles.x;
				int modelNumber = entity.modelNumber;
				if(modelNumber > -1) {
					BSPUtils.numVertices = 0;
					List<Face> faces = BSPUtils.GetFacesInModel(bspObject, bspObject.models[modelNumber]);
					if(faces != null && faces.Count > 0) {
						Dictionary<string, List<Mesh>> faceMeshes = new Dictionary<string, List<Mesh>>();
						Dictionary<string, Mesh> textureMeshes = new Dictionary<string, Mesh>();
						foreach(Face currentFace in faces) {
							if(currentFace.numVertices > 0 || currentFace.numEdges > 0) {
								TextureInfo TextureInfo = null;
								int textureIndex = BSPUtils.GetTextureIndex(bspObject, currentFace);
								if(bspObject.textures[textureIndex].texAxes != null) {
									TextureInfo = bspObject.textures[textureIndex].texAxes;
								} else {
									if (currentFace.textureInfo > -1) {
										TextureInfo = bspObject.texInfo[currentFace.textureInfo];
									} else {
										if(currentFace.plane >= 0) { // If not we've hit a Q3 wall. Never mind that, Q3 stores UVs directly.
											Vector3[] axes = TextureInfo.TextureAxisFromPlane(bspObject.planes[currentFace.plane]);
											TextureInfo = new TextureInfo(axes[0], axes[1], Vector2.zero, Vector2.one, 0, bspObject.FindTexDataWithTexture("tools/toolsclip"), 0);
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
									if(textureDict.ContainsKey(bspObject.textures[textureIndex].name)) {
										faceMesh = BSPUtils.LegacyBuildFaceMesh(meshCorners, triangles, TextureInfo, entity.origin, textureDict[bspObject.textures[textureIndex].name]);
									} else {
										faceMesh = BSPUtils.LegacyBuildFaceMesh(meshCorners, triangles, TextureInfo, entity.origin, null);
									}
								}
								if (!faceMeshes.ContainsKey(bspObject.textures[textureIndex].name)) {
									faceMeshes.Add(bspObject.textures[textureIndex].name, new List<Mesh>());
								}
								if(faceMesh != null) {
									faceMeshes[bspObject.textures[textureIndex].name].Add(faceMesh);
								}
							}
						}
						foreach(string key in faceMeshes.Keys) {
							textureMeshes.Add(key, BSPUtils.CombineAllMeshes(faceMeshes[key].ToArray<Mesh>(), entityGameObject.transform, true, false));
						}
						/*if(BSPUtils.numVertices < 65535) { // If we can combine all the faces into one mesh and use a single game object
							Mesh entityMesh = BSPUtils.CombineAllMeshes(textureMeshes.Values.ToArray<Mesh>(), entityGameObject.transform, false, false);
							entityGameObject.AddComponent<MeshFilter>().mesh = entityMesh;
							Material[] sharedMaterials = new Material[textureMeshes.Count];
							for(int i = 0; i < textureMeshes.Count; i++) {
								sharedMaterials[i] = materialDict[textureMeshes.Keys.ToArray<string>()[i]];
							}
							entityGameObject.AddComponent<MeshRenderer>().sharedMaterials = sharedMaterials;
							entityGameObject.AddComponent<MeshCollider>();
						} else {*/ // If there's too many vertices, we must treat each face as its own mesh
							foreach(string key in textureMeshes.Keys) {
								GameObject textureMeshGO = new GameObject(key);
								textureMeshGO.transform.parent = entityGameObject.transform;
								textureMeshGO.AddComponent<MeshFilter>().mesh = textureMeshes[key];
								textureMeshGO.AddComponent<MeshRenderer>().sharedMaterial = materialDict[key];
								textureMeshGO.AddComponent<MeshCollider>();
							}
						//}
					}
				} else {
					BSPUtils.ParseEntity(entity, bspGameObject, entityGameObject);
					entityGameObject.transform.eulerAngles = eulerangles;
				}

				entityGameObject.transform.position = origin;

				if (EntityGameObjectCreated != null) {
					EntityGameObjectCreated(entityGameObject, entity);
				}
			}
		}

		public static void LoadTextures(BSP bspObject) {
			Shader def = Shader.Find("Standard");
			foreach(LibBSP.Texture texture in bspObject.textures) {
				string globalTexturePath;
				if(texturePath.Contains(":")) {
					globalTexturePath = texturePath + "/" + texture.name;
				} else {
					globalTexturePath = Application.dataPath + "/" + texturePath + "/" + texture.name;
				}
				if (File.Exists(globalTexturePath + ".png")) {
					textureDict[texture.name] = new Texture2D(0, 0);
					textureDict[texture.name].LoadImage(File.ReadAllBytes(globalTexturePath + ".png"));
				} else if (File.Exists(globalTexturePath + ".jpg")) {
					textureDict[texture.name] = new Texture2D(0, 0);
					textureDict[texture.name].LoadImage(File.ReadAllBytes(globalTexturePath + ".jpg"));
				} else if (File.Exists(globalTexturePath + ".tga")) {
					textureDict[texture.name] = Paloma.TargaImage.LoadTargaImage(globalTexturePath + ".tga");
				}
				if (!textureDict.ContainsKey(texture.name) || textureDict[texture.name] == null) {
					Debug.LogWarning("Texture " + texture.name + " not found!");
				}
				materialDict[texture.name] = new Material(def);
				if (textureDict.ContainsKey(texture.name) && textureDict[texture.name] != null) {
					materialDict[texture.name].mainTexture = textureDict[texture.name];
				}
			}
		}

	}
}
