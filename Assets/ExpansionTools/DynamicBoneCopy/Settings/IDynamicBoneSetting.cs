using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

interface IDynamicBoneSetting
{

    /// <summary>
    /// ボーンデータをXMLに書き出し
    /// </summary>
    /// <param name="target"></param>
    /// <param name="exportName"></param>
    /// <param name="writePath"></param>
    /// <returns></returns>
    bool ExportXmlBone(DynamicBone target, string exportName, string writePath);

    /// <summary>
    /// コライダーデータをXMLに書き出し
    /// </summary>
    /// <param name="target"></param>
    /// <param name="exportName"></param>
    /// <param name="writePath"></param>
    /// <returns></returns>
    bool ExportXmlCollider(DynamicBoneCollider target, string exportName, string writePath);

    /// <summary>
    /// XMLデータをボーンに設定
    /// </summary>
    /// <param name="xmlPath"></param>
    /// <param name="name"></param>
    /// <param name="target"></param>
    void SetXmlToBone(string xmlPath, string name, DynamicBone target);

    /// <summary>
    /// XMLデータをコライダーに設定
    /// </summary>
    /// <param name="xmlPath"></param>
    /// <param name="name"></param>
    /// <param name="target"></param>
    void SetXmlToCollider(string xmlPath, string name, DynamicBoneCollider target);

}
