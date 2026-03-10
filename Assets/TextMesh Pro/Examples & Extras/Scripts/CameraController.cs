using UnityEngine;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


namespace TMPro.Examples
{
    public class CameraController : MonoBehaviour
    {
        public enum CameraModes { Follow, Isometric, Free }

        private Transform cameraTransform;
        private Transform dummyTarget;

        public Transform CameraTarget;

        public float FollowDistance = 30.0f;
        public float MaxFollowDistance = 100.0f;
        public float MinFollowDistance = 2.0f;

        public float ElevationAngle = 30.0f;
        public float MaxElevationAngle = 85.0f;
        public float MinElevationAngle = 0f;

        public float OrbitalAngle = 0f;

        public CameraModes CameraMode = CameraModes.Follow;

        public bool MovementSmoothing = true;
        public bool RotationSmoothing = false;
        private bool previousSmoothing;

        public float MovementSmoothingValue = 25f;
        public float RotationSmoothingValue = 5.0f;

        public float MoveSensitivity = 2.0f;

        private Vector3 currentVelocity = Vector3.zero;
        private Vector3 desiredPosition;
        private float mouseX;
        private float mouseY;
        private Vector3 moveVector;
        private float mouseWheel;

        private const float MouseDeltaScale = 0.02f;
        private const float MouseScrollScale = 0.01f;

        private const string event_SmoothingValue = "Slider - Smoothing Value";
        private const string event_FollowDistance = "Slider - Camera Zoom";


        void Awake()
        {
            if (QualitySettings.vSyncCount > 0)
                Application.targetFrameRate = 60;
            else
                Application.targetFrameRate = -1;

            cameraTransform = transform;
            previousSmoothing = MovementSmoothing;
        }


        // Use this for initialization
        void Start()
        {
            if (CameraTarget == null)
            {
                // If we don't have a target (assigned by the player, create a dummy in the center of the scene).
                dummyTarget = new GameObject("Camera Target").transform;
                CameraTarget = dummyTarget;
            }
        }

