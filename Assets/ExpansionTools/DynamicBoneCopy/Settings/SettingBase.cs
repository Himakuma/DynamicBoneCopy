using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;

public abstract class SettingBase : IDynamicBoneSetting
{

    public abstract bool ExportXmlBone(DynamicBone target, string exportName, string writePath);
    public abstract bool ExportXmlCollider(DynamicBoneCollider target, string exportName, string writePath);

    public abstract void SetXmlToBone(string xmlPath, string name, DynamicBone target);
    public abstract void SetXmlToCollider(string xmlPath, string name, DynamicBoneCollider target);

    /// <summary>
    /// Vector3用
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="target"></param>
    /// <param name="xmlDoc"></param>
    protected void SetVector3Param(XmlDocument xmlDoc, XmlElement parent, Vector3 target)
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
    /// Vector3変換
    /// </summary>
    /// <param name="param"></param>
    /// <returns></returns>
    protected Vector3 ConvertVector3(XmlElement param)
    {
        return new Vector3
        {
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
    protected AnimationCurve ConvertAnimationCurve(XmlElement param)
    {
        AnimationCurve val = new AnimationCurve
        {
            preWrapMode = (WrapMode)int.Parse(param.SelectSingleNode("preWrapMode").InnerText),
            postWrapMode = (WrapMode)int.Parse(param.SelectSingleNode("postWrapMode").InnerText)
        };

        foreach (XmlElement keyFrameData in param.SelectNodes("Keys/Key"))
        {
            Keyframe cloneKey = new Keyframe
            {
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
    /// AnimationCurve用
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <param name="parent"></param>
    /// <param name="target"></param>
    protected void SetAnimationCurveParam(XmlDocument xmlDoc, XmlElement parent, AnimationCurve target)
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