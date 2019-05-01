using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

namespace LightUtilities
{
    public class CineLightMixer : PlayableBehaviour {

        // Called each frame the mixer is active, after inputs are processed
        [HideInInspector][SerializeField]
        public GameObject lightTargetGO;
        public Transform lightTransform;
        public Transform lightPitchTransform;
        public Transform lightYawTransform;
        [HideInInspector]
        [SerializeField]
        public Light light;
        private bool isFading;
        private bool isCrossFading;
        private bool globalUseShadowCaster = false;
        public Vector3 attachmentPosition;

        LightParameters neutralLightParameters = new LightParameters(LightType.Spot, LightmapPresetBakeType.Realtime, true);
        LightParameters mixedLightParameters = new LightParameters(LightType.Spot, LightmapPresetBakeType.Realtime, true);

        CineLightParameters neutralCineLightParameters = new CineLightParameters(true);
        CineLightParameters mixedCineLightParameters = new CineLightParameters(true);

        ShadowCasterParameters mixedShadowCasterParameters = new ShadowCasterParameters(true);

        public override void OnGraphStart(Playable playable)
        {
            if (lightTargetGO == null)
            {
                lightTargetGO = new GameObject("LightTargetTimeline", typeof(CineLight));
            }

            else
                lightTargetGO.SetActive(true);

            base.OnGraphStart(playable);
        }

        public override void OnGraphStop(Playable playable)
        {
            if (lightTargetGO != null)
                GameObject.DestroyImmediate(lightTargetGO);
            base.OnGraphStop(playable);
        }

        public override void ProcessFrame(Playable handle, FrameData info, object playerData) {
            var count = handle.GetInputCount();

            var cineLight = lightTargetGO.GetComponent<CineLight>();

            GameObject attachmentTransform = playerData as GameObject;

            if (attachmentTransform != null)
            {
                cineLight.SetAttachmentTransform(attachmentTransform, true);
            }

            light = cineLight.GetComponentInChildren<Light>();

            globalUseShadowCaster = false;

            List<float> inputWeights = new List<float>();
            CineLightParameters fromCineLightParameters = new CineLightParameters();
            LightParameters fromLightParameters = new LightParameters();
            ShadowCasterParameters fromShadowCasterParameters = new ShadowCasterParameters();
            float fadeWeight = new float();

            CineLightParameters toCineLightParameters = new CineLightParameters();
            LightParameters toLightParameters = new LightParameters();
            ShadowCasterParameters toShadowCasterParameters = new ShadowCasterParameters();
            float crossFadeWeight = new float();

            //Short loop, feed data for most cases
            for (var i = 0; i < count; i++)
            {

                var inputHandle = handle.GetInput(i);
                var weight = handle.GetInputWeight(i);

                if (inputHandle.IsValid() &&
                    inputHandle.GetPlayState() == PlayState.Playing &&
                    weight > 0)
                {
                    var data = ((ScriptPlayable<CineLightClipPlayable>)inputHandle).GetBehaviour();
                    if (data != null)
                    {
                        if (weight > 0)
                        {
                            inputWeights.Add(weight);
                            if(inputWeights.Count == 1)
                            {
                                fromCineLightParameters = CineLightParameters.DeepCopy(data.cinelightParameters);
                                fromLightParameters = LightParameters.DeepCopy(data.lightParameters);
                                fromShadowCasterParameters = ShadowCasterParameters.DeepCopy(data.shadowCasterParameters);
                                fadeWeight = weight;
                            }
                            if(inputWeights.Count == 2)
                            {
                                toCineLightParameters = CineLightParameters.DeepCopy(data.cinelightParameters);
                                toLightParameters = LightParameters.DeepCopy(data.lightParameters);
                                toShadowCasterParameters = ShadowCasterParameters.DeepCopy(data.shadowCasterParameters);
                                crossFadeWeight = weight;
                            }
                            if (data.shadowCasterParameters.useShadowCaster == true)
                                globalUseShadowCaster = true;
                        }
                        if (inputWeights.Count > 2)
                            break;
                    }
                }
            }

            if(inputWeights.Count == 0)
            {
                lightTargetGO.SetActive(false);
                return;
            }
            lightTargetGO.SetActive(true);

            if (inputWeights.Count == 1)
            {
                isFading = true;
                isCrossFading = false;
            }
            else if (inputWeights.Count == 2)
            {
                isFading = false;
                isCrossFading = true;
            }
            else
            {
                isFading = false;
                isCrossFading = false;
            }

            if(isFading == true)
            {
                DoSingleClip(fromCineLightParameters,fromLightParameters,fromShadowCasterParameters,fadeWeight);
            }

            if(isCrossFading == true)
            {
                DoCrossFadeSettings(fromCineLightParameters,fromLightParameters,fromShadowCasterParameters,toCineLightParameters,toLightParameters,toShadowCasterParameters,crossFadeWeight,cineLight);
            }

            if (isFading == false && isCrossFading == false)
            {
                DoLongLoop(cineLight, handle, count);
            }

            mixedLightParameters.fadeDistance = 99999;
            mixedLightParameters.shadowFadeDistance = 99999;
            mixedLightParameters.mode = LightmapPresetBakeType.Realtime;

            LightingUtilities.ApplyLightParameters(light, mixedLightParameters);
		    CineLightUtilities.ApplyCineLightParameters(cineLight, mixedCineLightParameters);

            if (globalUseShadowCaster && cineLight.shadowCasterGO == null)
            {
                cineLight.useShadowCaster = true;
                cineLight.ApplyShadowCaster();
            }

            if (cineLight.shadowCasterGO != null)
            {
                cineLight.shadowCasterGO.GetComponent<MeshRenderer>().enabled = mixedShadowCasterParameters.useShadowCaster;
                cineLight.shadowCasterGO.transform.localScale = new Vector3(mixedShadowCasterParameters.shadowCasterSize.x, mixedShadowCasterParameters.shadowCasterSize.y, 1);
                cineLight.shadowCasterGO.transform.localPosition = new Vector3(mixedShadowCasterParameters.shadowCasterOffset.x, mixedShadowCasterParameters.shadowCasterOffset.y, -mixedShadowCasterParameters.shadowCasterDistance);
            }
        }

