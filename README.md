# Editor Extensions for Unity Editor

## What's this

This is toolset for Unity Editor.

This repository contains below classes..

---

### ObjectPostprocessor

- This is called after changing of any number of assets / gameObjects.

- Performable events are Added, Moved, Deleted, Duplicated, and Instantiated. 

---

### SettingXmlSerializer

- This is xml serializer supporting with UnityEngine.Object serialization.

- UnityEngine.Object is serialized to assetPath, relativeAssetPath, GUID.

- UnityEngine.Transform is serialized to relative transform path. (BTW, "root/parent/transform")

---

## License

MIT License
