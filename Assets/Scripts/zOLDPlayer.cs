using UnityEngine;

namespace Assets.Scripts
{
    public class Player : MonoBehaviour {
        public System.Action OnReachedEndOfLevel;

        public float speed = 7;
        public float smoothMoveTime = 0.1f;
        public float turnSpeed = 8;

        float angle;
        float smoothInputMagnitude;
        float smoothMoveVelocity;
        Vector3 velocity;

        Rigidbody rigidbody;
        bool disabled;

        // Use this for initialization
        void Start()
        {
            rigidbody = GetComponent<Rigidbody>();
            Guard.OnGuardHasSpottedPlayer += Disable;
        }

        void Update ()
        {
            Vector3 inputDirection = Vector3.zero;
            if (!disabled)
            {
                inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
            }
            float inputMagnitude = inputDirection.magnitude;
            smoothInputMagnitude = Mathf.SmoothDamp(smoothInputMagnitude, inputMagnitude, ref smoothMoveVelocity, smoothMoveTime);

            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
            angle = Mathf.LerpAngle(angle, targetAngle, Time.deltaTime * turnSpeed * inputMagnitude);

            velocity = transform.forward * speed * smoothInputMagnitude;
        }

        private void FixedUpdate()
        {
            rigidbody.MoveRotation(Quaternion.Euler(Vector3.up * angle));
            rigidbody.MovePosition(rigidbody.position + velocity * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (collider.tag == "Finish")
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
