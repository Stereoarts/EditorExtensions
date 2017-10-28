// Copyright (c) 2017 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace EditorExtensions
{

[InitializeOnLoad]
public class ObjectPostprocessorTest
{
	static ObjectPostprocessorTest()
	{
		ObjectPostprocessor.assetAdded += AssetAdded;
		ObjectPostprocessor.assetMoved += AssetMoved;
		ObjectPostprocessor.assetDeleted += AssetDeleted;
		ObjectPostprocessor.assetDuplicated += AssetDuplicated;
		ObjectPostprocessor.prefabConnected += PrefabConnected;

		ObjectPostprocessor.gameObjectAdded += GameObjectAdded;
		ObjectPostprocessor.gameObjectSceneMoved += GameObjectSceneMoved;
		ObjectPostprocessor.gameObjectDeleted += GameObjectDeleted;
		ObjectPostprocessor.gameObjectDuplicated += GameObjectDuplicated;
		ObjectPostprocessor.prefabInstanciated += PrefabInstantiated;
	}

    public static void AssetAdded( string assetPath )
	{
		Debug.Log( "AssetAdded: " + assetPath );
	}

    public static void AssetMoved( string assetPathFrom, string assetPathTo )
	{
		Debug.Log( "AssetMoved: from: " + assetPathFrom + " to: " + assetPathTo );
	}

    public static void AssetDeleted( string assetPath )
	{
		Debug.Log( "AssetDeleted: " + assetPath );
	}

    public static void AssetDuplicated( string assetPathFrom, string assetPathTo )
	{
		Debug.Log( "AssetDuplicated: from: " + assetPathFrom + " to: " + assetPathTo );
	}

    public static void PrefabConnected( GameObject gameObjectFrom, string assetPathTo )
	{
		Debug.Log( "PrefabConnected: from: " + gameObjectFrom + " to: " + assetPathTo );
	}

    public static void GameObjectAdded( GameObject gameObject )
	{
		Debug.Log( "GameObjectAdded: " + gameObject );
	}

    public static void GameObjectDeleted( Scene scene )
	{
		Debug.Log( "GameObjectDeleted: scene: " + scene.name );
	}

    public static void GameObjectSceneMoved( Scene sceneFrom, GameObject gameObject )
	{
		Debug.Log( "GameObjectSceneMoved: sceneFrom: " + sceneFrom + " to: " + gameObject );
	}

    public static void GameObjectDuplicated( GameObject gameObjectFrom, GameObject gameObjectTo )
	{
		Debug.Log( "GameObjectDuplicated: from: " + gameObjectFrom + " to: " + gameObjectTo );
	}

    public static void PrefabInstantiated( string assetPathFrom, GameObject gameObjectTo )
	{
		Debug.Log( "PrefabInstantiated: from: " + assetPathFrom + " to: " + gameObjectTo );
	}

}

}
