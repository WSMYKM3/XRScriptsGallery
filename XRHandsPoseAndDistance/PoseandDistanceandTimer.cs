//Pose and Distance and Timer, must finish pose in timer's time can invoke
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace UnityEngine.XR.Hands.Samples.GestureSample
{
    /// <summary>
    /// A gesture that detects when a hand is held in a static shape and orientation for a minimum amount of time.
    /// </summary>
    public class WSMStaticHandGesture : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The hand tracking events component to subscribe to receive updated joint data to be used for gesture detection.")]
        XRHandTrackingEvents m_HandTrackingEvents;

        [SerializeField]
        [Tooltip("The hand shape or pose that must be detected for the gesture to be performed.")]
        ScriptableObject m_HandShapeOrPose;

        [SerializeField]
        [Tooltip("The target Transform to user for target conditions in the hand shape or pose.")]
        Transform m_TargetTransform;

        [SerializeField]
        [Tooltip("The image component that draws the background for gesture icons.")]
        Image m_Background;

        [SerializeField]
        [Tooltip("The event fired when the gesture is performed.")]
        UnityEvent m_GesturePerformed;

        [SerializeField]
        [Tooltip("The event fired when the gesture is ended.")]
        UnityEvent m_GestureEnded;

        [SerializeField]
        [Tooltip("The minimum amount of time the hand must be held in the required shape and orientation for the gesture to be performed.")]
        float m_MinimumHoldTime = 0.2f;

        [SerializeField]
        [Tooltip("The interval at which the gesture detection is performed.")]
        float m_GestureDetectionInterval = 0.1f;

        [SerializeField]
        [Tooltip("The static gestures associated with this gestures handedness.")]
        StaticHandGesture[] m_StaticGestures;

        [SerializeField]
        [Tooltip("The minimum distance the hand needs to move for gesture detection (in meters).")]
        float m_MinimumMovementDistance = 0.1f;

        [SerializeField]
        [Tooltip("Maximum time allowed to complete the movement (in seconds)")]
        float m_MaxMovementTime = 1.0f;

        XRHandShape m_HandShape;
        XRHandPose m_HandPose;
        bool m_WasDetected;
        bool m_PerformedTriggered;
        float m_TimeOfLastConditionCheck;
        float m_HoldStartTime;
        Color m_BackgroundDefaultColor;
        Color m_BackgroundHiglightColor = new Color(0f, 0.627451f, 1f);

        // Variables for tracking hand movement
        private Vector3 m_InitialHandPosition;
        private bool m_IsTrackingMovement;
        private float m_CurrentMovementDistance;
        private float m_MovementStartTime;

        /// <summary>
        /// The hand tracking events component to subscribe to receive updated joint data to be used for gesture detection.
        /// </summary>
        public XRHandTrackingEvents handTrackingEvents
        {
            get => m_HandTrackingEvents;
            set => m_HandTrackingEvents = value;
        }

        /// <summary>
        /// The hand shape or pose that must be detected for the gesture to be performed.
        /// </summary>
        public ScriptableObject handShapeOrPose
        {
            get => m_HandShapeOrPose;
            set => m_HandShapeOrPose = value;
        }

        /// <summary>
        /// The target Transform to user for target conditions in the hand shape or pose.
        /// </summary>
        public Transform targetTransform
        {
            get => m_TargetTransform;
            set => m_TargetTransform = value;
        }

        /// <summary>
        /// The image component that draws the background for gesture icons.
        /// </summary>
        public Image background
        {
            get => m_Background;
            set => m_Background = value;
        }

        /// <summary>
        /// The event fired when the gesture is performed.
        /// </summary>
        public UnityEvent gesturePerformed
        {
            get => m_GesturePerformed;
            set => m_GesturePerformed = value;
        }

        /// <summary>
        /// The event fired when the gesture is ended.
        /// </summary>
        public UnityEvent gestureEnded
        {
            get => m_GestureEnded;
            set => m_GestureEnded = value;
        }

        /// <summary>
        /// The minimum amount of time the hand must be held in the required shape and orientation for the gesture to be performed.
        /// </summary>
        public float minimumHoldTime
        {
            get => m_MinimumHoldTime;
            set => m_MinimumHoldTime = value;
        }

        /// <summary>
        /// The interval at which the gesture detection is performed.
        /// </summary>
        public float gestureDetectionInterval
        {
            get => m_GestureDetectionInterval;
            set => m_GestureDetectionInterval = value;
        }

        /// <summary>
        /// The minimum distance the hand needs to move for gesture detection.
        /// </summary>
        public float minimumMovementDistance
        {
            get => m_MinimumMovementDistance;
            set => m_MinimumMovementDistance = value;
        }

        /// <summary>
        /// Maximum time allowed to complete the movement.
        /// </summary>
        public float maxMovementTime
        {
            get => m_MaxMovementTime;
            set => m_MaxMovementTime = value;
        }

        void Awake()
        {
            m_BackgroundDefaultColor = m_Background.color;
        }

        void OnEnable()
        {
            m_HandTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);

            m_HandShape = m_HandShapeOrPose as XRHandShape;
            m_HandPose = m_HandShapeOrPose as XRHandPose;
            if (m_HandPose != null && m_HandPose.relativeOrientation != null)
                m_HandPose.relativeOrientation.targetTransform = m_TargetTransform;
        }

        void OnDisable() => m_HandTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);

        void OnJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
        {
            if (!isActiveAndEnabled || Time.timeSinceLevelLoad < m_TimeOfLastConditionCheck + m_GestureDetectionInterval)
                return;

            var detected =
                m_HandTrackingEvents.handIsTracked &&
                m_HandShape != null && m_HandShape.CheckConditions(eventArgs) ||
                m_HandPose != null && m_HandPose.CheckConditions(eventArgs);

            // Get the current hand position using wrist joint
            XRHandJoint wristJoint = eventArgs.hand.GetJoint(XRHandJointID.Wrist);
            Vector3 currentHandPosition = wristJoint.TryGetPose(out Pose wristPose) ? wristPose.position : Vector3.zero;

            if (!m_WasDetected && detected)
            {
                m_HoldStartTime = Time.timeSinceLevelLoad;
                m_InitialHandPosition = currentHandPosition;
                m_IsTrackingMovement = true;
                m_MovementStartTime = Time.time;
                Debug.Log($"Started tracking movement from position: {m_InitialHandPosition}");
            }
            else if (m_WasDetected && !detected)
            {
                m_PerformedTriggered = false;
                m_GestureEnded?.Invoke();
                m_Background.color = m_BackgroundDefaultColor;
                m_IsTrackingMovement = false;
                Debug.Log("Gesture detection ended");
            }

            m_WasDetected = detected;

            // Calculate movement distance if tracking
            if (m_IsTrackingMovement)
            {
                float elapsedTime = Time.time - m_MovementStartTime;
                m_CurrentMovementDistance = Vector3.Distance(m_InitialHandPosition, currentHandPosition);
                Debug.Log($"Current movement distance: {m_CurrentMovementDistance:F3}m, Time: {elapsedTime:F2}s");

                // Reset tracking if movement takes too long
                if (elapsedTime > m_MaxMovementTime && m_CurrentMovementDistance < m_MinimumMovementDistance)
                {
                    Debug.Log($"Movement time exceeded {m_MaxMovementTime} seconds, resetting tracking");
                    m_InitialHandPosition = currentHandPosition;
                    m_MovementStartTime = Time.time;
                    m_CurrentMovementDistance = 0f;
                }
            }

            if (!m_PerformedTriggered && detected)
            {
                var holdTimer = Time.timeSinceLevelLoad - m_HoldStartTime;
                float elapsedTime = Time.time - m_MovementStartTime;
                if (holdTimer > m_MinimumHoldTime && 
                    m_CurrentMovementDistance >= m_MinimumMovementDistance && 
                    elapsedTime <= m_MaxMovementTime)
                {
                    Debug.Log($"Gesture performed! Movement distance: {m_CurrentMovementDistance:F3}m in {elapsedTime:F2}s");
                    m_GesturePerformed?.Invoke();
                    m_PerformedTriggered = true;
                    m_Background.color = m_BackgroundHiglightColor;
                }
            }

            m_TimeOfLastConditionCheck = Time.timeSinceLevelLoad;
        }
    }
}
