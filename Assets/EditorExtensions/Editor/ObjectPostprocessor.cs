// Copyright (c) 2017 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

//#define OBJECTPOSTPROCESSOR_DEBUGLOG

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace EditorExtensions
{

[InitializeOnLoad]
public class ObjectPostprocessor : AssetPostprocessor
{
    public delegate void AssetAddedHandler( string assetPath );
	public static event AssetAddedHandler assetAdded;

    public delegate void AssetMovedHandler( string assetPathFrom, string assetPathTo );
	public static event AssetMovedHandler assetMoved;

    public delegate void AssetDeletedHandler( string assetPath );
	public static event AssetDeletedHandler assetDeleted;

    public delegate void AssetDuplicatedHandler( string assetPathFrom, string assetPathTo );
	public static event AssetDuplicatedHandler assetDuplicated;

    public delegate void PrefabConnectedHandler( GameObject gameObjectFrom, string assetPathTo );
	public static event PrefabConnectedHandler prefabConnected;

    public delegate void GameObjectAddedHandler( GameObject gameObject );
	public static event GameObjectAddedHandler gameObjectAdded;

    public delegate void GameObjectDeletedHandler( Scene scene );
	public static event GameObjectDeletedHandler gameObjectDeleted;

    public delegate void GameObjectSceneMovedHandler( Scene sceneFrom, GameObject gameObject );
	public static event GameObjectSceneMovedHandler gameObjectSceneMoved;

    public delegate void GameObjectDuplicatedHandler( GameObject gameObjectFrom, GameObject gameObjectTo );
	public static event GameObjectDuplicatedHandler gameObjectDuplicated;

    public delegate void PrefabInstantiatedHandler( string assetPathFrom, GameObject gameObjectTo );
	public static event PrefabInstantiatedHandler prefabInstantiated;

	//--------------------------------------------------------------------------------------------------------

	static void _AssetAdded( string assetPath )
	{
		DebugLog( "AssetAdded: " + assetPath );
		if( assetAdded != null ) {
			assetAdded( assetPath );
		}
	}

	static void _AssetMoved( string assetPathFrom, string assetPathTo )
	{
		DebugLog( "AssetMoved: from: " + assetPathFrom + " to: " + assetPathTo );
		if( assetMoved != null ) {
			assetMoved( assetPathFrom, assetPathTo );
		}
	}

	static void _AssetDeleted( string assetPath )
	{
		DebugLog( "AssetDeleted: " + assetPath );
		if( assetDeleted != null ) {
			assetDeleted( assetPath );
		}
	}

	static void _AssetDuplicated( string assetPathFrom, string assetPathTo )
	{
		DebugLog( "AssetDuplicated: from: " + assetPathFrom + " to: " + assetPathTo );
		if( assetDuplicated != null ) {
			assetDuplicated( assetPathFrom, assetPathTo );
		}
	}

	static void _PrefabConnected( GameObject gameObjectFrom, string assetPathTo )
	{
		DebugLog( "PrefabConnected: gameObjectFrom: " + gameObjectFrom.name + " assetPathTo: " + assetPathTo );
		if( prefabConnected != null ) {
			prefabConnected( gameObjectFrom, assetPathTo );
		}
	}

	static void _GameObjectAdded( GameObject gameObject )
	{
		DebugLog( "GameObjectAdded: gameObject: " + gameObject.name );
		if( gameObjectAdded != null ) {
			gameObjectAdded( gameObject );
		}
	}

	static void _GameObjectDeleted( Scene scene )
	{
		DebugLog( "GameObjectDeleted: scene: " + scene.name );
		if( gameObjectDeleted != null ) {
			gameObjectDeleted( scene );
		}
	}

	static void _GameObjectSceneMoved( Scene sceneFrom, GameObject gameObject )
	{
		DebugLog( "GameObjectSceneMoved: sceneFrom: " + sceneFrom.name + " gameObject: " + gameObject.name );
		if( gameObjectSceneMoved != null ) {
			gameObjectSceneMoved( sceneFrom, gameObject );
		}
	}

	static void _GameObjectDuplicated( GameObject gameObjectFrom, GameObject gameObjectTo )
	{
		DebugLog( "GameObjectDuplicated: gameObjectFrom: " + gameObjectFrom.name + " gameObjectTo: " + gameObjectTo.name );
		if( gameObjectDuplicated != null ) {
			gameObjectDuplicated( gameObjectFrom, gameObjectTo );
		}
	}

	static void _PrefabInstantiated( string assetPathFrom, GameObject gameObjectTo )
	{
		DebugLog( "PrefabInstantiated: assetPathFrom: " + assetPathFrom + " gameObjectTo: " + gameObjectTo.name );
		if( prefabInstantiated != null ) {
			prefabInstantiated( assetPathFrom, gameObjectTo );
		}
	}

	//--------------------------------------------------------------------------------------------------------

	[System.Diagnostics.Conditional("OBJECTPOSTPROCESSOR_DEBUGLOG")]
	static void DebugLog( string text )
	{
		Debug.Log( text );
	}

	//--------------------------------------------------------------------------------------------------------

	static bool _isPlaying;

	static HashSet<string> _assetPaths;
	static HashSet<string> _inSelection_importedAssetPaths = new HashSet<string>();
	static string[] _selectionPaths;

	static _GameObjectsInScenes _gameObjectsInScenes;

	static GameObject _prev_selectionGameObject;
	static GameObject _selectionGameObject;
	static GameObject[] _prev_selectionGameObjects;
	static GameObject[] _selectionGameObjects;
	static _GameObjectHierarchy _prev_selectionGameObjectHierarchy;
	static _GameObjectHierarchy _selectionGameObjectHierarchy;
	static _GameObjectSiblings[] _prev_selectionGameObjectSiblings;
	static _GameObjectSiblings[] _selectionGameObjectSiblings;

	//--------------------------------------------------------------------------------------------------------

	static ObjectPostprocessor()
	{
		_assetPaths = _GetAssetPaths();

		_SetIsPlaying( EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying );

		EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
		Selection.selectionChanged += SelectionChanged;
	}

	static HashSet<string> _GetAssetPaths()
	{
		var assetPaths = new HashSet<string>();
		var allAssetPaths = AssetDatabase.GetAllAssetPaths();
		if( allAssetPaths != null ) {
			foreach( var assetPath in allAssetPaths )  {
				if( _IsTargetAssetPath( assetPath ) ) {
					assetPaths.Add( assetPath );
				}
			}
		}

		return assetPaths;
	}
	
	static void _SetIsPlaying( bool isPlaying )
	{
		_isPlaying = isPlaying;
		if( _isPlaying ) {
			_gameObjectsInScenes = null;

			_UpdateSelection();
			_UpdatePrevSelection();

			EditorApplication.hierarchyWindowChanged -= HierarchyWindowChanged;
		} else {
			_gameObjectsInScenes = new _GameObjectsInScenes();

			_UpdateSelection();
			_UpdatePrevSelection();

			EditorApplication.hierarchyWindowChanged += HierarchyWindowChanged;
		}
	}

	static void PlaymodeStateChanged()
	{
		bool isPlaying = (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying);
		if( _isPlaying != isPlaying ) {
			_SetIsPlaying( isPlaying );
		}
	}

	#if false
	static GameObject CreateGameObjectInScene( UnityEngine.SceneManagement.Scene scene, string name, HideFlags hideFlags )
	{
		var gameObject = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags( name, hideFlags );
		UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene( gameObject, scene );
		return gameObject;
	}
	#endif
	
	static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
    {
		if( deletedAssets != null ) {
			for( int i = 0; i < deletedAssets.Length; ++i ) {
				if( _IsTargetAssetPath( deletedAssets[i] ) ) {
					_assetPaths.Remove( deletedAssets[i] );
					_inSelection_importedAssetPaths.Remove( deletedAssets[i] );
					_StringsSetNull( _selectionPaths, deletedAssets[i] );

					_AssetDeleted( deletedAssets[i] );
				}
			}
		}

		if( movedAssets != null ) {
			for( int i = 0; i < movedAssets.Length; ++i ) {
				if( _IsTargetAssetPath( movedAssets[i] ) ) {
					if( _assetPaths.Remove( movedFromAssetPaths[i] ) ) {
						_assetPaths.Add( movedAssets[i] );
					}
					if( _inSelection_importedAssetPaths.Remove( movedFromAssetPaths[i] ) ) {
						_inSelection_importedAssetPaths.Add( movedAssets[i] );
					}
					_StringsExchange( _selectionPaths, movedFromAssetPaths[i], movedAssets[i] );

					_AssetMoved( movedFromAssetPaths[i], movedAssets[i] );
				}
			}
		}

		if( importedAssets != null ) {
			if( _IsNewAssetContains( importedAssets ) ) {
				string[] duplicatedAssetPaths = _GetDuplicatedAssetPaths( importedAssets );

				for( int i = 0; i < importedAssets.Length; ++i ) {
					if( _IsTargetAssetPath( importedAssets[i] ) && !_assetPaths.Contains( importedAssets[i] ) ) {
						_assetPaths.Add( importedAssets[i] );
						_inSelection_importedAssetPaths.Add( importedAssets[i] ); // Check for PrefabConnected
						if( duplicatedAssetPaths != null && duplicatedAssetPaths[i] != null ) {
							_AssetDuplicated( duplicatedAssetPaths[i], importedAssets[i] );
						} else {
							_AssetAdded( importedAssets[i] );
						}
					}
				}
			}
		}
    }

	static void _StringsSetNull( string[] strings, string str )
	{
		if( strings != null ) {
			for( int i = 0; i < strings.Length; ++i ) {
				if( strings[i] == str ) {
					strings[i] = null;
				}
			}
		}
	}

	static void _StringsExchange( string[] strings, string strFrom, string strTo )
	{
		if( strings != null ) {
			for( int i = 0; i < strings.Length; ++i ) {
				if( strings[i] == strFrom ) {
					strings[i] = strTo;
				}
			}
		}
	}

	static bool _IsTargetAssetPath( string assetPath )
	{
		return assetPath != null && assetPath.StartsWith("Assets/") && !assetPath.StartsWith("Assets/__DELETED_GUID_Trash/");
	}

	static bool _IsNewAssetContains( string[] importedAssets ) 
	{
		if( importedAssets != null ) {
			for( int i = 0; i < importedAssets.Length; ++i ) {
				if( _IsTargetAssetPath( importedAssets[i] ) && !_assetPaths.Contains( importedAssets[i] ) ) {
					return true;
				}
			}
		}

		return false;
	}

	static void HierarchyWindowChanged()
	{
		_CheckSceneChanged();
		_CheckSelectionObjects_GameObjectMoved();
	}

	static void _CheckSceneChanged()
	{
		if( _gameObjectsInScenes.CheckSceneChanged() ) {
			_prev_selectionGameObjectHierarchy.CheckSceneChanged();
			_selectionGameObjectHierarchy.CheckSceneChanged();
			_CheckSceneChanged( ref _prev_selectionGameObject, _prev_selectionGameObjects, _prev_selectionGameObjectSiblings );
			_CheckSceneChanged( ref _selectionGameObject, _selectionGameObjects, _selectionGameObjectSiblings );
		}
	}

	static void _CheckSceneChanged( ref GameObject gameObject, GameObject[] gameObjects, _GameObjectSiblings[] siblings )
	{
		if( gameObjects != null && siblings != null ) {
			for( int i = 0; i < siblings.Length; ++i ) {
				if( siblings[i] != null && !siblings[i].scene.isLoaded ) {
					if( gameObject == gameObjects[i] ) {
						gameObject = null;
					}
					gameObjects[i] = null;
					siblings[i] = null;
				}
			}
		}
	}

	static void _CheckSelectionObjects_GameObjectMoved()
	{
		if( _selectionGameObjectHierarchy == null ||
			_selectionGameObjectSiblings == null ||
			_selectionGameObjects == null ) {
			return;
		}

		for( int i = 0; i < _selectionGameObjects.Length; ++i ) {
			var siblings = _selectionGameObjectSiblings[i];
			var gameObject = _selectionGameObjects[i];
			if( siblings != null && gameObject != null ) {
				if( siblings.scene != gameObject.scene ) {
					_selectionGameObjectSiblings[i] = _selectionGameObjectHierarchy.Move( siblings, gameObject );
					_gameObjectsInScenes.Remove( gameObject );
					_gameObjectsInScenes.Add( gameObject );
					_GameObjectSceneMoved( siblings.scene, gameObject );
				}
			}
		}
	}

	static void _UpdatePrevSelection()
	{
		_prev_selectionGameObject = _selectionGameObject;
		_prev_selectionGameObjects = _selectionGameObjects;
		_prev_selectionGameObjectSiblings = _selectionGameObjectSiblings;
		_prev_selectionGameObjectHierarchy = _selectionGameObjectHierarchy;
	}

	static void _UpdateSelection()
	{
		var selectionObjects = Selection.objects;
		_selectionPaths = _CollectSelectionPaths( selectionObjects );
		
		_selectionGameObject = null;
		_selectionGameObjects = null;
		_selectionGameObjectHierarchy = null;
		_selectionGameObjectSiblings = null;

		if( _isPlaying ) {
			return;
		}

		if( selectionObjects != null && selectionObjects.Length > 0 ) {
			if( _selectionPaths == null || !_StringsIsFully( _selectionPaths ) ) {
				var selectionObject = Selection.activeObject;
				_selectionGameObjects = new GameObject[selectionObjects.Length];
				for( int i = 0; i < selectionObjects.Length; ++i ) {
					if( selectionObjects[i] != null ) {
						if( _selectionPaths == null || string.IsNullOrEmpty( _selectionPaths[i] ) ) {
							_selectionGameObjects[i] = selectionObjects[i] as GameObject;
							if( selectionObjects[i] == selectionObject ) {
								_selectionGameObject = _selectionGameObjects[i];
							}
						}
					}
				}
			}
		}

		if( _selectionGameObjects != null && _selectionGameObjects.Length > 0 ) {
			_selectionGameObjectHierarchy = new _GameObjectHierarchy( _selectionGameObjects );
			_selectionGameObjectSiblings = new _GameObjectSiblings[_selectionGameObjects.Length];
			for( int i = 0; i < _selectionGameObjects.Length; ++i ) {
				_selectionGameObjectSiblings[i] = _selectionGameObjectHierarchy.GetSiblings( _selectionGameObjects[i] );
			}
		} else {
			_selectionGameObjectHierarchy = new _GameObjectHierarchy( null ); // Note: Collection Root only.(for "GameObject/Creat Empty")
		}
	}

	static void SelectionChanged()
	{
		_UpdatePrevSelection();
		_UpdateSelection();

		_CheckSelectionObjects();
	}

	static string[] _CollectSelectionPaths( UnityEngine.Object[] selectionObjects )
	{
		if( selectionObjects != null ) {
			string[] selectionPaths = null;
			for( int i = 0; i < selectionObjects.Length; ++i ) {
				var assetPath = AssetDatabase.GetAssetPath( selectionObjects[i] );
				if( !string.IsNullOrEmpty( assetPath ) ) {
					if( selectionPaths == null ) {
						selectionPaths = new string[selectionObjects.Length];
					}
					selectionPaths[i] = assetPath;
				}
			}

			return selectionPaths;
		}

		return null;
	}
	
	/*
		Note: Increment Patterns for Asset Names.

		AssetName
		AssetName 1
		...
		
		AssetName 0
		AssetName 1
		...

		AssetName -1
		AssetName -2
		...
	*/

	struct _AssetPathTemp
	{
		public string assetPath;
		public string assetPathBase; // Without number and extention.
		public string extension;

		public ulong assetNum;

		public _AssetPathTemp( string assetPath )
		{
			this.assetPath = assetPath;
			this.assetPathBase = assetPath;
			this.extension = null;
			this.assetNum = 0;

			if( assetPath == null ) {
				return;
			}

			int length = assetPath.Length;
			if( length == 0 ) {
				return;
			}

			int extensionPos;
			int i = _SkipExtention( assetPath, out extensionPos );
			if( extensionPos >= 0 ) {
				this.extension = assetPath.Substring( extensionPos );
			}

			ulong num = 0;
			int numPos = -1;
			unchecked {
				for( ulong scl = 1; i >= 0; --i, scl *= 10 ) {
					char c = assetPath[i];
					ulong t = (ulong)(c - '0');
					if( t <= 9 ) {
						numPos = i;
						num += t * scl;
					} else {
						break;
					}
				}
			}

			this.assetNum = num;

			if( numPos >= 0 ) {
				this.assetPathBase = this.assetPath.Substring( 0, numPos );
			} else if( extensionPos >= 0 ) {
				this.assetPathBase = this.assetPath.Substring( 0, extensionPos ) + " ";
			}
		}

		public void Increment()
		{
			++this.assetNum;

			if( this.assetPathBase != null ) {
				this.assetPath = this.assetPathBase + this.assetNum.ToString();
				if( this.extension != null ) {
					this.assetPath += this.extension;
				}
			}
		}

		static int _SkipExtention( string objectName, out int extensionPos )
		{
			extensionPos = -1;

			if( objectName == null ) {
				return 0;
			}

			int length = objectName.Length;
			if( length > 0 ) {
				for( int i = length - 1; i >= 0; --i ) {
					char c = objectName[i];
					if( c == '.' ) {
						if( i > 0 ) {
							extensionPos = i; // Has extention.
							return i - 1;
						} else {
							return length - 1; // No extention.
						}
					} else if( c == '/' || c == '\\' ) {
						return length - 1; // No extention.
					}
				}

				return length - 1;
			}

			return 0;
		}
	}

	// Note: Must be updated when naming rules are changed.
	static string[] _GetDuplicatedAssetPaths( string[] importedAssets )
	{
		if( _assetPaths == null || _selectionPaths == null || importedAssets == null ) {
			return null;
		}

		string[] sourceAssetPaths = null;
		HashSet<string> reservedAssetPaths = null;

		for( int i = 0; i < _selectionPaths.Length; ++i ) {
			string assetPath = _selectionPaths[i];
			if( string.IsNullOrEmpty( assetPath ) ) {
				continue;
			}

			_AssetPathTemp destAssetPathTemp = new _AssetPathTemp( assetPath );

			for(;;) {
				destAssetPathTemp.Increment();
				if( _assetPaths.Contains( destAssetPathTemp.assetPath ) ) {
					continue;
				}
				if( reservedAssetPaths != null && reservedAssetPaths.Contains( destAssetPathTemp.assetPath ) ) {
					continue;
				}

				break;
			}

			int index = _StringsIndexOf( importedAssets, destAssetPathTemp.assetPath );
			if( index >= 0 ) {
				if( sourceAssetPaths == null ) {
					sourceAssetPaths = new string[importedAssets.Length];
					reservedAssetPaths = new HashSet<string>( _assetPaths );
				}
				sourceAssetPaths[index] = assetPath;
				reservedAssetPaths.Add( destAssetPathTemp.assetPath );
			}
		}

		return sourceAssetPaths;
	}

	static bool _StringsIsFully( string[] strings )
	{
		if( strings != null ) {
			foreach( var str in strings ) {
				if( string.IsNullOrEmpty( str ) ) {
					return false;
				}
			}

			return true;
		} else {
			return false;
		}
	}

	static bool _StringsIsEmpty( string[] strings )
	{
		if( strings != null ) {
			foreach( var str in strings ) {
				if( !string.IsNullOrEmpty( str ) ) {
					return false;
				}
			}

			return true;
		} else {
			return true;
		}
	}

	static int _StringsIndexOf( string[] strings, string str )
	{
		if( strings != null && str != null ) {
			for( int i = 0; i < strings.Length; ++i ) {
				if( strings[i] != null && strings[i] == str ) {
					return i;
				}
			}
		}

		return -1;
	}
	
	/*
		Note: Increment Patterns for GameObject Names.

		GameObject
		GameObject (1)
		...
		
		GameObject (0)
		GameObject (1)
		...

		GameObject (-1)
		GameObject (-1) (1)
		GameObject (-1) (2)
		...

		GameObject -(0)
		GameObject - (1)
		GameObject - (2)
		...

		GameObject -(1)
		GameObject - (1)
		GameObject - (2)
		...

		GameObject -(2)
		GameObject - (2)
		GameObject - (3)
		...

		(0)
		 (1)
		...

		(1)
		 (1)
		...
	*/

	static bool _CheckNewInstanceAvailable()
	{
		if( _prev_selectionGameObjectHierarchy == null ||
			_selectionGameObjects == null ||
			_selectionGameObjects.Length == 0 ) {
			return false;
		}

		bool isNewInstanceAvailable = false;
		for( int i = 0; i < _selectionGameObjects.Length; ++i ) {
			GameObject destGameObject = _selectionGameObjects[i];
			if( destGameObject == null ) {
				continue; // Asset.
			}

			if( _gameObjectsInScenes.Contains( destGameObject ) ) {
				continue; // Not new instance.
			}

			isNewInstanceAvailable = true;

			var destSiblings = _prev_selectionGameObjectHierarchy.GetSiblings( destGameObject );
			if( destSiblings != null && !destSiblings.Contains( destGameObject ) ) {
				destSiblings.markNewInstanceAvailable = true; // Note: Edit _prev_selectionGameObjectHierarchy directlly.
			}
		}

		return isNewInstanceAvailable;
	}

	// Note: Must be updated when naming rules are changed.
	static GameObject[] _GetDuplicatedGameObjects()
	{
		if( _prev_selectionGameObjectHierarchy == null ||
			_prev_selectionGameObjects == null ||
			_prev_selectionGameObjects.Length == 0 ||
			_selectionGameObjects == null ||
			_selectionGameObjects.Length == 0 ) {
			return null;
		}

		Scene activeScene = new Scene();
		if( _prev_selectionGameObject != null ) {
			activeScene = _prev_selectionGameObject.scene;
		}

		GameObject[] duplicatedGameObjects = null;

		for( int i = 0; i < _prev_selectionGameObjects.Length; ++i ) {
			var sourceGameObject = _prev_selectionGameObjects[i];
			if( sourceGameObject == null ) {
				continue; // Skip asset.
			}

			var index = _GetDuplicatedGameObjectIndex( sourceGameObject, activeScene );
			if( index >= 0 ) {
				if( duplicatedGameObjects == null ) {
					duplicatedGameObjects = new GameObject[_selectionGameObjects.Length];
				}
				duplicatedGameObjects[index] = sourceGameObject;
			}
		}
		
		return duplicatedGameObjects;
	}
	
	static int _GetDuplicatedGameObjectIndex( GameObject sourceGameObject, Scene activeScene )
	{
		var sourceSiblings = _prev_selectionGameObjectHierarchy.GetSiblings( sourceGameObject );
		if( sourceSiblings == null || !sourceSiblings.markNewInstanceAvailable ) {
			return -1;
		}

		string gameObjectNewName = sourceGameObject.name;
		if( sourceGameObject.scene == activeScene ) { // Note: This rule is for active scene only. (Might be bug.)
			// Generate duplicated name.
			var gameObjectNameTemp = new _GameObjectNameTemp( sourceGameObject.name );
			for(;;) {
				gameObjectNameTemp.Increment();
				if( !sourceSiblings.Contains( gameObjectNameTemp.gameObjectName ) ) {
					gameObjectNewName = gameObjectNameTemp.gameObjectName;
					break;
				}
			}
		}

		for( int i = 0; i < _selectionGameObjects.Length; ++i ) {
			GameObject destGameObject = _selectionGameObjects[i];
			if( destGameObject == null ) {
				continue; // Asset or same instance.
			}
			if( _gameObjectsInScenes.Contains( destGameObject ) ) {
				continue; // Not new instance.
			}

			var destSiblings = _prev_selectionGameObjectHierarchy.GetSiblings( destGameObject );
			if( destSiblings == null ) {
				continue; // No siblings. (New selected.)
			}

			if( destSiblings.Contains( destGameObject ) ) {
				continue; // Failsafe.
			}

			if( destSiblings != sourceSiblings ) {
				continue; // Different hierarchy.
			}

			if( destGameObject.name == gameObjectNewName ) {
				sourceSiblings.Add( destGameObject ); // Note: Edit _prev_selectionGameObjectHierarchy directlly.
				return i;
			}
		}

		return -1;
	}

	struct _GameObjectNameTemp
	{
		public string gameObjectName;
		public string gameObjectNameBase; // Without number and extention.

		public ulong gameObjectNum;

		public _GameObjectNameTemp( string gameObjectName )
		{
			this.gameObjectName = gameObjectName;
			this.gameObjectNameBase = gameObjectName;
			this.gameObjectNum = 0;

			if( gameObjectName == null ) {
				return;
			}

			this.gameObjectNameBase = gameObjectName + " ";

			int length = gameObjectName.Length;
			if( length < 3 ) {
				return;
			}
			if( gameObjectName[length - 1] != ')' ) {
				return;
			}

			int i = length - 2;
			ulong num = 0;
			int numPos = -1;
			bool bracketFound = false;
			unchecked {
				for( ulong scl = 1; i >= 0; --i, scl *= 10 ) {
					char c = gameObjectName[i];
					ulong t = (ulong)(c - '0');
					if( t <= 9 ) {
						numPos = i;
						num += t * scl;
					} else {
						if( c != '(' ) {
							return;
						}

						bracketFound = true;
						break;
					}
				}
			}

			if( !bracketFound || numPos <= 0 ) {
				return;
			}

			this.gameObjectNum = num;
			this.gameObjectNameBase = this.gameObjectName.Substring( 0, numPos - 1 );
			if( string.IsNullOrEmpty( this.gameObjectNameBase ) ) {
				this.gameObjectNameBase = " ";
			}
		}

		public void Increment()
		{
			++this.gameObjectNum;

			if( this.gameObjectNameBase != null ) {
				this.gameObjectName = this.gameObjectNameBase + '(' + this.gameObjectNum.ToString() + ')';
			}
		}
	}

	static void _CheckSelectionObjects()
	{
		var dragAndDropObjects = DragAndDrop.objectReferences;
		foreach( var importedAssetPath in _inSelection_importedAssetPaths ) {
			_CheckSelectionObjects_PrefabConnected( dragAndDropObjects, importedAssetPath );
		}
		_inSelection_importedAssetPaths.Clear();

		if( !_isPlaying && _gameObjectsInScenes != null ) {
			_gameObjectsInScenes.CheckDeleted();

			if( _CheckNewInstanceAvailable() ) {
				var duplicatedGameObjects = _GetDuplicatedGameObjects();
				if( _selectionGameObjects != null ) {
					for( int i = 0; i < _selectionGameObjects.Length; ++i ) {
						if( _selectionGameObjects[i] != null ) {
							if( !_gameObjectsInScenes.Contains( _selectionGameObjects[i] ) ) {
								_gameObjectsInScenes.Add( _selectionGameObjects[i] );
								_CheckSelectionObjects_GameObject( dragAndDropObjects, _selectionGameObjects[i],
									(duplicatedGameObjects != null) ? duplicatedGameObjects[i] : null );
							}
						}
					}
				}
			}
		}
	}
	
	static bool _CheckSelectionObjects_PrefabConnected( UnityEngine.Object[] dragAndDropObjects, string assetPath )
	{
		if( dragAndDropObjects != null ) {
			foreach( var obj in dragAndDropObjects ) {
				var gameObject = obj as GameObject;
				if( gameObject != null ) {
					var prefabParent = PrefabUtility.GetPrefabParent( gameObject );
					if( prefabParent != null && AssetDatabase.GetAssetPath( prefabParent ) == assetPath ) {
						_PrefabConnected( gameObject, assetPath );
						return true;
					}
				}
			}
		}

		return false;
	}

	static void _CheckSelectionObjects_GameObject( UnityEngine.Object[] dragAndDropObjects, GameObject gameObject, GameObject duplicatedGameObject )
	{
		if( _CheckSelectionObjects_PrefabInstantiated( dragAndDropObjects, gameObject ) ) {
			return;
		}

		if( duplicatedGameObject != null ) {
			_GameObjectDuplicated( duplicatedGameObject, gameObject );
			return;
		}

		// Finally, this gameObject will be added
		_GameObjectAdded( gameObject );
	}

	static bool _CheckSelectionObjects_PrefabInstantiated( UnityEngine.Object[] dragAndDropObjects, GameObject gameObject )
	{
		if( dragAndDropObjects != null ) {
			var prefabParent = PrefabUtility.GetPrefabParent( gameObject );
			if( prefabParent != null && _Contains( dragAndDropObjects, prefabParent ) ) {
				_PrefabInstantiated( AssetDatabase.GetAssetPath( prefabParent ), gameObject );
				return true;
			}
		}

		return false;
	}

	static bool _Contains< Type >( Type[] values, Type value )
		where Type : class
	{
		if( values != null ) {
			foreach( var v in values ) {
				if( v == value ) {
					return true;
				}
			}
		}

		return false;
	}

	//--------------------------------------------------------------------------------------------------------------------

	class _GameObjectsInScenes
	{
		static Dictionary<Scene, HashSet<GameObject>> _gameObjectsInScenes;

		public _GameObjectsInScenes()
		{
			_gameObjectsInScenes = _GetGameObjectsInScenes();
		}

		Dictionary<Scene, HashSet<GameObject>> _GetGameObjectsInScenes()
		{
			var sceneCount = EditorSceneManager.sceneCount;
			var gameObjectsInScenes = new Dictionary<Scene, HashSet<GameObject>>();
			for( int i = 0; i < sceneCount; ++i ) {
				var scene = EditorSceneManager.GetSceneAt( i );
				if( scene.isLoaded ) {
					gameObjectsInScenes.Add( scene, _GetGameObjectsInScene( scene ) );
				}
			}

			return gameObjectsInScenes;
		}

		HashSet<GameObject> _GetGameObjectsInScene( Scene scene )
		{
			var gameObjects = new HashSet<GameObject>();
			var rootGameObjects = scene.GetRootGameObjects();
			foreach( var rootGameObject in rootGameObjects ) {
				if( rootGameObject != null ) {
					_GetGameObjectsInScenes( gameObjects, rootGameObject.transform );
				}
			}

			return gameObjects;
		}

		void _GetGameObjectsInScenes( HashSet<GameObject> gameObjects, Transform transform )
		{
			if( transform != null ) {
				gameObjects.Add( transform.gameObject );

				var childCount = transform.childCount;
				for( int i = 0; i < childCount; ++i ) {
					_GetGameObjectsInScenes( gameObjects, transform.GetChild( i ) );
				}
			}
		}

		public void Add( GameObject gameObject )
		{
			if( gameObject != null ) {
				HashSet<GameObject> gameObjects;
				if( !_gameObjectsInScenes.TryGetValue( gameObject.scene, out gameObjects ) ) {
					gameObjects = new HashSet<GameObject>();
					_gameObjectsInScenes.Add( gameObject.scene, gameObjects );
				}

				_GetGameObjectsInScenes( gameObjects, gameObject.transform );
			}
		}

		public void Remove( GameObject gameObject )
		{
			if( gameObject != null ) {
				foreach( var gameObjectInScene in _gameObjectsInScenes ) {
					if( gameObjectInScene.Value.Remove( gameObject ) ) {
						gameObjectInScene.Value.RemoveWhere( g => g.transform.IsChildOf( gameObject.transform ) );
						return;
					}
				}
			}
		}

		public bool Contains( GameObject gameObject )
		{
			Scene tempScene;
			return _TryGetSceneFromGameObject( gameObject, out tempScene );
		}

		static bool _TryGetSceneFromGameObject( GameObject gameObject, out Scene scene )
		{
			if( gameObject != null ) {
				foreach( var gameObjectInScenes in _gameObjectsInScenes ) {
					if( gameObjectInScenes.Value.Contains( gameObject ) ) {
						scene = gameObjectInScenes.Key;
						return true;
					}
				}
			}

			scene = new Scene();
			return false;
		}

		public void CheckDeleted()
		{
			foreach( var gameObjectInScene in _gameObjectsInScenes ) {
				if( gameObjectInScene.Value.RemoveWhere( g => g == null ) > 0 ) {
					_GameObjectDeleted( gameObjectInScene.Key );
				}
			}
		}

		public bool CheckSceneChanged()
		{
			bool changedAnything = false;

			for(;;) {
				bool removedAnything = false;
				foreach( var gameObjectInScene in _gameObjectsInScenes ) {
					if( !gameObjectInScene.Key.isLoaded ) {
						_gameObjectsInScenes.Remove( gameObjectInScene.Key );
						changedAnything = true;
						removedAnything = true;
						break;
					}
				}
				if( !removedAnything ) {
					break;
				}
			}

			var sceneCount = SceneManager.sceneCount;
			for( int i = 0; i < sceneCount; ++i ) {
				var scene = SceneManager.GetSceneAt( i );
				if( scene.isLoaded && !_gameObjectsInScenes.ContainsKey( scene ) ) {
					_gameObjectsInScenes.Add( scene, _GetGameObjectsInScene( scene ) );
					changedAnything = true;
				}
			}

			return changedAnything;
		}
	}

	class _GameObjectSiblings
	{
		List<GameObject> gameObjects = new List<GameObject>();
		HashSet<string> names = new HashSet<string>();

		public Scene scene;
		public bool markNewInstanceAvailable = false;

		public _GameObjectSiblings( Scene scene )
		{
			this.scene = scene;
		}

		public _GameObjectSiblings( Scene scene, GameObject[] gameObjects )
		{
			this.scene = scene;
			Add( gameObjects );
		}

		public void Add( GameObject gameObject )
		{
			if( gameObject != null ) {
				this.gameObjects.Add( gameObject );
				this.names.Add( gameObject.name );
			}
		}

		public void Add( GameObject[] gameObjects )
		{
			if( gameObjects != null ) {
				foreach( var gameObject in gameObjects ) {
					Add( gameObject );
				}
			}
		}

		public bool Contains( GameObject gameObject )
		{
			if( gameObject != null ) {
				return this.gameObjects.Contains( gameObject );
			}

			return false;
		}

		public bool Contains( string name )
		{
			if( name != null ) {
				return this.names.Contains( name );
			}

			return false;
		}

		public void Remove( GameObject gameObject )
		{
			if( gameObject != null ) {
				this.gameObjects.Remove( gameObject );
				this.names.Remove( gameObject.name );
			}
		}
	}

	class _GameObjectHierarchy
	{
		Dictionary<Scene, _GameObjectSiblings> _rootSiblings = new Dictionary<Scene, _GameObjectSiblings>();
		Dictionary<Transform, _GameObjectSiblings> _siblings = new Dictionary<Transform, _GameObjectSiblings>();

		public _GameObjectHierarchy( GameObject[] selectionGameObjects )
		{
			// Note: Collect minimum siblings.

			_CollectSiblingsInScenes(); // for "GameObject/Create Empty"

			if( selectionGameObjects != null ) {
				foreach( var gameObject in selectionGameObjects ) {
					if( gameObject != null ) {
						_CollectSiblings( gameObject.transform ); // for "GameObject/Create Empty Child"
						_CollectSiblings( gameObject.transform.parent ); // for "Edit/Duplicate"
					}
				}
			}
		}

		public _GameObjectSiblings GetSiblings( GameObject gameObject )
		{
			_GameObjectSiblings siblings;
			if( gameObject != null ) {
				var parent = gameObject.transform.parent;
				if( parent == null ) {
					if( _rootSiblings.TryGetValue( gameObject.scene, out siblings ) ) {
						return siblings;
					}
				} else {
					if( _siblings.TryGetValue( parent, out siblings ) ) {
						return siblings;
					}
				}
			}

			return null;
		}

		public _GameObjectSiblings Move( _GameObjectSiblings siblingsFrom, GameObject gameObject )
		{
			if( siblingsFrom != null && gameObject != null ) {
				siblingsFrom.Remove( gameObject );

				var siblingsTo = GetSiblings( gameObject );
				if( siblingsTo != null ) {
					siblingsTo.Add( gameObject );
					return siblingsTo;
				}

				if( gameObject.transform.parent != null ) {
					return _CollectSiblings( gameObject.transform.parent );
				} else {
					return _CollectSiblingsInScene( gameObject.scene );
				}
			}

			return null;
		}

		void _CollectSiblingsInScenes()
		{
			var sceneCount = SceneManager.sceneCount;
			for( int i = 0; i < sceneCount; ++i ) {
				_CollectSiblingsInScene( SceneManager.GetSceneAt( i ) );
			}
		}

		_GameObjectSiblings _CollectSiblingsInScene( Scene scene )
		{
			if( scene.isLoaded ) {
				var siblings = new _GameObjectSiblings( scene, scene.GetRootGameObjects() );
				_rootSiblings.Add( scene, siblings );
				return siblings;
			} else {
				return null;
			}
		}

		_GameObjectSiblings _CollectSiblings( Transform parent )
		{
			if( parent == null ) {
				return null;
			}

			_GameObjectSiblings siblings;
			if( _siblings.TryGetValue( parent, out siblings ) ) {
				return siblings;
			}

			siblings = new _GameObjectSiblings( parent.gameObject.scene );
			for( int i = 0; i < parent.childCount; ++i ) {
				siblings.Add( parent.GetChild( i ).gameObject );
			}
			_siblings.Add( parent, siblings );
			return siblings;
		}

		public void CheckSceneChanged()
		{
			for(;;) {
				bool removedAnything = false;
				foreach( var rootSiblings in _rootSiblings ) {
					if( !rootSiblings.Key.isLoaded ) {
						_rootSiblings.Remove( rootSiblings.Key );
						removedAnything = true;
						break;
					}
				}
				if( !removedAnything ) {
					break;
				}
			}

			for(;;) {
				bool removedAnything = false;
				foreach( var siblings in _siblings ) {
					if( !siblings.Value.scene.isLoaded ) {
						_siblings.Remove( siblings.Key );
						removedAnything = true;
						break;
					}
				}
				if( !removedAnything ) {
					break;
				}
			}

			var sceneCount = SceneManager.sceneCount;
			for( int i = 0; i < sceneCount; ++i ) {
				var scene = SceneManager.GetSceneAt( i );
				if( scene.isLoaded && !_rootSiblings.ContainsKey( scene ) ) {
					_CollectSiblingsInScene( scene );
				}
			}
		}
	}
}

}
