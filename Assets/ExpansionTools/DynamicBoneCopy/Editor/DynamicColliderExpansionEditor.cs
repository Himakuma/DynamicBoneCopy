using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DynamicBoneCollider))]
public class DynamicColliderExpansionEditor : Editor
{
    public class BoneColliderData
    {
        public string name = string.Empty;
        public string filePath = string.Empty;
    }

    /// <summary>
    /// ファイル保存ディレクトリ
    /// </summary>
    private const string DATAS_BASE_DIR = "ExpansionTools/DynamicBoneCopy/Datas";

    private const string SAVE_KEY_BONE_NAME = "ExpansionTools_DynamicBoneCopy_ColliderName";
    private const string SAVE_KEY_FILE_NAME = "ExpansionTools_DynamicBoneCopy_FileName";

    private static DynamicBoneCollider dynamicBoneColliderCopy = null;
    private static Dictionary<string, BoneColliderData> boneColliderDatas = null;

    private void OnEnable()
    {
        LoadBoneList();
    }

    public override void OnInspectorGUI()
    {
        RightMenu(GUILayoutUtility.GetLastRect());
        base.OnInspectorGUI();
    }

    /// <summary>
    /// 左クリックメニュー
    /// </summary>
    /// <param name="rect"></param>
    private void RightMenu(Rect rect)
    {
        GUI.enabled = true;
        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy"), false, () => {
                dynamicBoneColliderCopy = (DynamicBoneCollider)target;
            });

            if (dynamicBoneColliderCopy == null)
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }
            else
            {
                menu.AddItem(new GUIContent("Paste"), false, Paste);
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Export"), false, () => {
                Export();
            });

            menu.AddSeparator("");
            foreach (string boneKey in boneColliderDatas.Keys)
            {
                BoneColliderData boneColliderData = boneColliderDatas[boneKey];
                menu.AddItem(new GUIContent("Collider/" + boneColliderData.name), false, BoneSettiong, boneColliderData);
            }
            menu.ShowAsContext();
        }
    }

    /// <summary>
    ///  設定値の反映
    /// </summary>
    /// <param name="obj"></param>
    private void BoneSettiong(object obj)
    {
        BoneColliderData boneColliderData = (BoneColliderData)obj;
        DynamicBoneSettingV1 setting = new DynamicBoneSettingV1();
        DynamicBoneCollider bone = (DynamicBoneCollider)target;
        setting.SetXmlToCollider(boneColliderData.filePath, boneColliderData.name, bone);
    }

    /// <summary>
    /// ボーンリストの読込
    /// </summary>
    private void LoadBoneList()
    {
        boneColliderDatas = new Dictionary<string, BoneColliderData>();
        string datasDir = Path.Combine(Application.dataPath, DATAS_BASE_DIR);
        foreach (string filePath in FileUtil.GetFileType(datasDir, "xml"))
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);
            foreach (XmlNode nodeData in xmlDoc.SelectNodes("DynamicBones/Collider"))
            {
                XmlNode nameNode = nodeData.SelectSingleNode("Name");
                BoneColliderData boneColliderData = new BoneColliderData
                {
                    filePath = Path.Combine(datasDir, filePath),
                    name = nameNode.InnerText
                };
                boneColliderDatas.Add(boneColliderData.filePath + "__Collider__" + boneColliderData.name, boneColliderData);
            }
        }
    }

    /// <summary>
    /// 出力
    /// </summary>
    private void Export()
    {
        string datasDir = Path.Combine(Application.dataPath, DATAS_BASE_DIR);
        FileTextConfirm window = EditorWindow.GetWindow<FileTextConfirm>(true, "ファイル名入力");
        window.Target = target;

        // 入力値の復元
        window.fileName = EditorPrefs.GetString(SAVE_KEY_FILE_NAME);
        window.saveName = EditorPrefs.GetString(SAVE_KEY_BONE_NAME);

        window.SetCallback(x => {
            EditorPrefs.SetString(SAVE_KEY_FILE_NAME, x.fileName);
            EditorPrefs.SetString(SAVE_KEY_BONE_NAME, x.name);

            string filePath = Path.Combine(datasDir, x.fileName + ".xml");
            string saveKey = filePath + "__Collider__" + x.name;
            if (boneColliderDatas.ContainsKey(saveKey))
            {
                bool isSave = EditorUtility.DisplayDialog("上書き保存", "同名称があります。上書きしますか？\n\n\nコライダー名：" + x.name
                    + "\nファイルパス：" + filePath, "はい", "いいえ");
                if (!isSave) {
                    return;
                }
            }

            DynamicBoneSettingV1 setting = new DynamicBoneSettingV1();
            if (setting.ExportXmlCollider((DynamicBoneCollider)x.target, x.name, filePath))
            {
                AssetDatabase.Refresh();
                window.Close();
                LoadBoneList();
            }
            else
            {
                EditorUtility.DisplayDialog("保存失敗", "ファイルの保存に失敗しました。もう一度やり直してください。\n※何度も発生する場合は、Unityを再起動してください。", "はい");
            }
        });
    }

    /// <summary>
    /// 貼り付け
    /// </summary>
    private void Paste()
    {
        DynamicBoneCollider toDynamicBoneCollider = (DynamicBoneCollider)target;
        toDynamicBoneCollider.m_Direction = dynamicBoneColliderCopy.m_Direction;
        toDynamicBoneCollider.m_Center = CopyVector3(dynamicBoneColliderCopy.m_Center);
        toDynamicBoneCollider.m_Bound = dynamicBoneColliderCopy.m_Bound;
        toDynamicBoneCollider.m_Radius = dynamicBoneColliderCopy.m_Radius;
        toDynamicBoneCollider.m_Height = dynamicBoneColliderCopy.m_Height;
    }


    /// <summary>
    /// Vector3コピー用
    /// </summary>
    /// <param name="copyVal"></param>
    /// <returns></returns>
    private Vector3 CopyVector3(Vector3 copyVal)
    {
        return new Vector3(copyVal.x, copyVal.y, copyVal.z);
    }
}
