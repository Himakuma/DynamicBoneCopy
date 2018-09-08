using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEngine;

public class DynamicBoneSettingV1 : IDynamicBoneSetting
{
    private const string DATA_VERSION = "1.0";
    private readonly string[] exportValNames = {
        "m_UpdateRate",
        "m_UpdateMode",
        "m_Damping",
        "m_DampingDistrib",
        "m_Elasticity",
        "m_ElasticityDistrib",
        "m_Stiffness",
        "m_StiffnessDistrib",
        "m_Inert",
        "m_InertDistrib",
        "m_Radius",
        "m_RadiusDistrib",
        "m_EndLength",
        "m_EndOffset",
        "m_Gravity",
        "m_Force",
        "m_FreezeAxis",
        "m_DistantDisable",
        "m_DistanceToObject"
    };


    /// <summary>
    /// ボーン名からエレメント取得
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private XmlElement GetBoneXmlElement(XmlDocument xmlDoc, string name)
    {
        XmlNodeList bones = xmlDoc.SelectNodes("DynamicBones/Bone");
        foreach (XmlNode boneNode in bones)
        {
            if (boneNode.SelectSingleNode("Name").InnerText == name)
            {
                return (XmlElement)boneNode;
            }
        }

        XmlElement root = (XmlElement)xmlDoc.SelectSingleNode("DynamicBones");

        XmlElement boneElem = xmlDoc.CreateElement("Bone");
        root.AppendChild(boneElem);

        XmlElement nameElem = xmlDoc.CreateElement("Name");
        nameElem.InnerText = name;
        boneElem.AppendChild(nameElem);

        XmlElement dataVersionElem = xmlDoc.CreateElement("DataVersion");
        dataVersionElem.InnerText = DATA_VERSION;
        boneElem.AppendChild(dataVersionElem);
        return boneElem;
    }

    /// <summary>
    /// パラメータ取得
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <param name="boneElem"></param>
    /// <returns></returns>
    private XmlElement GetParamsXmlElement(XmlDocument xmlDoc, XmlElement boneElem)
    {
        XmlElement paramsElem = (XmlElement)boneElem.SelectSingleNode("Params");
        if (paramsElem == null)
        {
            paramsElem = xmlDoc.CreateElement("Params");
            boneElem.AppendChild(paramsElem);
        }
        else
        {
            paramsElem.RemoveAll();
        }
        return paramsElem;
    }


    /// <summary>
    /// XMLデータ出力
    /// </summary>
    /// <param name="target"></param>
    /// <param name="exportName"></param>
    /// <param name="writePath"></param>
    /// <returns></returns>
    public bool ExportXml(DynamicBone target, string exportName, string writePath)
    {
        XmlDocument xmlDoc = new XmlDocument();
        if (File.Exists(writePath))
        {
            xmlDoc = new XmlDocument();
            xmlDoc.Load(writePath);
        }
        else
        {
            XmlDeclaration declaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.AppendChild(declaration);

            XmlElement root = xmlDoc.CreateElement("DynamicBones");
            xmlDoc.AppendChild(root);
        }

        XmlElement boneElem = GetBoneXmlElement(xmlDoc, exportName);
        XmlElement paramsElem = GetParamsXmlElement(xmlDoc, boneElem);
        foreach (string paramName in exportValNames)
        {
            AddParam(paramsElem, paramName, target, xmlDoc);
        }

        using (StringWriter writer = new StringWriter())
        using (XmlWriter xmlWriter = XmlWriter.Create(writer))
        {
            xmlDoc.WriteTo(xmlWriter);
            xmlWriter.Flush();
            return FileUtil.WriteText(writer.GetStringBuilder().ToString(), writePath, Encoding.UTF8, false);
        }
    }

