using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Utility
{
    public class AudioQueue : MonoBehaviour
    {
        public List<AudioClip> ClipQueue = new List<AudioClip>();


        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void AddToQueue(AudioClip clip)
        {
            ClipQueue.Add(clip);
        }

        public void PlayNextClip()
        {
            ClipQueue.RemoveAt(0);
            if (ClipQueue.Count > 0)
            {
                GetComponent<AudioSource>().clip = ClipQueue[0];
                GetComponent<AudioSource>().Play();
            }
            else
            {
                GetComponent<AudioSource>().Stop();
            }
        }


    }
}

