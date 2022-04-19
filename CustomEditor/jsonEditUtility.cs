using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data;
using System.Security.Cryptography;

public class UserInfoUtility : EditorWindow
{
    enum showOption
    {
        advanceFold = 0,
        advanceShow,
    }
    showOption option = showOption.advanceFold;

    GUIStyle lblStyle;
    GUIStyle leftStyle;

    [UnityEditor.MenuItem("MyUtil/Uesr Info Utility")]
    static void OpenWindow() 
    { 
        EditorWindow.GetWindowWithRect<UserInfoUtility>(new Rect(100, 100, 500, 800));
    }

    void setStyle()
    {
        lblStyle = new GUIStyle(EditorStyles.label);
        lblStyle.alignment = TextAnchor.MiddleCenter;

        leftStyle = new GUIStyle(EditorStyles.miniButtonLeft);
        leftStyle.alignment = TextAnchor.MiddleLeft;
    }

    private void OnGUI()
    {
        setStyle();
        SetSaveLoader();
        
        ReadUserInfo();
        ShowAdvanceUserInfo();
        SwitchShowOption();
    }

    SHA256Managed sha256 = new SHA256Managed();
    string passwdHash = "";
    string originHash = "2523223721120912412078115230199824315221661221012161532011051862511251524284128207249100";
    string inputPasswd = "Password required to enter advanced settings [pw : \"sbaek\"]";

