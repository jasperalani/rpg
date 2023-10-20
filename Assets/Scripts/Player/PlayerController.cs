using RIPTIDE.CameraController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.Combat;

namespace RPG.Player
{



    public class PlayerController : MonoBehaviour
    {

        public float clickDistance = 50;
        public GameObject selectedEnemy;


        // inputs
        public Controls controls;
        [SerializeField] private Vector2 inputs;
        [HideInInspector]
        public float rotation;
        [HideInInspector] public Vector2 inputNormalized;
        [SerializeField] bool run = true;
        [SerializeField] bool jump = false;
        [HideInInspector] public bool steer, AutoRun;


        //velocity
        [HideInInspector] Vector3 velocity;
        [SerializeField] float gravity = -9, velocityY, terminalVelocity = -25;
        [HideInInspector] float fallMault;

        //Running
        [HideInInspector] public float currentSpeed;
        public float baseSpeed = 1f, runSpeed = 4, rotationSpeed = 1.0f;

        //ground
        Vector3 forwardDirection, CollisionPoint;
        [HideInInspector] public float slopeAngle;
        [HideInInspector] public float directionAngle;
        [HideInInspector] public float strafeAngle;
        [HideInInspector] public float forwardAngle;
        [HideInInspector] float forwardMult, strafeMault;
        Ray groundRay;
        RaycastHit groundhit;

        //DebugGround
        [SerializeField] bool showFallNormal, showMoveDirection, showForwardDirection, showStrafeDirection, showGroundRay;

        //jump
        bool jumping, canJump = true;
        public float jumpSpeed, jumpHeight = 2;
        Vector3 jumpDirection;

        // reference
        CharacterController controller;
        public Transform groundDirection, falldirection, moveDirection;
        public cameraController maincam;
        private void Start()
        {
            controller = GetComponent<CharacterController>();
        }
        private void Update()
        {
            GetInputs();
            Locomotion();
            DetectClickInteraction();
        }
        void Locomotion()
        {
            GroundDirection();
            //rotation
            Vector3 characterRotation = transform.eulerAngles + new Vector3(0, rotation * rotationSpeed, 0);
            transform.eulerAngles = characterRotation;


            //Press space to jump 
            if (jump && controller.isGrounded && slopeAngle <= controller.slopeLimit && canJump)

            {
                Jump();
            }

            //running and walking 
            if (controller.isGrounded && slopeAngle <= controller.slopeLimit)
            {


                currentSpeed = baseSpeed;

                if (run)
                {
                    currentSpeed *= runSpeed;
                    if (inputNormalized.y < 0)
                    {
                        currentSpeed = currentSpeed / 2;
                    }

                }
            }
            else if (!controller.isGrounded || slopeAngle > controller.slopeLimit)
            {
                inputNormalized = Vector2.Lerp(inputNormalized, Vector2.zero, 0.025f);
                currentSpeed = Mathf.Lerp(currentSpeed, 0, 0.025f);
            }

            //Apply Gravity if not grounded
            if (!controller.isGrounded && velocityY > terminalVelocity)
            {
                velocityY += gravity * Time.deltaTime;
            }
            else if (controller.isGrounded && slopeAngle > controller.slopeLimit)
            {
                velocityY = Mathf.Lerp(velocityY, terminalVelocity, 0.25f);
            }






            // apply inputs
            if (!jumping)
            {
                //velocity = (groundDirection.forward * inputNormalized.magnitude) * (currentSpeed * forwardMult) + falldirection.up * (velocityY * fallMault);
                velocity = (groundDirection.forward * inputNormalized.y * forwardMult + groundDirection.right * inputNormalized.x * strafeMault); // appling movement direction inputs
                velocity *= currentSpeed; // applying current movespeed
                velocity += falldirection.up * (velocityY * fallMault); // gravity 
            }

            else
            {
                velocity = jumpDirection * jumpSpeed + Vector3.up * velocityY;
            }


            //moving controller
            controller.Move(velocity * Time.deltaTime);

            if (controller.isGrounded)
            {
                if (jumping)
                    jumping = false;

                if (!jump && !canJump)
                {
                    canJump = true;
                }

                velocityY = 0;
            }
        }

