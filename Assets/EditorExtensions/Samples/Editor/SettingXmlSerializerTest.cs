// Copyright (c) 2017 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace EditorExtensions
{

[InitializeOnLoad]
public class SettingXmlSerializerTest
{
	struct XmlTestClassA
	{
		public int a;
		public float b;
	}

	struct XmlTestClassB
	{
		public Vector3 v;
		public Quaternion q;
		public Matrix4x4 mat;
	}

	struct XmlTestRoot
	{
		public XmlTestClassA classA;
		public XmlTestClassB[] classB;
		public UnityEngine.Object objectC;
	}

	static SettingXmlSerializerTest()
	{
		var root = new XmlTestRoot();

		root.classA = new XmlTestClassA() {
			a = 1234,
			b = 5678.0f,
		};

		root.classB = new XmlTestClassB[] {
			new XmlTestClassB() {
				v = new Vector3(0.1f, 0.2f, 0.3f),
				q = Quaternion.Euler( 10.0f, 20.0f, 30.0f ),
				mat = Matrix4x4.identity,
			}
		};

		root.objectC = AssetDatabase.LoadMainAssetAtPath( "Assets/EditorExtensions/Samples/Editor/SettingXmlSerializerTest.cs" );

		SettingXmlSerializer.ExtraData extraData = new SettingXmlSerializer.ExtraData();
		extraData.rootDirectory = "Assets/EditorExtensions/Samples/Editor";
		var bytes = SettingXmlSerializer.SerializeXmlToBytes( root, extraData );
		System.IO.File.WriteAllBytes( "Assets/EditorExtensions/Samples/Editor/SettingXmlSerializerTest.xml", bytes );
	}
}

}