        // Update is called once per frame
        void LateUpdate()
        {
            GetPlayerInput();


            // Check if we still have a valid target
            if (CameraTarget != null)
            {
                if (CameraMode == CameraModes.Isometric)
                {
                    desiredPosition = CameraTarget.position + Quaternion.Euler(ElevationAngle, OrbitalAngle, 0f) * new Vector3(0, 0, -FollowDistance);
                }
                else if (CameraMode == CameraModes.Follow)
                {
                    desiredPosition = CameraTarget.position + CameraTarget.TransformDirection(Quaternion.Euler(ElevationAngle, OrbitalAngle, 0f) * (new Vector3(0, 0, -FollowDistance)));
                }
                else
                {
                    // Free Camera implementation
                }

                if (MovementSmoothing == true)
                {
                    // Using Smoothing
                    cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, desiredPosition, ref currentVelocity, MovementSmoothingValue * Time.fixedDeltaTime);
                    //cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, Time.deltaTime * 5.0f);
                }
                else
                {
                    // Not using Smoothing
                    cameraTransform.position = desiredPosition;
                }

                if (RotationSmoothing == true)
                    cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, Quaternion.LookRotation(CameraTarget.position - cameraTransform.position), RotationSmoothingValue * Time.deltaTime);
                else
                {
                    cameraTransform.LookAt(CameraTarget);
                }
            }
        }



        void GetPlayerInput()
        {
            moveVector = Vector3.zero;

            // Check Mouse Wheel Input prior to Shift Key so we can apply multiplier on Shift for Scrolling
            mouseWheel = ReadMouseScroll();

            int touchCount = GetActiveTouchCount();

            if (IsShiftHeld() || touchCount > 0)
            {
                mouseWheel *= 10;

                if (WasLetterPressedThisFrame('I'))
                    CameraMode = CameraModes.Isometric;

                if (WasLetterPressedThisFrame('F'))
                    CameraMode = CameraModes.Follow;

                if (WasLetterPressedThisFrame('S'))
                    MovementSmoothing = !MovementSmoothing;


                // Check for right mouse button to change camera follow and elevation angle
                if (IsMouseButtonPressed(1))
                {
                    Vector2 mouseDelta = ReadMouseDelta();
                    mouseY = mouseDelta.y;
                    mouseX = mouseDelta.x;

                    if (mouseY > 0.01f || mouseY < -0.01f)
                    {
                        ElevationAngle -= mouseY * MoveSensitivity;
                        // Limit Elevation angle between min & max values.
                        ElevationAngle = Mathf.Clamp(ElevationAngle, MinElevationAngle, MaxElevationAngle);
                    }

                    if (mouseX > 0.01f || mouseX < -0.01f)
                    {
                        OrbitalAngle += mouseX * MoveSensitivity;
                        if (OrbitalAngle > 360)
                            OrbitalAngle -= 360;
                        if (OrbitalAngle < 0)
                            OrbitalAngle += 360;
                    }
                }

                // Get Input from Mobile Device
                if (touchCount == 1 && TryGetTouchDelta(0, out Vector2 deltaPosition))
                {
                    // Handle elevation changes
                    if (deltaPosition.y > 0.01f || deltaPosition.y < -0.01f)
                    {
                        ElevationAngle -= deltaPosition.y * 0.1f;
                        // Limit Elevation angle between min & max values.
                        ElevationAngle = Mathf.Clamp(ElevationAngle, MinElevationAngle, MaxElevationAngle);
                    }


                    // Handle left & right
                    if (deltaPosition.x > 0.01f || deltaPosition.x < -0.01f)
                    {
                        OrbitalAngle += deltaPosition.x * 0.1f;
                        if (OrbitalAngle > 360)
                            OrbitalAngle -= 360;
                        if (OrbitalAngle < 0)
                            OrbitalAngle += 360;
                    }
                }

                // Check for left mouse button to select a new CameraTarget or to reset Follow position
                if (IsMouseButtonPressed(0))
                {
                    Camera mainCamera = Camera.main;
                    if (mainCamera != null)
                    {
                        Ray ray = mainCamera.ScreenPointToRay(ReadMousePosition());
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, 300, 1 << 10 | 1 << 11 | 1 << 12 | 1 << 14))
                        {
                            if (hit.transform == CameraTarget)
                            {
                                // Reset Follow Position
                                OrbitalAngle = 0;
                            }
                            else
                            {
                                CameraTarget = hit.transform;
                                OrbitalAngle = 0;
                                MovementSmoothing = previousSmoothing;
                            }
                        }
                    }
                }


                if (IsMouseButtonPressed(2))
                {
                    if (dummyTarget == null)
                    {
                        // We need a Dummy Target to anchor the Camera
                        dummyTarget = new GameObject("Camera Target").transform;
                        dummyTarget.position = CameraTarget.position;
                        dummyTarget.rotation = CameraTarget.rotation;
                        CameraTarget = dummyTarget;
                        previousSmoothing = MovementSmoothing;
                        MovementSmoothing = false;
                    }
                    else if (dummyTarget != CameraTarget)
                    {
                        // Move DummyTarget to CameraTarget
                        dummyTarget.position = CameraTarget.position;
                        dummyTarget.rotation = CameraTarget.rotation;
                        CameraTarget = dummyTarget;
                        previousSmoothing = MovementSmoothing;
                        MovementSmoothing = false;
                    }


                    Vector2 mouseDelta = ReadMouseDelta();
                    mouseY = mouseDelta.y;
                    mouseX = mouseDelta.x;

                    moveVector = cameraTransform.TransformDirection(mouseX, mouseY, 0);

                    dummyTarget.Translate(-moveVector, Space.World);
                }
            }

            // Check Pinching to Zoom in - out on Mobile device
            if (touchCount == 2
                && TryGetTouchState(0, out Vector2 touch0Position, out Vector2 touch0Delta)
                && TryGetTouchState(1, out Vector2 touch1Position, out Vector2 touch1Delta))
            {
                Vector2 touch0PrevPos = touch0Position - touch0Delta;
                Vector2 touch1PrevPos = touch1Position - touch1Delta;

                float prevTouchDelta = (touch0PrevPos - touch1PrevPos).magnitude;
                float touchDelta = (touch0Position - touch1Position).magnitude;

                float zoomDelta = prevTouchDelta - touchDelta;

                if (zoomDelta > 0.01f || zoomDelta < -0.01f)
                {
                    FollowDistance += zoomDelta * 0.25f;
                    // Limit FollowDistance between min & max values.
                    FollowDistance = Mathf.Clamp(FollowDistance, MinFollowDistance, MaxFollowDistance);
                }
            }

            // Check MouseWheel to Zoom in-out
            if (mouseWheel < -0.01f || mouseWheel > 0.01f)
            {
                FollowDistance -= mouseWheel * 5.0f;
                // Limit FollowDistance between min & max values.
                FollowDistance = Mathf.Clamp(FollowDistance, MinFollowDistance, MaxFollowDistance);
            }
        }


        private static bool IsShiftHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
