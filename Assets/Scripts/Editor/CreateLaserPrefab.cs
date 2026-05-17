using UnityEditor;
using UnityEngine;

public class CreateLaserPrefab
{
    [MenuItem("Tools/Create Laser Tripwire Prefab")]
    static void Create()
    {
        string dir = "Assets/Prefabs";
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        GameObject root = new GameObject("LaserTripwire");

        GameObject receiver = new GameObject("Receiver");
        receiver.transform.SetParent(root.transform);
        receiver.transform.localPosition = new Vector3(0, 1, 10);
        receiver.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

        MeshFilter mf = receiver.AddComponent<MeshFilter>();
        mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        MeshRenderer mr = receiver.AddComponent<MeshRenderer>();
        Material rxMat = new Material(Shader.Find("Standard"));
        rxMat.color = new Color(1f, 0.2f, 0.05f);
        rxMat.EnableKeyword("_EMISSION");
        rxMat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.1f) * 2f);
        AssetDatabase.CreateAsset(rxMat, $"{dir}/LaserReceiver.mat");
        mr.sharedMaterial = rxMat;

        Material beamMat = new Material(Shader.Find("Sprites/Default"));
        beamMat.color = Color.white;
        AssetDatabase.CreateAsset(beamMat, $"{dir}/LaserBeam.mat");

        Material glowMat = new Material(Shader.Find("Sprites/Default"));
        glowMat.color = Color.white;
        AssetDatabase.CreateAsset(glowMat, $"{dir}/LaserGlow.mat");

        LaserTripwire laser = root.AddComponent<LaserTripwire>();
        laser.emitter = receiver.transform;
        laser.beamMaterial = beamMat;
        laser.glowMaterial = glowMat;
        laser.beamColor = new Color(1f, 0.15f, 0.05f);
        laser.glowColor = new Color(1f, 0.3f, 0.1f);
        laser.beamWidth = 0.06f;
        laser.glowWidth = 0.3f;
        laser.catchTime = 0.5f;
        laser.maxLength = 30f;
        laser.obstructionMask = ~0;
        laser.pulseSpeed = 1.5f;
        laser.pulseWidth = 2f;
        laser.flickerAmount = 0.08f;
        laser.sparkInterval = 0.15f;

        laser.sweepEnabled = false;
        laser.sweepAxis = LaserTripwire.SweepAxis.Y;
        laser.sweepPattern = LaserTripwire.SweepPattern.Sine;
        laser.sweepAngles = new Vector3(0, 90, 0);
        laser.sweepSpeed = 30f;

        laser.moveEnabled = false;
        laser.moveSpeed = 3f;
        laser.movePingPong = true;
        laser.movePause = 0f;

        AudioSource hum = root.AddComponent<AudioSource>();
        hum.playOnAwake = false;
        hum.loop = true;
        hum.volume = 0.3f;
        laser.humSource = hum;

        AudioSource sfx = root.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        laser.sfxSource = sfx;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{dir}/LaserTripwire.prefab");
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("Laser tripwire prefab created at Assets/Prefabs/LaserTripwire.prefab");
    }
}