        private void DoSingleClip(CineLightParameters fromCineLightParameters, LightParameters fromLightParameters, ShadowCasterParameters fromShadowCasterParameters,float weight)
        {
            mixedCineLightParameters = fromCineLightParameters;
            mixedLightParameters = fromLightParameters;
            mixedShadowCasterParameters = fromShadowCasterParameters;
            mixedLightParameters.intensity = fromLightParameters.intensity * weight;
        }

        private void DoCrossFadeSettings(CineLightParameters fromCineLightParameters, LightParameters fromLightParameters, ShadowCasterParameters fromShadowCasterParameters, CineLightParameters toCineLightParameters, LightParameters toLightParameters, ShadowCasterParameters toShadowCasterParameters,float weight,CineLight cineLight)
        {
            mixedCineLightParameters = CineLightUtilities.LerpCineLightParameters(fromCineLightParameters, toCineLightParameters, weight);

            //Shortest Path Rotation
            if(Mathf.Abs(fromCineLightParameters.Yaw - toCineLightParameters.Yaw) > 180)
            {
                if(fromCineLightParameters.Yaw>0)
                    mixedCineLightParameters.Yaw = Mathf.Lerp(fromCineLightParameters.Yaw - 360 , toCineLightParameters.Yaw, weight);
                else
                    mixedCineLightParameters.Yaw = Mathf.Lerp(fromCineLightParameters.Yaw + 360, toCineLightParameters.Yaw, weight);
            }
            else
                mixedCineLightParameters.Yaw = Mathf.Lerp(fromCineLightParameters.Yaw, toCineLightParameters.Yaw, weight);

            mixedLightParameters = LightingUtilities.LerpLightParameters(fromLightParameters, toLightParameters, weight);

            mixedShadowCasterParameters.shadowCasterDistance = Mathf.Lerp(fromShadowCasterParameters.shadowCasterDistance, toShadowCasterParameters.shadowCasterDistance, weight);
            mixedShadowCasterParameters.shadowCasterOffset = Vector2.Lerp(fromShadowCasterParameters.shadowCasterOffset, toShadowCasterParameters.shadowCasterOffset, weight);
            mixedShadowCasterParameters.shadowCasterSize = Vector2.Lerp(fromShadowCasterParameters.shadowCasterSize, toShadowCasterParameters.shadowCasterSize, weight);
            
            //Doesn't interpolate
            if (weight < 0.5f)
            {
                cineLight.drawGizmo = fromCineLightParameters.drawGizmo;
                mixedShadowCasterParameters.useShadowCaster = fromShadowCasterParameters.useShadowCaster;
                mixedCineLightParameters.drawGizmo = fromCineLightParameters.drawGizmo;
            }
            if (weight > 0.5f)
            {
                cineLight.drawGizmo = toCineLightParameters.drawGizmo;
                mixedShadowCasterParameters.useShadowCaster = toShadowCasterParameters.useShadowCaster;
                mixedCineLightParameters.drawGizmo = toCineLightParameters.drawGizmo;
            }
        }

