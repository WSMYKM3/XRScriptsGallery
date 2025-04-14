//this script switch to xrhand, but no trigger button-like thing to start detecting  

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands; // 【新增】改为使用 XR Hands
using PDollarGestureRecognizer;
using System.IO;
using UnityEngine.Events;

public class MovementRecognizer : MonoBehaviour
{
    //【替换】XRNode 改为手部枚举
    public enum Handedness { Left, Right } //【新增】
    public Handedness handToTrack = Handedness.Right; //【新增】Inspector 可选左右手

    public float newPositionThresholdDistance = 0.05f;
    public GameObject debugCubePrefab;

    public bool creationMode = false;
    public string newGestureName;
    public float recognitionThreshold = 0.9f;
    public float recognitionDelay = 1.5f;
    private float timer = 0;

    [System.Serializable]
    public class UnityStringEvent : UnityEvent<string> { }
    public UnityStringEvent OnRecognized;

    private List<Gesture> trainingSet = new List<Gesture>();
    private bool isMoving = false;
    private List<Vector3> positionsList = new List<Vector3>();
    private int strokeID = 0;

    //【新增】XR Hands 子系统引用
    private XRHandSubsystem handSubsystem; //【新增】
    private bool hasPreviousPosition = false; //【可选，暂未用】

    void Start()
    {
        // 加载手势数据
        TextAsset[] gesturesXml = Resources.LoadAll<TextAsset>("GestureSet/");
        foreach (TextAsset gestureXml in gesturesXml)
            trainingSet.Add(GestureIO.ReadGestureFromXML(gestureXml.text));

        string[] gestureFiles = Directory.GetFiles(Application.persistentDataPath, "*.xml");
        foreach (var item in gestureFiles)
            trainingSet.Add(GestureIO.ReadGestureFromFile(item));

        //【新增】获取 XR Hands 子系统
        List<XRHandSubsystem> handSubsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetInstances(handSubsystems); //【新增】
        if (handSubsystems.Count > 0)
            handSubsystem = handSubsystems[0]; //【新增】
    }

    void Update()
    {
        //【替换】不再使用 InputHelpers 和 controller 检测
        if (handSubsystem == null) return;

        XRHand hand = (handToTrack == Handedness.Left) ? handSubsystem.leftHand : handSubsystem.rightHand; //【新增】

        if (!hand.isTracked) return;

        //【新增】从 XR Hands 中获取手指关节位置（index tip）
        if (!hand.TryGetJoint(XRHandJointID.IndexTip, out XRHandJoint joint)) return;
        Vector3 currentPosition = joint.Pose.position; //【新增】

        // 开始移动
        if (!isMoving)
        {
            strokeID = 0;
            StartMovement(currentPosition); //【修改】
        }
        // 正在移动
        else
        {
            if (timer > 0)
                strokeID++;

            timer = 0;
            UpdateMovement(currentPosition); //【修改】
        }

        //【新增】检测静止来触发结束
        if (isMoving && hand.TryGetJoint(XRHandJointID.IndexTip, out XRHandJoint updatedJoint))
        {
            Vector3 delta = updatedJoint.Pose.position - positionsList[positionsList.Count - 1];
            if (delta.magnitude < 0.005f) //【新增】几乎静止
            {
                timer += Time.deltaTime;
                if (timer > recognitionDelay)
                    EndMovement();
            }
        }
    }

    //【修改】将 movementSource.position 改为传入的位置
    void StartMovement(Vector3 startPosition)
    {
        Debug.Log("Start Movement");
        isMoving = true;
        positionsList.Clear();
        positionsList.Add(startPosition);

        if (debugCubePrefab)
            Destroy(Instantiate(debugCubePrefab, startPosition, Quaternion.identity), 3);
    }

    void EndMovement()
    {
        Debug.Log("End Movement");
        isMoving = false;

        Point[] pointArray = new Point[positionsList.Count];
        for (int i = 0; i < positionsList.Count; i++)
        {
            Vector2 screenPoint = Camera.main.WorldToScreenPoint(positionsList[i]);
            pointArray[i] = new Point(screenPoint.x, screenPoint.y, strokeID);
        }

        Gesture newGesture = new Gesture(pointArray);

        if (creationMode)
        {
            newGesture.Name = newGestureName;
            trainingSet.Add(newGesture);
            string fileName = Application.persistentDataPath + "/" + newGestureName + ".xml";
            GestureIO.WriteGesture(pointArray, newGestureName, fileName);
        }
        else
        {
            Result result = PointCloudRecognizer.Classify(newGesture, trainingSet.ToArray());
            Debug.Log(result.GestureClass + " : " + result.Score);
            if (result.Score > recognitionThreshold)
                OnRecognized.Invoke(result.GestureClass);
        }
    }

    //【修改】UpdateMovement 接收 currentPosition 参数
    void UpdateMovement(Vector3 currentPosition)
    {
        Debug.Log("Update Movement");

        Vector3 lastPosition = positionsList[positionsList.Count - 1];

        if (Vector3.Distance(currentPosition, lastPosition) > newPositionThresholdDistance)
        {
            positionsList.Add(currentPosition);

            if (debugCubePrefab)
                Destroy(Instantiate(debugCubePrefab, currentPosition, Quaternion.identity), 3);
        }
    }
}
