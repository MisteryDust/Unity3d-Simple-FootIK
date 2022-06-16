using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class IKSetting : MonoBehaviour
{
    public bool enableFeetIk = true; //是否开启ik
    [Range(0, 2)] [SerializeField] private float heightFromGroundRaycast = 1.2f; //从地面向上的cast距离
    [Range(0, 2)] [SerializeField] private float raycastDownDistance = 1.5f; //向下cast 距离
    [SerializeField] private LayerMask environmentLayer; //检测layer
    [SerializeField] private float pelvisOffset = 0f; //盆骨offset
    [Range(0, 1)] [SerializeField] private float pelvisUpAndDownSpeed = 0.28f; //盆骨赋值速度
    [Range(0, 1)] [SerializeField] private float feetToIkPositionSpeed = 0.5f; //足IK赋值速度
    public string leftFootAnimCurveName = "LeftFoot"; //权重曲线名称
    public string rightFootAnimCurveName = "RightFoot"; //权重曲线名称
    [Range(0, 100)] public float leftFootAngleOffset; //旋转偏移
    [Range(0, 100)] public float rightFootAngleOffset; //旋转偏移
    public bool useIkFeature = false; //是否使用IK旋转

    public bool showSolverDebug = true;// Debug绘制

    private Animator m_animator; //动画机

    private Vector3 _rightFootPosition, _leftFootPosition; //足部骨骼posiition
    private Vector3 _rightFootIkPosition, _leftFootIkPosition; //足部IK position
    private Quaternion _leftFootIkRotation, _rightFootIkRotation; //足部IK rotation
    private float _lastPelvisPositionY, _lastRightFootPositionY, _lastLeftFootPositionY; //上帧信息，用于lerp动画

    #region for Gizmos
    private Vector3 rightHitPoint;
    private Vector3 leftHitPoint;
    private bool flip = false;
    #endregion

    private void Start()
    {
        m_animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        if (!enableFeetIk) return;
        if (!m_animator) return;

        AdjustFeetTarget(ref _rightFootPosition, HumanBodyBones.RightFoot); //设置 足部骨骼posiition
        AdjustFeetTarget(ref _leftFootPosition, HumanBodyBones.LeftFoot); // 设置 足部骨骼posiition

        FootPositionSolver(_rightFootPosition, ref _rightFootIkPosition, ref _rightFootIkRotation, rightFootAngleOffset); //IK 解算
        FootPositionSolver(_leftFootPosition, ref _leftFootIkPosition, ref _leftFootIkRotation, leftFootAngleOffset);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!enableFeetIk) return;
        if (!m_animator) return;

        MovePelvisHeight(); //骨盆偏移

        m_animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, m_animator.GetFloat(rightFootAnimCurveName)); //设置pos 权重
        if (useIkFeature)
        {
            m_animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, m_animator.GetFloat(rightFootAnimCurveName)); //设置 rot 权重
        }
        MoveFeetToIkPoint(AvatarIKGoal.RightFoot, _rightFootIkPosition, _rightFootIkRotation, ref _lastRightFootPositionY); //设置ik goal坐标

        m_animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, m_animator.GetFloat(leftFootAnimCurveName));
        if (useIkFeature)
        {
            m_animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, m_animator.GetFloat(leftFootAnimCurveName));
        }
        MoveFeetToIkPoint(AvatarIKGoal.LeftFoot, _leftFootIkPosition, _leftFootIkRotation, ref _lastLeftFootPositionY);
    }

    void MoveFeetToIkPoint(AvatarIKGoal foot, Vector3 positionIkHolder, Quaternion rotationIkHolder, ref float lastFootPositionY)
    {
        Vector3 targetIkPosition = m_animator.GetIKPosition(foot); //获取animator IK Goal 的 原本 pos

        if (positionIkHolder != Vector3.zero) //如果新的IK pos 不为 0 
        {
            targetIkPosition = transform.InverseTransformPoint(targetIkPosition); //把原本的ik goal 的pos转换到本地坐标系
            positionIkHolder = transform.InverseTransformPoint(positionIkHolder); //把现在的ik goal 的pos转到本地坐标系

            float yVar = Mathf.Lerp(lastFootPositionY, positionIkHolder.y, feetToIkPositionSpeed); //进行插值
            targetIkPosition.y += yVar;
            lastFootPositionY = yVar;

            targetIkPosition = transform.TransformPoint(targetIkPosition); //把新的ik goal pos转到世界坐标系

            m_animator.SetIKRotation(foot, rotationIkHolder); //旋转赋予
        }
        m_animator.SetIKPosition(foot, targetIkPosition); //位置赋予
    }

    void MovePelvisHeight() //调整pelvis，保证IK 能达到（比如左右脚高度差那种）
    {
        if (_rightFootIkPosition == Vector3.zero || _leftFootIkPosition == Vector3.zero || _lastPelvisPositionY == 0f)
        {
            _lastPelvisPositionY = m_animator.bodyPosition.y;
            return;
        }

        float lOffsetPosition = _leftFootIkPosition.y - transform.position.y; //左脚ik pos与当前transform的高度差
        float rOffsetPosition = _rightFootIkPosition.y - transform.position.y; //右脚ik pos 与当前transform的高度差

        //选择较小值（在以vector3.up为正轴的情况下）
        //如果是正值，则向上偏移距离较小的。
        //如果是负值，则向下偏移距离较大的。
        float totalOffset = (lOffsetPosition < rOffsetPosition) ? lOffsetPosition : rOffsetPosition;

        Vector3 newPelvisPosition = m_animator.bodyPosition + Vector3.up * totalOffset; //新的骨盆位置计算： 原位置+ up方向 * offset。
        newPelvisPosition.y = Mathf.Lerp(_lastPelvisPositionY, newPelvisPosition.y, pelvisUpAndDownSpeed); //插值动画
        m_animator.bodyPosition = newPelvisPosition; //赋值
        _lastPelvisPositionY = m_animator.bodyPosition.y; //记录信息
    }

    void FootPositionSolver(Vector3 fromSkyPosition, ref Vector3 feetIkPosition, ref Quaternion feetIkRotation, float angleOffset)
    {
        if (showSolverDebug)
            Debug.DrawLine(fromSkyPosition, fromSkyPosition + Vector3.down * (raycastDownDistance + heightFromGroundRaycast), Color.green);

        if (Physics.Raycast(fromSkyPosition, Vector3.down, out var feetOutHit, raycastDownDistance + heightFromGroundRaycast, environmentLayer))
        {
            feetIkPosition = fromSkyPosition; //保存x,z值。
            feetIkPosition.y = feetOutHit.point.y + pelvisOffset; //hit pos 的 Y 赋值

            feetIkRotation = Quaternion.FromToRotation(Vector3.up, feetOutHit.normal) * transform.rotation; //计算法向偏移
            feetIkRotation = Quaternion.AngleAxis(angleOffset, Vector3.up) * feetIkRotation; //计算额外的偏移

            return;
        }
        feetIkPosition = Vector3.zero; //没有hit，归零
    }

    void AdjustFeetTarget(ref Vector3 feetPosition, HumanBodyBones foot)
    {
        feetPosition = m_animator.GetBoneTransform(foot).position; //获取人形足部的transform position
        feetPosition.y = transform.position.y + heightFromGroundRaycast; //y的值会加上【向上检测的距离】，主要是防止卡模型。
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(rightHitPoint, 0.01f);
        Gizmos.DrawSphere(leftHitPoint, 0.01f);
    }
}
