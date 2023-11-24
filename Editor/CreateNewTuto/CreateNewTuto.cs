using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;
using System.IO;
using System;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Flupi.Tools
{
    public class CreateNewTuto : EditorWindow
    {
        //Utils
        private readonly Regex reg = new("(?:[^a-z0-9 ]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private readonly string gameManagerCode = @"using UnityEngine;

public class {0} : MonoBehaviour
{{
    #region Singleton
    private static {0} instance;
    public static {0} Instance => instance;

    private void Awake()
    {{
        if (instance != null)
            Destroy(this);

        instance = this;
        DontDestroyOnLoad(this);
    }}
    #endregion

    // Start is called before the first frame update
    void Start()
    {{

    }}

    // Update is called once per frame
    void Update()
    {{

    }}
}}
";
        private bool showOptionToAddScript;

        //Tuto params
        private string proposedTutoName = "";
        private bool overwriteIfNeeded;
        private bool needScriptableObjects;
        private bool needResources;

        //Error management
        private bool error = false;
        private string errorMessage;

        //Data
        private string tutoName;

        [MenuItem("Tools/Tuto Creator")]
        public static void ShowMyEditor()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = GetWindow<CreateNewTuto>();
            wnd.titleContent = new GUIContent("Tuto Creator");

            // Limit size of the window
            wnd.minSize = new Vector2(300, 150);
            wnd.maxSize = new Vector2(1920, 720);
        }

        private void OnGUI()
        {
            try
            {
                proposedTutoName = EditorGUILayout.TextField("Tuto Name", proposedTutoName);
                ExtractFinalTutoName(proposedTutoName);
                EditorGUILayout.LabelField("Final Name", tutoName);
                overwriteIfNeeded = EditorGUILayout.ToggleLeft("OverWrite if Needed", overwriteIfNeeded);
                needScriptableObjects = EditorGUILayout.ToggleLeft("Add ScriptableObjects Folder", needScriptableObjects);
                needResources = EditorGUILayout.ToggleLeft("Add Resources Folder", needResources);

                if (GUILayout.Button("Create Tuto Folder"))
                {
                    CreateTutoFolder();
                    showOptionToAddScript = true;
                }

                if (showOptionToAddScript)
                    if (GUILayout.Button("Add GameManager Script To GameObject"))
                        AddGMScript();

            }
            catch (Exception e)
            {
                SetError(e.Message);
            }

            if (error)
                EditorGUILayout.LabelField($"Error : {errorMessage}");
        }

        private void ExtractFinalTutoName(string proposition)
        {
            if (string.IsNullOrEmpty(proposition))
                throw new Exception("null or empty proposition");

            string[] cutFinalTutoName = reg.Replace(Reencode(proposition), "").Trim().Split(" ");

            string[] clone = cutFinalTutoName.Clone() as string[];
            for (int i = 0; i < cutFinalTutoName.Length; i++)
            {
                for (int j = 0; j < cutFinalTutoName[i].Length; j++)
                {
                    if (j == 0)
                    {
                        clone[i] = char.ToUpper(cutFinalTutoName[i][0], CultureInfo.InvariantCulture) + cutFinalTutoName[i][1..];
                    }
                    else
                    {
                        clone[i] = clone[i][0..j] + cutFinalTutoName[i][j] + (j + 1 == cutFinalTutoName[i].Length ? "" : cutFinalTutoName[i][(j + 1)..]);
                    }
                }
            }

            string result = "";

            foreach (string part in clone)
                result += part;

            if (result != tutoName)
            {
                SetError();
                tutoName = result;
            }
        }

        private string Reencode(string accentedStr)
        {
            var normalizedString = accentedStr.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder
                .ToString()
                .Normalize(NormalizationForm.FormC);
        }

        private void CreateTutoFolder()
        {
            string folderPath = $"Assets/{tutoName}";

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                if (overwriteIfNeeded)
                {
                    AssetDatabase.DeleteAsset(folderPath);
                    AssetDatabase.Refresh();
                }
                else
                    throw new Exception($"A Tuto named {tutoName} already exists !");
            }

            AssetDatabase.CreateFolder("Assets", tutoName);

            AssetDatabase.CreateFolder($"{folderPath}", "_Scripts");
            AssetDatabase.CreateFolder($"{folderPath}", "Prefabs");

            if (needScriptableObjects)
                AssetDatabase.CreateFolder($"{folderPath}", "ScriptableObjects");

            if (needResources)
                AssetDatabase.CreateFolder($"{folderPath}", "Resources");

            CreateGameManager($"{folderPath}/_Scripts", tutoName);
            PutGameObjectInShowCase(tutoName);
            SetError();
        }

        private void CreateGameManager(string folderPath, string finalTutoName)
        {
            var className = string.Format("{0}GameManager", finalTutoName);
            var path = folderPath + "/" + className + ".cs";

            var scriptFile = new StreamWriter(path);
            scriptFile.Write(string.Format(gameManagerCode, className));
            scriptFile.Close();

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();
        }

        private void PutGameObjectInShowCase(string finalTutoName)
        {
            Scene showCase = EditorSceneManager.GetActiveScene();

            if (!showCase.IsValid() || !showCase.isLoaded)
                showCase = EditorSceneManager.OpenScene(showCase.path, OpenSceneMode.Additive);

            foreach (GameObject otherGO in showCase.GetRootGameObjects())
                if (otherGO.name == finalTutoName)
                    return;

            GameObject go = new(finalTutoName);
            SceneManager.MoveGameObjectToScene(go, showCase);
            EditorSceneManager.SaveScene(showCase);
        }

        private void AddGMScript()
        {
            throw new Exception("BAHAHA I TROLLED YOU");
        }

        private void SetError(string _errorMessage = "")
        {
            error = _errorMessage != "";
            errorMessage = _errorMessage;
            Debug.Log(_errorMessage);
        }
    }
}