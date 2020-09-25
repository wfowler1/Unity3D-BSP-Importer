using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using LibBSP;

namespace BSPImporter {

	/// <summary>
	/// Class used for importing BSPs at runtime or edit-time.
	/// </summary>
	public class BSPLoader {

		/// <summary>
		/// Enum with options for combining <see cref="Mesh"/>es in the BSP import process.
		/// </summary>
		public enum MeshCombineOptions {
			/// <summary>
			/// Do not combine <see cref="Mesh"/>es.
			/// </summary>
			None,
			/// <summary>
			/// Combine all <see cref="Mesh"/>es in an <see cref="Entity"/> which use the same <see cref="Material"/>.
			/// </summary>
			PerMaterial,
			/// <summary>
			/// Combine all <see cref="Mesh"/>es in an <see cref="Entity"/> into a single <see cref="Mesh"/>
			/// </summary>
			PerEntity,
		}
		
		/// <summary>
		/// Enum with flags defining which generated assets to save from the import process, at edit-time only.
		/// </summary>
		[Flags]
		public enum AssetSavingOptions {
			/// <summary>
			/// Do not save any assets.
			/// </summary>
			None = 0,
			/// <summary>
			/// Save generated <see cref="Material"/> assets only.
			/// </summary>
			Materials = 1,
			/// <summary>
			/// Save generated <see cref="Mesh"/> assets only.
			/// </summary>
			Meshes = 2,
			/// <summary>
			/// Save generated <see cref="Material"/> and <see cref="Mesh"/> assets.
			/// </summary>
			MaterialsAndMeshes = 3,
			/// <summary>
			/// Save generated <see cref="GameObject"/> as a prefab only.
			/// </summary>
			Prefab = 4,
			/// <summary>
			/// Save generated <see cref="Material"/> assets and <see cref="GameObject"/> prefab.
			/// </summary>
			MaterialsAndPrefab = 5,
			/// <summary>
			/// Save generated <see cref="Mesh"/> assets and <see cref="GameObject"/> prefab.
			/// </summary>
			MeshesAndPrefab = 6,
			/// <summary>
			/// Save all generated <see cref="Mesh"/> and <see cref="Material"/> assets and <see cref="GameObject"/> prefab.
			/// </summary>
			MaterialsMeshesAndPrefab = 7,
		}

		/// <summary>
		/// Struct containing various settings for the BSP Import process.
		/// </summary>
		[Serializable]
		public struct Settings {
			/// <summary>
			/// The path to the BSP file.
			/// </summary>
			public string path;
			/// <summary>
			/// The path to the textures for the BSP file. At edit-time, if the path is within the Assets folder, links textures with generated <see cref="Material"/>s.
			/// </summary>
			public string texturePath;
			/// <summary>
			/// How to combine generated <see cref="Mesh"/> objects.
			/// </summary>
			public MeshCombineOptions meshCombineOptions;
			/// <summary>
			/// At edit-time, which generated assets should be saved into the Assets folder.
			/// </summary>
			public AssetSavingOptions assetSavingOptions;
			/// <summary>
			/// At edit=time, path within Assets to save generated <see cref="Material"/>s to.
			/// </summary>
			public string materialPath;
			/// <summary>
			/// At edit=time, path within Assets to save generated <see cref="Mesh"/>es to.
			/// </summary>
			public string meshPath;
			/// <summary>
			/// Amount of detail used to tessellate patch curves into <see cref="Mesh"/>es.
			/// Higher values give smoother curves with exponentially more vertices.
			/// </summary>
			public int curveTessellationLevel;
			/// <summary>
			/// Callback that runs for each <see cref="Entity"/> after <see cref="Mesh"/>es are
			/// generated and the hierarchy is set up. Can be used to add custom post-processing
			/// to generated <see cref="GameObject"/>s using <see cref="Entity"/> information. Also
			/// contains a <see cref="List{T}"/> of <see cref="EntityInstance"/>s for each
			/// <see cref="Entity"/> the <see cref="Entity"/> targets.
			/// </summary>
			public Action<EntityInstance, List<EntityInstance>> entityCreatedCallback;
		}

