using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class PlaySteps : MonoBehaviour{
    PlayableDirector director;
    public List<Step> steps;//list for every steps to play after a pause siganl

    void Start(){
        director = GetComponent<PlayableDirector>();
    }

    //the line below is very impoortant, because
    [System.Serializable]
    public class Step{
        //the attributes in inspector
        public string name;
        public float time;//the start time of next active state
        public bool hasPlayed = false;
    }

    public void PlayStep(int index){
        Step step = steps[index];
        //flipflop
        if(!step.hasPlayed){
            step.hasPlayed = true;

            director.Stop();
            director.time = step.time;//line 17
            director.Play();
        }

    }
}