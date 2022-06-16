using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class Movement : MonoBehaviour
{
    public string moveTriggerName = "Move";
    public string idleTriggerName = "Idle";
    public string locomotionXFloatName = "LocomotionX";
    public string locomotionYFloatName = "LocomotionY";
    public string fightTriggerName = "Fight";

    public float moveSpeed = 10;
    public float rotateSpeed = 90;
    public Transform followTransform;

    private Animator m_animator;
    private CharacterController m_characterController;

    private int mvoeHash;
    private int idleHash;
    private int locomotionXHash;
    private int locomotionYHash;
    private float m_gravity = -9.8f;

    // Start is called before the first frame update
    void Start()
    {
        m_animator = GetComponent<Animator>();
        m_characterController = GetComponent<CharacterController>();

        mvoeHash = Animator.StringToHash(moveTriggerName);
        idleHash = Animator.StringToHash(idleTriggerName);
        locomotionXHash = Animator.StringToHash(locomotionXFloatName);
        locomotionYHash = Animator.StringToHash(locomotionYFloatName);

        if (followTransform == null)
        {
            followTransform = this.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        CheckInput();
        SyncTransform();
    }
    private void SyncTransform()
    {
        followTransform.position = this.transform.position;
    }
    private void CheckInput()
    {
        var locomotionX = Input.GetAxis("Horizontal");
        var locomotionY = Input.GetAxis("Vertical");
        var deltaX = Input.GetAxis("Mouse X");
        var deltaY = Input.GetAxis("Mouse Y");
        if (Input.GetKeyDown(KeyCode.Q))
        {
            m_animator.SetTrigger(fightTriggerName);
        }
        m_animator.SetFloat(locomotionXHash, locomotionX);
        m_animator.SetFloat(locomotionYHash, locomotionY);
        var moveVec = followTransform.forward * moveSpeed * locomotionY * Time.deltaTime;
        var gravity = m_gravity * Time.deltaTime;
        if (m_characterController.isGrounded)
        {
            gravity = 0;
        }
        moveVec.y = gravity;
        m_characterController.Move(moveVec);
        followTransform.rotation *= Quaternion.Euler(0, deltaX * rotateSpeed * Time.deltaTime, 0);

        if (locomotionY > 0)
        {
            var deltaQ = Quaternion.FromToRotation(transform.forward, followTransform.forward);
            transform.rotation *= deltaQ;
        }
    }
}
