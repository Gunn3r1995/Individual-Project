using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class CameraController : MonoBehaviour
    {
        public GameObject Player;

        private Vector3 offset;

        // Use this for initialization
        void Start()
        {
            if (Player == null)
            {
                Player = GameObject.FindWithTag("Player");
            }
            offset = transform.position - Player.transform.position;
        }

        // Update is called once per frame
        void LateUpdate()
        {
            transform.position = Player.transform.position + offset;
        }
    }
}
