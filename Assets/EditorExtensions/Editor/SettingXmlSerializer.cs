// Copyright (c) 2017 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using UnityEngine;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;

namespace EditorExtensions
{

public static class SettingXmlSerializer
{
	public class ExtraData
	{
		public string rootDirectory; // root directory for the serialization target.
		public Transform rootTransform; // root transform for the serialization target.
		public Dictionary<int, UnityEngine.Object> objectMap; // If this value is null, all meta:instanceID are removed in xml.
	}

	static class AssetPathHelper
	{
		static bool _IsSeparater( char c )
		{
			return c == '/' || c == '\\';
		}
		
		static int _GetDirectoryDepth( string directory )
		{
			if( string.IsNullOrEmpty( directory ) ) {
				return 0;
			}

			bool elem = false;
			int depth = 0;
			for( int i = 0; i < directory.Length; ++i ) {
				if( !_IsSeparater( directory[i] ) ) {
					if( !elem ) {
						elem = true;
						++depth;
					}
				} else {
					elem = false;
				}
			}

			return depth;
		}

		static string _RemoveSeparatorOnHead( string path )
		{
			if( path != null && path.Length > 0 && _IsSeparater( path[0] ) ) {
				return path.Substring( 1 );
			} else {
				return path;
			}
		}

		public static string GenerateRelativeAssetPath( string baseDirectory, string targetPath )
		{
			if( string.IsNullOrEmpty( baseDirectory ) || string.IsNullOrEmpty( targetPath ) ) {
				return targetPath;
			}

			int minLength = (baseDirectory.Length < targetPath.Length) ? baseDirectory.Length : targetPath.Length;
			int matchLength = 0;
			for( int i = 0; i < minLength; ++i ) {
				if( _IsSeparater( baseDirectory[i] ) && _IsSeparater( targetPath[i] ) ) {
					matchLength = i + 1;
				} else {
					if( baseDirectory[i] != targetPath[i] ) {
						break;
					}
					if( i + 1 == minLength ) {
						if( targetPath.Length > minLength && _IsSeparater( targetPath[minLength] ) ) {
							matchLength = i + 1;
						}
					}
				}
			}

			if( matchLength > 0 ) {
				baseDirectory = baseDirectory.Substring( matchLength );
				targetPath = targetPath.Substring( matchLength );
			}

			targetPath = _RemoveSeparatorOnHead( targetPath );

			int baseDepth = _GetDirectoryDepth( baseDirectory );
			if( baseDepth > 0 ) {
				var str = new System.Text.StringBuilder();
				for( int i = 0; i < baseDepth; ++i ) {
					str.Append("../");
				}
				return str.ToString() + targetPath;
			} else {
				return targetPath;
			}
		}

		public static string GenerateAssetPath( string rootDirectory, string relativeAssetPath )
		{
			if( string.IsNullOrEmpty( rootDirectory ) || string.IsNullOrEmpty( relativeAssetPath ) ) {
				return relativeAssetPath;
			}

			while( relativeAssetPath.StartsWith("../") || relativeAssetPath.StartsWith("..\\") ) {
				relativeAssetPath = relativeAssetPath.Substring( 3 );
				rootDirectory = System.IO.Path.GetDirectoryName( rootDirectory );
				if( rootDirectory == null ) {
					return "";
				}
			}

			return rootDirectory + '/' + relativeAssetPath;
		}
	}

	public class ObjectMeta
	{
		public string name;
		public string typeName;
		public int instanceID; // for objectMap only.
		public string assetPath;
		public string guid;
		public string relativeAssetPath;
		public string transformPath; // for Transform / GameObject