		/// <summary>
		/// Struct linking a generated <see cref="GameObject"/> with the <see cref="Entity"/> used to create it.
		/// </summary>
		public struct EntityInstance {
			/// <summary>
			/// The <see cref="Entity"/> used to generate <see cref="gameObject"/>.
			/// </summary>
			public Entity entity;
			/// <summary>
			/// The <see cref="GameObject"/> generated from <see cref="entity"/>.
			/// </summary>
			public GameObject gameObject;
		}

		/// <summary>
		/// Is the game currently running?
		/// </summary>
		public static bool IsRuntime {
			get {
#if UNITY_EDITOR
				return EditorApplication.isPlaying;
#else
				return true;
#endif
			}
		}

		/// <summary>
		/// The <see cref="Settings"/> to use to load a <see cref="BSP"/>.
		/// </summary>
		public Settings settings;

		private BSP bsp;
		private GameObject root;
		private List<EntityInstance> entityInstances = new List<EntityInstance>();
		private Dictionary<string, List<EntityInstance>> namedEntities = new Dictionary<string, List<EntityInstance>>();
		private Dictionary<string, Material> materialDirectory = new Dictionary<string, Material>();

		/// <summary>
		/// Loads a <see cref="BSP"/> into Unity using the settings in <see cref="settings"/>.
		/// </summary>
		public void LoadBSP() {
			if (string.IsNullOrEmpty(settings.path) || !File.Exists(settings.path)) {
				Debug.LogError("Cannot import " + settings.path + ": The path is invalid.");
				return;
			}
			BSP bsp = new BSP(settings.path);
			try {
				LoadBSP(bsp);
			} catch (Exception e) {
#if UNITY_EDITOR
				EditorUtility.ClearProgressBar();
#endif
#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
				Debug.LogException(e);
#else
				Debug.LogError(e.ToString() + "\nat " + e.StackTrace);
#endif
			}
		}

		/// <summary>
		/// Loads <paramref name="bsp"/> into Unity using the settings in <see cref="settings"/>.
		/// </summary>
		/// <param name="bsp">The <see cref="BSP"/> object to import into Unity</param>
		public void LoadBSP(BSP bsp) {
			if (bsp == null) {
				Debug.LogError("Cannot import BSP: The object was null.");
				return;
			}
			this.bsp = bsp;

			for (int i = 0; i < bsp.entities.Count; ++i) {
				Entity entity = bsp.entities[i];
#if UNITY_EDITOR
				if (EditorUtility.DisplayCancelableProgressBar("Importing BSP", entity.ClassName + (!string.IsNullOrEmpty(entity.Name) ? " " + entity.Name : ""), i / (float)bsp.entities.Count)) {
					EditorUtility.ClearProgressBar();
					return;
				}
#endif
				EntityInstance instance = CreateEntityInstance(entity);
				entityInstances.Add(instance);
				namedEntities[entity.Name].Add(instance);

				int modelNumber = entity.ModelNumber;
				if (modelNumber >= 0) {
					BuildMesh(instance);
				} else {
					Vector3 angles = entity.Angles;
					instance.gameObject.transform.rotation = Quaternion.Euler(-angles.x, angles.y, angles.z);
				}

				instance.gameObject.transform.position = entity.Origin.SwizzleYZ().ScaleInch2Meter();
			}
			
			root = new GameObject(Path.GetFileNameWithoutExtension(bsp.filePath));
			foreach (KeyValuePair<string, List<EntityInstance>> pair in namedEntities) {
				SetUpEntityHierarchy(pair.Value);
			}

			if (settings.entityCreatedCallback != null) {
				foreach (EntityInstance instance in entityInstances) {
					string target = instance.entity["target"];
					if (namedEntities.ContainsKey(target) && !string.IsNullOrEmpty(target)) {
						settings.entityCreatedCallback(instance, namedEntities[target]);
					} else {
						settings.entityCreatedCallback(instance, new List<EntityInstance>(0));
					}
				}
			}

#if UNITY_EDITOR
			if (!IsRuntime) {
				if ((settings.assetSavingOptions & AssetSavingOptions.Prefab) > 0) {
					string prefabPath = Path.Combine(Path.Combine("Assets", settings.meshPath), bsp.MapNameNoExtension + ".prefab").Replace('\\', '/');
					Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
#if UNITY_2018_3_OR_NEWER
					PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
#elif !UNITY_3_4
					PrefabUtility.CreatePrefab(prefabPath, root, ReplacePrefabOptions.ConnectToPrefab);
#else
					UnityEngine.Object newPrefab = EditorUtility.CreateEmptyPrefab(prefabPath);
					EditorUtility.ReplacePrefab(root, newPrefab, ReplacePrefabOptions.ConnectToPrefab);
#endif
				}
				AssetDatabase.Refresh();
			}
			EditorUtility.ClearProgressBar();
#endif
		}