#else
            return false;
#endif
        }

        private static bool WasLetterPressedThisFrame(char letter)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            switch (char.ToUpperInvariant(letter))
            {
                case 'I': return keyboard.iKey.wasPressedThisFrame;
                case 'F': return keyboard.fKey.wasPressedThisFrame;
                case 'S': return keyboard.sKey.wasPressedThisFrame;
                default: return false;
            }
#else
            return false;
#endif
        }

        private static bool IsMouseButtonPressed(int button)
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            switch (button)
            {
                case 0: return mouse.leftButton.isPressed;
                case 1: return mouse.rightButton.isPressed;
                case 2: return mouse.middleButton.isPressed;
                default: return false;
            }
#else
            return false;
#endif
        }

        private static Vector2 ReadMouseDelta()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            return mouse != null ? mouse.delta.ReadValue() * MouseDeltaScale : Vector2.zero;
#else
            return Vector2.zero;
#endif
        }

        private static float ReadMouseScroll()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            return mouse != null ? mouse.scroll.ReadValue().y * MouseScrollScale : 0f;
#else
            return 0f;
#endif
        }

        private static Vector2 ReadMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
#else
            return Vector2.zero;
#endif
        }

        private static int GetActiveTouchCount()
        {
#if ENABLE_INPUT_SYSTEM
            var touchScreen = Touchscreen.current;
            if (touchScreen == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < touchScreen.touches.Count; i++)
            {
                if (touchScreen.touches[i].press.isPressed)
                {
                    count++;
                }
            }

            return count;
#else
            return 0;
#endif
        }

        private static bool TryGetTouchState(int activeTouchIndex, out Vector2 position, out Vector2 delta)
        {
            position = Vector2.zero;
            delta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var touchScreen = Touchscreen.current;
            if (touchScreen == null)
            {
                return false;
            }

            int currentActiveIndex = 0;
            for (int i = 0; i < touchScreen.touches.Count; i++)
            {
                var touch = touchScreen.touches[i];
                if (!touch.press.isPressed)
                {
                    continue;
                }

                if (currentActiveIndex == activeTouchIndex)
                {
                    position = touch.position.ReadValue();
                    delta = touch.delta.ReadValue();
                    return true;
                }

                currentActiveIndex++;
            }
#endif

            return false;
        }

        private static bool TryGetTouchDelta(int activeTouchIndex, out Vector2 deltaPosition)
        {
            deltaPosition = Vector2.zero;
            if (!TryGetTouchState(activeTouchIndex, out _, out Vector2 delta))
            {
                return false;
            }

            if (delta.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            deltaPosition = delta;
            return true;
        }
    }
}