        #region //Groundstuff
        void GroundDirection()
        {
            //Setting forwarddirection
            //setting forward Direction to Controller position
            forwardDirection = transform.position;

            //Setting forward direction based on inputs
            if (inputNormalized.magnitude > 0)
                forwardDirection += transform.forward * inputNormalized.y + transform.right * inputNormalized.x;

            else
                forwardDirection += transform.forward;
            //setting ground direction to look in the forward direction normal. 
            moveDirection.LookAt(forwardDirection);
            falldirection.rotation = transform.rotation;
            groundDirection.rotation = transform.rotation;


            // setting ground ray 
            groundRay.origin = transform.position + CollisionPoint + Vector3.up * 0.05f;
            groundRay.direction = Vector3.down;

            if (showGroundRay)
                Debug.DrawLine(groundRay.origin, groundRay.origin + Vector3.down * 0.3f, Color.red);

            forwardMult = 1;
            fallMault = 1;
            strafeMault = 1;

            if (Physics.Raycast(groundRay, out groundhit, 0.3f))
            {
                slopeAngle = Vector3.Angle(transform.up, groundhit.normal);
                directionAngle = Vector3.Angle(moveDirection.forward, groundhit.normal) - 90;

                if (directionAngle < 0 && slopeAngle <= controller.slopeLimit)
                {
                    forwardAngle = Vector3.Angle(transform.forward, groundhit.normal) - 90; // checking forwardAngle to the slope
                    forwardMult = 1 / Mathf.Cos(forwardAngle * Mathf.Deg2Rad); // applying the forward movement multiplier based on the wardangle
                    groundDirection.eulerAngles += new Vector3(-forwardAngle, 0, 0); // rotation groundDirection X 

                    strafeAngle = Vector3.Angle(groundDirection.right, groundhit.normal) - 90;
                    strafeMault = 1 / Mathf.Cos(strafeAngle * Mathf.Deg2Rad); // applying strafe movement multiplayer based on strafe angle
                    groundDirection.eulerAngles += new Vector3(0, 0, strafeAngle);
                }
                else if (slopeAngle > controller.slopeLimit)
                {
                    float groundDistance = Vector3.Distance(groundRay.origin, groundhit.point);

                    if (groundDistance <= 0.1f)
                    {
                        fallMault = 1 / Mathf.Cos((90 - slopeAngle) * Mathf.Deg2Rad);

                        Vector3 groundCross = Vector3.Cross(groundhit.normal, Vector3.up);
                        falldirection.rotation = Quaternion.FromToRotation(transform.up, Vector3.Cross(groundCross, groundhit.normal));
                    }


                }
            }
            DebugGroundNormals();

        }
        #endregion
        void Jump()
        {
            //set jumping to true
            if (!jumping)
            {
                jumping = true;
                canJump = false;
            }

            // set jump direction and speed
            jumpDirection = (transform.forward * inputs.y + transform.right * inputs.x).normalized;
            jumpSpeed = currentSpeed;

            // set velocity Y
            velocityY = Mathf.Sqrt(-gravity * jumpHeight);
        }

