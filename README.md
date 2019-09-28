# Unity3D BSP Importer
### A lightweight plugin for importing BSP maps into Unity3D as meshes. Currently only imports World and Entities and their surfaces.

## Usage
Simply use git to pull the repository into your Unity project. You don't _have_ to put it in Plugins but it is recommended. Be sure to either pull submodules, or import <a href="https://github.com/wfowler1/LibBSP">LibBSP</a> separately into your Unity project as well.

There are several settings for importing a BSP into Unity:
- Path: The path to the BSP file in the filesystem. This can be anywhere on the hard drive.
- Texture path: The path to the textures folder containing textures to use on the imported BSPs. This can point to a folder anywhere in the filesystem, or to a folder in the project's /Assets folder.
	- If it points to a folder in Assets, it will use the textures in the project in PNG, JPG or TGA formats.
	- If it points to a folder elsewhere in the filesystem (this is the only option at runtime), it can load images of format PNG, JPG or TGA but Material instances cannot be saved into the project.
- Materials path: The path within Assets to save generated Materials to or load from. If a material already exists at the same path, it will use it rather than generate a new one.
- Mesh path: The path within Assets to save generated meshes to, as well as the prefab for the map.
- Mesh Combining: Contains options for combining meshes after they are generated. This can clean up the hierarchy and will save fewer meshes into the Assets folder.
	- None: Meshes will not be combined. This will create a GameObject for every face in the map and place them in the hierarchy, under a GameObject for their material, under their Entity's GameObject.
	- Per Material: This will only combine faces which use the same material if possible, and place the GameObject for it under their Entity's GameObject.
	- Per Entity: This will combine all meshes generated for an Entity into one Mesh. This will have the cleanest hierarchy and save the fewest assets, but maps with a lot of vertices or curves may generate too many vertices for a single mesh and cause corruption.
- Assets to save: What generated assets will be saved into the project, in the editor only.
	- None: No assets will be saved into the project. You can save the scene and everything will be serialized into the scene, which will bloat the size of your scene, and the assets can't be reused for other imports.
	- Materials: Only materials will be saved into the project. Meshes and the map will not be saved. You cannot save materials into the project if the textures aren't in the project as well (in the Assets folder).
	- Meshes: Only meshes will be saved into the project. Not recommended, the meshes will not keep their material references.
	- Materials and Meshes: All generated Materials and Meshes will be saved into the project, but no prefab will be created for the BSP. The scene can be saved without issue.
	- Prefab: Only saves a map prefab into the project. Not recommended, only the GameObject hierarchy can be saved.
	- Materials and Prefab: Generated materials and prefab will be saved into the project. Not recommended, generated Meshes will survive only in the scene if saved.
	- Meshes and Prefab: Genereated meshes and prefab will be saved into the project. Not recommended, generated Materials will survive only in the scene if saved.
	- Materials Meshes and Prefab: This is the safest bet. Everything is saved into the project and can be opened, modified and saved. Materials may be reused for multiple BSP imports.
- Curve Detail: Games based on the Quake 3 engine have a special type of surface called a Bezier patch, built from curves. This option controls the amount of detail used to tessellate these curves into triangles. Higher values give smoother curves, at the cost of exponentially more vertices. Use with caution, especially if combining meshes Per Entity. Excessively high values can create too many vertices and cause mesh corruption.

## Example usage
Each of these options exist in a `Settings` class within the BSPLoader class. Simply create a new Settings object:
```cs
BSPLoader.Settings settings = new BSPLoader.Settings();
settings.path = "Path_to_BSP";
settings.texturePath = "Path_to_textures";
// etc.
```
Create an instance of the BSPLoader class, set the settings and go!
```cs
BSPLoader loader = new BSPLoader();
loader.settings = settings;
loader.LoadBSP();
```

## Can this be used in a game build or while the editor is playing?
Yes! This plugin works at runtime as well as edit-time. This can give your players a way to load new levels into your game. The settings work a bit differently, though.
- Texture Path cannot point into the Assets folder at runtime, so textures must be loaded from elsewhere in the filesystem.
- Materials path, Mesh path and Assets to Save settings do nothing. At runtime, instances of these are created in memory and are lost when the scene is unloaded.

Give your players a way to set the appropriate options and they can take control!

## What if I want custom behavior for certain Entities?
This is easily done. Included in the `Settings` for an Import is an `entityCreatedCallback` member. You can set this up to call a method you write to postprocess entities however you'd like. You are also given a List of EntityInstances containing all Entities with their name matching the the given Entity's target.
```cs
settings.entityCreatedCallback = OnEntityCreated;

//...

private void OnEntityCreated(BSPLoader.EntityInstance instance, List<EntityInstance> targets) {
	if (instance.entity.className == "func_door") {
		// Attach a Door script you've written
		Door door = instance.gameObject.AddComponent<Door>();
		door.moveSpeed = instance.entity.GetFloat("speed");
		// Handle other properties.
	} else if (instance.entity.className == "func_button") {
		Button button = instance.gameObject.AddComponent<Button>();
		button.targets = targets;
		// ...
	}
}
```

## I use an ancient version of Unity and I refuse to update!
No problem! I've gone to great lengths to ensure compatibility with older Unity versions. I've tested with versions all the way back to Unity 3.4.0 from July 2011, which is the oldest version still available through the Unity3D website.
The only limitation is runtime loading of TGA files may not work properly before Unity 5.0.

## What this is NOT
This library only imports entities and their visual surfaces as meshes into Unity. This does not convert MDL files or implement Entity behavior, this is left up to the user (you!) to tailor behavior to your purposes.

## Okay, so how do I use it?
This library includes a simple editor window you can use to test it out. You can find this under BSP Importer->Import BSP in the main Unity editor window. The code for this window is in `Util\Editor\BSPImporterWindow.cs` and this file can be safely deleted without causing issues. The window's code can be used as an example of how to set up the process.

## License
<a href="http://creativecommons.org/publicdomain/zero/1.0/" rel="license"><img src="https://licensebuttons.net/p/zero/1.0/88x31.png" alt="Creative Commons 0" /></a><br>
To the extent possible under law, <a href="https://github.com/wfowler1">wfowler1</a> has waived all copyright and related or neighboring rights to Unity3D BSP Importer. This work is published from: United States.
