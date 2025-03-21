//this script should work with another script with invoke() in line 136, in this script it is set to a string(line 32) to send to another script
//this script also has a delay function to detetct multiple strokes with a timer(line 73-75)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using PDollarGestureRecognizer;//namespace of PDollar function
using System.IO;
using UnityEngine.Events;

public class MovementRecognizer : MonoBehaviour
{
    public XRNode inputSource;//it is set in inspector with right hand now, but can be changed
    public UnityEngine.XR.Interaction.Toolkit.InputHelpers.Button inputButton;
    public float inputThreshold = 0.1f;
    public Transform movementSource;//add to positionsList list, need to set in inspector, right hand controller now

    public float newPositionThresholdDistance = 0.05f;//only exceed this threshhold, the new movementSource.position will be add to list
    public GameObject debugCubePrefab;//this is instantiated in StartMovement()

    //Creation Mode and set its name
    public bool creationMode = true;//check in inspector if it is set to trye first, if recognize mode, then uncheck it
    public string newGestureName;

    public float recognitionThreshold = 0.9f;
    
    //set delay for multiple strokes
    public float recognitionDelay = 1.5f;
    private float timer = 0;
    
    [System.Serializable]
    public class UnityStringEvent : UnityEvent<string> { }
    public UnityStringEvent OnRecognized;//this is thge grey event box in inspector, and here I set it trigger OnRecognized.Invoke() in recognize mode

    private List<Gesture> trainingSet = new List<Gesture>();//initialize
    private bool isMoving = false;
    private List<Vector3> positionsList = new List<Vector3>();//add to UpdateMovement(), and also use its postion to add to a Point list []
    private int strokeID = 0;

    // Start is called before the first frame update
    void Start()
    {
        //THIS 3 LIENS ARE NOT FROM THE VIDEO AND ARE ADDED TO ADD PREMADE GESTURES MADE DURING TUTORIAL
        //THAT ARE NOT IN YOUR FILES YET
        TextAsset[] gesturesXml = Resources.LoadAll<TextAsset>("GestureSet/");
        foreach (TextAsset gestureXml in gesturesXml)
            trainingSet.Add(GestureIO.ReadGestureFromXML(gestureXml.text));

        //find all files with .xml, this is for loading at start
        //foreach here is to add xml to trainningset
        string[] gestureFiles = Directory.GetFiles(Application.persistentDataPath, "*.xml");
        foreach (var item in gestureFiles)
        {
            trainingSet.Add(GestureIO.ReadGestureFromFile(item));
        }
    }

    // Update is called once per frame
    void Update()
    {
        UnityEngine.XR.Interaction.Toolkit.InputHelpers.IsPressed(InputDevices.GetDeviceAtXRNode(inputSource), inputButton, out bool isPressed, inputThreshold);//check if player trigger the inputbutton with threshhold

        //Start The Movement
        if(!isMoving && isPressed)
        {
            strokeID = 0;
            StartMovement();
        }
        //start the timer whrn release the trigger, if it exceed the delaytime, then end The Movement
        else if(isMoving && !isPressed)
        {
            timer += Time.deltaTime;
            if (timer > recognitionDelay)
                EndMovement();
        }
        //Updating The Movement
        else if(isMoving && isPressed)
        {
            if(timer > 0)
            {
                strokeID++;
            }

            timer = 0;
            UpdateMovement();
        }
    }

    //Start The Movement Function
    void StartMovement()
    {
        Debug.Log("Start Movement");
        isMoving = true;
        positionsList.Clear();
        positionsList.Add(movementSource.position);

        if(debugCubePrefab)
            Destroy(Instantiate(debugCubePrefab, movementSource.position, Quaternion.identity),3);//this line first instantiate and destory after 3 seconds
    }

    //End The Movement Function
    void EndMovement()
    {
        Debug.Log("End Movement");
        isMoving = false;

        //Create The Gesture FRom The Position List with the positionsList
        Point[] pointArray = new Point[positionsList.Count];

        for (int i = 0; i < positionsList.Count; i++)
        {
            //transform the points from 3d to 2d because algorithm only works in 2d with WorldToScreenPoint
            Vector2 screenPoint = Camera.main.WorldToScreenPoint(positionsList[i]);
            pointArray[i] = new Point(screenPoint.x, screenPoint.y, 0);
        }

        Gesture newGesture = new Gesture(pointArray);

        //Add A new gesture to training set
        if(creationMode)
        {
            newGesture.Name = newGestureName;
            trainingSet.Add(newGesture);
            
            //below two lines are for storing the new gesture/customized ones
            string fileName = Application.persistentDataPath + "/" + newGestureName + ".xml";
            GestureIO.WriteGesture(pointArray, newGestureName, fileName);
        }
        //recognize mode and this will give feedback according the input
        else
        {
            Result result = PointCloudRecognizer.Classify(newGesture, trainingSet.ToArray());
            Debug.Log(result.GestureClass + result.Score);
            if(result.Score > recognitionThreshold)
            {
                OnRecognized.Invoke(result.GestureClass);
            }
        }
    }

    //Update The Movement Function
    void UpdateMovement()
    {
        Debug.Log("Update Movement");
        Vector3 lastPosition = positionsList[positionsList.Count - 1];

        if(Vector3.Distance(movementSource.position,lastPosition) > newPositionThresholdDistance)
        {
            positionsList.Add(movementSource.position);
            if (debugCubePrefab)
                Destroy(Instantiate(debugCubePrefab, movementSource.position, Quaternion.identity), 3);
        }
    }
}