		public static ObjectMeta GetAt( UnityEngine.Object obj, ExtraData extraData )
		{
			ObjectMeta r = new ObjectMeta();
			if( obj != null ) {
				r.name = obj.name;
				r.typeName = obj.GetType().FullName;
				r.instanceID = obj.GetInstanceID();
				r.assetPath = UnityEditor.AssetDatabase.GetAssetPath( obj );
				if( !string.IsNullOrEmpty(r.assetPath) ) {
					r.guid = UnityEditor.AssetDatabase.AssetPathToGUID( r.assetPath );

					if( !string.IsNullOrEmpty( extraData.rootDirectory ) ) {
						r.relativeAssetPath = AssetPathHelper.GenerateRelativeAssetPath( extraData.rootDirectory, r.assetPath );
					}
				}

				r.transformPath = "";
				if( extraData != null && extraData.rootTransform != null ) {
					Transform transform = null;
					if( obj is GameObject ) {
						transform = ((GameObject)obj).transform;
					} else if( obj is Transform ) {
						transform = (Transform)obj;
					}

					if( transform != null && transform.IsChildOf( extraData.rootTransform ) ) {
						r.transformPath = _GetTransformPath( extraData.rootTransform, transform );
					}
				}
			}

			return r;
		}

		static string _GetTransformPath( Transform parent, Transform child )
		{
			if( parent != null && child != null && parent != child ) {
				bool isAdded = false;
				System.Text.StringBuilder transformPath = new System.Text.StringBuilder();
				while( child != null ) {
					if( isAdded ) {
						transformPath.Insert( 0, "/" );
					} else {
						isAdded = true;
					}
					transformPath.Insert( 0, child.name );
					child = child.parent;
					if( child == parent ) {
						return transformPath.ToString();
					}
				}
			}

			return "";
		}

		public UnityEngine.Object Find( ExtraData extraData )
		{
			if( extraData != null ) {
				if( extraData.objectMap != null ) {
					UnityEngine.Object obj;
					if( extraData.objectMap.TryGetValue( this.instanceID, out obj ) ) {
						return obj;
					}
				}
			}

			if( !string.IsNullOrEmpty( transformPath ) && extraData.rootTransform != null ) {
				var transform = extraData.rootTransform.FindChild( transformPath );
				if( transform != null ) {
					if( this.typeName == "UnityEngine.Transform" ) {
						return transform;
					} else if( this.typeName == "UnityEngine.GameObject" ) {
						return transform.gameObject;
					}
				}
			}

			string modifiedAssetPath = this.assetPath;
			if( !string.IsNullOrEmpty( this.relativeAssetPath ) && !string.IsNullOrEmpty( extraData.rootDirectory ) ) {
				modifiedAssetPath = AssetPathHelper.GenerateAssetPath( extraData.rootDirectory, this.relativeAssetPath );
			}

			// GUID & assetPath(relativeAssetPath)
			if( !string.IsNullOrEmpty( this.guid ) ) {
				string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath( this.guid );
				if( !string.IsNullOrEmpty( assetPath ) ) {
					if( assetPath == modifiedAssetPath || assetPath == this.assetPath ) {
						var obj = UnityEditor.AssetDatabase.LoadMainAssetAtPath( assetPath );
						if( obj != null && obj.GetType().FullName == this.typeName ) {
							return obj;
						}
					}
				}
			}

			// Relative assetPath
			if( !string.IsNullOrEmpty( modifiedAssetPath ) ) {
				var obj = UnityEditor.AssetDatabase.LoadMainAssetAtPath( modifiedAssetPath );
				if( obj != null && obj.GetType().FullName == this.typeName ) {
					return obj;
				}
			}

			// Abusolute assetPath
			if( !string.IsNullOrEmpty( this.assetPath ) ) {
				var obj = UnityEditor.AssetDatabase.LoadMainAssetAtPath( this.assetPath );
				if( obj != null && obj.GetType().FullName == this.typeName ) {
					return obj;
				}
			}

			// Finally, find by guid only.
			if( !string.IsNullOrEmpty( this.guid ) ) {
				string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath( this.guid );
				if( !string.IsNullOrEmpty( assetPath ) ) {
					var obj = UnityEditor.AssetDatabase.LoadMainAssetAtPath( assetPath );
					if( obj != null && obj.GetType().FullName == this.typeName ) {
						return obj;
					}
				}
			}

			return null;
		}

