using UnityEngine;
using UnityEngine.Rendering;

// ReSharper disable once CheckNamespace
namespace MobfishCardboard
{
    public class CardboardMainCamera : MonoBehaviour
    {
        // Only used in dontDestroyAndSingleton
        private static CardboardMainCamera _instance;

        [Header("Cameras")]
        [SerializeField]
        private Camera novrCam;

        [SerializeField]
        private Camera leftCam;

        [SerializeField]
        private Camera rightCam;

        [SerializeField]
        private GameObject vrCamGroup;

        [SerializeField]
        private GameObject novrCamGroup;

        [Header("Options")]
        [SerializeField]
        private bool defaultEnableVRView;

        [Tooltip(
            "Set this GameObject DontDestroyOnLoad and Singleton. If it's not needed or any parent GameObject already have DontDestroyOnLoad, disable it")]
        [SerializeField]
        private bool dontDestroyAndSingleton = true;

        private RenderTextureDescriptor _eyeRenderTextureDesc;
        private bool _overlayIsOpen;

        private void Awake()
        {
            Application.targetFrameRate = CardboardUtility.GetTargetFramerate();

            if (dontDestroyAndSingleton)
            {
                if (_instance == null)
                {
                    DontDestroyOnLoad(gameObject);

                    _instance = this;
                }
                else if (_instance != this)
                {
                    Destroy(gameObject);

                    return;
                }
            }

            SetupRenderTexture();

            CardboardManager.InitCardboard();
            CardboardManager.SetVRViewEnable(defaultEnableVRView);
        }

        private void Start()
        {
            RefreshCamera();
            CardboardManager.deviceParamsChangeEvent += RefreshCamera;

            SwitchVRCamera();
            CardboardManager.enableVRViewChangedEvent += SwitchVRCamera;
        }

        private void OnDestroy()
        {
            CardboardManager.deviceParamsChangeEvent -= RefreshCamera;
            CardboardManager.enableVRViewChangedEvent -= SwitchVRCamera;
        }

        private void SetupRenderTexture()
        {
            SetupEyeRenderTextureDescription();

            var newLeft = new RenderTexture(_eyeRenderTextureDesc);
            var newRight = new RenderTexture(_eyeRenderTextureDesc);
            leftCam.targetTexture = newLeft;
            rightCam.targetTexture = newRight;

            CardboardManager.SetRenderTexture(newLeft, newRight);
        }

        private void SetupEyeRenderTextureDescription()
        {
            var resolution = CardboardUtility.GetAdjustedScreenResolution();

            _eyeRenderTextureDesc = new RenderTextureDescriptor
            {
                dimension = TextureDimension.Tex2D,
                width = resolution.x / 2,
                height = resolution.y,
                depthBufferBits = 16,
                volumeDepth = 1,
                msaaSamples = 1,
                vrUsage = VRTextureUsage.OneEye
            };

#if UNITY_2019_1_OR_NEWER
            eyeRenderTextureDesc.graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);

            Debug.LogFormat("CardboardMainCamera.SetupEyeRenderTextureDescription(), graphicsFormat={0}",
                eyeRenderTextureDesc.graphicsFormat);
#endif
        }

        private void SwitchVRCamera()
        {
            vrCamGroup.SetActive(CardboardManager.enableVRView);
            novrCamGroup.SetActive(!CardboardManager.enableVRView);
        }

        private void RefreshCamera()
        {
            if (!CardboardManager.profileAvailable)
            {
                return;
            }

            RefreshCamera_Eye(leftCam,
                CardboardManager.projectionMatrixLeft, CardboardManager.eyeFromHeadMatrixLeft);

            RefreshCamera_Eye(rightCam,
                CardboardManager.projectionMatrixRight, CardboardManager.eyeFromHeadMatrixRight);
        }

        private static void RefreshCamera_Eye(Camera eyeCam, Matrix4x4 projectionMat, Matrix4x4 eyeFromHeadMat)
        {
            if (!projectionMat.Equals(Matrix4x4.zero))
            {
                eyeCam.projectionMatrix = projectionMat;
            }

            if (!eyeFromHeadMat.Equals(Matrix4x4.zero))
            {
                var eyeFromHeadPoseGL = CardboardUtility.GetPoseFromTRSMatrix(eyeFromHeadMat);
                eyeFromHeadPoseGL.position.x = -eyeFromHeadPoseGL.position.x;
                eyeCam.transform.localPosition = eyeFromHeadPoseGL.position;
                eyeCam.transform.localRotation = eyeFromHeadPoseGL.rotation;
            }
        }
    }
}
