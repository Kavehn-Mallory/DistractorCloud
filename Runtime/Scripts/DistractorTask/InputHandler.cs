using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DistractorClouds.DistractorTask
{
    public class InputHandler : MonoBehaviour
    {
        private MagicLeapInputs _mlInputs;
        private MagicLeapInputs.ControllerActions _controllerActions;
        
        public Action OnBumperDown = delegate { };


#if UNITY_EDITOR
        private Mouse _activeMouse;
#endif
        
 
        void Start()
        {
            _mlInputs = new MagicLeapInputs();
            _mlInputs.Enable();
            _controllerActions = new MagicLeapInputs.ControllerActions(_mlInputs);
            _controllerActions.Bumper.performed += HandleOnBumper;
            
#if UNITY_EDITOR
            _activeMouse = Mouse.current;
#endif
        }


#if UNITY_EDITOR

        private void Update()
        {
            if (_activeMouse.rightButton.wasPressedThisFrame)
            {
                OnBumperDown.Invoke();
            }
        }
#endif

        private void HandleOnBumper(InputAction.CallbackContext obj)
        {
            bool bumperDown = obj.ReadValueAsButton();
            Debug.Log("The Bumper is pressed down " + bumperDown);
            if (bumperDown)
            {
                OnBumperDown.Invoke();
            }
        }
 
        void OnDestroy()
        {         
            _mlInputs.Dispose();
        }
    }
}