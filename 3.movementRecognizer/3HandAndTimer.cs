using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.Events;
using PDollarGestureRecognizer;
using System.IO;

public class MovementRecognizer2 : MonoBehaviour
{
    public enum Handedness { Left, Right }
    public Handedness handToTrack = Handedness.Right;

    public GameObject debugCubePrefab;

    public bool creationMode = false; // 目前未启用
    public string newGestureName;

    public float recognitionThreshold = 0.85f;//compare to score

    [Header("Gesture Detection Settings")]
    public float gestureCheckInterval = 2f;       // 每隔多久检测一次
    public float recognitionCooldown = 3f;        // 成功识别后的冷却时间
    public float newPositionThresholdDistance = 0.05f;

    [System.Serializable]
    public class UnityStringEvent : UnityEvent<string> { }
    public UnityStringEvent OnRecognized;

    private XRHandSubsystem handSubsystem;
    private List<Vector3> positionsList = new List<Vector3>();
    private List<Gesture> trainingSet = new List<Gesture>();

    private float gestureCheckTimer = 0f;
    private bool cooldown = false;
    private float cooldownTimer = 0f;

    void Start()
    {
        // 加载预设手势
        TextAsset[] gesturesXml = Resources.LoadAll<TextAsset>("GestureSet/");
        foreach (TextAsset gestureXml in gesturesXml)
            trainingSet.Add(GestureIO.ReadGestureFromXML(gestureXml.text));

        string[] gestureFiles = Directory.GetFiles(Application.persistentDataPath, "*.xml");
        foreach (var file in gestureFiles)
            trainingSet.Add(GestureIO.ReadGestureFromFile(file));

        // 获取 XR Hands 子系统
        List<XRHandSubsystem> subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
            handSubsystem = subsystems[0];
    }

    void Update()
    {
        if (handSubsystem == null) return;

        XRHand hand = (handToTrack == Handedness.Left) ? handSubsystem.leftHand : handSubsystem.rightHand;
        if (!hand.isTracked) return;

        XRHandJoint joint = hand.GetJoint(XRHandJointID.IndexTip);
        if (!joint.TryGetPose(out Pose pose)) return;

        Vector3 currentPosition = pose.position;

        UpdateMovement(currentPosition);

        // Cooldown 控制
        if (cooldown)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= recognitionCooldown)
            {
                cooldown = false;
                cooldownTimer = 0f;
            }
        }

        // 计时器触发手势识别
        gestureCheckTimer += Time.deltaTime;
        if (!cooldown && gestureCheckTimer >= gestureCheckInterval)
        {
            gestureCheckTimer = 0f;
            TryRecognizeGesture();
        }
    }


    void UpdateMovement(Vector3 currentPosition)
    {
        if (positionsList.Count == 0 || Vector3.Distance(currentPosition, positionsList[positionsList.Count - 1]) > newPositionThresholdDistance)
        {
            positionsList.Add(currentPosition);

            if (debugCubePrefab)
                Destroy(Instantiate(debugCubePrefab, currentPosition, Quaternion.identity), 3);
        }
    }

    void TryRecognizeGesture()
    {
        if (positionsList.Count < 5) return;

        Point[] pointArray = new Point[positionsList.Count];
        for (int i = 0; i < positionsList.Count; i++)
        {
            Vector2 screenPoint = Camera.main.WorldToScreenPoint(positionsList[i]);
            pointArray[i] = new Point(screenPoint.x, screenPoint.y, 0);
        }

        Gesture newGesture = new Gesture(pointArray);
        Result result = PointCloudRecognizer.Classify(newGesture, trainingSet.ToArray());
        Debug.Log($"Gesture result: {result.GestureClass} - {result.Score}");

        if (result.Score > recognitionThreshold)
        {
            OnRecognized.Invoke(result.GestureClass);
            cooldown = true;
            positionsList.Clear(); // 清除当前轨迹准备下次
        }
    }
}
