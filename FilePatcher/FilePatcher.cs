#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class FilePatcher : EditorWindow
{
    private const string CLASS_NAME_PREFIX = "m_TargetAssemblyTypeName: ";
    private const string METHOD_NAME_PREFIX = "m_MethodName: ";

    private const string SCENE_PREFIX = "[SCENE] ";
    private const string PREFAB_PREFIX = "[PREFAB] ";

    private List<string> _fileNames = new List<string>();
    private List<string> _filePaths = new List<string>();
    
    private Vector2 _scrollPosition = Vector2.zero;

    private bool _includePrefabs = true;
    private bool _includeScenes = true;

    private string _specificSearchPath = "";
    private string _className = "";
    private string _oldMethodName = "";
    private string _newMethodName = "";

    [MenuItem("Window/FilePatcher")]
    public static void ShowWindow()
    {
        GetWindow(typeof(FilePatcher));
    }
    private void OnGUI() 
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Find"))
        {
            FindFiles();
        }
        if (GUILayout.Button("Remove All"))
        {
            RemoveAllFiles();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Specific search path: ", GUILayout.Width(130));
        GUILayout.Label("Assets\\", GUILayout.Width(50));
        _specificSearchPath = GUILayout.TextField(_specificSearchPath);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Search for: ", GUILayout.Width(70));
        _includeScenes = GUILayout.Toggle(_includeScenes, "Scenes", GUILayout.Width(65));
        _includePrefabs = GUILayout.Toggle(_includePrefabs, "Prefabs"); 
        GUILayout.EndHorizontal();

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
        for (int i = 0; i < _fileNames.Count; i++)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUILayout.Label(_fileNames[i]);
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                DeleteFileAtIndex(i);
            }

            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();

        if(_filePaths.Count > 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Class name:", GUILayout.Width(120));
            _className = GUILayout.TextField(_className);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Old method name:", GUILayout.Width(120));
            _oldMethodName = GUILayout.TextField(_oldMethodName);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("New method name:", GUILayout.Width(120));
            _newMethodName = GUILayout.TextField(_newMethodName);
            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(_oldMethodName.Length == 0 || _newMethodName.Length == 0 || _className.Length == 0);
            {
                if (GUILayout.Button("Replace"))
                {
                    PatchFiles();
                }
            }
        }
    }
    private void DeleteFileAtIndex(int index)
    {
        _filePaths.RemoveAt(index);
        _fileNames.RemoveAt(index);
    }
    private void RemoveAllFiles()
    {
        _fileNames.Clear();
        _filePaths.Clear();
    }
    private void FindFiles()
    {
        RemoveAllFiles();

        string searchPath = Application.dataPath;
        if(_specificSearchPath.Length > 0) searchPath += "\\" + _specificSearchPath;

        try
        {
            if(_includeScenes)
            {
                string[] sceneFiles = Directory.GetFiles(searchPath, "*.unity", SearchOption.AllDirectories);
                _filePaths.AddRange(sceneFiles);
                foreach (string scene in sceneFiles)
                {
                    _fileNames.Add(SCENE_PREFIX + Path.GetFileNameWithoutExtension(scene));
                }
            }

            if(_includePrefabs)
            {
                string[] prefabFiles = Directory.GetFiles(searchPath, "*.prefab", SearchOption.AllDirectories);
                _filePaths.AddRange(prefabFiles);
                foreach (string prefab in prefabFiles)
                {
                    _fileNames.Add(PREFAB_PREFIX + Path.GetFileNameWithoutExtension(prefab));
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }
    private void PatchFiles()
    {
        string oldMethodName = METHOD_NAME_PREFIX + _oldMethodName;
        string newMethodName = METHOD_NAME_PREFIX + _newMethodName;
        
        List<string> deletedFilesNames = new List<string>();
        List<string> deletedFilesPaths = new List<string>();

        try
        {
            for (int i = 0; i < _filePaths.Count; i++)
            {
                string filePath = _filePaths[i];
                
                if(!File.Exists(filePath))
                {
                    deletedFilesNames.Add(_fileNames[i]);
                    deletedFilesPaths.Add(filePath);
                    continue;
                } 

                string oldMethodPattern = System.String.Format(@"\b{0}\b", oldMethodName);
                string classPattern = CLASS_NAME_PREFIX + _className;

                string[] contentLines = File.ReadAllLines(filePath);
                for (int j = 0; j < contentLines.Length; j++)
                {
                    if(contentLines[j].Contains(classPattern))
                    {
                        contentLines[j + 1] = Regex.Replace(contentLines[j + 1], oldMethodPattern, newMethodName);
                        continue;
                    }
                }

                File.WriteAllLines(filePath, contentLines);
                AssetDatabase.Refresh();
            }

            foreach (var deletedScenesName in deletedFilesNames)
            {
                _fileNames.Remove(deletedScenesName);
            }
            foreach (var deletedScenesPath in deletedFilesPaths)
            {
                _filePaths.Remove(deletedScenesPath);
            }

            if(deletedFilesNames.Count != 0)
            {
                Debug.LogWarning("Some files could not be found and have been removed from the list");
            }
        }
        catch (System.Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }
}
#endif