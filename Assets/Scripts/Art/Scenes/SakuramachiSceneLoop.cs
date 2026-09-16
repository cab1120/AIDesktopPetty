using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace AIDesktopPetty.Art.Scenes
{
    /// <summary>One deterministic scene pose for each second of the Timeline.</summary>
    [DisallowMultipleComponent]
    public sealed class SakuramachiSceneLoop : MonoBehaviour
    {
        public const double CycleSeconds = 180;
        public const float RevealTime = 10, StopTime = 30, DepartureTime = 45, HiddenTime = 65;

        [Header("列车：坐标相对于 Railway Frame")]
        public Transform railwayFrame;
        public Transform train;
        public Vector3 stationPosition;
        public Vector3 entryPosition;
        public Vector3 exitPosition;
        public float tunnelMinX;
        public float tunnelMaxX;

        [Serializable]
        public sealed class Gate
        {
            public Transform hinge;
            public Vector3 raisedEuler;
            public Vector3 loweredEuler;
        }

        [Header("两侧铰链：保留导入后的局部轴")]
        public Gate[] gates = Array.Empty<Gate>();
        [Header("两侧六个灯罩，不包含描边")]
        public Renderer[] lenses = Array.Empty<Renderer>();
        [Tooltip("材质中的线性颜色，与 Sakuramachi 的 AuthoredColor 一致。")]
        public Vector4 green = new Vector4(.08f, .85f, .45f, 1);
        public Vector4 red = new Vector4(.95f, .055f, .045f, 1);
        public Vector4 grey = new Vector4(.12f, .12f, .12f, 1);
        [Min(.1f)] public float flashesPerSecond = 2;
        [Min(0)] public float greenEmission = .1f;
        [Min(0)] public float redEmission = 1.2f;

        [Header("可选：随车速变化的行驶声，独占此 AudioSource")]
        public AudioSource movementAudio;
        public AudioClip movementLoop;
        [Range(0, 1)] public float movementVolume = .6f;
        public Vector2 movementPitch = new Vector2(.7f, 1.15f);
        public float NormalizedSpeed { get; private set; }
        
        [Header("运动曲线")]
        [Range(1f, 5f)] public float arrivalEasePower = 2f;
        [Range(0.5f, 3f)] public float departureEasePower = 2f;

        struct LensSlot
        {
            public Renderer renderer;
            public int index;
            public MaterialPropertyBlock original, working;
        }
        readonly List<LensSlot> slots = new List<LensSlot>();
        readonly List<LensSlot> trainSlots = new List<LensSlot>();
        Renderer[] trainRenderers;
        bool[] trainVisibility;
        Quaternion[] originalGateRotations;
        Vector3 originalTrainPosition;
        bool captured;
        PlayableDirector owningDirector;
        static readonly int AuthoredColor = Shader.PropertyToID("_AuthoredColor");
        static readonly int UseAuthoredColor = Shader.PropertyToID("_UseAuthoredColor");
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        static readonly int EmissionStrength = Shader.PropertyToID("_EmissionStrength");

        public bool ValidateBindings(out string error)
        {
            if (railwayFrame == null || train == null) { error = "缺少列车或轨道坐标系引用。"; return false; }
            if (gates == null || gates.Length != 2 || Array.Exists(gates, g => g == null || g.hinge == null))
            { error = "需要两个有效栏杆铰链。"; return false; }
            if (lenses == null || lenses.Length != 6 || Array.Exists(lenses, r => r == null))
            { error = "需要左右两侧共六个灯罩。"; return false; }
            if ((entryPosition.x - stationPosition.x) * (exitPosition.x - stationPosition.x) >= 0 || tunnelMinX >= tunnelMaxX)
            { error = "停车点必须位于出入口之间，隧道裁剪范围必须有效。"; return false; }
            error = null;
            return true;
        }

        public void CaptureOriginalState()
        {
            if (captured || !ValidateBindings(out _)) return;
            originalTrainPosition = train.localPosition;
            trainRenderers = train.GetComponentsInChildren<Renderer>(true);
            trainVisibility = Array.ConvertAll(trainRenderers, r => r.enabled);
            originalGateRotations = Array.ConvertAll(gates, g => g.hinge.localRotation);
            trainSlots.Clear();
            foreach (var renderer in trainRenderers)
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    var original = new MaterialPropertyBlock();
                    var working = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(original, i);
                    renderer.GetPropertyBlock(working, i);
                    trainSlots.Add(new LensSlot { renderer = renderer, index = i, original = original, working = working });
                }
            slots.Clear();
            foreach (var lens in lenses)
                for (int i = 0; i < lens.sharedMaterials.Length; i++)
                {
                    var original = new MaterialPropertyBlock();
                    var working = new MaterialPropertyBlock();
                    lens.GetPropertyBlock(original, i);
                    lens.GetPropertyBlock(working, i);
                    slots.Add(new LensSlot { renderer = lens, index = i, original = original, working = working });
                }
            captured = true;
        }

        /// <param name="playAudio">True only for forward runtime playback, never editor scrubbing.</param>
        public void Evaluate(double seconds, bool playAudio = false)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
            CaptureOriginalState();
            if (!captured) return;
            float t = (float)((seconds % CycleSeconds + CycleSeconds) % CycleSeconds);
            Vector3 position = entryPosition;
            NormalizedSpeed = 0;
            float arrivalSpeed = Mathf.Abs(entryPosition.x - stationPosition.x) / 10f;
            float departureSpeed = Mathf.Abs(stationPosition.x - exitPosition.x) / 10f;
            float maxSpeed = Mathf.Max(arrivalSpeed, departureSpeed);
            if (t >= RevealTime && t < StopTime)
            {
                float u = (t - RevealTime) / (StopTime - RevealTime);
                float ease = Mathf.Pow(1 - u, arrivalEasePower);
                position = Vector3.LerpUnclamped(stationPosition, entryPosition, ease);
                NormalizedSpeed = arrivalSpeed * arrivalEasePower * Mathf.Pow(1 - u, arrivalEasePower - 1) / maxSpeed;
            }
            else if (t >= StopTime && t < DepartureTime)
            {
                position = stationPosition;
            }
            else if (t >= DepartureTime && t < HiddenTime)
            {
                float u = (t - DepartureTime) / (HiddenTime - DepartureTime);
                float safeU = Mathf.Max(u, 0.0001f);
                float ease = Mathf.Pow(u, departureEasePower);
                position = Vector3.LerpUnclamped(stationPosition, exitPosition, ease);
                NormalizedSpeed = departureSpeed * departureEasePower * Mathf.Pow(safeU, departureEasePower - 1) / maxSpeed;
            }
            // Reset only while all train renderers (including outlines and shadows) are hidden.
            bool visible = t >= RevealTime && t < HiddenTime;
            for (int i = 0; i < trainRenderers.Length; i++)
                if (trainRenderers[i] != null) trainRenderers[i].enabled = visible && trainVisibility[i];
            train.position = railwayFrame.TransformPoint(position);
            Vector4 row = railwayFrame.worldToLocalMatrix.GetRow(0);
            Vector4 minPlane = new Vector4(row.x, row.y, row.z, row.w - tunnelMinX);
            Vector4 maxPlane = new Vector4(-row.x, -row.y, -row.z, tunnelMaxX - row.w);
            foreach (var slot in trainSlots)
            {
                if (slot.renderer == null) continue;
                slot.working.SetFloat("_TunnelClip", 1);
                slot.working.SetVector("_TunnelPlaneMin", minPlane);
                slot.working.SetVector("_TunnelPlaneMax", maxPlane);
                slot.renderer.SetPropertyBlock(slot.working, slot.index);
            }

            float lowered = t < 5 ? Smooth(t / 5) : t < StopTime ? 1 :
                t < StopTime + 5 ? 1 - Smooth((t - StopTime) / 5) : 0;
            foreach (var gate in gates)
                gate.hinge.localRotation = Quaternion.Slerp(Quaternion.Euler(gate.raisedEuler),
                    Quaternion.Euler(gate.loweredEuler), lowered);

            bool flashing = t < 3 || (t >= StopTime && t < StopTime + 3);
            float flashTime = t < 3 ? t : t - StopTime;
            bool greyPhase = flashing && (Mathf.FloorToInt(flashTime * Mathf.Max(.1f, flashesPerSecond) * 2) % 2 == 0);
            bool isRed = t >= 3 && t < StopTime + 3;
            Vector4 color = greyPhase ? grey : isRed ? red : green;
            float emission = greyPhase ? 0 : isRed ? redEmission : greenEmission;
            foreach (var slot in slots)
            {
                if (slot.renderer == null) continue;
                slot.working.SetFloat(UseAuthoredColor, 1);
                slot.working.SetVector(AuthoredColor, color);
                slot.working.SetColor(BaseColor, (Color)color);
                slot.working.SetColor(EmissionColor, (Color)color);
                slot.working.SetFloat(EmissionStrength, emission);
                slot.renderer.SetPropertyBlock(slot.working, slot.index);
            }
            UpdateAudio(playAudio && Application.isPlaying);
        }

        static float Smooth(float value) => value * value * (3 - 2 * value);

        void UpdateAudio(bool playing)
        {
            if (movementAudio == null) return;
            if (!playing || movementLoop == null || NormalizedSpeed <= .001f)
            {
                if (movementAudio.isPlaying) movementAudio.Stop();
                return;
            }
            if (movementAudio.clip != movementLoop)
            {
                movementAudio.Stop();
                movementAudio.clip = movementLoop;
            }
            movementAudio.loop = true;
            movementAudio.volume = movementVolume * Mathf.Sqrt(NormalizedSpeed);
            movementAudio.pitch = Mathf.Lerp(movementPitch.x, movementPitch.y, NormalizedSpeed);
            if (!movementAudio.isPlaying) movementAudio.Play();
        }

        public void SilenceAudio()
        {
            if (movementAudio != null && movementAudio.isPlaying) movementAudio.Stop();
        }

        public void RestoreOriginalState()
        {
            SilenceAudio();
            NormalizedSpeed = 0;
            if (!captured) return;
            if (train != null) train.localPosition = originalTrainPosition;
            for (int i = 0; i < trainRenderers.Length; i++)
                if (trainRenderers[i] != null) trainRenderers[i].enabled = trainVisibility[i];
            for (int i = 0; i < gates.Length; i++)
                if (gates[i].hinge != null) gates[i].hinge.localRotation = originalGateRotations[i];
            foreach (var slot in slots)
                if (slot.renderer != null) slot.renderer.SetPropertyBlock(slot.original.isEmpty ? null : slot.original, slot.index);
            foreach (var slot in trainSlots)
                if (slot.renderer != null) slot.renderer.SetPropertyBlock(slot.original.isEmpty ? null : slot.original, slot.index);
            slots.Clear();
            trainSlots.Clear();
            captured = false;
        }

        void OnEnable()
        {
            owningDirector = GetComponent<PlayableDirector>();
            if (owningDirector != null) owningDirector.stopped += OnDirectorStopped;
        }

        void OnDirectorStopped(PlayableDirector director) => RestoreOriginalState();

        void OnDisable()
        {
            if (owningDirector != null) owningDirector.stopped -= OnDirectorStopped;
            RestoreOriginalState();
        }
        void OnDestroy() => RestoreOriginalState();

        void OnDrawGizmosSelected()
        {
            if (railwayFrame == null) return;
            Gizmos.color = Color.cyan;
            var a = railwayFrame.TransformPoint(entryPosition);
            var b = railwayFrame.TransformPoint(stationPosition);
            var c = railwayFrame.TransformPoint(exitPosition);
            Gizmos.DrawLine(a, c);
            Gizmos.DrawWireSphere(a, .12f);
            Gizmos.DrawWireSphere(b, .12f);
            Gizmos.DrawWireSphere(c, .12f);
        }
    }
}
