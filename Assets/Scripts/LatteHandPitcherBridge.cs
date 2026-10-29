using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

/// <summary>
/// Optional XR Hands bridge. Attach to a scene object and assign the pitcher
/// and MilkPourController. Disabled or untracked hands never authorize pouring.
/// </summary>
public class LatteHandPitcherBridge : MonoBehaviour
{
    [SerializeField] private Transform pitcher;
    [SerializeField] private MilkPourController milkPourController;
    [SerializeField] private bool useRightHand = true;
    [SerializeField] private float pinchThreshold = 0.03f;
    [SerializeField] private float positionSmoothing = 12f;
    [SerializeField] private float rotationSmoothing = 12f;
    [SerializeField] private Vector3 localPositionOffset;
    [SerializeField] private Vector3 localEulerOffset;

    private XRHandSubsystem subsystem;
    private bool hasPose;

    private void OnEnable()
    {
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        subsystem = subsystems.Count > 0 ? subsystems[0] : null;
        hasPose = false;
        if (milkPourController != null) milkPourController.SetPinchPourAllowed(false);
    }

    private void Update()
    {
        if (milkPourController == null || pitcher == null || subsystem == null || !subsystem.running)
        {
            StopPouring();
            return;
        }

        XRHand hand = useRightHand ? subsystem.rightHand : subsystem.leftHand;
        if (!hand.isTracked)
        {
            StopPouring();
            return;
        }

        XRHandJoint palm = hand.GetJoint(XRHandJointID.Palm);
        XRHandJoint index = hand.GetJoint(XRHandJointID.IndexTip);
        XRHandJoint thumb = hand.GetJoint(XRHandJointID.ThumbTip);

        if (!palm.TryGetPose(out Pose palmPose) ||
            !index.TryGetPose(out Pose indexPose) ||
            !thumb.TryGetPose(out Pose thumbPose))
        {
            StopPouring();
            return;
        }

        // XR joint poses are tracking-space coordinates; convert through XR Origin.
        Transform trackingOrigin = transform;
        Vector3 targetPosition = trackingOrigin.TransformPoint(
            palmPose.position + palmPose.rotation * localPositionOffset);
        Quaternion targetRotation = trackingOrigin.rotation * palmPose.rotation *
            Quaternion.Euler(localEulerOffset);

        float positionT = 1f - Mathf.Exp(-positionSmoothing * Time.deltaTime);
        float rotationT = 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime);
        if (!hasPose)
        {
            pitcher.SetPositionAndRotation(targetPosition, targetRotation);
            hasPose = true;
        }
        else
        {
            pitcher.position = Vector3.Lerp(pitcher.position, targetPosition, positionT);
            pitcher.rotation = Quaternion.Slerp(pitcher.rotation, targetRotation, rotationT);
        }

        milkPourController.SetPinchPourAllowed(
            Vector3.Distance(indexPose.position, thumbPose.position) <= pinchThreshold);
    }

    private void StopPouring()
    {
        hasPose = false;
        if (milkPourController != null) milkPourController.SetPinchPourAllowed(false);
    }

    private void OnDisable()
    {
        StopPouring();
    }
}
