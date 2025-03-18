//this script is to set in UnityEvent triggered event PlaySteps[index] in inspector 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class PlaySteps : MonoBehaviour
{
    PlayableDirector director;
    public List<Step> steps;

    void Start()
    {
        director = GetComponent<PlayableDirector>();
    }

    [System.Serializable]
    public class Step
    {
        public string name;
        public float time;
        public bool hasPlayed = false;//false as default, only UnityEvent or UnityEvent<GameObject> is invoked, then switch to true as line 30
    }

    public void PlayStepIndex(int index)
    {
        Step step = steps[index];

        if(!step.hasPlayed)
        {
            step.hasPlayed = true;

            director.Stop();
            director.time = step.time;
            director.Play();
        }
    }

}
