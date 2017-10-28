// Copyright (c) 2017 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

namespace EditorExtensions
{

public static class CopyUtility
{
	static bool _IsAssignable( System.Type type )
	{
		if( type != null ) {
			if( type.IsArray ) {
				return false;
			}

			if( type.IsPrimitive || type == typeof(string) ) {
				return true;
			}
			if( typeof(UnityEngine.Object).IsAssignableFrom( type ) ) {
				return true;
			}
		}

		return false;
	}

	public static Type DeepCopy< Type >( Type obj )
	{
		if( obj != null ) {
			var objType = obj.GetType();

			if( objType.IsArray ) {
				var objArray = (System.Array)(object)obj;
				var objArrayLength = objArray.Length;
				var newObj = (System.Array)System.Activator.CreateInstance( objType, objArrayLength );
				if( newObj != null ) {
					if( _IsAssignable( objType.GetElementType() ) ) {
						for( int i = 0; i < objArrayLength; ++i ) {
							newObj.SetValue( objArray.GetValue( i ), i );
						}
					} else {
						for( int i = 0; i < objArrayLength; ++i ) {
							newObj.SetValue( DeepCopy( objArray.GetValue( i ) ), i );
						}
					}
				}

				return (Type)(object)newObj;
			} else {
				if( !_IsAssignable( objType ) ) {
					try {
						var newObj = System.Activator.CreateInstance( objType );
						if( newObj != null ) {
							var objFields = objType.GetFields( System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public );
							if( objFields != null ) {
								foreach( var objField in objFields ) {
									objField.SetValue( newObj, DeepCopy( objField.GetValue( obj ) ) );
								}
							}
								
							return (Type)newObj;
						}
					} catch( System.Exception e ) {
						UnityEngine.Debug.LogError( e.ToString() );
					}
				}
				return obj;
			}
		}

		return default(Type);
	}
}

}