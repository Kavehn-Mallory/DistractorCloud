using System;
using DistractorClouds.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace DistractorClouds.DistractorTask
{
    public class InputHandler : PersistentSingleton<InputHandler>
    {
        private CustomMagicLeapOpenXRInput _customMagicLeapInputs;
        private CustomMagicLeapOpenXRInput.ControllerActions _controllerActions;
        private CustomMagicLeapOpenXRInput.EditorActions _editorActions;
        
        public Action OnBumperDown = delegate { };

        public Quaternion PointerRotation => ReadPointerRotation();



        public Vector3 PointerPosition => ReadPointerPosition();


        public Ray PointerAsRay => ReadPointerPositionAsRay();

        public Vector3 GetSearchAreaPosition(float distanceFromController)
        {
#if UNITY_EDITOR
            return PointerAsRay.GetPoint(distanceFromController);
#endif
            var forward = PointerRotation * Vector3.forward;
            return PointerPosition + forward * distanceFromController;
        }

        public event Action OnRecenter = delegate { };

        
        
 
        void Awake()
        {
            _customMagicLeapInputs = new CustomMagicLeapOpenXRInput();
            _customMagicLeapInputs.Enable();

            //Initialize the ControllerActions using the Magic Leap Input
            _controllerActions = new CustomMagicLeapOpenXRInput.ControllerActions(_customMagicLeapInputs);

            //Subscribe to your choice of the controller events
            _controllerActions.Bumper.performed += HandleOnBumper;
            _controllerActions.TriggerHold.performed += OnTriggerHold;

            _editorActions = new CustomMagicLeapOpenXRInput.EditorActions(_customMagicLeapInputs);


        }
        
        private Quaternion ReadPointerRotation()
        {
#if UNITY_EDITOR
            if (Application.isPlaying && Camera.main)
                return Camera.main.transform.rotation;
#endif

            return _controllerActions.PointerRotation.ReadValue<Quaternion>();
        }
        
        private Vector3 ReadPointerPosition()
        {
            return _controllerActions.PointerPosition.ReadValue<Vector3>();
        }

        private Ray ReadPointerPositionAsRay()
        {
#if UNITY_EDITOR
            if (Application.isPlaying && Camera.main)
            {
                var position = _editorActions.MousePosition.ReadValue<Vector2>();
                return Camera.main.ScreenPointToRay(new Vector3(position.x, position.y, Camera.main.nearClipPlane));
            }

            return new Ray();
#endif
        }

        private void OnTriggerHold(InputAction.CallbackContext obj)
        {
            Debug.Log("Trigger was held down");
            OnRecenter.Invoke();
        }

        

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
            _customMagicLeapInputs?.Dispose();
        }
    }
}