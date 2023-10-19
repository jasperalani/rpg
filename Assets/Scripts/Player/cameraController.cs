using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RIPTIDE.CameraController
{
    //input Variables 

    
    public class cameraController : MonoBehaviour
    {


        // keycodes 

        KeyCode leftMouse = KeyCode.Mouse0, rightMouse = KeyCode.Mouse1, middleMouse = KeyCode.Mouse2;

        //camStates
        public CameraState cameraState = CameraState.CameraNone;

        // Options 
        public CameraMoveState camMoveState = CameraMoveState.OnlyWhileMoving;
        [Range(0.25f, 1.75f)]
        public float cameraAdjustSpeed = 1;

        // collision
        public bool collisionDebug;
        public float collisionCushion = 0.35f;
        public float clipCushion = 1.5f;
        public int rayGridX = 9, rayGridY = 5;
        float adjustedDistance;
        public LayerMask collisionMask;
       Vector3[] camClip, clipDirection, playerClip, rayColOrgin, rayColPoint;
        bool[] rayColHit;
        Ray camRay;
        RaycastHit camRayHit;

        // camera Variables 

        public float CameraHeight = 1.75f, CameraMaxDistance = 25;
        float CameraMaxTilt = 90;
        [Range(0,4)]
        public float CameraSpeed = 2;
        float currentPan, currentTilt = 10, currentDistance = 5;

        //CameraSmoothing
        float panAngle, panOffset;
        bool camXAdjust, camYAdjust;
        float rotationXCushion = 3, rotationXSpeed = 0;
        float yRotMin = 0, yRotMax = 20, rotationYSpeed = 0;

        // references
        PlayerController player;
        public Transform tilt;
        Camera mainCam;

       public bool autoRunReset = false;

        private void Awake()
        {
            transform.SetParent(null);
        }

        // Start is called before the first frame update
        void Start()
        {
        player = FindObjectOfType<PlayerController>();
            player.maincam = this;
            mainCam = Camera.main;

            transform.position = player.transform.position + Vector3.up * CameraHeight;
            transform.rotation = player.transform.rotation;

            tilt.eulerAngles = new Vector3(currentTilt, transform.eulerAngles.y, transform.eulerAngles.z);
            mainCam.transform.position += tilt.forward * -currentDistance;


            CameraClipInfo();
        }

        // Update is called once per frame
        void Update()
        {
        if (!Input.GetKey(leftMouse) && !Input.GetKey(rightMouse) && !Input.GetKey(middleMouse)) // if no mouse button is pressed
            {
                cameraState = CameraState.CameraNone;
            } else if (Input.GetKey(leftMouse) && !Input.GetKey(rightMouse) && !Input.GetKey(middleMouse)) // if left mouse button is pressed
                {
                cameraState = CameraState.CameraRotate;
            }else if (!Input.GetKey(leftMouse) && Input.GetKey(rightMouse) && !Input.GetKey(middleMouse)) // if right mouse button is pressed
            {
                cameraState = CameraState.CameraSteer;
            }else if ((Input.GetKey(leftMouse) && Input.GetKey(rightMouse)) || Input.GetKey(middleMouse)) // if left and reight mouse or middle is pressed
            {
               cameraState = CameraState.CameraRun;
            }

        if(rayGridX * rayGridY != rayColOrgin.Length)
            {
                CameraClipInfo();
            }
             
            cameraCollision();
                cameraInputs();
            
        }

        private void LateUpdate()
        {
            panAngle = Vector3.SignedAngle(transform.forward, player.transform.forward, Vector3.up);
            switch(camMoveState)
            {
                case CameraMoveState.OnlyWhileMoving:
                    if (player.inputNormalized.magnitude > 0 || player.rotation != 0)
                    {
                        CameraXAdjust();
                        CameraYAdjust();
                    }
                       
                    break;
                case CameraMoveState.OnlyHorizontalWhileMoving:
                    if (player.inputNormalized.magnitude > 0 || player.rotation !=0)
                    {

                        CameraXAdjust();
                    }

                    break;
                case CameraMoveState.AlwaysAdjust:
                    CameraXAdjust();
                    CameraYAdjust();
                    break;
                case CameraMoveState.NeverAdjust:
                    CameraNeverAdjust();
                    break;

            }
            cameraTransform();
        }

        void CameraClipInfo()
        {
            camClip = new Vector3[4];

            mainCam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), mainCam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, camClip);

            clipDirection = new Vector3[4];
            playerClip = new Vector3[4];

            int rays = rayGridX * rayGridY;

            rayColOrgin = new Vector3[rays];
            rayColPoint = new Vector3[rays];
            rayColHit = new bool[rays];
        }
        void cameraCollision()
        {
            float camDistance = currentDistance + collisionCushion;

            for(int i = 0; i<camClip.Length; i++)
            {
                Vector3 clipPoint = mainCam.transform.up * camClip[i].y + mainCam.transform.right * camClip[i].x;
                clipPoint *= clipCushion;
                clipPoint += mainCam.transform.forward * camClip[i].z;
                clipPoint += transform.position - (tilt.forward * CameraMaxDistance);

                Vector3 playerPoint = mainCam.transform.up * camClip[i].y + mainCam.transform.right * camClip[i].x;
                playerPoint += transform.position;

                clipDirection[i] = (clipPoint - playerPoint).normalized;
                playerClip[i] = playerPoint;

     

            }

            int currentRay = 0;
            bool isColliding = false;

            float rayX = rayGridX - 1;
            float rayY = rayGridY - 1;

            for(int x = 0; x < rayGridX; x++)
            {

                Vector3 CU_Point = Vector3.Lerp(clipDirection[1], clipDirection[2], x / rayX);
                Vector3 CL_Point = Vector3.Lerp(clipDirection[0], clipDirection[3], x / rayX);

                Vector3 PU_Point = Vector3.Lerp(playerClip[1], playerClip[2], x / rayX);
                Vector3 PL_Point = Vector3.Lerp(playerClip[0], playerClip[3], x / rayX);

                for (int y = 0; y < rayGridY; y++)
                {
                    camRay.origin = Vector3.Lerp(PU_Point, PL_Point, y / rayY);
                    camRay.direction = Vector3.Lerp(CU_Point, CL_Point, y / rayY);
                    rayColOrgin[currentRay] = camRay.origin;


                    if (Physics.Raycast(camRay, out camRayHit, camDistance, collisionMask))
                    {
                        isColliding = true;
                        rayColHit[currentRay] = true;
                        rayColPoint[currentRay] = camRayHit.point;

                        if (collisionDebug)
                        {
                            Debug.DrawLine(camRay.origin, camRayHit.point, Color.cyan);
                            Debug.DrawLine(camRayHit.point, camRay.origin + camRay.direction * camDistance, Color.magenta);
                        }
                            
                    }
                    else
                    {
                        if (collisionDebug)
                            Debug.DrawLine(camRay.origin, camRay.origin + camRay.direction * camDistance, Color.cyan);
                        rayColHit[currentRay] = false;
                    }


                    


                    currentRay++;
                }

            }
            if (isColliding)
            {
                float minRayDistance = float.MaxValue;
                currentRay = 0;

                for (int i = 0; i < rayColHit.Length; i++)
                {
                    if (rayColHit[i])
                    {
                        float colDistance = Vector3.Distance(rayColOrgin[i], rayColPoint[i]);

                        if (colDistance < minRayDistance)
                        {
                            minRayDistance = colDistance;
                            currentRay = i;
                        }
                    }
                }

                Vector3 clipCenter = transform.position - (tilt.forward * currentDistance);

                adjustedDistance = Vector3.Dot(-mainCam.transform.forward, clipCenter - rayColPoint[currentRay]);
                adjustedDistance = currentDistance - (adjustedDistance + collisionCushion);
                adjustedDistance = Mathf.Clamp(adjustedDistance, 0, CameraMaxDistance);
            }else
            {
                adjustedDistance = currentDistance;
            }
            

            //if(Physics.Raycast(camRay, out camRayHit, camDistance, collisionMask))
            //{
            //    adjustedDistance = Vector3.Distance(camRay.origin, camRayHit.point)-collisionCushion;
            //}else
            //{
            //    adjustedDistance = currentDistance;
            //}

         
        }
        void cameraInputs()
        {
            if (cameraState != CameraState.CameraNone)
            {
                if (!camYAdjust && (camMoveState == CameraMoveState.AlwaysAdjust || camMoveState == CameraMoveState.OnlyWhileMoving))
                {
                    camYAdjust = true;
                }
                   
                if (cameraState == CameraState.CameraRotate)
                {
                    if (!camXAdjust && camMoveState != CameraMoveState.NeverAdjust)
                    {
                        camXAdjust = true;
                    }
                        

                    if (player.steer)
                        player.steer = false;

                    currentPan += Input.GetAxis("Mouse X") * CameraSpeed;
                }
                else if (cameraState == CameraState.CameraSteer || cameraState == CameraState.CameraRun )
                {
                    if (!player.steer)
                    {
                        Vector3 playerReset = player.transform.eulerAngles;
                        playerReset.y = transform.eulerAngles.y;

                        player.transform.eulerAngles = playerReset;

                        player.steer = true;
                    }
                }
                

                currentTilt -= Input.GetAxis("Mouse Y") * CameraSpeed;
                currentTilt = Mathf.Clamp(currentTilt, -CameraMaxTilt, CameraMaxTilt);
            }else
            {
                if (player.steer)
                {
                    player.steer = false;
                }
            }

            currentDistance -= Input.GetAxis("Mouse ScrollWheel") * 2;
            currentDistance = Mathf.Clamp(currentDistance, 0, CameraMaxDistance);
        }
        void CameraYAdjust()
        {
            if (cameraState == CameraState.CameraNone)
            {
                if(camYAdjust)
                {
                    rotationYSpeed += (Time.deltaTime / 5) * cameraAdjustSpeed;

                    if(currentTilt >= yRotMax || currentTilt <= yRotMin)
                    {
                        currentTilt = Mathf.Lerp(currentTilt, yRotMax / 2, rotationYSpeed);
                    }else if (currentTilt < yRotMax && currentTilt > yRotMin)
                    {
                        camYAdjust = false;
                    }
                    else
                    {
                        if (rotationYSpeed > 0 )
                        {
                            rotationYSpeed = 0;
                        }
                    }
                }
            }
        }
        void CameraXAdjust()
        {
            if (cameraState != CameraState.CameraRotate)
            {
                if (camXAdjust)
                {
                    rotationXSpeed += Time.deltaTime * cameraAdjustSpeed;

                    if (Mathf.Abs (panAngle) > rotationXCushion)
                    {
                        currentPan = Mathf.Lerp(currentPan, currentPan + panAngle, rotationXSpeed);
                    }else
                    {
                        camXAdjust = false;
                    }

                }
                else
                {
                    if (rotationXSpeed >0)
                    {
                        rotationXSpeed = 0;
                    }
                    currentPan = player.transform.eulerAngles.y;
                }
               

            }
        }
        void CameraNeverAdjust()
        {
            switch (cameraState)
            {
                case CameraState.CameraSteer:
                case CameraState.CameraRun:
                    if (panOffset != 0)
                    {
                        panOffset = 0;
                    }

                    currentPan = player.transform.eulerAngles.y;
                   
                    break;
                case CameraState.CameraNone:
                    currentPan = player.transform.eulerAngles.y - panOffset;

                    break;

                case CameraState.CameraRotate:
                    panOffset = panAngle;
                    break;
            }
        }
        void cameraTransform()
        {

       

           

            if (cameraState == CameraState.CameraRun)
            {
                player.AutoRun = true;

                if (!autoRunReset)
                {
                    autoRunReset = true;
                }
                

            }else if (autoRunReset)
            {
                player.AutoRun = false;
                autoRunReset = false;
            }
                
            

            transform.position = player.transform.position + Vector3.up * CameraHeight;
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, currentPan, transform.eulerAngles.z);
            tilt.eulerAngles = new Vector3 (currentTilt, tilt.eulerAngles.y , tilt.eulerAngles.z);
            mainCam.transform.position = transform.position + tilt.forward * -adjustedDistance;
        }
    }
    public enum CameraState { CameraNone, CameraRotate, CameraSteer, CameraRun}

    public enum CameraMoveState { OnlyWhileMoving, OnlyHorizontalWhileMoving, AlwaysAdjust, NeverAdjust}
}