        //Not sure I want to support blending more than 2 lights
        private void DoLongLoop(CineLight cineLight, Playable handle, int count)
        {
            //Check if this ever happens
            if(Application.isEditor || Debug.isDebugBuild)
                Debug.Log("Blending between more than 2 Cine lights on the timeline");

            for (var i = 0; i < count; i++)
            {

                var inputHandle = handle.GetInput(i);
                var weight = handle.GetInputWeight(i);

                if (inputHandle.IsValid() &&
                    inputHandle.GetPlayState() == PlayState.Playing &&
                    weight > 0)
                {
                    var data = ((ScriptPlayable<CineLightClipPlayable>)inputHandle).GetBehaviour();
                    if (data != null)
                    {
                        var lerpedLightParameters = new LightParameters();
                        lerpedLightParameters = LightingUtilities.LerpLightParameters(neutralLightParameters, data.lightParameters, isFading ? 1 : weight);

                        //Not using shortest path
                        mixedCineLightParameters.Yaw += Mathf.Lerp(neutralCineLightParameters.Yaw, data.cinelightParameters.Yaw, isFading ? 1 : weight);
                        mixedCineLightParameters.Pitch += Mathf.Lerp(neutralCineLightParameters.Pitch, data.cinelightParameters.Pitch, isFading ? 1 : weight);
                        mixedCineLightParameters.Roll += Mathf.Lerp(neutralCineLightParameters.Roll, data.cinelightParameters.Roll, isFading ? 1 : weight);
                        mixedCineLightParameters.distance += Mathf.Lerp(neutralCineLightParameters.distance, data.cinelightParameters.distance, isFading ? 1 : weight);
                        mixedCineLightParameters.offset += Vector3.Lerp(neutralCineLightParameters.offset, data.cinelightParameters.offset, isFading ? 1 : weight);
                        mixedCineLightParameters.linkToCameraRotation = data.cinelightParameters.linkToCameraRotation;
                        if (weight > 0.5f)
                        {
                            cineLight.drawGizmo = data.cinelightParameters.drawGizmo;
                        }
                        if (weight == 1 || isFading)
                            mixedLightParameters.shadows = data.lightParameters.shadows;

                        mixedLightParameters.intensity += Mathf.Lerp(neutralLightParameters.intensity, data.lightParameters.intensity, weight);
                        mixedLightParameters.range += lerpedLightParameters.range;
                        mixedLightParameters.colorFilter += lerpedLightParameters.colorFilter;
                        mixedLightParameters.lightAngle += lerpedLightParameters.lightAngle;
                        mixedLightParameters.cullingMask = lerpedLightParameters.cullingMask;
                        mixedLightParameters.shadowQuality = lerpedLightParameters.shadowQuality;
                        mixedLightParameters.affectDiffuse = lerpedLightParameters.affectDiffuse;
                        mixedLightParameters.affectSpecular = lerpedLightParameters.affectSpecular;
                        mixedLightParameters.normalBias += lerpedLightParameters.normalBias;
                        mixedLightParameters.ShadowNearClip += lerpedLightParameters.ShadowNearClip;
                        mixedLightParameters.viewBiasMin += lerpedLightParameters.viewBiasMin;
                        mixedLightParameters.viewBiasScale += lerpedLightParameters.viewBiasScale;
                        mixedLightParameters.shadowStrength += lerpedLightParameters.shadowStrength;
                        mixedLightParameters.shadowResolution += lerpedLightParameters.shadowResolution;
                        mixedLightParameters.innerSpotPercent += lerpedLightParameters.innerSpotPercent;
                        mixedLightParameters.maxSmoothness += lerpedLightParameters.maxSmoothness;
                        mixedLightParameters.fadeDistance += lerpedLightParameters.fadeDistance;
                        mixedLightParameters.shadowFadeDistance += lerpedLightParameters.shadowFadeDistance;

                        mixedShadowCasterParameters.shadowCasterDistance += Mathf.Lerp(0, data.shadowCasterParameters.shadowCasterDistance, isFading ? 1 : weight);
                        mixedShadowCasterParameters.shadowCasterOffset += Vector2.Lerp(Vector2.zero, data.shadowCasterParameters.shadowCasterOffset, isFading ? 1 : weight);
                        mixedShadowCasterParameters.shadowCasterSize += Vector2.Lerp(Vector2.zero, data.shadowCasterParameters.shadowCasterSize, isFading ? 1 : weight);

                        if (weight > 0.5 || isFading)
                        {
                            mixedShadowCasterParameters.useShadowCaster = data.shadowCasterParameters.useShadowCaster;
                            mixedCineLightParameters.drawGizmo = data.cinelightParameters.drawGizmo;
                            mixedLightParameters.lightCookie = data.lightParameters.lightCookie;
                        }
                    }
                }
            }
        }
    }
}