        void GetInputs()
        {

            if (controls.AutoRun.GetControlBindingDown())
            {
                AutoRun = !AutoRun;
            }
            //forward backwards controls

            inputs.y = Axis(controls.Forwards.GetControlBinding(), controls.Backwards.GetControlBinding());

            if (inputs.y != 0 && !maincam.autoRunReset)
            {
                AutoRun = false;
            }

            if (AutoRun)
            {
                inputs.y += Axis(true, false);

                inputs.y = Mathf.Clamp(inputs.y, -1, 1);
            }

            #region
            //// forwards
            //if (controls.Forwards.GetControlBinding())
            //    inputs.y = 1;

            ////backward
            //if (controls.Backwards.GetControlBinding())
            //{
            //    if (controls.Forwards.GetControlBinding())
            //        inputs.y = 0;
            //    else
            //        inputs.y = -1;
            //}
            ////FW nothing
            //if (!controls.Backwards.GetControlBinding() && !controls.Forwards.GetControlBinding())
            //{
            //    inputs.y = 0;

            //}

            ////Strafeleft
            //if (controls.strafeleft.GetControlBinding())
            //    inputs.x = -1;

            ////strafeRight
            //if (controls.straferight.GetControlBinding())
            //{
            //    if (controls.strafeleft.GetControlBinding())
            //        inputs.x = 0;
            //    else
            //        inputs.x = +1;
            //}
            //Strafeleft+rigth = nothing

            #endregion // back up code for moving

            //STRAFELEFT AND RIGHT 

            inputs.x = Axis(controls.straferight.GetControlBinding(), controls.strafeleft.GetControlBinding());

            if (steer)
            {
                inputs.x += rotation = Axis(controls.RotateRight.GetControlBinding(), controls.RotateLeft.GetControlBinding());

                inputs.x = Mathf.Clamp(inputs.x, -1, 1);
            }

            if (steer)
            {
                rotation = (Input.GetAxis("Mouse X") * maincam.CameraSpeed);

            }
            else
            {
                float strafeSpeedModifier = 0.60f;
                if (jumping)
                {
                    strafeSpeedModifier = 0.25f;
                }
                rotation = Axis(controls.RotateRight.GetControlBinding(), controls.RotateLeft.GetControlBinding()) * strafeSpeedModifier;

            }



            if (!controls.straferight.GetControlBinding() && !controls.strafeleft.GetControlBinding())
            {
                inputs.x = 0;

            }
            ////Rotation
            //if (controls.RotateRight.GetControlBinding())
            //    rotation= 1;

            ////strafeRight
            //if (controls.RotateLeft.GetControlBinding())
            //{
            //    if (controls.RotateRight.GetControlBinding())
            //        rotation = 0;
            //    else
            //        rotation = -1;
            //}
            ////Strafeleft+rigth = nothing
            //if (!controls.RotateRight.GetControlBinding() && !controls.RotateLeft.GetControlBinding())
            //{
            //    rotation = 0;

            //}
            // togglerun 
            if (controls.walkRun.GetControlBindingDown())
            {
                run = !run;
            }

            // jumping 
            jump = controls.Jump.GetControlBinding();

            inputNormalized = inputs.normalized;


        }
        public float Axis(bool pos, bool neg)
        {
            float axis = 0;

            if (pos)
                axis += 1;

            if (neg)
                axis -= 1;
            return axis;
        }

        void DebugGroundNormals()
        {
            Vector3 lineStart = transform.position + Vector3.up * 0.05f;
            if (showMoveDirection)
            {
                Debug.DrawLine(lineStart, lineStart + moveDirection.forward * 0.5f, Color.cyan);

            }

            if (showForwardDirection)
            {
                Debug.DrawLine(lineStart - groundDirection.forward * 0.5f, lineStart + groundDirection.forward * 0.5f, Color.blue);

            }

            if (showStrafeDirection)
            {
                Debug.DrawLine(lineStart - groundDirection.right * 0.5f, lineStart + groundDirection.right * 0.5f, Color.red);

            }
            if (showFallNormal)
            {
                Debug.DrawLine(lineStart, lineStart + falldirection.forward * 0.5f, Color.green);

            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            CollisionPoint = hit.point;
            CollisionPoint = CollisionPoint - transform.position;
        }

        void DetectClickInteraction()
        {
            RaycastHit[] hits = Physics.RaycastAll(GetMouseHitRay());

            foreach (RaycastHit hit in hits)
            {

                RaycastHit hitEnemy;
                if (Physics.Raycast(GetMouseHitRay(), out hitEnemy, clickDistance))
                {
                    EnemyTarget target = hit.transform.GetComponent<EnemyTarget>();
                    if (target == null) continue;

                    if (Input.GetMouseButtonDown(0))
                    {
                        GetComponent<PlayerCombat>().SelectEnemy(target);
                        selectedEnemy = hit.transform.gameObject;
                    }

                    if (Input.GetMouseButtonDown(1))
                    {
                        selectedEnemy = hit.transform.gameObject;
                        GetComponent<PlayerCombat>().AutoAttack(target);
                    }
                }

            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                DeselectEnemy();
            }
        }

        private static Ray GetMouseHitRay()
        {
            return Camera.main.ScreenPointToRay(Input.mousePosition);
        }

        void DeselectEnemy()
        {
            selectedEnemy = null;
        }

    }








}