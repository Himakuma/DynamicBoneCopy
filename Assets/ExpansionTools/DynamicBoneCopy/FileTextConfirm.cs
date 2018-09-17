using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class FileTextConfirm : EditorWindow
{
    public class FileInfoData {
        public string fileName = string.Empty;
        public string name = string.Empty;
        public object target = null;
    }

    public string fileName = string.Empty;

    public string saveName = string.Empty;

    private object target = null;

    private Action<FileInfoData> callback;

    public object Target
    {
        get
        {
            return target;
        }

        set
        {
            target = value;
        }
    }

    public void SetCallback(Action<FileInfoData> callback)
    {
        this.callback = callback;
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("ファイル名", GUILayout.Width(100));
        fileName = GUILayout.TextField(fileName, GUILayout.Width(200));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("保存名称", GUILayout.Width(100));
        saveName = GUILayout.TextField(saveName, GUILayout.Width(200));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("OK", GUILayout.Height(20f), GUILayout.Width(100)))
        {
            FileInfoData fileInfoData = new FileInfoData
            {
                fileName = fileName,
                name = saveName,
                target = Target
            };
            callback(fileInfoData);
            Close();
        }
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button("キャンセル", GUILayout.Height(20f), GUILayout.Width(100)))
        {
            Close();
        }
        GUILayout.EndHorizontal();
    }
}