using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEngine;
    
public class DynamicBoneSettingV1 : SettingBase
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
        "m_DistanceToObject",
        "m_Colliders"
    };

    /// <summary>
    /// XMLデータ出力
    /// </summary>
    /// <param name="target"></param>
    /// <param name="exportName"></param>
    /// <param name="writePath"></param>
    /// <returns></returns>
    public override bool ExportXmlBone(DynamicBone target, string exportName, string writePath)
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
    ///  ボーンXMLデータ設定
    /// </summary>
    /// <param name="xmlPath"></param>
    /// <param name="name"></param>
    /// <param name="target"></param>
    public override void SetXmlToBone(string xmlPath, string name, DynamicBone target)
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
    /// <param name="targetElem"></param>
    /// <returns></returns>
    private XmlElement GetParamsXmlElement(XmlDocument xmlDoc, XmlElement targetElem)
    {
        XmlElement paramsElem = (XmlElement)targetElem.SelectSingleNode("Params");
        if (paramsElem == null)
        {
            paramsElem = xmlDoc.CreateElement("Params");
            targetElem.AppendChild(paramsElem);
        }
        else
        {
            paramsElem.RemoveAll();
        }
        return paramsElem;
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
        else if (fieldInfo.FieldType == typeof(Vector3))
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
        else if (fieldInfo.FieldType == typeof(List<DynamicBoneColliderBase>))
        {
            List<DynamicBoneColliderBase> colliders = (List<DynamicBoneColliderBase>)fieldInfo.GetValue(target);
            fieldInfo.SetValue(target, ConvertColliders(colliders, param));
        }
        else
        {
            fieldInfo.SetValue(target, param.InnerText);
        }
    }

    /// <summary>
    /// List<DynamicBoneColliderBase>変換
    /// </summary>
    /// <param name="targetColliders"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    private List<DynamicBoneColliderBase> ConvertColliders(List<DynamicBoneColliderBase> targetColliders, XmlElement param)
    {
        if (targetColliders == null)
        {
            targetColliders = new List<DynamicBoneColliderBase>();
        }

        XmlNodeList colliderDatas = param.SelectNodes("Colliders/Collider");
        for (int i = 0; i < colliderDatas.Count; i++)
        {
            if (i < targetColliders.Count)
            {
                DynamicBoneCollider targetCollider = (DynamicBoneCollider)targetColliders[i];
                ConvertColliderParamCopy(targetCollider, colliderDatas[i]);
            }
            else
            {
                targetColliders.Add(null);
            }
        }
        return targetColliders;
    }

    /// <summary>
    /// DynamicBoneCollider変換
    /// </summary>
    /// <param name="targetCollider"></param>
    /// <param name="colliderData"></param>
    private void ConvertColliderParamCopy(DynamicBoneCollider targetCollider, XmlNode colliderData)
    {
        targetCollider.m_Direction = (DynamicBoneCollider.Direction)Enum.Parse(typeof(DynamicBoneCollider.Direction), colliderData.SelectSingleNode("m_Direction").InnerText);
        targetCollider.m_Center = ConvertVector3((XmlElement)colliderData.SelectSingleNode("m_Center"));
        targetCollider.m_Bound = (DynamicBoneCollider.Bound)Enum.Parse(typeof(DynamicBoneCollider.Bound), colliderData.SelectSingleNode("m_Bound").InnerText);
        targetCollider.m_Radius = float.Parse(colliderData.SelectSingleNode("m_Radius").InnerText);
        targetCollider.m_Height = float.Parse(colliderData.SelectSingleNode("m_Height").InnerText);
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
        else if (fieldInfo.FieldType == typeof(List<DynamicBoneColliderBase>))
        {
            XmlElement collidersElem = xmlDoc.CreateElement("Colliders");
            
            List<DynamicBoneColliderBase> colliders = (List<DynamicBoneColliderBase>)fieldInfo.GetValue(target);
            foreach (DynamicBoneColliderBase collider in colliders)
            {
                XmlElement colliderElem = xmlDoc.CreateElement("Collider");
                SetBoneCollider(xmlDoc, colliderElem, (DynamicBoneCollider)collider);
                collidersElem.AppendChild(colliderElem);
            }
            param.AppendChild(collidersElem);
        }
        else
        {
            object val = fieldInfo.GetValue(target);
            param.InnerText = val.ToString() ?? "";
        }
        parent.AppendChild(param);
    }

    /// <summary>
    /// DynamicBoneColliderBase用
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <param name="parent"></param>
    /// <param name="target"></param>
    private void SetBoneCollider(XmlDocument xmlDoc, XmlElement parent, DynamicBoneCollider target)
    {
        XmlElement mDirection = xmlDoc.CreateElement("m_Direction");
        mDirection.InnerText = target.m_Direction.ToString();

        XmlElement mCenter = xmlDoc.CreateElement("m_Center");
        SetVector3Param(xmlDoc, mCenter, target.m_Center);

        XmlElement mBound = xmlDoc.CreateElement("m_Bound");
        mBound.InnerText = target.m_Bound.ToString();

        XmlElement mRadius = xmlDoc.CreateElement("m_Radius");
        mRadius.InnerText = target.m_Radius.ToString();

        XmlElement mHeight = xmlDoc.CreateElement("m_Height");
        mHeight.InnerText = target.m_Height.ToString();

        parent.AppendChild(mDirection);
        parent.AppendChild(mCenter);
        parent.AppendChild(mBound);
        parent.AppendChild(mRadius);
        parent.AppendChild(mHeight);
    }


    /// <summary>
    /// コライダー名からエレメント取得
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private XmlElement GetColliderXmlElement(XmlDocument xmlDoc, string name)
    {
        XmlNodeList colliders = xmlDoc.SelectNodes("DynamicBones/Collider");
        foreach (XmlNode boneNode in colliders)
        {
            if (boneNode.SelectSingleNode("Name").InnerText == name)
            {
                return (XmlElement)boneNode;
            }
        }

        XmlElement root = (XmlElement)xmlDoc.SelectSingleNode("DynamicBones");
        XmlElement boneElem = xmlDoc.CreateElement("Collider");
        root.AppendChild(boneElem);

        XmlElement nameElem = xmlDoc.CreateElement("Name");
        nameElem.InnerText = name;
        boneElem.AppendChild(nameElem);

        XmlElement dataVersionElem = xmlDoc.CreateElement("DataVersion");
        dataVersionElem.InnerText = DATA_VERSION;
        boneElem.AppendChild(dataVersionElem);
        return boneElem;
    }

    public override bool ExportXmlCollider(DynamicBoneCollider target, string exportName, string writePath)
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

        XmlElement colliderElem = GetBoneColliderXmlElement(xmlDoc, exportName);
        XmlElement paramsElem = GetParamsXmlElement(xmlDoc, colliderElem);
        XmlElement paramElem = xmlDoc.CreateElement("Param");
        
        XmlElement colliderParamElem = xmlDoc.CreateElement("Collider");
        SetBoneCollider(xmlDoc, colliderParamElem, target);

        paramElem.AppendChild(colliderParamElem);
        paramsElem.AppendChild(paramElem);


        using (StringWriter writer = new StringWriter())
        using (XmlWriter xmlWriter = XmlWriter.Create(writer))
        {
            xmlDoc.WriteTo(xmlWriter);
            xmlWriter.Flush();
            return FileUtil.WriteText(writer.GetStringBuilder().ToString(), writePath, Encoding.UTF8, false);
        }
    }

    /// <summary>
    /// コライダーXMLデータ設定
    /// </summary>
    /// <param name="xmlPath"></param>
    /// <param name="name"></param>
    /// <param name="target"></param>
    public override void SetXmlToCollider(string xmlPath, string name, DynamicBoneCollider target)
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(xmlPath);
        foreach (XmlNode nodeData in xmlDoc.SelectNodes("DynamicBones/Collider"))
        {
            if (name == nodeData.SelectSingleNode("Name").InnerText)
            {
                ConvertColliderParamCopy(target, nodeData.SelectSingleNode("Params/Param/Collider"));
                return;
            }
        }
    }

    /// <summary>
    /// ボーン名からエレメント取得
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private XmlElement GetBoneColliderXmlElement(XmlDocument xmlDoc, string name)
    {
        XmlNodeList colliders = xmlDoc.SelectNodes("DynamicBones/Collider");
        foreach (XmlNode colliderNode in colliders)
        {
            if (colliderNode.SelectSingleNode("Name").InnerText == name)
            {
                return (XmlElement)colliderNode;
            }
        }

        XmlElement root = (XmlElement)xmlDoc.SelectSingleNode("DynamicBones");

        XmlElement colliderElem = xmlDoc.CreateElement("Collider");
        root.AppendChild(colliderElem);

        XmlElement nameElem = xmlDoc.CreateElement("Name");
        nameElem.InnerText = name;
        colliderElem.AppendChild(nameElem);

        XmlElement dataVersionElem = xmlDoc.CreateElement("DataVersion");
        dataVersionElem.InnerText = DATA_VERSION;
        colliderElem.AppendChild(dataVersionElem);
        return colliderElem;
    }
}

