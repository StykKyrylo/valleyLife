﻿using Plugins.Lowscope.ComponentSaveSystem.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Plugins.Lowscope.ComponentSaveSystem.Data
{
    public class SaveGameJSON : SaveGame, IConvertSaveGame
    {
        [Serializable]
        public struct MetaData
        {
            public int gameVersion;
            public string creationDate;
            public string timePlayed;
            public string modificationDate;
        }

        [Serializable]
        public struct Data
        {
            public string guid;
            public string data;
            public string scene;
        }

        [SerializeField] internal MetaData metaData;
        [SerializeField] internal List<Data> saveData = new List<Data>();

        // Stored in dictionary for quick lookup
        [NonSerialized]
        internal Dictionary<string, int> saveDataCache = new Dictionary<string, int>(StringComparer.Ordinal);

        // Used to track which save ids are assigned to a specific scene
        // This makes it posible to wipe all data from a specific scene.
        [NonSerialized] private Dictionary<string, List<string>> sceneObjectIDS = new Dictionary<string, List<string>>();

        public override void ReadSaveFromPath(string savePath)
        {
            string data = "";
            byte[] bytes = new byte[2];
            using (FileStream fs = new FileStream(savePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Read(bytes, 0, 1);
            }

            if (Encoding.ASCII.GetString(bytes).Contains("{"))
            {
                data = File.ReadAllText(savePath, Encoding.UTF8);
            }
            else
            {
                using (var reader = new BinaryReader(File.Open(savePath, FileMode.Open)))
                {
                    data = reader.ReadString();
                }
            }

            if (string.IsNullOrEmpty(data))
            {
                Log(string.Format("Save file empty: {0}. It will be automatically removed", savePath));
                File.Delete(savePath);
                return;
            }

            SaveGameJSON saveGame = JsonUtility.FromJson<SaveGameJSON>(data);
            this.metaData = saveGame.metaData;
            this.saveData = saveGame.saveData;
        }

        public override void WriteSaveFile(SaveGame saveGame, string savePath)
        {
            string json = JsonUtility.ToJson(saveGame, SaveSettings.Get().useJsonPrettyPrint);

            if (!SaveSettings.Get().legacyJSONWriting)
            {
                File.WriteAllText(savePath, json);
            }
            else
            {
                // Old methodology of writing save file. Left for compatibility with older versions.
                // Turning it off means you won't support older versions of your application that used the previous version
                // Of the Component Save System.
                using (var writer = new BinaryWriter(File.Open(savePath, FileMode.Create)))
                {
                    writer.Write(json);
                }
            }
        }

        public override string Get(string id)
        {
            int saveIndex;

            if (saveDataCache.TryGetValue(id, out saveIndex))
            {
                return saveData[saveIndex].data;
            }
            else
            {
                return null;
            }
        }

        public override void OnAfterLoad()
        {
            gameVersion = metaData.gameVersion;

            DateTime.TryParse(metaData.creationDate, out creationDate);
            TimeSpan.TryParse(metaData.timePlayed, out timePlayed);
            DateTime.TryParse(metaData.modificationDate, out modificationDate);

            if (saveData.Count > 0)
            {
                // Clear all empty data on load.
                int dataCount = saveData.Count;
                for (int i = dataCount - 1; i >= 0; i--)
                {
                    if (string.IsNullOrEmpty(saveData[i].data))
                        saveData.RemoveAt(i);
                }

                for (int i = 0; i < saveData.Count; i++)
                {
                    if (!saveDataCache.ContainsKey(saveData[i].guid))
                    {
                        saveDataCache.Add(saveData[i].guid, i);
                        AddSceneID(saveData[i].scene, saveData[i].guid);
                    }
                }
            }
        }

        public override void OnBeforeWrite()
        {
            if (creationDate == default(DateTime))
            {
                creationDate = DateTime.Now;
            }

            metaData.creationDate = creationDate.ToString();
            metaData.gameVersion = gameVersion;
            metaData.timePlayed = timePlayed.ToString();
            metaData.modificationDate = DateTime.Now.ToString();
            modificationDate = DateTime.Now;
        }

        public override void Remove(string id)
        {
            int saveIndex;

            if (saveDataCache.TryGetValue(id, out saveIndex))
            {
                // Zero out the string data, it will be wiped on next cache initialization.
                saveData[saveIndex] = new Data();
                saveDataCache.Remove(id);
                sceneObjectIDS.Remove(id);
            }
        }

        public override void Set(string id, string data, string scene)
        {
            int saveIndex;

            if (saveDataCache.TryGetValue(id, out saveIndex))
            {
                saveData[saveIndex] = new Data() { guid = id, data = data, scene = scene };
            }
            else
            {
                Data newSaveData = new Data() { guid = id, data = data, scene = scene };

                saveData.Add(newSaveData);
                saveDataCache.Add(id, saveData.Count - 1);
                AddSceneID(scene, id);
            }
        }

        public override void WipeSceneData(string sceneName)
        {
            List<string> value;
            if (sceneObjectIDS.TryGetValue(sceneName, out value))
            {
                int elementCount = value.Count;
                for (int i = elementCount - 1; i >= 0; i--)
                {
                    Remove(value[i]);
                    value.RemoveAt(i);
                }
            }
            else
            {
                Debug.Log("Scene is already wiped!");
            }
        }

        /// <summary>
        /// Adds the index to a list that is identifyable by scene
        /// Makes it easy to remove save data related to a scene name.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="index"></param>
        protected void AddSceneID(string scene, string id)
        {
            List<string> value;
            if (sceneObjectIDS.TryGetValue(scene, out value))
            {
                value.Add(id);
            }
            else
            {
                List<string> list = new List<string>();
                list.Add(id);
                sceneObjectIDS.Add(scene, list);
            }
        }

        public override void Dispose()
        {
            saveData.Clear();
            saveDataCache.Clear();
            sceneObjectIDS.Clear();
        }

        public SaveGame ConvertTo(StorageType storageType, string filePath, bool replace = true)
        {
            return null;
        }
    }
}