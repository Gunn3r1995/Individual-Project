using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        public System.Action OnReachedEndOfLevel;
        public float walkSpeed = 2;
        public float runSpeed = 6;
        public float gravity = -12;
        public float jumpHeight = 1;
        [Range(0, 1)]
        public float airControlPercent;

        public float turnSmoothTime = 0.2f;
        float turnSmoothVelocity;

        public float speedSmoothTime = 0.1f;
        float speedSmoothVelocity;
        float currentSpeed;
        float velocityY;

        Animator animator;
        Transform cameraT;
        CharacterController characterController;
        bool disabled;

        // Use this for initialization
        void Start()
        {
            animator = GetComponent<Animator>();
            cameraT = Camera.main.transform;
            characterController = GetComponent<CharacterController>();
            Guard.OnGuardHasSpottedPlayer += Disable;
        }

        // Update is called once per frame
        void Update()
        {
            // Input
            Vector2 inputDirection = Vector3.zero;
            if (!disabled)
            {
                inputDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            }
            bool running = Input.GetKey(KeyCode.LeftShift);

            Move(inputDirection, running);
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }

            // Animator
            float animationSpeedPercent = ((running) ? currentSpeed / runSpeed : currentSpeed / walkSpeed * 0.5f);
            animator.SetFloat("speedPercent", animationSpeedPercent, speedSmoothTime, Time.deltaTime);
        }

        void Move(Vector2 inputDir, bool running)
        {
            if (inputDir != Vector2.zero)
            {
                float targetRotation = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg + cameraT.eulerAngles.y;
                transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, GetModifiedSmoothTime(turnSmoothTime));
            }

            float targetSpeed = ((running) ? runSpeed : walkSpeed) * inputDir.magnitude;
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, GetModifiedSmoothTime(speedSmoothTime));

            velocityY += Time.deltaTime * gravity;
            Vector3 velocity = transform.forward * currentSpeed + Vector3.up * velocityY;

            characterController.Move(velocity * Time.deltaTime);
            currentSpeed = new Vector2(characterController.velocity.x, characterController.velocity.z).magnitude;

            if (characterController.isGrounded)
            {
                velocityY = 0;
            }
        }

        void Jump()
        {
            if (characterController.isGrounded)
            {
                float jumpVelocity = Mathf.Sqrt(-2 * gravity * jumpHeight);
                velocityY = jumpVelocity;
            }
        }

        float GetModifiedSmoothTime(float smoothTime)
        {
            if (characterController.isGrounded)
            {
                return smoothTime;
            }

            if (airControlPercent == 0)
            {
                return float.MaxValue;
            }
            return smoothTime / airControlPercent;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider.tag == "Finish")
            {
                Disable();
                if (OnReachedEndOfLevel != null)
                {
                    OnReachedEndOfLevel();
                }
            }
        }

        private void OnDestroy()
        {
            Guard.OnGuardHasSpottedPlayer -= Disable;
        }

        void Disable()
        {
            disabled = true;
        }
    }
}