    protected void SwitchShowOption()
    {
        if (option == showOption.advanceFold)
        {
            inputPasswd = EditorGUILayout.TextField("Advance Passwd : ", inputPasswd);
            byte[] encryptBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(inputPasswd));
            passwdHash = "";
            foreach (byte data in encryptBytes)
                passwdHash += data.ToString();
            if (GUILayout.Button("Enter") && passwdHash == originHash)
                option = showOption.advanceShow;
        }
        if (option == showOption.advanceShow && GUILayout.Button("Close Advance Setting"))
            option = showOption.advanceFold;
    }

    //==================== [ userInfo AdvanceSetter ] ======================//

    Vector2 scrollPosition;
    string userInfoOrigin = "";
    List<string> userNames = new List<string>();
    List<JToken> userValues = new List<JToken>();
    List<object> userAryValues = new List<object>();
    List<object> userObjectAry = new List<object>();
    List<bool> userShowOption = new List<bool>();
    JObject JsonUserInfo;

    void ShowAdvanceUserInfo()
    {
        if (option == showOption.advanceShow && GUILayout.Button("Refresh")) { ClearUserData(); }
        if (userInfoOrigin == null || userNames.Count == 0 || userValues.Count == 0)
            return;
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.Space(15);
        int counter = 0;
        bool isMultiple = false;
        foreach (string name in userNames.ToArray())
        {
            if (option == showOption.advanceFold && name != "mainLevel") { counter++; continue; }
            isMultiple = userAryValues[counter] != null;

            ShowInfoLable(name, counter, isMultiple);
            ShowUtilityButton(name, counter, isMultiple);
            
            counter++;
        }
        GUILayout.EndScrollView();
    }

    void ShowInfoLable(string name, int counter, bool isMultiple)
    {
        if (!isMultiple)
            showSingleToken(name, counter);
        else
        {
            string[] ary = userAryValues[counter] as string[];
            if (ary != null)
                showArrayToken(name, counter);
            else
                showObjectToken(name, counter);
        }
    }

    void showSingleToken(string name, int counter) 
    {
        userValues[counter] = EditorGUILayout.TextField($"{name} : ", trimString(userValues[counter].ToString()));
    }
    void showArrayToken(string name, int counter)
    {
        if (userShowOption[counter])
        {
            string[] ary = userAryValues[counter] as string[];
            for (int i = 0; i < ary.Length; i++)
                ary[i] = EditorGUILayout.TextField($"{name}n{i}: ", ary[i]);
            userAryValues[counter] = ary;
        }
        else
            GUILayout.Label($"{name} : {trimString(userValues[counter].ToString())}", EditorStyles.wordWrappedLabel, GUILayout.Width(450));
    }
    void showObjectToken(string name, int counter)
    {
        if (userShowOption[counter])
        {
            if (userObjectAry.Count == 0)
            {
                JArray array = userAryValues[counter] as JArray;
                foreach (JToken token in array)
                    userObjectAry.Add(JObject.Parse(token.ToString()));
            }

            GUILayout.Label(name);
            for (int i = 0; i < userObjectAry.Count; i++)
            {
                if ((userObjectAry[i] as JObject) != null)
                    foreach (JProperty property in (userObjectAry[i] as JObject).Properties())
                        if (property.Value as JArray == null)
                            property.Value = EditorGUILayout.TextField($"{property.Name}: ", trimString(property.Value.ToString()));
            }
        }
        else
            EditorGUILayout.LabelField($"{name} : {trimString(userValues[counter].ToString())}", GUILayout.Width(450));
    }

    void ShowUtilityButton(string name, int counter, bool isMultiple)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(isMultiple ? 260 : 362));
        if (isMultiple && GUILayout.Button($"{(userShowOption[counter] ? "Hide" : "ShowDetail")}", GUILayout.Width(100)))
        {
            bool showOption = userShowOption[counter];
            for (int i = 0; i < userShowOption.Count; i++)
                userShowOption[i] = false;
            userShowOption[counter] = !showOption;
            userObjectAry.Clear();
        }
        if (GUILayout.Button("change", GUILayout.Width(100)))
        {
            if (isMultiple && userShowOption[counter])
                if (userObjectAry.Count == 0)
                    userValues[counter] = JToken.FromObject(userAryValues[counter]);
                else
                {
                    for (int i = 0; i < (userAryValues[counter] as JArray).Count; i++)
                        (userAryValues[counter] as JArray)[i] = userObjectAry[i] as JObject;
                    userValues[counter] = userAryValues[counter] as JArray;
                }
            JsonUserInfo[name] = userValues[counter];
            changeUserInfo();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    void changeUserInfo()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        string textString = trimString(JsonUserInfo.ToString());
        Debug.Log(textString);

        WriteUserInfo(textString);
        AssetDatabase.Refresh();
        ClearUserData();
        ReadUserInfo();
    }

    void WriteUserInfo(string message)
    {
        string filePath = path + "/" + userInfoName;

        File.Delete(filePath);
        FileStream fileStream
            = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
        StreamWriter writer = new StreamWriter(fileStream, System.Text.Encoding.Unicode);

        writer.WriteLine(message);
        writer.Close();
    }

    private void ReadUserInfo()
    {
        if (userNames.Count > 0 && userValues.Count > 0)
            return;
        if ((userInfoOrigin = ReadFileToString()) == null)
            return;

        ClearUserData();

        JsonUserInfo = JObject.Parse(userInfoOrigin);
        foreach (JProperty property in JsonUserInfo.Properties())
        {
            if (property.Name.Contains("parsing"))
                continue;

            userNames.Add(property.Name);
            userValues.Add(property.Value);
            if (property.Value as JArray != null)
                try { userAryValues.Add(property.Value.ToObject<string[]>()); } 
                catch (System.Exception e) { userAryValues.Add(property.Value as JArray); }
            else
                userAryValues.Add(null);
            userShowOption.Add(false);
        }
    }

    private void ClearUserData()
    {
        userNames.Clear();
        userValues.Clear();
        userAryValues.Clear();
        userShowOption.Clear();
        userObjectAry.Clear();
    }

    //==================== [ userInfo SAVE & LOADER ] ======================//

    string path = "Assets/Resources/Json/";
    string[] fileArray = { "save1.json", "save2.json", "save3.json", "save4.json", "save5.json" };
    string userInfoName = "userInfo.json";
    string text = "";
    bool userInfoExist = false;

    private void SetSaveLoader()
    {
       
        string Title = "";
        GUILayout.Label(Title);

        if (userInfoExist = FileExistCheck(userInfoName))
            text = $"[ {userInfoName} ] was saved at [ {File.GetLastWriteTime(path + userInfoName)} ]\n\n";
        else
            text = $"{userInfoName} not Exist\n\n";

        if (GUILayout.Button("Delete userInfo", GUILayout.Width(100)))
            DeleteData(userInfoName);
        GUILayout.Label(text);

        foreach (string fileName in fileArray)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(fileName, lblStyle, GUILayout.Width(70));
            if (FileExistCheck(userInfoName) &&
                GUILayout.Button(FileExistCheck(fileName) ? "OverWrite" : "Save", GUILayout.Width(70)))
                SaveData(fileName);

            if (FileExistCheck(fileName))
            {
                if (GUILayout.Button("Load", GUILayout.Width(70)))
                    LoadData(fileName);
                if (GUILayout.Button("Delete", GUILayout.Width(70)))
                    DeleteData(fileName);

                if (FileExistCheck(fileName))
                    GUILayout.Label(File.GetLastWriteTime(path + fileName).ToString(), lblStyle);
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(50);
        GUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(380));
        if (GUILayout.Button("ClearAll", EditorStyles.miniButtonRight, GUILayout.Width(100)))
            foreach (string fileName in fileArray)
                DeleteData(fileName);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
    }

    //==================== [ File Utils ] ======================//

    private void SaveData(string name)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        if (FileExistCheck(userInfoName))
        {
            if(FileExistCheck(name))
                File.Delete(path + name);
            File.Copy(path + userInfoName, path + name);
        }
        AssetDatabase.Refresh();
    }

    private void LoadData(string name)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        if (FileExistCheck(userInfoName))
            File.Delete(path + userInfoName);
        if (FileExistCheck(name))
            File.Copy(path + name, path + userInfoName);
        AssetDatabase.Refresh();
    }

    private void DeleteData(string name)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        if (FileExistCheck(name))
            File.Delete(path + name);
        AssetDatabase.Refresh();
    }

    private bool FileExistCheck(string name)
    {
        if (File.Exists(path + name))
            return true;
        else
            return false;
    }

    string ReadFileToString()
    {
        string result = "";
        try
        {
            using (StreamReader stream = File.OpenText(path + userInfoName))
                result = stream.ReadToEnd();
        }
        catch (Exception e) { Debug.Log(e.Message); return null; }
        return result;
    }

    string trimString(string input)
    { 
        return input.Replace("\r", "").Replace("\n", "").Replace(" ", "");
    }

    string newLineString(string input)
    {
        for (int i = 1; i < input.Length; i++)
            if (i % 50 == 0)
                input.Insert(i, "\n");
        return input;
    }
}
