using System;
using UnityEngine;
using Game.Parkour;
namespace Game.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class RigidbodyFirstPersonController : MonoBehaviour
    {
        [Serializable]
        public class MovementSettings
        {
            public float ForwardSpeed = 8.0f;   // Speed when walking forward
            public float BackwardSpeed = 4.0f;  // Speed when walking backwards
            public float StrafeSpeed = 4.0f;    // Speed when walking sideways
            public float SpeedInAir = 8.0f;   // Speed when onair
            public float JumpForce = 30f;

            [HideInInspector] public float CurrentTargetSpeed = 8f;

            public void UpdateDesiredTargetSpeed(Vector2 input)
            {
                if (input == Vector2.zero) return;
                if (input.x > 0 || input.x < 0)
                {
                    //strafe
                    CurrentTargetSpeed = StrafeSpeed;
                }
                if (input.y < 0)
                {
                    //backwards
                    CurrentTargetSpeed = BackwardSpeed;
                }
                if (input.y > 0)
                {
                    //forwards
                    //handled last as if strafing and moving forward at the same time forwards speed should take precedence
                    CurrentTargetSpeed = ForwardSpeed;
                }
            }
        }


        public bool canrotate;
        public Camera cam;
        public MovementSettings movementSettings = new MovementSettings();
        public MouseLook mouseLook = new MouseLook();
        public Vector3 relativevelocity;

        public GameObject markObj;
        public Mark mark;

        public DetectObs detectGround;
        public DetectObs DetectWallL, DetectWallR;
        private float StepTimer = 0.7f;
        private float StepTimerDown;
        private AudioSource audioSource;
        public AudioClip[] Stone;
        public AudioClip[] Ground;
        public AudioClip[] Jump;

        public bool Wallrunning;

        private Rigidbody m_RigidBody;
        private CapsuleCollider m_Capsule;
        private float m_YRotation;
        private bool m_IsGrounded;

        private void Start()
        {
           MouseLook.cursor_disabled();
        }

        public Vector3 Velocity
        {
            get { return m_RigidBody.velocity; }
        }

        public bool Grounded
        {
            get { return m_IsGrounded; }
        }

        private void Awake()
        {
            canrotate = true;
            m_RigidBody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            audioSource = GetComponent<AudioSource>();
            mouseLook.Init(transform, cam.transform);
            mark = new Mark(gameObject, markObj);
        }

        private void OnCollisionStay(Collision collision)
        {
            mark.advancing(collision);
        }


        private void Update()
        {
            relativevelocity = transform.InverseTransformDirection(m_RigidBody.velocity);
            if (m_IsGrounded && Input.GetKeyDown(KeyCode.Space))
            {
                NormalJump();
            }
            else if (Wallrunning)
            {
                StepTimerDown -= Time.deltaTime;
                MoveSounds(DetectWallL);
                MoveSounds(DetectWallR);
            }
        }


        private void LateUpdate()
        {
            if (canrotate)
            {
                RotateView();
            }
            else
            {
                mouseLook.LookOveride(transform, cam.transform);
            }
        }
        public void CamGoBack(float speed)
        {
            mouseLook.CamGoBack(transform, cam.transform, speed);
        }
        public void CamGoBackAll()
        {
            mouseLook.CamGoBackAll(transform, cam.transform);
        }
        private void FixedUpdate()
        {
            GroundCheck();
            MoveSounds(detectGround);
            Vector2 input = GetInput();

            float h = input.x;
            float v = input.y;
            Vector3 inputVector = new Vector3(h, 0, v);
            inputVector = Vector3.ClampMagnitude(inputVector, 1);

            //grounded
            if ((Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon) && m_IsGrounded && !Wallrunning)
            {
                if (Input.GetAxisRaw("Vertical") > 0.3f)
                {
                    m_RigidBody.AddRelativeForce(0, 0, Time.deltaTime * 1000f * movementSettings.ForwardSpeed * Mathf.Abs(inputVector.z));
                }
                if (Input.GetAxisRaw("Vertical") < -0.3f)
                {
                    m_RigidBody.AddRelativeForce(0, 0, Time.deltaTime * 1000f * -movementSettings.BackwardSpeed * Mathf.Abs(inputVector.z));
                }
                if (Input.GetAxisRaw("Horizontal") > 0.5f)
                {
                    m_RigidBody.AddRelativeForce(Time.deltaTime * 1000f * movementSettings.StrafeSpeed * Mathf.Abs(inputVector.x), 0, 0);
                }
                if (Input.GetAxisRaw("Horizontal") < -0.5f)
                {
                    m_RigidBody.AddRelativeForce(Time.deltaTime * 1000f * -movementSettings.StrafeSpeed * Mathf.Abs(inputVector.x), 0, 0);
                }

            }
            //inair
            if ((Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon) && !m_IsGrounded && !Wallrunning)
            {
                if (Input.GetAxisRaw("Vertical") > 0.3f)
                {
                    m_RigidBody.AddRelativeForce(0, 0, Time.deltaTime * 1000f * movementSettings.SpeedInAir * Mathf.Abs(inputVector.z));
                }
                if (Input.GetAxisRaw("Vertical") < -0.3f)
                {
                    m_RigidBody.AddRelativeForce(0, 0, Time.deltaTime * 1000f * -movementSettings.SpeedInAir * Mathf.Abs(inputVector.z));
                }
                if (Input.GetAxisRaw("Horizontal") > 0.5f)
                {
                    m_RigidBody.AddRelativeForce(Time.deltaTime * 1000f * movementSettings.SpeedInAir * Mathf.Abs(inputVector.x), 0, 0);
                }
                if (Input.GetAxisRaw("Horizontal") < -0.5f)
                {
                    m_RigidBody.AddRelativeForce(Time.deltaTime * 1000f * -movementSettings.SpeedInAir * Mathf.Abs(inputVector.x), 0, 0);
                }

            }
        }

        public void NormalJump()
        {
            JumpSounds();
            m_RigidBody.velocity = new Vector3(m_RigidBody.velocity.x, 0f, m_RigidBody.velocity.z);
            m_RigidBody.AddForce(new Vector3(0f, movementSettings.JumpForce, 0f), ForceMode.Impulse);
        }
        public void SwitchDirectionJump()
        {
            m_RigidBody.velocity = transform.forward * m_RigidBody.velocity.magnitude;
            m_RigidBody.AddForce(new Vector3(0f, movementSettings.JumpForce, 0f), ForceMode.Impulse);
        }

        private Vector2 GetInput()
        {
            Vector2 input = new Vector2
            {
                x = Input.GetAxisRaw("Horizontal"),
                y = Input.GetAxisRaw("Vertical")
            };
            movementSettings.UpdateDesiredTargetSpeed(input);
            return input;
        }


        private void RotateView()
        {
            //avoids the mouse looking if the game is effectively paused
            if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

            // get the rotation before it's changed
            float oldYRotation = transform.eulerAngles.y;

            mouseLook.LookRotation(transform, cam.transform);
        }


        /// sphere cast down just beyond the bottom of the capsule to see if the capsule is colliding round the bottom
        private void GroundCheck()
        {
            m_IsGrounded = detectGround.Obstruction;
        }
        private void MoveSounds(DetectObs detct)
        {
            if (StepTimerDown > 0)
            {
                StepTimerDown -= Time.deltaTime;
            }
            else if (StepTimerDown < 0)
            {
                StepTimerDown = 0;
            }
            else if (StepTimerDown == 0)
            {
                bool isMove = Input.GetAxisRaw("Vertical") != 0 || Input.GetAxisRaw("Horizontal") != 0;
                if ((m_IsGrounded || Wallrunning) && isMove)
                {
                    if (detct.tags.Contains("Ground"))
                        audioSource.PlayOneShot(Ground[UnityEngine.Random.Range(0, Ground.Length)]);
                    if (detct.tags.Contains("Stone"))
                        audioSource.PlayOneShot(Stone[UnityEngine.Random.Range(0, Stone.Length)]);
                }
                StepTimerDown = StepTimer;
            }
            else
            {
                Debug.LogError("Wat?");
            }
        }
        private void JumpSounds()
        {
            if (Wallrunning || m_IsGrounded)
            {
                audioSource.PlayOneShot(Jump[UnityEngine.Random.Range(0, Jump.Length)]);
            }
        }
    }
}
