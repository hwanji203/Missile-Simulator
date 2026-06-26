using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MVP.System.GenerateUI
{
    [DefaultExecutionOrder(0)]
    [RequireComponent(typeof(Camera))]
    public class WorldUICamera : MonoBehaviour
    {
        private Camera _mainCam;
        private Camera _worldCam;

        private void Awake()
        {
            _worldCam = GetComponent<Camera>();
            _mainCam = Camera.main;

            _worldCam.orthographic = _mainCam.orthographic;
            if (_mainCam.orthographic)
            {
                _worldCam.orthographicSize = _mainCam.orthographicSize;
            }
            else
            {
                _worldCam.aspect = _mainCam.aspect;
                _worldCam.fieldOfView = _mainCam.fieldOfView;
            }
            _worldCam.nearClipPlane = _mainCam.nearClipPlane;
            _worldCam.farClipPlane = _mainCam.farClipPlane;

            var cameraData = _mainCam.GetComponent<UniversalAdditionalCameraData>();
            cameraData.cameraStack.Add(_worldCam);
        }
    }
}
