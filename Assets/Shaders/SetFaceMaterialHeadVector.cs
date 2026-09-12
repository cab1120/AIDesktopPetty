using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///设置面部材质的方向向量
///</summary>
[ExecuteInEditMode]//在编辑器中执行
public class SetFaceMaterialHeadVector : MonoBehaviour
{
    public Transform Head;//头部骨骼
    public Transform HeadForward; //前方
    public Transform HeadRight; //右侧
    public Transform HeadUp; // 上方
    public Material FaceMaterial;

    void Update()
    {
        //归一化向量
        Vector3 headForward = Vector3.Normalize(HeadForward.position - Head.position);
        Vector3 headRight = Vector3.Normalize(HeadRight.position - Head.position);
        Vector3 headUp = Vector3.Normalize(HeadUp.position - Head.position);
        //传递向量
        FaceMaterial.SetVector("_HeadForward", headForward);
        FaceMaterial.SetVector("_HeadRight", headRight);
        FaceMaterial.SetVector("_HeadUp", headUp);
    }
}