		/// <summary>
		/// Loads the <see cref="Texture2D"/> at <paramref name="texturePath"/> and returns it.
		/// </summary>
		/// <param name="texturePath">
		/// The path to the <see cref="Texture2D"/>. If within Assets, it will use the texture
		/// asset rather than loading it directly from the HDD.
		/// </param>
		/// <param name="textureIsAsset">Is <paramref name="texturePath"/> within this project's Assets directory?</param>
		/// <returns>The loaded <see cref="Texture2D"/>.</returns>
		private Texture2D LoadTextureAtPath(string texturePath, bool textureIsAsset) {
#if UNITY_EDITOR
			if (textureIsAsset) {
				return AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D)) as Texture2D;
			} else
#endif
			{
				if (texturePath.EndsWith(".tga")) {
					return Paloma.TargaImage.LoadTargaImage(texturePath);
				} else {
					Texture2D texture = new Texture2D(0, 0);
					texture.LoadImage(File.ReadAllBytes(texturePath));
					return texture;
				}
			}
		}

		/// <summary>
		/// Creates a <see cref="Material"/> object for <paramref name="textureName"/>, or loads it from Assets
		/// if it already exists at edit-time.
		/// </summary>
		/// <param name="textureName">Name of the <see cref="Texture2D"/> to load.</param>
		public void LoadMaterial(string textureName) {
#if UNITY_5 || UNITY_5_3_OR_NEWER
			Shader def = Shader.Find("Standard");
#else
			Shader def = Shader.Find("Diffuse");
#endif
			Shader fallbackShader = Shader.Find("VR/SpatialMapping/Wireframe");

			string texturePath;
			bool textureIsAsset = false;
			if (settings.texturePath.Contains(":")) {
				texturePath = Path.Combine(settings.texturePath, textureName).Replace('\\', '/');
#if UNITY_EDITOR
				if (texturePath.StartsWith(Application.dataPath)) {
					texturePath = "Assets/" + texturePath.Substring(Application.dataPath.Length + 1);
					textureIsAsset = true;
				} else if ((settings.assetSavingOptions & AssetSavingOptions.Materials) > 0) {
					Debug.LogWarning("Using a texture path outside of Assets will not work with material saving enabled.");
				}
#endif
			} else {
#if UNITY_EDITOR
				texturePath = Path.Combine(Path.Combine("Assets", settings.texturePath), textureName).Replace('\\', '/');
				textureIsAsset = true;
#else
				texturePath = Path.Combine(settings.texturePath, textureName).Replace('\\', '/');
#endif
			}

			Texture2D texture = null;
			try {
				if (File.Exists(texturePath + ".png")) {
					texture = LoadTextureAtPath(texturePath + ".png", textureIsAsset);
				} else if (File.Exists(texturePath + ".jpg")) {
					texture = LoadTextureAtPath(texturePath + ".jpg", textureIsAsset);
				} else if (File.Exists(texturePath + ".tga")) {
					texture = LoadTextureAtPath(texturePath + ".tga", textureIsAsset);
				}
			} catch { }
			if (texture == null) {
				Debug.LogWarning("Texture " + textureName + " could not be loaded (does the file exist?)");
			}

			Material material = null;
			bool materialIsAsset = false;
#if UNITY_EDITOR
			string materialPath = Path.Combine(Path.Combine("Assets", settings.materialPath), textureName + ".mat").Replace('\\', '/');
			if (!IsRuntime && (settings.assetSavingOptions & AssetSavingOptions.Materials) > 0) {
				material = AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material)) as Material;
			}
			if (material != null) {
				materialIsAsset = true;
			} else
