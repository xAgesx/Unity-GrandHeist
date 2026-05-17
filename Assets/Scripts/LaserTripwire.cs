using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserTripwire : MonoBehaviour
{
    [Header("Beam")]
    public float maxLength = 30f;
    public LayerMask obstructionMask = ~0;
    public float catchTime = 0.5f;
    public Transform emitter;

    [Header("Sweep")]
    public bool sweepEnabled;
    public enum SweepAxis { X, Y, Z, XY, XZ, YZ, All }
    public SweepAxis sweepAxis = SweepAxis.Y;
    public enum SweepPattern { Sine, Triangle, Sawtooth, Square, Random }
    public SweepPattern sweepPattern = SweepPattern.Sine;
    public Vector3 sweepAngles = new Vector3(0, 90, 0);
    public float sweepSpeed = 30f;
    public float sweepOffset;

    [Header("Movement")]
    public bool moveEnabled;
    public Transform[] moveWaypoints;
    public float moveSpeed = 3f;
    public bool movePingPong = true;
    public float movePause;

    [Header("Visuals")]
    public Color beamColor = new Color(1f, 0.15f, 0.05f);
    public Color glowColor = new Color(1f, 0.3f, 0.1f);
    public float beamWidth = 0.06f;
    public float glowWidth = 0.3f;
    public float pulseSpeed = 1.5f;
    public float pulseWidth = 2f;
    public float flickerAmount = 0.08f;
    public Material beamMaterial;
    public Material glowMaterial;

    [Header("Sparks")]
    public ParticleSystem sparkPrefab;
    public float sparkInterval = 0.15f;

    [Header("Audio")]
    public AudioSource humSource;
    public AudioSource sfxSource;
    public AudioClip alertSound;
    public AudioClip catchSound;
    public float humPitchNear = 1.3f;
    public float humPitchFar = 0.7f;

    Transform player;
    LineRenderer beamLR;
    LineRenderer glowLR;
    int rayMask;
    float touchTimer;
    float sparkTimer;
    float pulsePhase;
    float beatMultiplier = 1f;
    bool triggered;
    bool wasTouching;

    Vector3 beamOrigin;
    Vector3 beamEndPos;
    bool beamBlocked;
    RaycastHit beamHit;

    List<ParticleSystem> sparks = new List<ParticleSystem>();
    Gradient beamGrad = new Gradient();
    Gradient glowGrad = new Gradient();
    GradientColorKey[] beamCK = new GradientColorKey[5];
    GradientAlphaKey[] beamAK = new GradientAlphaKey[5];
    GradientColorKey[] glowCK = new GradientColorKey[5];
    GradientAlphaKey[] glowAK = new GradientAlphaKey[5];

    int moveIndex;
    int moveDir = 1;
    float movePauseTimer;
    Vector3 startPos;

    const int SEGMENTS = 48;

    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        startPos = transform.position;

        beamLR = MakeLR("Beam", beamWidth, beamMaterial);
        glowLR = MakeLR("Glow", glowWidth, glowMaterial);

        rayMask = obstructionMask;
        if (player != null)
            rayMask &= ~(1 << player.gameObject.layer);

        if (humSource != null)
        {
            humSource.loop = true;
            humSource.Play();
        }
    }

    LineRenderer MakeLR(string name, float width, Material mat)
    {
        GameObject go = new GameObject(name);
        Transform parent = emitter != null ? emitter : transform;
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = SEGMENTS;
        lr.startWidth = width;
        lr.endWidth = width;
        if (mat != null) lr.material = mat;
        return lr;
    }

    void Update()
    {
        if (triggered) return;

        UpdateSweep();
        UpdateMovement();
        CastBeam();
        UpdatePulse();
        UpdateFlicker();
        UpdateSparks();
        UpdateDetection();
        UpdateAudio();
    }

    void UpdateSweep()
    {
        if (!sweepEnabled) return;

        float t = Time.time * sweepSpeed + sweepOffset;
        float v = SweepValue(t);

        Vector3 euler = Vector3.zero;
        switch (sweepAxis)
        {
            case SweepAxis.X:   euler.x = v * sweepAngles.x; break;
            case SweepAxis.Y:   euler.y = v * sweepAngles.y; break;
            case SweepAxis.Z:   euler.z = v * sweepAngles.z; break;
            case SweepAxis.XY:  euler.x = v * sweepAngles.x; euler.y = v * sweepAngles.y; break;
            case SweepAxis.XZ:  euler.x = v * sweepAngles.x; euler.z = v * sweepAngles.z; break;
            case SweepAxis.YZ:  euler.y = v * sweepAngles.y; euler.z = v * sweepAngles.z; break;
            case SweepAxis.All: euler = v * sweepAngles; break;
        }

        Transform tr = emitter != null ? emitter : transform;
        tr.localRotation = Quaternion.Euler(euler);
    }

    float SweepValue(float t)
    {
        switch (sweepPattern)
        {
            case SweepPattern.Sine:
                return Mathf.Sin(t * Mathf.Deg2Rad);
            case SweepPattern.Triangle:
                return Mathf.PingPong(t / 90f, 2f) - 1f;
            case SweepPattern.Sawtooth:
                return ((t % 360f) / 180f) - 1f;
            case SweepPattern.Square:
                return Mathf.Sin(t * Mathf.Deg2Rad) > 0f ? 1f : -1f;
            case SweepPattern.Random:
                return Mathf.PerlinNoise(t * 0.05f, 0f) * 2f - 1f;
            default:
                return 0f;
        }
    }

    void UpdateMovement()
    {
        if (!moveEnabled || moveWaypoints == null || moveWaypoints.Length == 0) return;

        if (movePauseTimer > 0f)
        {
            movePauseTimer -= Time.deltaTime;
            return;
        }

        Transform target = moveWaypoints[moveIndex];
        if (target == null) return;

        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            if (movePingPong)
            {
                moveIndex += moveDir;
                if (moveIndex >= moveWaypoints.Length || moveIndex < 0)
                {
                    moveDir *= -1;
                    moveIndex += moveDir;
                }
            }
            else
            {
                moveIndex = (moveIndex + 1) % moveWaypoints.Length;
            }
            movePauseTimer = movePause;
        }
    }

    void CastBeam()
    {
        beamOrigin = emitter != null ? emitter.position : transform.position;
        Vector3 fwd = emitter != null ? emitter.forward : transform.forward;
        Vector3 target = beamOrigin + fwd * maxLength;

        beamBlocked = Physics.Linecast(beamOrigin, target, out beamHit, rayMask);
        beamEndPos = beamBlocked ? beamHit.point : target;
    }

    void UpdatePulse()
    {
        float dT = Time.deltaTime * pulseSpeed * beatMultiplier;
        pulsePhase += dT;

        float beamLen = Vector3.Distance(beamOrigin, beamEndPos);
        Color col = GetBeamColor();
        float pulse01 = (Mathf.Sin(pulsePhase) + 1f) * 0.5f;
        float pw = pulseWidth / Mathf.Max(beamLen, 0.01f);

        Color bright = col * 1.8f; bright.a = 1f;
        Color dim = col * 0.6f; dim.a = 0.3f;

        beamCK[0] = new GradientColorKey(col, 0f);
        beamCK[1] = new GradientColorKey(dim, Mathf.Max(0f, pulse01 - pw));
        beamCK[2] = new GradientColorKey(bright, pulse01);
        beamCK[3] = new GradientColorKey(dim, Mathf.Min(1f, pulse01 + pw));
        beamCK[4] = new GradientColorKey(col, 1f);
        beamAK[0] = new GradientAlphaKey(1f, 0f);
        beamAK[1] = new GradientAlphaKey(0.6f, Mathf.Max(0f, pulse01 - pw));
        beamAK[2] = new GradientAlphaKey(1f, pulse01);
        beamAK[3] = new GradientAlphaKey(0.6f, Mathf.Min(1f, pulse01 + pw));
        beamAK[4] = new GradientAlphaKey(1f, 1f);
        beamGrad.SetKeys(beamCK, beamAK);
        beamLR.colorGradient = beamGrad;

        Color gCol = glowColor * 0.4f;
        Color gBright = glowColor * 0.8f; gBright.a = 0.3f;
        Color gDim = glowColor * 0.2f; gDim.a = 0.05f;

        glowCK[0] = new GradientColorKey(gCol, 0f);
        glowCK[1] = new GradientColorKey(gDim, Mathf.Max(0f, pulse01 - pw));
        glowCK[2] = new GradientColorKey(gBright, pulse01);
        glowCK[3] = new GradientColorKey(gDim, Mathf.Min(1f, pulse01 + pw));
        glowCK[4] = new GradientColorKey(gCol, 1f);
        glowAK[0] = new GradientAlphaKey(0.4f, 0f);
        glowAK[1] = new GradientAlphaKey(0.15f, Mathf.Max(0f, pulse01 - pw));
        glowAK[2] = new GradientAlphaKey(0.5f, pulse01);
        glowAK[3] = new GradientAlphaKey(0.15f, Mathf.Min(1f, pulse01 + pw));
        glowAK[4] = new GradientAlphaKey(0.4f, 1f);
        glowGrad.SetKeys(glowCK, glowAK);
        glowLR.colorGradient = glowGrad;

        Vector3[] pts = new Vector3[SEGMENTS];
        for (int i = 0; i < SEGMENTS; i++)
            pts[i] = Vector3.Lerp(beamOrigin, beamEndPos, i / (SEGMENTS - 1f));
        beamLR.SetPositions(pts);
        glowLR.SetPositions(pts);
    }

    void UpdateFlicker()
    {
        float f = 1f + Random.Range(-flickerAmount, flickerAmount);
        beamLR.widthMultiplier = f;
        glowLR.widthMultiplier = 1f + (f - 1f) * 0.3f;
    }

    void UpdateSparks()
    {
        if (!beamBlocked || sparkPrefab == null) return;

        sparkTimer -= Time.deltaTime;
        if (sparkTimer > 0f) return;
        sparkTimer = sparkInterval;

        Vector3 pos = beamHit.point + Random.onUnitSphere * 0.1f;
        pos.y = beamHit.point.y;
        var spark = Instantiate(sparkPrefab, pos, Quaternion.LookRotation(beamHit.normal));
        sparks.Add(spark);
        var main = spark.main;
        main.startColor = beamColor;

        sparks.RemoveAll(s => s == null || !s.isPlaying);
    }

    void UpdateDetection()
    {
        if (player == null) return;

        float d = DistToSegment(player.position, beamOrigin, beamEndPos);
        bool touching = d < 0.5f;

        if (touching)
        {
            touchTimer += Time.deltaTime;
            if (touchTimer >= catchTime)
                CatchPlayer();
        }
        else
        {
            touchTimer = Mathf.Max(0f, touchTimer - Time.deltaTime * 2f);
        }

        if (touching && !wasTouching && sfxSource != null && alertSound != null)
            sfxSource.PlayOneShot(alertSound);

        float near = DistToSegment(player.position, beamOrigin, beamEndPos);
        beatMultiplier = near < 3f
            ? Mathf.Lerp(1f, 4f, 1f - Mathf.Clamp01(near / 3f))
            : 1f;

        wasTouching = touching;
    }

    void UpdateAudio()
    {
        if (humSource == null || player == null) return;

        float near = Mathf.Clamp01(1f - DistToSegment(player.position, beamOrigin, beamEndPos) / 8f);
        humSource.pitch = Mathf.Lerp(humPitchFar, humPitchNear, near);
        humSource.volume = Mathf.Lerp(0.2f, 0.7f, near);
    }

    void CatchPlayer()
    {
        triggered = true;
        StartCoroutine(FlashRoutine());

        if (sfxSource != null && catchSound != null)
            sfxSource.PlayOneShot(catchSound);

        if (GameManager.Instance != null)
        {
            // GameManager.Instance.timesCaught++;
            GameManager.Instance.TriggerGameOver();
        }
    }

    IEnumerator FlashRoutine()
    {
        float t = 0f;
        while (t < 0.3f)
        {
            float a = Mathf.PingPong(t * 20f, 1f);
            beamLR.startColor = Color.white * a;
            beamLR.endColor = Color.white * a;
            glowLR.startColor = Color.white * a * 0.5f;
            glowLR.endColor = Color.white * a * 0.5f;
            t += Time.deltaTime;
            yield return null;
        }
    }

    Color GetBeamColor() => Color.Lerp(beamColor, Color.white * 0.9f, touchTimer / catchTime);

    float DistToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        Vector3 ap = p - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.001f) return Vector3.Distance(p, a);
        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / lenSq);
        return Vector3.Distance(p, a + ab * t);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origin = emitter != null ? emitter.position : transform.position;
        Vector3 fwd = emitter != null ? emitter.forward : transform.forward;
        Gizmos.color = beamColor;
        Gizmos.DrawLine(origin, origin + fwd * maxLength);

        if (sweepEnabled)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawRay(origin, Quaternion.Euler(-sweepAngles * 0.5f) * fwd * maxLength);
            Gizmos.DrawRay(origin, Quaternion.Euler(sweepAngles * 0.5f) * fwd * maxLength);
        }

        if (moveEnabled && moveWaypoints != null && moveWaypoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            Vector3 prev = transform.position;
            foreach (var wp in moveWaypoints)
            {
                if (wp == null) continue;
                Gizmos.DrawSphere(wp.position, 0.2f);
                Gizmos.DrawLine(prev, wp.position);
                prev = wp.position;
            }
        }
    }
}
