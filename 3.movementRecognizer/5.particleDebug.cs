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
    public float movementThreshold = 0.02f;        // How much movement is considered "moving"
    public float stillnessDuration = 0.5f;         // How long hand needs to be still to trigger recognition
    public float recognitionCooldown = 3f;         // 成功识别后的冷却时间
    public float newPositionThresholdDistance = 0.05f;

    [Header("Visualization Settings")]
    public float visualEffectDuration = 2f;  // How long the visual effect should last
    public bool useParticleSystem = false;   // Toggle if using particle system
    private List<GameObject> activeVisualEffects = new List<GameObject>();

    [System.Serializable]
    public class UnityStringEvent : UnityEvent<string> { }
    public UnityStringEvent OnRecognized;

    private XRHandSubsystem handSubsystem;
    private List<Vector3> positionsList = new List<Vector3>();
    private List<Gesture> trainingSet = new List<Gesture>();

    private bool isMoving = false;
    private float stillnessTimer = 0f;
    private Vector3 lastPosition;
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

        // Handle cooldown
        if (cooldown)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= recognitionCooldown)
            {
                cooldown = false;
                cooldownTimer = 0f;
            }
            return;
        }

        // Check if hand is moving
        float movement = Vector3.Distance(currentPosition, lastPosition);
        bool currentlyMoving = movement > movementThreshold;

        if (currentlyMoving)
        {
            isMoving = true;
            stillnessTimer = 0f;
            UpdateMovement(currentPosition);
        }
        else if (isMoving)
        {
            // Hand was moving but has stopped
            stillnessTimer += Time.deltaTime;
            if (stillnessTimer >= stillnessDuration)
            {
                // Hand has been still long enough, try to recognize the gesture
                TryRecognizeGesture();
                isMoving = false;
            }
        }

        lastPosition = currentPosition;
    }

    void UpdateMovement(Vector3 currentPosition)
    {
        if (positionsList.Count == 0 || Vector3.Distance(currentPosition, positionsList[positionsList.Count - 1]) > newPositionThresholdDistance)
        {
            positionsList.Add(currentPosition);

            if (debugCubePrefab)
            {
                GameObject visualEffect = Instantiate(debugCubePrefab, currentPosition, Quaternion.identity);
                
                if (useParticleSystem)
                {
                    ParticleSystem particles = visualEffect.GetComponent<ParticleSystem>();
                    if (particles != null)
                    {
                        var main = particles.main;
                        main.stopAction = ParticleSystemStopAction.Destroy;
                        particles.Play();
                    }
                }
                else
                {
                    // If it's a regular GameObject (like a cube), destroy it after duration
                    Destroy(visualEffect, visualEffectDuration);
                }

                activeVisualEffects.Add(visualEffect);
            }
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
            cooldownTimer = 0f;
        }
        
        // Clear visual effects after recognition attempt
        foreach (var effect in activeVisualEffects)
        {
            if (effect != null)
            {
                if (useParticleSystem)
                {
                    ParticleSystem particles = effect.GetComponent<ParticleSystem>();
                    if (particles != null)
                    {
                        particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                        Destroy(effect, visualEffectDuration);
                    }
                }
                else
                {
                    Destroy(effect);
                }
            }
        }
        activeVisualEffects.Clear();
        positionsList.Clear();
    }

    void OnDisable()
    {
        // Clean up any remaining effects when the script is disabled
        foreach (var effect in activeVisualEffects)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }
        activeVisualEffects.Clear();
    }
}
