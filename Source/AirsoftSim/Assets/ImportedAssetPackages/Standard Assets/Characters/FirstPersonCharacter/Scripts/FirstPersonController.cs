using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using UnityEngine.Networking;

#pragma warning disable 618, 649
namespace UnityStandardAssets.Characters.FirstPerson {
    [RequireComponent(typeof (CharacterController))]
    public class FirstPersonController : NetworkBehaviour {

        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_CrouchSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;

        Camera m_Camera;
        bool m_Jump;
        Vector2 m_Input;
        Vector3 m_MoveDir = Vector3.zero;
        CharacterController m_CharacterController;
        CollisionFlags m_CollisionFlags;
        bool m_PreviouslyGrounded;
        Vector3 m_OriginalCameraPosition;
        float m_StepCycle;
        float m_NextStep;
        bool m_Jumping;

        public bool block = false;
        public bool crouching = false;
        private float walkSpeed;
        public bool runningIsAvailable = true;
        public bool walkingJumpingIsAvailable = true;
        public GameObject nonterrLeftFootStepSound;
        public GameObject nonterrRightFootStepSound;
        public GameObject terrLeftFootStepSound;
        public GameObject terrRightFootStepSound;
        public GameObject nonterrJumpSound;
        public GameObject terrJumpSound;
        public GameObject nonterrLandingSound;
        public GameObject terrLandingSound;

        public float ammunitionLoad = 0.0f;
        [SerializeField] float max_endurance = 100.0f;
        [SerializeField] float endurance;

        void Start() {
            if (!isLocalPlayer) enabled = false;
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle / 2f;
            m_Jumping = false;
			m_MouseLook.Init(transform , m_Camera.transform);
            walkSpeed = m_WalkSpeed;
            endurance = max_endurance;
        }

        void Update() {
            if (crouching) m_WalkSpeed = m_CrouchSpeed;
            else m_WalkSpeed = walkSpeed;

            RotateView();
            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump && !block && walkingJumpingIsAvailable && IsJumpingAvailable()) m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded) {
                StartCoroutine(m_JumpBob.DoBobCycle());
                LocalPlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded) m_MoveDir.y = 0f;

            m_PreviouslyGrounded = m_CharacterController.isGrounded;
        }

        void FixedUpdate() {
            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_MoveDir.x = desiredMove.x * speed;
            m_MoveDir.z = desiredMove.z * speed;

            if (m_CharacterController.isGrounded) {
                m_MoveDir.y = -m_StickToGroundForce;
                if (m_Jump) {
                    m_MoveDir.y = m_JumpSpeed;
                    LocalPlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
            } else m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);
        }

        public void Rest() {
            endurance += (endurance <= 0.95f * max_endurance) ? 0.05f * max_endurance : (max_endurance - endurance);
        }

        void ProgressStepCycle(float speed) {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) * Time.fixedDeltaTime;
            if (!(m_StepCycle > m_NextStep)) return;
            m_NextStep = m_StepCycle + m_StepInterval;
        }

        public bool IsJumpingAvailable() { return endurance > max_endurance * 0.1f; }

        public bool IsRunningAvailable() { return endurance > max_endurance * 0.05f; }

        [Client] void LocalPlayJumpSound() {
            endurance -= (endurance >= 2f * ammunitionLoad) ? 2f * ammunitionLoad : endurance;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, -transform.up, out hit, 100f)) {
                if (hit.collider.gameObject && hit.collider.gameObject.name == "Terrain") CmdPlayJumpSound(true);
                else CmdPlayJumpSound(false);
            }
        }

        [Command] void CmdPlayJumpSound(bool isTerrain) {
            GameObject jump_sound;
            if (isTerrain) jump_sound = Instantiate(terrJumpSound, transform.position, Quaternion.identity);
            else jump_sound = Instantiate(nonterrJumpSound, transform.position, Quaternion.identity);
            if (jump_sound) NetworkServer.Spawn(jump_sound);
        }

        [Client] void LocalPlayLandingSound() {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, -transform.up, out hit, 100f)) {
                if (hit.collider.gameObject && hit.collider.gameObject.name == "Terrain") CmdPlayLandingSound(true);
                else CmdPlayLandingSound(false);
            }
            m_NextStep = m_StepCycle + 0.5f;
        }

        [Command] void CmdPlayLandingSound(bool isTerrain) {
            GameObject landing_sound;
            if (isTerrain) landing_sound = Instantiate(terrLandingSound, transform.position, Quaternion.identity);
            else landing_sound = Instantiate(nonterrLandingSound, transform.position, Quaternion.identity);
            if (landing_sound) NetworkServer.Spawn(landing_sound);
        }

        public void PlayFootStepSound(bool isLeft) { LocalPlayFootStepSound(isLeft); }
        [Client] void LocalPlayFootStepSound(bool isLeftStep) {
            if (!m_CharacterController.isGrounded) return;
            if (!m_IsWalking) endurance -= (endurance >= ammunitionLoad) ? ammunitionLoad : endurance;
            else endurance += (endurance <= 0.99f * max_endurance) ? 0.01f * max_endurance : (max_endurance - endurance);
            RaycastHit hit;
            if (Physics.Raycast(transform.position, -transform.up, out hit, 100f)) {
                if (hit.collider.gameObject && hit.collider.gameObject.name == "Terrain") {
                    if (isLeftStep) CmdPlayLeftFootStepSound(true);
                    else CmdPlayRightFootStepSound(true);
                } else {
                    if (isLeftStep) CmdPlayLeftFootStepSound(false);
                    else CmdPlayRightFootStepSound(false);
                }
            }
        }

        [Command] void CmdPlayLeftFootStepSound(bool isTerrain) {
            GameObject foot_sound;
            if (isTerrain) foot_sound = Instantiate(terrLeftFootStepSound, transform.position, Quaternion.identity);
            else foot_sound = Instantiate(nonterrLeftFootStepSound, transform.position, Quaternion.identity);
            if (foot_sound) NetworkServer.Spawn(foot_sound);
        }

        [Command] void CmdPlayRightFootStepSound(bool isTerrain) {
            GameObject foot_sound;
            if (isTerrain) foot_sound = Instantiate(terrRightFootStepSound, transform.position, Quaternion.identity);
            else foot_sound = Instantiate(nonterrRightFootStepSound, transform.position, Quaternion.identity);
            if (foot_sound) NetworkServer.Spawn(foot_sound);
        }

        void UpdateCameraPosition(float speed) {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob) {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded) {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            } else {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }

        private void GetInput(out float speed) {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            if (block || !walkingJumpingIsAvailable) {
                horizontal = 0.0f;
                vertical = 0.0f;
            }

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = Input.GetAxis("Run") == 0 || !IsRunningAvailable();
#endif
            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1) m_Input.Normalize();

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0) {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }

        private void RotateView() {
            if (block) return;
            m_MouseLook.LookRotation (transform, m_Camera.transform);
        }
    }
}
