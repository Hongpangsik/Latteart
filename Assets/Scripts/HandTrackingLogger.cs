using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public class HandTrackingLogger : MonoBehaviour, ILatteInput
{
    private XRHandSubsystem handSubsystem;

    private Vector3 smoothedLeftPos;
    private Vector3 smoothedRightPos;
    private float filterFactor = 0.15f;

    private bool isLeftFirstFrame = true;
    private bool isRightFirstFrame = true;

    [Header("Gesture Settings")]
    [SerializeField] private float pinchThreshold = 0.03f; // 엄지-검지 거리 3cm 이내 시 Pinch 판정

    private LatteHandFrameData currentFrameData;
    void Start()
    {
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
            handSubsystem = subsystems[0];
    }

    void Update()
    {
        if (handSubsystem == null || !handSubsystem.running) return;
        //TrackHand(handSubsystem.leftHand, "Left"); //메타퀘스트 기본 기능 덕분에 컵 잡는 동작은 정상 작동
        TrackHand(handSubsystem.rightHand, "Right");

        /* 시작화면에서 아트 그릴 손 선택(오른손잡이 vs 왼손잡이)
         if (targetHand == Handedness.Right)
        {
            TrackHand(handSubsystem.rightHand, "Right");
        }
        else
        {
            TrackHand(handSubsystem.leftHand, "Left");
        }*/
    }

    void TrackHand(XRHand hand, string label)
    {
        if (!hand.isTracked)
        {
            currentFrameData = new LatteHandFrameData(Vector3.zero, Time.time, false, false);

            //손 추적 이탈 시 LPF 위치 및 프레임 플래그를 함께 리셋
            if (label == "Right") isRightFirstFrame = true;
            else if (label == "Left") isLeftFirstFrame = true;

            return;
        }

        // 검지 끝(IndexTip)과 엄지 끝(ThumbTip) 조인트 정보 가져오기
        var indexJoint = hand.GetJoint(XRHandJointID.IndexTip);
        var thumbJoint = hand.GetJoint(XRHandJointID.ThumbTip);

        if (indexJoint.TryGetPose(out Pose indexPose))
        {
            Vector3 pos = indexPose.position;
            Vector3 filteredPosition = pos;

            // 1. LPF 필터 좌표 보정
            if (label == "Left")
            {
                if (isLeftFirstFrame) { smoothedLeftPos = pos; isLeftFirstFrame = false; }
                else { smoothedLeftPos = Vector3.Lerp(smoothedLeftPos, pos, filterFactor); }
                filteredPosition = smoothedLeftPos;
            }
            else if (label == "Right")
            {
                if (isRightFirstFrame) { smoothedRightPos = pos; isRightFirstFrame = false; }
                else { smoothedRightPos = Vector3.Lerp(smoothedRightPos, pos, filterFactor); }
                filteredPosition = smoothedRightPos;
            }

            // 2. Pinch (우유 붓기) 제스처 계산
            bool isPinch = false;
            if (thumbJoint.TryGetPose(out Pose thumbPose))
            {
                float distance = Vector3.Distance(indexPose.position, thumbPose.position);
                isPinch = distance <= pinchThreshold;
            }

            // 3. 구조체 패킹
            currentFrameData = new LatteHandFrameData(filteredPosition, Time.time, hand.isTracked, isPinch);

            // 4. 콘솔 출력 (우유 붓기 제스처 상태 포함)
            Debug.Log($"[{label}] 시간:{currentFrameData.timestamp:F2} | 보정 좌표 X:{currentFrameData.indexTipPosition.x:F3} Y:{currentFrameData.indexTipPosition.y:F3} Z:{currentFrameData.indexTipPosition.z:F3} | Pinch:{currentFrameData.isPouring}");
        }
    }
    public LatteHandFrameData GetHandFrameData()
    {
        return currentFrameData;
    }
}