    /// <summary>
    ///  XMLデータ設定
    /// </summary>
    /// <param name="xmlPath"></param>
    /// <param name="name"></param>
    /// <param name="target"></param>
    public void SetXmlToBone(string xmlPath, string name, DynamicBone target)
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(xmlPath);
        foreach (XmlNode nodeData in xmlDoc.SelectNodes("DynamicBones/Bone"))
        {
            if (name == nodeData.SelectSingleNode("Name").InnerText)
            {
                foreach (XmlNode param in nodeData.SelectNodes("Params/Param"))
                {
                    SetParam(target, (XmlElement)param);
                }
                return;
            }
        }
    }

    /// <summary>
    /// パラメータ設定
    /// </summary>
    /// <param name="target"></param>
    /// <param name="param"></param>
    private void SetParam(DynamicBone target, XmlElement param)
    {
        string name = param.GetAttribute("name");
        FieldInfo fieldInfo = typeof(DynamicBone).GetField(name);

        if (fieldInfo.FieldType == typeof(float))
        {
            fieldInfo.SetValue(target, float.Parse(param.InnerText));
        }
        else if(fieldInfo.FieldType == typeof(Vector3))
        {
            fieldInfo.SetValue(target, ConvertVector3(param));
        }
        else if (fieldInfo.FieldType == typeof(AnimationCurve))
        {
            fieldInfo.SetValue(target, ConvertAnimationCurve(param));
        }
        else if (fieldInfo.FieldType == typeof(DynamicBone.UpdateMode))
        {
            fieldInfo.SetValue(target, (DynamicBone.UpdateMode)Enum.Parse(typeof(DynamicBone.UpdateMode), param.InnerText));
        }
        else if (fieldInfo.FieldType == typeof(DynamicBone.FreezeAxis))
        {
            fieldInfo.SetValue(target, (DynamicBone.FreezeAxis)Enum.Parse(typeof(DynamicBone.FreezeAxis), param.InnerText));
        }
        else if (fieldInfo.FieldType == typeof(bool))
        {
            fieldInfo.SetValue(target, bool.Parse(param.InnerText));
        }
        else
        {
            fieldInfo.SetValue(target, param.InnerText);
        }
    }

    /// <summary>
    /// Vector3変換
    /// </summary>
    /// <param name="param"></param>
    /// <returns></returns>
    private Vector3 ConvertVector3(XmlElement param)
    {
        return new Vector3 {
            x = float.Parse(param.SelectSingleNode("x").InnerText),
            y = float.Parse(param.SelectSingleNode("y").InnerText),
            z = float.Parse(param.SelectSingleNode("z").InnerText)
        };
    }

    /// <summary>
    /// AnimationCurve変換
    /// </summary>
    /// <param name="param"></param>
    /// <returns></returns>
    private AnimationCurve ConvertAnimationCurve(XmlElement param)
    {
        AnimationCurve val = new AnimationCurve
        {
            preWrapMode = (WrapMode) int.Parse(param.SelectSingleNode("preWrapMode").InnerText),
            postWrapMode = (WrapMode)int.Parse(param.SelectSingleNode("postWrapMode").InnerText)
        };


        foreach (XmlElement keyFrameData in param.SelectNodes("Keys/Key"))
        {
            Keyframe cloneKey = new Keyframe {
                time = float.Parse(keyFrameData.SelectSingleNode("time").InnerText),
                value = float.Parse(keyFrameData.SelectSingleNode("value").InnerText),
                inTangent = float.Parse(keyFrameData.SelectSingleNode("inTangent").InnerText),
                outTangent = float.Parse(keyFrameData.SelectSingleNode("outTangent").InnerText)
            };
            val.AddKey(cloneKey);
        }

        return val;
    }

    /// <summary>
    /// パラメータ設定追記
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="name"></param>
    /// <param name="target"></param>
    /// <param name="xmlDoc"></param>
    private void AddParam(XmlElement parent, string name, DynamicBone target, XmlDocument xmlDoc)
    {
        XmlElement param = xmlDoc.CreateElement("Param");
        FieldInfo fieldInfo = typeof(DynamicBone).GetField(name);
        param.SetAttribute("name", name);

        if (fieldInfo.FieldType == typeof(Vector3))
        {
            Vector3 vector3Val = (Vector3)fieldInfo.GetValue(target);
            SetVector3Param(xmlDoc, param, vector3Val);
        }
        else if (fieldInfo.FieldType == typeof(AnimationCurve))
        {
            AnimationCurve animationCurveVal = (AnimationCurve)fieldInfo.GetValue(target);
            SetAnimationCurveParam(xmlDoc, param, animationCurveVal);
        }
        else
        {
            object val = fieldInfo.GetValue(target);
            param.InnerText = val.ToString() ?? "";
        }
        parent.AppendChild(param);
    }

    /// <summary>
    /// Vector3用
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="target"></param>
    /// <param name="xmlDoc"></param>
    private void SetVector3Param(XmlDocument xmlDoc, XmlElement parent, Vector3 target)
    {
        XmlElement x = xmlDoc.CreateElement("x");
        x.InnerText = target.x.ToString();

        XmlElement y = xmlDoc.CreateElement("y");
        y.InnerText = target.y.ToString();

        XmlElement z = xmlDoc.CreateElement("z");
        z.InnerText = target.z.ToString();

        parent.AppendChild(x);
        parent.AppendChild(y);
        parent.AppendChild(z);
    }

    /// <summary>
    /// AnimationCurve用
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <param name="parent"></param>
    /// <param name="target"></param>
    private void SetAnimationCurveParam(XmlDocument xmlDoc, XmlElement parent, AnimationCurve target)
    {
        XmlElement preWrapMode = xmlDoc.CreateElement("preWrapMode");
        preWrapMode.InnerText = ((int)target.preWrapMode).ToString();

        XmlElement postWrapMode = xmlDoc.CreateElement("postWrapMode");
        postWrapMode.InnerText = ((int)target.preWrapMode).ToString();

        XmlElement keys = xmlDoc.CreateElement("Keys");
        foreach (Keyframe addKey in target.keys)
        {
            AddKeyframe(xmlDoc, keys, addKey);
        }
        parent.AppendChild(preWrapMode);
        parent.AppendChild(postWrapMode);
        parent.AppendChild(keys);
    }

    /// <summary>
    /// Keyframe用
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <param name="parent"></param>
    /// <param name="key"></param>
    private void AddKeyframe(XmlDocument xmlDoc, XmlElement parent, Keyframe key)
    {
        XmlElement keyElement = xmlDoc.CreateElement("Key");

        XmlElement time = xmlDoc.CreateElement("time");
        time.InnerText = key.time.ToString();

        XmlElement value = xmlDoc.CreateElement("value");
        value.InnerText = key.value.ToString();

        XmlElement inTangent = xmlDoc.CreateElement("inTangent");
        inTangent.InnerText = key.inTangent.ToString();

        XmlElement outTangent = xmlDoc.CreateElement("outTangent");
        outTangent.InnerText = key.outTangent.ToString();

        keyElement.AppendChild(time);
        keyElement.AppendChild(value);
        keyElement.AppendChild(inTangent);
        keyElement.AppendChild(outTangent);
        parent.AppendChild(keyElement);
    }
}

