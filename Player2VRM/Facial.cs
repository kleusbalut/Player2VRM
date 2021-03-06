﻿using System.Collections;
using UnityEngine;
using VRM;
#pragma warning disable IDE0051

namespace Player2VRM.Facial
{
    static class Config
    {
        // まばたき
        public static float blinkTime = 0.2f;      // まばたき時間
        public static float blinkInterval = 3f;    // 乱数による最大瞬き間隔
        public static float faceTweenTime = 0.2f; // 表情遷移時間

        // 表情キーアサイン (Alt+)
        public static KeyCode keyFun = KeyCode.H;
        public static KeyCode keyJoy = KeyCode.J;
        public static KeyCode keySorrow = KeyCode.K;
        public static KeyCode keyAngry = KeyCode.L;
    }

    public enum FaceType
    {
        Neutral,
        Joy,
        Angry,
        Sorrow,
        Fun,
    }

    class EyeCtrl : MonoBehaviour
    {
        VRMBlendShapeProxy blendProxy;
        VRMLookAtHead lookAt;
        float nextBlinkTime;
        bool isBlinking;
        readonly BlendShapeKey shapeKeyBlink = BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink);

        /// <summary>目パチ有効/無効</summary>
        public bool BlinkEnabled
        {
            get => blinkEnabled;
            set
            {
                if (blinkEnabled == value)
                    return;
                blinkEnabled = value;
                if (blinkEnabled)
                    SetNextBlink();
                else
                    nextBlinkTime = float.MaxValue;
            }
        }
        bool blinkEnabled;


        /// <summary>対象を見る</summary>
        /// <param name="target">nullでニュートラル</param>
        public void SetLookAt(Transform target)
        {
            if (lookAt)
                lookAt.Target = target;
        }

        void Start()
        {
            blendProxy = GetComponent<VRMBlendShapeProxy>();
            lookAt = GetComponent<VRMLookAtHead>();
            if (blendProxy == null)
                enabled = false;
            BlinkEnabled = true;
        }

        void Update()
        {
            if (nextBlinkTime > Time.time || isBlinking)
                return;

            StartCoroutine(Blink());
        }

        IEnumerator Blink()
        {
            isBlinking = true;
            nextBlinkTime = float.MaxValue;
            var blink = Config.blinkTime;
            var dnormal = 2 / Config.blinkTime;
            var half = Config.blinkTime / 2;

            while (blink > 0)
            {
                blink -= Time.deltaTime;
                if (blink < 0)
                    blink = 0;

                blendProxy.AccumulateValue(shapeKeyBlink, 1 - Mathf.Abs((blink - half) * dnormal));
                yield return new WaitForEndOfFrame();
            }
            if (blinkEnabled) SetNextBlink();
            isBlinking = false;
        }

        void SetNextBlink()
        {
            nextBlinkTime = Time.time + Random.value * Config.blinkInterval + Config.blinkInterval * 0.1f;
        }
    }

    internal class ShapeWeight
    {
        public BlendShapeKey ShapeKey { get; private set; }
        public float Weight { get; private set; }

        float targetWeight;
        float animTime;

        public ShapeWeight(BlendShapePreset prisetKey)
        {
            ShapeKey = BlendShapeKey.CreateFromPreset(prisetKey);
        }

        public void Enable(bool enabled)
        {
            animTime = Config.faceTweenTime;
            targetWeight = enabled ? 1 : 0;
        }

        public void Update(float dltTime)
        {
            if (animTime > 0)
            {
                animTime -= dltTime;
                if (animTime < 0)
                    animTime = 0;
                Weight = Mathf.Lerp(targetWeight, Weight, animTime / Config.faceTweenTime);
            }
        }
    }


    class FaceCtrl : MonoBehaviour
    {
        VRMBlendShapeProxy blendProxy;
        EyeCtrl facialEye;
        FaceType currentFace;

        readonly ShapeWeight[] shapeWeights = new ShapeWeight[] {
            new ShapeWeight(BlendShapePreset.Joy),
            new ShapeWeight(BlendShapePreset.Angry),
            new ShapeWeight(BlendShapePreset.Sorrow),
            new ShapeWeight(BlendShapePreset.Fun),
        };

        public void SetEmote(FaceType emote)
        {
            if (currentFace == emote)
                return;

            if (currentFace != FaceType.Neutral)
                shapeWeights[(int)currentFace - 1].Enable(false);
            bool isEyeBlinkEnable = false;
            if (emote != FaceType.Neutral)
                shapeWeights[(int)emote - 1].Enable(true);
            else
                isEyeBlinkEnable = true;

            if (facialEye)
                facialEye.BlinkEnabled = isEyeBlinkEnable;

            currentFace = emote;
        }

        void Start()
        {
            blendProxy = GetComponent<VRMBlendShapeProxy>();
            facialEye = GetComponent<EyeCtrl>();
            if (blendProxy == null)
                enabled = false;
        }

        void Update()
        {
            // 表情（埋め込みキーアサインです・・・）
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                if (Input.GetKey(Config.keyFun))
                    SetEmote(FaceType.Fun);
                else if (Input.GetKey(Config.keyJoy))
                    SetEmote(FaceType.Joy);
                else if (Input.GetKey(Config.keySorrow))
                    SetEmote(FaceType.Sorrow);
                else if (Input.GetKey(Config.keyAngry))
                    SetEmote(FaceType.Angry);
                else
                    SetEmote(FaceType.Neutral);
            }
            else
            {
                SetEmote(FaceType.Neutral);
            }

            var dlt = Time.deltaTime;
            foreach (var shape in shapeWeights)
            {
                shape.Update(dlt);
                blendProxy.AccumulateValue(shape.ShapeKey, shape.Weight);
            }
        }
    }
}

