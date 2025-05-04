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

        public Quaternion PointerRotation => rotationInputAction.ReadValue<Quaternion>();
        public Vector3 PointerPosition => positionInputAction.ReadValue<Vector3>();
        
        [SerializeField]
        private InputAction positionInputAction = 
            new InputAction(binding:"<MagicLeapController>/pointer/position", expectedControlType: "Vector3");

        [SerializeField]
        private InputAction rotationInputAction = new InputAction(binding: "<MagicLeapController>/pointer/rotation",
            expectedControlType: "Quaternion");

#if UNITY_EDITOR
        private Mouse _activeMouse;
#endif
        
 
        void Start()
        {
            
            positionInputAction.Enable();

            rotationInputAction.Enable();
            
            
            
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