		public void WriteToXml( XmlWriter writer, ExtraData extraData = null )
		{
			writer.WriteAttributeString( "name", null, this.name );
			writer.WriteAttributeString( "typeName", null, this.typeName );
			if( extraData != null && extraData.objectMap != null ) {
				writer.WriteAttributeString( "instanceID", null, this.instanceID.ToString() );
			}
			if( !string.IsNullOrEmpty(this.assetPath) ) {
				writer.WriteAttributeString( "assetPath", null, this.assetPath );
			}
			if( !string.IsNullOrEmpty(this.relativeAssetPath) ) {
				writer.WriteAttributeString( "relativeAssetPath", null, this.relativeAssetPath );
			}
			if( !string.IsNullOrEmpty(this.guid) ) {
				writer.WriteAttributeString( "guid", null, this.guid );
			}
			if( !string.IsNullOrEmpty(this.transformPath) ) {
				writer.WriteAttributeString( "transformPath", null, this.transformPath );
			}
		}

		public void ReadFromXml( XmlReader reader )
		{
			this.name = reader.GetAttribute("name");
			this.typeName = reader.GetAttribute("typeName");
			int.TryParse( reader.GetAttribute("instanceID"), out this.instanceID );
			this.guid = reader.GetAttribute("guid");
			this.transformPath = reader.GetAttribute("transformPath");
			this.assetPath = reader.GetAttribute("assetPath");
			this.relativeAssetPath = reader.GetAttribute("relativeAssetPath");
		}
	}

	public static void WriteXml( XmlWriter writer, object obj, ExtraData extraData = null )
	{
		writer.WriteStartDocument();
		if( obj != null ) {
			writer.WriteStartElement( _GetXmlLocalName(obj.GetType()) );
			writer.WriteAttributeString( "xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance" );
			writer.WriteAttributeString( "xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema" );
			_WriteXmlFields( writer, obj, extraData );
			writer.WriteEndElement();
		}
		writer.WriteEndDocument();
	}

	static void _WriteXmlFields( XmlWriter writer, object obj, ExtraData extraData )
	{
		if( obj == null ) {
			return;
		}

		foreach( var field in obj.GetType().GetFields( BindingFlags.Public | BindingFlags.Instance ) ) {
			var value = field.GetValue( obj );
			writer.WriteStartElement(field.Name);
			if( value != null ) {
				if( field.FieldType.IsArray ) {
					var array = (System.Array)value;
					var valueType = field.FieldType.GetElementType();
					var xmlElementType = _GetXmlLocalName( valueType );
					for( int i = 0; i < array.Length; ++i ) {
						writer.WriteStartElement( xmlElementType );
						_WriteXmlValue( writer, array.GetValue( i ), extraData );
						writer.WriteEndElement();
					}
				} else {
					_WriteXmlValue( writer, value, extraData );
				}
			}
			writer.WriteEndElement();
		}
	}

	static void _WriteXmlValue( XmlWriter writer, object value, ExtraData extraData )
	{
		if( value == null ) {
			return;
		}

		if( typeof(UnityEngine.Object).IsAssignableFrom( value.GetType() ) ) {
			_WriteXmlValue_UnityEngine_Object( writer, value, extraData );
		} else {
			if( value.GetType().IsPrimitive ) {
				writer.WriteString( _ToString( value ) );
			} else if( value.GetType() == typeof(string) ) {
				writer.WriteString( (string)value );
			} else {
				_WriteXmlFields( writer, value, extraData );
			}
		}
	}

	static void _WriteXmlValue_UnityEngine_Object( XmlWriter writer, object value, ExtraData extraData )
	{
		var meta = ObjectMeta.GetAt( (UnityEngine.Object)value, extraData );
		meta.WriteToXml( writer, extraData );
		if( extraData != null && extraData.objectMap != null ) {
			extraData.objectMap.Add( meta.instanceID, (UnityEngine.Object)value );
		}
	}

