using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class HumanoidNetworkPlayer : NetworkBehaviour
{
    //First define some global variables in order to speed up the Update() function
    GameObject myXRRig;
    XRInputModalityManager HaCM;                    //what mode are we in: controller, hands, none?
    Transform myXRLH, myXRRH, myXRLC, myXRRC, myXRCam;  //positions and rotations
    Transform avHead, avLeft, avRight, avBody;          //avatars moving parts 

    float avtScale, avtHeight, avtToeYPos, avtEyeYPos;  //floats for Autotune
    bool autoTune = false;

    //For animation control
    InputAction rightTurnAction, leftMoveAction;
    Animator avtAnimator;

    //FINGERS 20240104 - get link to the Fingers controller component
    FingersToXRConnector fingersToXR;

    //WFS BUG
    bool avatarHasController;
    AvatarHasController avatarHCState;

    //GRAB 20240104
    //NetworkObject avtLeftGrabbedObject, avtRightGrabbedObject;
    GrabbedObjectController gOC;

    //FP3P 20240104 - get link to the FP3P component
    FirstThirdPersonController fP3P;

    //some fine tuning parameters if needed
    [SerializeField]
    private Vector3 avatarLeftPositionOffset, avatarRightPositionOffset;
    [SerializeField]
    private Quaternion avatarLeftRotationOffset, avatarRightRotationOffset;
    [SerializeField]
    private Vector3 avatarHeadPositionOffset;
    [SerializeField]
    private Quaternion avatarHeadRotationOffset;

    //BUGFIXES
    Vector3 avatarOldPosition;
    Vector3 cameraPOffset;
    [SerializeField] bool avatarMovesWithRotation = false;              //BUGFIX: UI setting for 2 UX options
    [SerializeField] float animationTreshold = 0.0125f;                 //BUGFIX - moving the joystick and/or real world beyond treshold will trigger animations    

    // Start is called before the first frame update
    public override void OnNetworkSpawn()
    {
        var myID = transform.GetComponent<NetworkObject>().NetworkObjectId;
        if (IsOwnedByServer)
            transform.name = "Host:" + myID;    //this must be the host
        else
            transform.name = "Client:" + myID; //this must be the client 

        //WFS BUG
        avatarHCState = transform.Find("XR IK Rig").Find("AvatarHasController").GetComponent<AvatarHasController>();

        //Auto-IK - launch before the return!
        var ikRig = transform.Find("XR IK Rig").transform.GetComponent<XR_IK_Rig>();
        ikRig.InitializeIK();

        if (!IsOwner) return;
        
        //FINGERS 20240104
        fingersToXR = GetComponent<FingersToXRConnector>();
        if (!fingersToXR) Debug.LogError("FATAL: Can't find Fingers to XR component!");
        else fingersToXR.Initialize();

        myXRRig = GameObject.Find("XR Origin (XR Rig)");
        if (myXRRig)
        {
            Debug.Log("Found XR Origin");
            //Initialize Grab Event Handler object
            myXRRig.GetComponent<XRGrabEventHandler>().avatarNetworkObjectId = myID;
            myXRRig.GetComponent<XRGrabEventHandler>().avatarObject = transform.gameObject;
        }
        else Debug.Log("Could not find XR Origin!");

        //Animation - intercept joystick status
        rightTurnAction = GameObject.Find("Turn").GetComponent<ActionBasedContinuousTurnProvider>().rightHandTurnAction.action;
        leftMoveAction = GameObject.Find("Move").GetComponent<ActionBasedContinuousMoveProvider>().leftHandMoveAction.action;
        avtAnimator = transform.GetComponent<Animator>();

        //pointers to the XR RIg
        HaCM = myXRRig.GetComponent<XRInputModalityManager>();
        myXRLC = HaCM.leftController.transform;
        myXRRC = HaCM.rightController.transform;
        myXRCam = GameObject.Find("Main Camera").transform;

        //FINGERSTOXR COMPONENT - 20240104 - Some duplication with FingersToXRConnector component as we need myXRLH/RH here too
        if (fingersToXR.handVisualizer)     
        {
            myXRLH = GameObject.Find("LeftHandDebugDrawJoints").transform.Find("Palm").transform;
            myXRRH = GameObject.Find("RightHandDebugDrawJoints").transform.Find("Palm").transform;
        } 

        //pointers to the avatar
        avLeft = transform.Find("XR IK Rig").Find("Left Arm IK").Find("Left Arm IK_target");
        avRight = transform.Find("XR IK Rig").Find("Right Arm IK").Find("Right Arm IK_target");
        avHead = transform.Find("XR IK Rig").Find("Head IK").Find("Head IK_target");
        avBody = transform;

        //GRABBED OBJECT COMPONENT - 20240104
        gOC = GetComponent<GrabbedObjectController>();
        if (!gOC) Debug.LogError("FATAL: cannot find Grabbed Object Controller component!");
        else gOC.Initialize(avLeft, avRight);

        //Vivox
        var vTog = GameObject.Find("Toggle").GetComponent<Toggle>();
        if (vTog.isOn)
        {
            GameObject.Find("Network Manager").GetComponent<VivoxPlayer>().SignIntoVivox();
        }

        //FP3P COMPONENT 20240104 - call & initialize component
        fP3P = GetComponent<FirstThirdPersonController>();
        fP3P.Initialize();
        cameraPOffset = myXRRig.transform.rotation * fP3P.FP3P_Offset(); //global coordinates
    }

    //WARNING: only works when the avatar is local to this object (ie. not for child SMRs)
    private void OnAnimatorIK(int layerIndex)
    {
        if (!IsOwner) return;
        if (!avtAnimator) return;

        if (!autoTune)
        {
            avtEyeYPos = avHead.position.y*1.14f;
            avtToeYPos = avtAnimator.GetIKPosition(AvatarIKGoal.LeftFoot).y;        //get the position of the foot from the IK system
            avtHeight = avtEyeYPos - avtToeYPos;
            avtScale = (myXRCam.position.y != 0 ? avtHeight / myXRCam.position.y : 1);
            myXRRig.transform.localScale = new Vector3(avtScale, avtScale, avtScale);   //resize the XR origin object to match the avatar
            Debug.Log("XR Origin scaled to " + avtScale);
            
            if (fingersToXR.handVisualizer) //FINGERS 20240104
            {
                //Initial set of the global position of the avatar hands
                avLeft.position = (HaCM.m_LeftInputMode == XRInputModalityManager.InputMode.MotionController ? myXRLC.position : myXRLH.position);
                avRight.position = (HaCM.m_RightInputMode == XRInputModalityManager.InputMode.MotionController ? myXRRC.position : myXRRH.position);
                fingersToXR.MapHands();
            }
            autoTune = true;
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        if (!IsOwner)    
        {
            //for remote clients, check whether hands/controllers status was changed
            if (avatarHCState.HasChanged()) //changed -> disable IK on hands
            {
                int x = avatarHCState.get();
                Debug.Log("Client with id " + transform.GetComponent<NetworkObject>().NetworkObjectId + " switched to " + (x == 0 ? "controllers" : "hands"));
                AvatarTurnOnOffHandsIK((float)x);
                avatarHCState.Reset();
            }
            //All the below code is irrelevant for remote clients so return!
            return; 
        }
        
        if (!myXRRig) return;
        if (!autoTune) return;
        if (!fingersToXR.handVisualizer) return;

        //GRAB
        if (fP3P.switchFP3P()) Debug.Log("Changed FP/3P state");

        switch (HaCM.m_LeftInputMode)
        {
            case XRInputModalityManager.InputMode.MotionController:
                if (avLeft)
                {
                    avLeft.position = myXRLC.position + cameraPOffset;
                    avLeft.rotation = myXRLC.rotation * avatarLeftRotationOffset;

                   if (!avatarHasController)
                    {
                        AvatarTurnOnOffHandsIK(0); //WFS BUG: turn OFF finger IK
                        avatarHCState.set(0);
                    }
                    avatarHasController = true;
                }
                break;

            case XRInputModalityManager.InputMode.TrackedHand:
                if (avLeft)
                {
                    avLeft.position = myXRLH.position + cameraPOffset;
                    avLeft.rotation = myXRLH.rotation * avatarLeftRotationOffset;

                    //FINGERS 20240104 - simplified call
                    fingersToXR.LinkAvtFingersToXROrigin(avLeft);

                    if (avatarHasController)
                    {
                        AvatarTurnOnOffHandsIK(1);  //WFS BUG: turn ON finger IK
                        avatarHCState.set(1);
                    }
                    avatarHasController = false;
                }
                break;

            case XRInputModalityManager.InputMode.None:
                break;
        }

        switch (HaCM.m_RightInputMode)
        {
            case XRInputModalityManager.InputMode.MotionController:
                if (avRight)
                {
                    avRight.position = myXRRC.position + cameraPOffset;
                    avRight.rotation = myXRRC.rotation * avatarRightRotationOffset;

                    float rightControllerRotateValueX = rightTurnAction.ReadValue<Vector2>()[0];
                    if (rightControllerRotateValueX == 0)
                        avtAnimator.SetBool("IsRotating", false);
                    else
                    {
                        avtAnimator.SetBool("IsRotating", true);
                        avtAnimator.SetFloat("RotationX", rightControllerRotateValueX);
                    }
                }
                break;

            case XRInputModalityManager.InputMode.TrackedHand:
                if (avRight)
                {
                    avRight.position = myXRRH.position + cameraPOffset;
                    avRight.rotation = myXRRH.rotation * avatarRightRotationOffset;

                    //FINGERS 20240104 - simplified call
                    fingersToXR.LinkAvtFingersToXROrigin(avRight);
                }
                break;

            case XRInputModalityManager.InputMode.None:
                break;
        }

        if (avHead)
        {
            avHead.rotation = myXRCam.rotation * avatarHeadRotationOffset;
        }

        if (avBody)
        {
            Vector3 avBodyDelta = new Vector3(avBody.position.x - avatarOldPosition.x, 0, avatarOldPosition.z - avBody.position.z);   //invert z coords
            avBodyDelta = avBody.rotation * avBodyDelta;

            //Trigger animations
            if (Mathf.Abs(avBodyDelta.z) > animationTreshold)
            {
                avtAnimator.SetBool("IsMoving", true);
                avtAnimator.SetFloat("DirectionY", -avBodyDelta.z * 50);      //as the oldpos-current position is fairly small, we need to magnify to make the animation explicit
            }
            else avtAnimator.SetFloat("DirectionY", 0);

            if (Mathf.Abs(avBodyDelta.x) > animationTreshold)
            {
                avtAnimator.SetBool("IsMoving", true);
                avtAnimator.SetFloat("DirectionX", avBodyDelta.x * 50);
            }
            else avtAnimator.SetFloat("DirectionX", 0);

            if (Mathf.Abs(avBodyDelta.x) + Mathf.Abs(avBodyDelta.z) < animationTreshold) avtAnimator.SetBool("IsMoving", false);

            //Add animations when we move the avatar because of head movement
            avatarOldPosition = avBody.position;

            //FP3P 20240104: TWO UX OPTIONS
            //  OPTION 1: avatarposition moves when you rotate your head
            //  OPTION 2: avatarposition does not when you rotate your head, the avatarbody just rotates with you
            cameraPOffset = (avatarMovesWithRotation ? avBody.rotation : myXRRig.transform.rotation) * fP3P.FP3P_Offset();

            //BUGFIX: Move avatar both with Left Controller and real world movements 
            avBody.position = new Vector3(myXRCam.position.x, myXRRig.transform.position.y, myXRCam.position.z) + cameraPOffset;

            //Rotate the body with the head horizontal angle - can't use Eulerangles as they are glitchy!
            Vector3 forward = avHead.forward;
            forward.y = 0;
            avBody.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }

    //MOVE TO FUTURE XR IK RIG SCRIPT!
    void AvatarTurnOnOffHandsIK(float val)  //note this is required for both owners and remote clients!
    {
        foreach (var x in transform.Find("XR IK Rig").GetComponentsInChildren<ChainIKConstraint>()) x.weight = val;
    }

   

}