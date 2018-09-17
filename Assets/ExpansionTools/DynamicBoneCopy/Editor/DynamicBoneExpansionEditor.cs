using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DynamicBone))]
public class DynamicBoneExpansionEditor : Editor
{
    public class BoneData
    {
        public string name = string.Empty;
        public string filePath = string.Empty;
    }

    /// <summary>
    /// ファイル保存ディレクトリ
    /// </summary>
    private const string DATAS_BASE_DIR = "ExpansionTools/DynamicBoneCopy/Datas";

    private const string SAVE_KEY_BONE_NAME = "ExpansionTools_DynamicBoneCopy_BoneName";
    private const string SAVE_KEY_FILE_NAME = "ExpansionTools_DynamicBoneCopy_FileName";

    private static DynamicBone dynamicBoneCopy = null;
    private static Dictionary<string, BoneData> boneDatas = null;

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
                dynamicBoneCopy = (DynamicBone)target;
            });

            if (dynamicBoneCopy == null)
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
            foreach (string boneKey in boneDatas.Keys)
            {
                BoneData boneData = boneDatas[boneKey];
                menu.AddItem(new GUIContent("Bone/" + boneData.name), false, BoneSettiong, boneData);
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
        BoneData boneData = (BoneData)obj;
        DynamicBoneSettingV1 setting = new DynamicBoneSettingV1();
        DynamicBone bone = (DynamicBone)target;
        setting.SetXmlToBone(boneData.filePath, boneData.name, bone);
    }

    /// <summary>
    /// ボーンリストの読込
    /// </summary>
    private void LoadBoneList()
    {
        boneDatas = new Dictionary<string, BoneData>();
        string datasDir = Path.Combine(Application.dataPath, DATAS_BASE_DIR);
        foreach (string filePath in FileUtil.GetFileType(datasDir, "xml"))
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);
            foreach (XmlNode nodeData in xmlDoc.SelectNodes("DynamicBones/Bone"))
            {
                XmlNode nameNode = nodeData.SelectSingleNode("Name");
                BoneData boneData = new BoneData
                {
                    filePath = Path.Combine(datasDir, filePath),
                    name = nameNode.InnerText
                };
                boneDatas.Add(boneData.filePath + "__" + boneData.name, boneData);
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
            string saveKey = filePath + "__" + x.name;
            if (boneDatas.ContainsKey(saveKey))
            {
                bool isSave = EditorUtility.DisplayDialog("上書き保存", "同名称があります。上書きしますか？\n\n\nボーン名：" + x.name
                    + "\nファイルパス：" + filePath, "はい", "いいえ");
                if (!isSave) {
                    return;
                }
            }

            DynamicBoneSettingV1 setting = new DynamicBoneSettingV1();
            if (setting.ExportXmlBone((DynamicBone)x.target, x.name, filePath))
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
        DynamicBone toDynamicBone = (DynamicBone)target;

        toDynamicBone.m_UpdateRate = dynamicBoneCopy.m_UpdateRate;
        toDynamicBone.m_UpdateMode = dynamicBoneCopy.m_UpdateMode;
        toDynamicBone.m_Damping = dynamicBoneCopy.m_Damping;
        toDynamicBone.m_DampingDistrib = CopyAnimationCurve(dynamicBoneCopy.m_DampingDistrib);
        toDynamicBone.m_Elasticity = dynamicBoneCopy.m_Elasticity;
        toDynamicBone.m_ElasticityDistrib = CopyAnimationCurve(dynamicBoneCopy.m_ElasticityDistrib);
        toDynamicBone.m_Stiffness = dynamicBoneCopy.m_Stiffness;
        toDynamicBone.m_StiffnessDistrib = CopyAnimationCurve(dynamicBoneCopy.m_StiffnessDistrib);
        toDynamicBone.m_Inert = dynamicBoneCopy.m_Inert;
        toDynamicBone.m_InertDistrib = CopyAnimationCurve(dynamicBoneCopy.m_InertDistrib);
        toDynamicBone.m_Radius = dynamicBoneCopy.m_Radius;
        toDynamicBone.m_RadiusDistrib = CopyAnimationCurve(dynamicBoneCopy.m_RadiusDistrib);
        toDynamicBone.m_EndLength = dynamicBoneCopy.m_EndLength;
        toDynamicBone.m_EndOffset = CopyVector3(dynamicBoneCopy.m_EndOffset);
        toDynamicBone.m_Gravity = CopyVector3(dynamicBoneCopy.m_Gravity);
        toDynamicBone.m_Force = CopyVector3(dynamicBoneCopy.m_Force);

        toDynamicBone.m_FreezeAxis = dynamicBoneCopy.m_FreezeAxis;
        toDynamicBone.m_DistantDisable = dynamicBoneCopy.m_DistantDisable;
        toDynamicBone.m_DistanceToObject = dynamicBoneCopy.m_DistanceToObject;

        if (dynamicBoneCopy.m_Colliders != null)
        {
            toDynamicBone.m_Colliders = new List<DynamicBoneColliderBase>();
            foreach (DynamicBoneColliderBase dynamicBoneColliderCopy in dynamicBoneCopy.m_Colliders)
            {
                toDynamicBone.m_Colliders.Add(null);
            }
        }


        if (dynamicBoneCopy.m_Exclusions != null)
        {
            if (toDynamicBone.m_Exclusions == null)
            {
                toDynamicBone.m_Exclusions = new List<Transform>();
            }
            for (int i = 0; i < dynamicBoneCopy.m_Exclusions.Count - toDynamicBone.m_Exclusions.Count; i++)
            {
                toDynamicBone.m_Exclusions.Add(null);
            }
        }


        /**
         * TODO：Transformの扱いについて
         * 処理対象の座標ように保持している情報の為、コピーの必要ない？？
         * Gameオブジェクトの名称を取得して、検索、自動設定だけがよい？？

        ・残コピー変数一覧
        Transform m_Root = null;
        List<Transform> m_Exclusions = null;
        Transform m_ReferenceObject = null;
        */
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="copyVal"></param>
    /// <returns></returns>
    private AnimationCurve CopyAnimationCurve(AnimationCurve copyVal)
    {
        AnimationCurve toVal = new AnimationCurve
        {
            preWrapMode = copyVal.preWrapMode,
            postWrapMode = copyVal.postWrapMode
        };

        foreach (Keyframe formKey in copyVal.keys)
        {
            Keyframe cloneKey = new Keyframe(formKey.time, formKey.value, formKey.inTangent, formKey.outTangent);
            toVal.AddKey(cloneKey);
        }
        return toVal;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="copyVal"></param>
    /// <returns></returns>
    private Vector3 CopyVector3(Vector3 copyVal)
    {
        return new Vector3(copyVal.x, copyVal.y, copyVal.z);
    }
}