	static string _ToString( object value )
	{
		return value.ToString();
	}

	static string _GetXmlLocalName( System.Type type )
	{
		if( type.IsPrimitive ) {
			if( type == typeof(bool) ) {
				return "boolean";
			} else if( type == typeof(System.SByte) ) {
				return "byte";
			} else if( type == typeof(System.Int16) ) {
				return "short";
			} else if( type == typeof(System.Int32) ) {
				return "int";
			} else if( type == typeof(System.Int64) ) {
				return "long";
			} else if( type == typeof(System.Byte) ) {
				return "unsignedByte";
			} else if( type == typeof(System.UInt16) ) {
				return "unsignedShort";
			} else if( type == typeof(System.UInt32) ) {
				return "unsignedInt";
			} else if( type == typeof(System.UInt64) ) {
				return "unsignedLong";
			} else if( type == typeof(float) ) {
				return "float";
			} else if( type == typeof(double) ) {
				return "double";
			} else {
				return "string";
			}
		} else if( type == typeof(string) ){
			return "string";
		} else {
			return type.Name;
		}
	}

	public static System.Object ReadXml( XmlReader reader, System.Type type, ExtraData extraData = null )
	{
		while( reader.Read() ) {
			//Debug.Log( "NodeType: " + reader.NodeType + " Name:" + reader.Name + " Value:" + reader.Value + " ValueType:" + reader.ValueType );
			switch( reader.NodeType ) {
			case XmlNodeType.Element:
				return _ReadXmlElement( reader, type, extraData );
			}
		}

		Debug.LogWarning( "Element is not terminated." );
		return null;
	}

	static void _SkipXmlElement( XmlReader reader )
	{
		while( reader.Read() ) {
			//Debug.Log( "NodeType: " + reader.NodeType + " Name:" + reader.Name + " Value:" + reader.Value + " ValueType:" + reader.ValueType );
			switch( reader.NodeType ) {
			case XmlNodeType.Element:
				_SkipXmlElement( reader );
				break;
			case XmlNodeType.EndElement:
				return;
			}
		}

		Debug.LogWarning( "Element is not terminated." );
	}

	static object _ReadXmlElement( XmlReader reader, System.Type elementType, ExtraData extraData )
	{
		System.Object obj = null;

		System.Collections.ArrayList arrayList = null;
		System.Type arrayElementType = null;
		if( elementType.IsArray ) {
			arrayList = new System.Collections.ArrayList();
			arrayElementType = elementType.GetElementType();
		}

		while( reader.Read() ) {
			//Debug.Log( "NodeType: " + reader.NodeType + " Name:" + reader.Name + " Value:" + reader.Value + " ValueType:" + reader.ValueType );
			switch( reader.NodeType ) {
			case XmlNodeType.Element:
				if( elementType.IsArray ) {
					arrayList.Add( _ReadXmlElement( reader, arrayElementType, extraData ) );
				} else {
					var field = elementType.GetField( reader.Name, BindingFlags.Public | BindingFlags.Instance );
					if( field != null ) {
						if( obj == null ) {
							try {
								obj = System.Activator.CreateInstance( elementType ); 
							} catch( System.Exception ) {
								Debug.LogWarning( "Instanciate failed: " + elementType.ToString() );
							}
						}

						if( obj != null ) { // Failsafe.
							if( typeof(UnityEngine.Object).IsAssignableFrom( field.FieldType ) ) {
								var element = _ReadXmlElement_UnityEngine_Object( reader, field.FieldType, extraData );
								if( element != null ) {
									try {
										field.SetValue( obj, element );
									} catch( System.Exception e ) { // Type mismatch.
										Debug.LogWarning( e.ToString() );
									}
								}
							} else {
								var element = _ReadXmlElement( reader, field.FieldType, extraData );
								if( element != null ) {
									try {
										field.SetValue( obj, element );
									} catch( System.Exception e ) { // Type mismatch.
										Debug.LogWarning( e.ToString() );
									}
								}
							}
						} else {
							_SkipXmlElement( reader );
						}
					} else {
						_SkipXmlElement( reader );
					}
				}
				break;
			case XmlNodeType.Text:
				if( !elementType.IsArray ) { // Failsafe.
					if( typeof(UnityEngine.Object).IsAssignableFrom( elementType ) ) {
						Debug.LogWarning( "Unsupported type: " + elementType.ToString() );
					} else {
						if( elementType == typeof(string) ) {
							obj = reader.Value;
						} else {
							var parse = elementType.GetMethod("Parse",
								BindingFlags.Public | BindingFlags.Static, null, new [] { typeof(string) }, null);
							if( parse != null ) {
								try {
									obj = parse.Invoke(null, new object[] { reader.Value });
								} catch( System.Exception ) {
									Debug.LogWarning( "Parse failed: " + elementType.ToString() + " Value: " + reader.Value );
								}
							} else {
								Debug.LogWarning( "Unsupported type: " + elementType.ToString() );
							}
						}
					}
				} else {
					Debug.LogWarning( "Unknown flow." );
				}
				break;
			case XmlNodeType.EndElement:
				if( elementType.IsArray ) {
					obj = System.Array.CreateInstance( arrayElementType, arrayList.Count );
					for( int i = 0; i < arrayList.Count; ++i ) {
						((System.Array)obj).SetValue( arrayList[i], i );
					}
				}

				return obj;
			}
		}