#endif
			{
				material = new Material(def);
				material.name = textureName;
			}

			if (!materialIsAsset) {
				if (texture != null) {
					material.mainTexture = texture;
				} else if (fallbackShader != null) {
					material = new Material(fallbackShader);
				}
#if UNITY_EDITOR
				if (!IsRuntime && (settings.assetSavingOptions & AssetSavingOptions.Materials) > 0) {
					Directory.CreateDirectory(Path.GetDirectoryName(materialPath));
					AssetDatabase.CreateAsset(material, materialPath);
				}
#endif
			}

			materialDirectory[textureName] = material;
		}

		/// <summary>
		/// Creates an <see cref="EntityInstance"/> for <paramref name="entity"/> and creates a new
		/// <see cref="GameObject"/> for it.
		/// </summary>
		/// <param name="entity">The <see cref="Entity"/> to create an <see cref="EntityInstance"/> for.</param>
		/// <returns>The generated <see cref="EntityInstance"/>.</returns>
		protected EntityInstance CreateEntityInstance(Entity entity) {
			// Entity.name guaranteed not to be null, empty string is a valid Dictionary key
			if (!namedEntities.ContainsKey(entity.Name) || namedEntities[entity.Name] == null) {
				namedEntities[entity.Name] = new List<EntityInstance>();
			}
			EntityInstance instance = new EntityInstance() {
				entity = entity,
				gameObject = new GameObject(entity.ClassName + (!string.IsNullOrEmpty(entity.Name) ? " " + entity.Name : string.Empty))
			};

			return instance;
		}

		/// <summary>
		/// Sets up the hierarchy for all <see cref="GameObject"/>s in <paramref name="instances"/> according
		/// to the hierarchy in the <see cref="BSP"/>. Currently only applies to Source engine.
		/// </summary>
		/// <param name="instances">A <see cref="List{EntityInstance}"/> with all <see cref="EntityInstance"/> objects.</param>
		protected void SetUpEntityHierarchy(List<EntityInstance> instances) {
			foreach (EntityInstance instance in instances) {
				SetUpEntityHierarchy(instance);
			}
		}

		/// <summary>
		/// Finds the <see cref="EntityInstance"/> corresponding to the 'parentname' in <paramref name="instance"/>'s <see cref="Entity"/>
		/// and set the <see cref="GameObject"/> int <paramref name="instance"/> as a child of the parent's <see cref="GameObject"/>.
		/// </summary>
		/// <param name="instance"><see cref="EntityInstance"/> to find the parent's <see cref="EntityInstance"/> for.</param>
		protected void SetUpEntityHierarchy(EntityInstance instance) {
			if (!instance.entity.ContainsKey("parentname")) {
				instance.gameObject.transform.parent = root.transform;
				return;
			}
			if (namedEntities.ContainsKey(instance.entity["parentname"])) {
				if (namedEntities[instance.entity["parentname"]].Count > 1) {
					Debug.LogWarning(string.Format("Entity \"{0}\" claims to have parent \"{1}\" but more than one matching entity exists.",
						instance.gameObject.name,
						instance.entity["parentname"]), instance.gameObject);
				}
				instance.gameObject.transform.parent = namedEntities[instance.entity["parentname"]][0].gameObject.transform;
			} else {
				Debug.LogWarning(string.Format("Entity \"{0}\" claims to have parent \"{1}\" but no such entity exists.",
					instance.gameObject.name,
					instance.entity["parentname"]), instance.gameObject);
			}
		}

		/// <summary>
		/// Builds all <see cref="Mesh"/> objects for the <see cref="Entity"/> in <paramref name="instance"/> instance,
		/// combines them if necessary using <see cref="settings"/>.meshCombineOptions and adds the meshes to the
		/// <see cref="GameObject"/> in <paramref name="instance"/>.
		/// </summary>
		/// <param name="instance">The <see cref="EntityInstance"/> to build <see cref="Mesh"/>es for.</param>
		protected void BuildMesh(EntityInstance instance) {
			int modelNumber = instance.entity.ModelNumber;
			Model model = bsp.models[modelNumber];
			Dictionary<string, List<Mesh>> textureMeshMap = new Dictionary<string, List<Mesh>>();
			GameObject gameObject = instance.gameObject;

			List<Face> faces = bsp.GetFacesInModel(model);
			int i = 0;
			for (i = 0; i < faces.Count; ++i) {
				Face face = faces[i];
				if (face.NumEdgeIndices <= 0 && face.NumVertices <= 0) {
					continue;
				}
				
				int textureIndex = bsp.GetTextureIndex(face);
				string textureName = "";
				if (textureIndex >= 0) {
					LibBSP.Texture texture = bsp.textures[textureIndex];
					textureName = LibBSP.Texture.SanitizeName(texture.Name, bsp.version);

					if (!textureMeshMap.ContainsKey(textureName) || textureMeshMap[textureName] == null) {
						textureMeshMap[textureName] = new List<Mesh>();
					}

					textureMeshMap[textureName].Add(CreateFaceMesh(face, textureName));
				}
			}

			if (modelNumber == 0) {
				if (bsp.lodTerrains != null) {
					foreach (LODTerrain lodTerrain in bsp.lodTerrains) {
						if (lodTerrain.TextureIndex >= 0) {
							LibBSP.Texture texture = bsp.textures[lodTerrain.TextureIndex];
							string textureName = texture.Name;

							if (!textureMeshMap.ContainsKey(textureName) || textureMeshMap[textureName] == null) {
								textureMeshMap[textureName] = new List<Mesh>();
							}

							textureMeshMap[textureName].Add(CreateLoDTerrainMesh(lodTerrain, textureName));
						}
					}
				}
			}

			if (settings.meshCombineOptions != MeshCombineOptions.None) {
				Mesh[] textureMeshes = new Mesh[textureMeshMap.Count];
				Material[] materials = new Material[textureMeshes.Length];
				i = 0;
				foreach (KeyValuePair<string, List<Mesh>> pair in textureMeshMap) {
					textureMeshes[i] = MeshUtils.CombineAllMeshes(pair.Value.ToArray(), true, false);
					if (materialDirectory.ContainsKey(pair.Key)) {
						materials[i] = materialDirectory[pair.Key];
					}
					if (settings.meshCombineOptions == MeshCombineOptions.PerMaterial) {
						GameObject textureGameObject = new GameObject(pair.Key);
						textureGameObject.transform.parent = gameObject.transform;
						textureGameObject.transform.localPosition = Vector3.zero;
						if (textureMeshes[i].normals.Length == 0 || textureMeshes[i].normals[0] == Vector3.zero) {
							textureMeshes[i].RecalculateNormals();
						}
						textureMeshes[i].AddMeshToGameObject(new Material[] { materials[i] }, textureGameObject);
#if UNITY_EDITOR
						if (!IsRuntime && (settings.assetSavingOptions & AssetSavingOptions.Meshes) > 0) {
							string meshPath = Path.Combine(Path.Combine(Path.Combine("Assets", settings.meshPath), bsp.MapNameNoExtension), "mesh_" + textureMeshes[i].GetHashCode() + ".asset").Replace('\\', '/');
							Directory.CreateDirectory(Path.GetDirectoryName(meshPath));
							AssetDatabase.CreateAsset(textureMeshes[i], meshPath);
						}
#endif
					}
					++i;
				}

				if (settings.meshCombineOptions != MeshCombineOptions.PerMaterial) {
					Mesh mesh = MeshUtils.CombineAllMeshes(textureMeshes, false, false);
					mesh.TransformVertices(gameObject.transform.localToWorldMatrix);
					if (mesh.normals.Length == 0 || mesh.normals[0] == Vector3.zero) {
						mesh.RecalculateNormals();
					}
					mesh.AddMeshToGameObject(materials, gameObject);
#if UNITY_EDITOR
					if (!IsRuntime && (settings.assetSavingOptions & AssetSavingOptions.Meshes) > 0) {
						string meshPath = Path.Combine(Path.Combine(Path.Combine("Assets", settings.meshPath), bsp.MapNameNoExtension), "mesh_" + mesh.GetHashCode() + ".asset").Replace('\\', '/');
						Directory.CreateDirectory(Path.GetDirectoryName(meshPath));
						AssetDatabase.CreateAsset(mesh, meshPath);
					}
#endif
				}
			} else {
				i = 0;
				foreach (KeyValuePair<string, List<Mesh>> pair in textureMeshMap) {
					GameObject textureGameObject = new GameObject(pair.Key);
					textureGameObject.transform.parent = gameObject.transform;
					textureGameObject.transform.localPosition = Vector3.zero;
					Material material = materialDirectory[pair.Key];
					foreach (Mesh mesh in pair.Value) {
						GameObject faceGameObject = new GameObject("Face");
						faceGameObject.transform.parent = textureGameObject.transform;
						faceGameObject.transform.localPosition = Vector3.zero;
						if (mesh.normals.Length == 0 || mesh.normals[0] == Vector3.zero) {
							mesh.RecalculateNormals();
						}
						mesh.AddMeshToGameObject(new Material[] { material }, faceGameObject);
#if UNITY_EDITOR
						if (!IsRuntime && (settings.assetSavingOptions & AssetSavingOptions.Meshes) > 0) {
							string meshPath = Path.Combine(Path.Combine(Path.Combine("Assets", settings.meshPath), bsp.MapNameNoExtension), "mesh_" + mesh.GetHashCode() + ".asset").Replace('\\', '/');
							Directory.CreateDirectory(Path.GetDirectoryName(meshPath));
							AssetDatabase.CreateAsset(mesh, meshPath);
						}
#endif
					}
					++i;
				}
			}

		}

		/// <summary>
		/// Creates a <see cref="Mesh"/> appropriate for <paramref name="face"/>.
		/// </summary>
		/// <param name="face">The <see cref="Face"/> to create a <see cref="Mesh"/> for.</param>
		/// <param name="textureName">The name of the texture/shader applied to the <see cref="Face"/>.</param>
		/// <returns>The <see cref="Mesh"/> generated for <paramref name="face"/>.</returns>
		protected Mesh CreateFaceMesh(Face face, string textureName) {
			Vector2 dims;
			if (!materialDirectory.ContainsKey(textureName)) {
				LoadMaterial(textureName);
			}
			if (materialDirectory[textureName].HasProperty("_MainTex") && materialDirectory[textureName].mainTexture != null) {
				dims = new Vector2(materialDirectory[textureName].mainTexture.width, materialDirectory[textureName].mainTexture.height);
			} else {
				dims = new Vector2(128, 128);
			}

			Mesh mesh;
			if (face.DisplacementIndex >= 0) {
				mesh = MeshUtils.CreateDisplacementMesh(bsp, face, dims);
			} else {
				mesh = MeshUtils.CreateFaceMesh(bsp, face, dims, settings.curveTessellationLevel);
			}

			return mesh;
		}

		/// <summary>
		/// Creates a <see cref="Mesh"/> appropriate for <paramref name="lodTerrain"/>.
		/// </summary>
		/// <param name="lodTerrain">The <see cref="LODTerrain"/> to create a <see cref="Mesh"/> for.</param>
		/// <param name="textureName">The name of the texture/shader applied to the <see cref="LODTerrain"/>.</param>
		/// <returns>The <see cref="Mesh"/> generated for <paramref name="lodTerrain"/>.</returns>
		protected Mesh CreateLoDTerrainMesh(LODTerrain lodTerrain, string textureName) {
			if (!materialDirectory.ContainsKey(textureName)) {
				LoadMaterial(textureName);
			}

			return MeshUtils.CreateMoHAATerrainMesh(bsp, lodTerrain);
		}
	}
}
