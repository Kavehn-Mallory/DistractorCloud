using System;
using UnityEngine;

namespace DistractorClouds.DistractorTask
{
    public class RecenterPathComponent : MonoBehaviour
    {

        [SerializeField]
        private Transform rigCamera;

        private void OnEnable()
        {
            InputHandler.Instance.OnRecenter += RecenterPath;
        }

        private void OnDisable()
        {
            InputHandler.Instance.OnRecenter -= RecenterPath;
        }


        public void RecenterPath()
        {
            transform.SetPositionAndRotation(rigCamera.position, rigCamera.rotation);
        }
    }
}