		Debug.LogWarning( "Element is not terminated." );
		return null;
	}

	static UnityEngine.Object _ReadXmlElement_UnityEngine_Object( XmlReader reader, System.Type elementType, ExtraData extraData = null )
	{
		var meta = new ObjectMeta();
		meta.ReadFromXml( reader );
		return meta.Find( extraData );
	}

	public static string SerializeXmlToString( object settings, ExtraData extraData )
	{
		try {
			using( var stringWriter = new System.IO.StringWriter() ) {
				var xmlWriterSettings = new XmlWriterSettings {
					Indent = true,
				};
				using( XmlWriter xmlWriter = XmlWriter.Create( stringWriter, xmlWriterSettings ) ) {
					SettingXmlSerializer.WriteXml( xmlWriter, settings, extraData );
				}
				return stringWriter.ToString();
			}
		} catch( System.Exception e ) {
			Debug.LogError( e.ToString() );
			return null;
		}
	}

	public static byte[] SerializeXmlToBytes( object settings, ExtraData extraData )
	{
		try {
			using( var memoryStream = new System.IO.MemoryStream() ) {
				var xmlWriterSettings = new XmlWriterSettings {
					Indent = true,
					Encoding = new System.Text.UTF8Encoding(true),
				};
				using( XmlWriter xmlWriter = XmlWriter.Create( memoryStream, xmlWriterSettings ) ) {
					WriteXml( xmlWriter, settings, extraData );
				}
				return memoryStream.ToArray();
			}
		} catch( System.Exception e ) {
			Debug.LogError( e.ToString() );
			return null;
		}
	}
		
	public static Type DeserializeXml< Type >( string serializedString, ExtraData extraData )
	{
		try {
			using( var stringReader = new System.IO.StringReader( serializedString ) ) {
				using( XmlReader reader = XmlReader.Create( stringReader ) ) {
					var r = (Type)ReadXml(reader, typeof(Type), extraData);
					return r;
				}
			}
		} catch( System.Exception e ) {
			Debug.LogError( e.ToString() );
			return default(Type);
		}
	}

	public static Type DeserializeXml< Type >( byte[] bytes, ExtraData extraData )
	{
		try {
			using( var memoryStream = new System.IO.MemoryStream( bytes ) ) {
				using( XmlReader reader = XmlReader.Create( memoryStream ) ) {
					var r = (Type)ReadXml(reader, typeof(Type), extraData);
					return r;
				}
			}
		} catch( System.Exception e ) {
			Debug.LogError( e.ToString() );
			return default(Type);
		}
	}
}

}
