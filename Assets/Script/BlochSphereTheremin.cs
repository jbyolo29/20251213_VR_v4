using UnityEngine;
using TMPro;
using Oculus.VR;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(MeshRenderer))]
public class BlochSphereThereminDayNight : MonoBehaviour
{
    [Header("Hand Tracking")]
    public OVRSkeleton leftHandSkeleton;
    public OVRSkeleton rightHandSkeleton;

    [Header("Sound Mapping (inside sphere)")]
    [Tooltip("節拍速度範圍（BPM，每分鐘節拍數）")]
    public float bpmMin = 60f;     // 左側（慢節奏，60 BPM）
    public float bpmMax = 180f;    // 右側（快節奏，180 BPM）
    public float lerpSpeed = 10f;
    [Tooltip("節拍器音效（若未設置，將生成預設音效）")]
    public AudioClip metronomeClip;

    [Header("Material Alpha Mapping (use Y)")]
    [Tooltip("布洛赫球的材質（需要有 _BaseAlpha 屬性）")]
    public Material sphereMaterial;

    [Tooltip("最小透明度（Y=-1，0%）")]
    public float minAlpha = 0f;    // 底部完全透明
    [Tooltip("最大透明度（Y=+1，80%）")]
    public float maxAlpha = 0.8f;  // 頂部部分透明
    [Tooltip("無互動時的預設透明度（50%）")]
    public float defaultAlpha = 0.5f; // 無互動時 50% 透明

    [Header("UI (optional)")]
    public TextMeshProUGUI overlayText;  // 顯示 BPM/Alpha

    // internals
    private SphereCollider sphereCol;
    private AudioSource audioSource;
    private MeshRenderer meshRenderer;
    private float curBPM, tgtBPM;
    private float curAlpha, tgtAlpha; // 新增 curAlpha 確保平滑過渡
    private float nextTickTime;

    void Awake()
    {
        sphereCol = GetComponent<SphereCollider>();
        audioSource = GetComponent<AudioSource>();
        meshRenderer = GetComponent<MeshRenderer>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        if (metronomeClip == null)
        {
            metronomeClip = CreateDefaultMetronomeClip();
        }

        // 初始化透明度為 defaultAlpha
        if (sphereMaterial && sphereMaterial.HasProperty("_BaseAlpha"))
        {
            sphereMaterial.SetFloat("_BaseAlpha", defaultAlpha);
        }
        curAlpha = defaultAlpha;
    }

    void Update()
    {
        // 取得左右手的食指指尖位置
        Vector3 leftTipPos = Vector3.zero;
        bool leftValid = false;
        if (leftHandSkeleton != null && leftHandSkeleton.Bones.Count > 0 && leftHandSkeleton.IsDataValid)
        {
            leftTipPos = leftHandSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_IndexTip].Transform.position;
            leftValid = true;
        }

        Vector3 rightTipPos = Vector3.zero;
        bool rightValid = false;
        if (rightHandSkeleton != null && rightHandSkeleton.Bones.Count > 0 && rightHandSkeleton.IsDataValid)
        {
            rightTipPos = rightHandSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_IndexTip].Transform.position;
            rightValid = true;
        }

        // 找在球內的手指指尖（取更靠近中心的那個）
        Vector3 lLocal = Vector3.zero;
        float lDist = float.MaxValue;
        bool inL = leftValid && IsInside(leftTipPos, out lLocal, out lDist);

        Vector3 rLocal = Vector3.zero;
        float rDist = float.MaxValue;
        bool inR = rightValid && IsInside(rightTipPos, out rLocal, out rDist);

        Vector3 useLocal = Vector3.zero;
        bool inside = false;

        if (inL && inR) { inside = true; useLocal = (lDist < rDist) ? lLocal : rLocal; }
        else if (inL) { inside = true; useLocal = lLocal; }
        else if (inR) { inside = true; useLocal = rLocal; }

        // 映射：X→節拍速度（BPM）、Y→透明度
        float alphaParam = 0.5f;
        if (inside)
        {
            Vector3 cL = sphereCol.center;
            float r = sphereCol.radius;
            Vector3 v = useLocal - cL;

            float xN = Mathf.Clamp(v.x / r, -1f, 1f); // -1(左/慢)->+1(右/快)
            float yN = Mathf.Clamp(v.y / r, -1f, 1f); // -1(底/透明)->+1(頂/不透明)

            float tX = (xN + 1f) * 0.5f;
            float tY = (yN + 1f) * 0.5f;
            tgtBPM = Mathf.Lerp(bpmMin, bpmMax, tX);
            alphaParam = tY;
            tgtAlpha = Mathf.Lerp(minAlpha, maxAlpha, tY);
        }
        else
        {
            tgtBPM = 0f; // 停止節拍
            tgtAlpha = defaultAlpha; // 無互動時設為 50%
        }

        // 平滑過渡
        curBPM = Mathf.Lerp(curBPM, tgtBPM, Time.deltaTime * lerpSpeed);
        curAlpha = Mathf.Lerp(curAlpha, tgtAlpha, Time.deltaTime * lerpSpeed);

        // 節拍器播放
        if (curBPM > 0f && Time.time >= nextTickTime)
        {
            audioSource.PlayOneShot(metronomeClip, 1.0f); // 固定音量
            nextTickTime = Time.time + (60f / curBPM);
        }

        // 更新材質透明度
        UpdateAlpha(curAlpha);

        // UI
        if (overlayText)
        {
            overlayText.text = inside
                ? $"BPM: {curBPM:0.0}   Alpha: {curAlpha:0.00}"
                : $"(outside) BPM: {curBPM:0.0}   Alpha: {curAlpha:0.00}";
        }
    }

    bool IsInside(Vector3 tipPos, out Vector3 localPos, out float distFromCenter)
    {
        localPos = Vector3.zero;
        distFromCenter = float.MaxValue;

        Vector3 pL = transform.InverseTransformPoint(tipPos);
        localPos = pL;
        Vector3 cL = sphereCol.center;
        float r = sphereCol.radius;
        distFromCenter = (pL - cL).magnitude;
        return distFromCenter <= r + 1e-6f;
    }

    void UpdateAlpha(float alpha)
    {
        if (sphereMaterial && sphereMaterial.HasProperty("_BaseAlpha"))
        {
            sphereMaterial.SetFloat("_BaseAlpha", alpha);
        }
    }

    AudioClip CreateDefaultMetronomeClip()
    {
        int sampleRate = 44100;
        int length = (int)(sampleRate * 0.1f); // 0.1 秒音效
        float[] samples = new float[length];
        float frequency = 1000f; // 1000 Hz 「滴」聲
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)sampleRate;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * (1f - t / 0.1f); // 衰減包絡
        }
        AudioClip clip = AudioClip.Create("DefaultMetronomeTick", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}