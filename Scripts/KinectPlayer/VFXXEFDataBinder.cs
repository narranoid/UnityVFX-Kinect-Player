using System;
using System.Collections;
using System.Collections.Generic;
using KinectXEFTools;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

[AddComponentMenu("VFX/Property Binders/XEF Data Binder")]
[VFXBinder("XEF/XEF Data")]
class VFXXEFDataBinder : VFXBinderBase
{
    [Serializable]
    public struct VFXPropertyXEFJointPair
    {
        public string VFXProperty { get { return (string)vfxProperty; } set { vfxProperty = value; } }
        [VFXPropertyBinding("UnityEngine.Vector4"), SerializeField]
        private ExposedProperty vfxProperty;

        public XEFJointType XEFJoint { get { return xefJoint; } set { xefJoint = value; } }
        [SerializeField]
        private XEFJointType xefJoint;
    }

    public string CountProperty { get { return (string)m_CountProperty; } set { m_CountProperty = value; } }
    [VFXPropertyBinding("System.UInt32"), SerializeField]
    protected ExposedProperty m_CountProperty = "Point Count";

    public string ColorTextureProperty { get { return (string)m_ColorTextureProperty; } set { m_ColorTextureProperty = value; } }
    [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
    protected ExposedProperty m_ColorTextureProperty = "Color Texture";

    public string DepthTextureProperty { get { return (string)m_DepthTextureProperty; } set { m_DepthTextureProperty = value; } }
    [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
    protected ExposedProperty m_DepthTextureProperty = "Depth Texture";

    public string BodyIndexTextureProperty { get { return (string)m_BodyIndexTextureProperty; } set { m_BodyIndexTextureProperty = value; } }
    [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
    protected ExposedProperty m_BodyIndexTextureProperty = "Body Index Texture";

    public string ColorProjectionMatrixProperty { get { return (string)m_ColorProjectionMatrixProperty; } set { m_ColorProjectionMatrixProperty = value; } }
    [VFXPropertyBinding("UnityEngine.Matrix4x4"), SerializeField]
    protected ExposedProperty m_ColorProjectionMatrixProperty = "Color Projection Matrix";

    [SerializeField]
    private List<VFXPropertyXEFJointPair> jointMappings = new List<VFXPropertyXEFJointPair>();

    [SerializeField] private bool syncVisualEffectWithPlayer = false;

    public XEFPlayer player;

    public override bool IsValid(VisualEffect component)
    {
        return player != null;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if(syncVisualEffectWithPlayer)
        {
            VisualEffect effect = GetComponent<VisualEffect>();
            effect.pause = true;
            player.OnFrameAdvance += OnPlayerFrameAdvance;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if(syncVisualEffectWithPlayer)
        {
            player.OnFrameAdvance -= OnPlayerFrameAdvance;
        }
    }

    private void OnPlayerFrameAdvance()
    {
        if (syncVisualEffectWithPlayer)
        {
            //Debug.Log("YEAH");
            VisualEffect effect = GetComponent<VisualEffect>();
            effect.AdvanceOneFrame();
        }
    }

    public override void UpdateBinding(VisualEffect component)
    {
        if(Application.isPlaying)
        {
            component.SetTexture(ColorTextureProperty, player.ColorTexture);
            component.SetTexture(DepthTextureProperty, player.DepthTexture);
            //component.SetMatrix4x4(ColorProjectionMatrixProperty, player.GPUProjectionMatrix);
            if(player.BodyIndexTexture)
                component.SetTexture(BodyIndexTextureProperty, player.BodyIndexTexture);
            component.SetUInt(CountProperty, (uint)player.PointCount);

            foreach(VFXPropertyXEFJointPair jointMapping in jointMappings)
            {
                Vector3 jointPos = player.GetJointPosition(jointMapping.XEFJoint);
                component.SetVector3(jointMapping.VFXProperty, jointPos);
            }
        }
    }

    public override string ToString()
    {
        if(player != null)
        {
            Vector2Int res = player.Resolution;
            return string.Format("XEF Data : '{0}x{1} points' -> {2}", 
                res.x, res.y, player.gameObject.name);
        }

        return "XEF Data : Needs XEF Player...";
    }
}