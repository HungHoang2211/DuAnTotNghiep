using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
// Đã sửa tên Class thành PhatplayerMove để khớp với tên file
public class PhatplayerMove : MonoBehaviour
{
    [Header("Tốc độ di chuyển")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float sneakSpeed = 1.5f;
    public float rotationSpeed = 10f;

    private Animator _animator;
    private CharacterController _characterController;

    private int _paramMoveSpeed;
    private int _paramMoveMode;

    // Các biến Parameter Animator
    private int _paramActionUseContainer;
    private int _paramActionStop;
    private int _paramActionAttackMeleeFists;
    private int _paramActionIndex;
    private int _paramActionGatherHands;

    private float _gravity = -9.81f;
    private float _velocityY;

    // Các cờ trạng thái khóa di chuyển
    private bool _isSneakingToggle = false;
    private bool _isLooting = false;
    private bool _isAttacking = false;
    private bool _isPickingUp = false;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _characterController = GetComponent<CharacterController>();

        _paramMoveSpeed = Animator.StringToHash("MoveSpeed");
        _paramMoveMode = Animator.StringToHash("MoveMode");
        _paramActionUseContainer = Animator.StringToHash("ActionUseContainer");
        _paramActionStop = Animator.StringToHash("ActionStop");
        _paramActionAttackMeleeFists = Animator.StringToHash("ActionAttackMeleeFists");
        _paramActionIndex = Animator.StringToHash("ActionIndex");
        _paramActionGatherHands = Animator.StringToHash("ActionGatherHands");
    }

    void Update()
    {
        // ----------------------------------------------------
        // 1. NHẶT ĐỒ TRONG RƯƠNG (F)
        // ----------------------------------------------------
        if (Input.GetKeyDown(KeyCode.F) && !_isAttacking && !_isPickingUp)
        {
            _isLooting = !_isLooting;
            if (_isLooting)
            {
                _animator.SetBool(_paramActionStop, false);
                _animator.SetTrigger(_paramActionUseContainer);
            }
            else
            {
                _animator.SetBool(_paramActionStop, true);
            }
        }

        // ----------------------------------------------------
        // 2. NHẶT VẬT PHẨM DƯỚI ĐẤT (E = Nhẹ, R = Nặng)
        // ----------------------------------------------------
        if (!_isLooting && !_isAttacking && !_isPickingUp)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                StartCoroutine(PickupRoutine(false));
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                StartCoroutine(PickupRoutine(true));
            }
        }

        // ----------------------------------------------------
        // 3. TẤN CÔNG (ĐÈ CHUỘT TRÁI ĐỂ ĐÁNH LIÊN TỤC)
        // ----------------------------------------------------
        // Đã sửa thành GetMouseButton (đè chuột) thay vì GetMouseButtonDown (click 1 lần)
        if (Input.GetMouseButton(0) && !_isLooting && !_isAttacking && !_isPickingUp)
        {
            StartCoroutine(AttackRoutine());
        }

        // ----------------------------------------------------
        // 4. LÉN LÚT (C)
        // ----------------------------------------------------
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            _isSneakingToggle = !_isSneakingToggle;
        }

        // ----------------------------------------------------
        // 5. DI CHUYỂN & ANIMATOR
        // ----------------------------------------------------
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (!_isPickingUp)
        {
            int currentMoveMode = _isSneakingToggle ? 4 : 0;
            _animator.SetInteger(_paramMoveMode, currentMoveMode);
        }

        float currentMoveSpeed = 0f;
        float animSpeed = 0f;

        if (!_isLooting && !_isAttacking && !_isPickingUp)
        {
            if (direction.magnitude >= 0.1f)
            {
                if (_isSneakingToggle)
                {
                    currentMoveSpeed = sneakSpeed;
                    animSpeed = 0.5f;
                }
                else if (isRunning)
                {
                    currentMoveSpeed = runSpeed;
                    animSpeed = 1f;
                }
                else
                {
                    currentMoveSpeed = walkSpeed;
                    animSpeed = 0.5f;
                }

                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        _animator.SetFloat(_paramMoveSpeed, animSpeed);

        // ----------------------------------------------------
        // 6. DI CHUYỂN VẬT LÝ
        // ----------------------------------------------------
        Vector3 moveVelocity = direction * currentMoveSpeed;

        if (_characterController.isGrounded && _velocityY < 0)
        {
            _velocityY = -2f;
        }
        _velocityY += _gravity * Time.deltaTime;
        moveVelocity.y = _velocityY;

        _characterController.Move(moveVelocity * Time.deltaTime);
    }

    // ----------------------------------------------------
    // COROUTINES XỬ LÝ HÀNH ĐỘNG
    // ----------------------------------------------------
    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _animator.SetBool(_paramActionStop, false);

        int randomAttack = Random.Range(0, 4);
        _animator.SetFloat(_paramActionIndex, (float)randomAttack);
        _animator.SetTrigger(_paramActionAttackMeleeFists);

        // Chờ đòn đánh thực hiện xong (thời gian đã giảm xuống 0.6f cho khớp với tốc độ đánh nhanh)
        yield return new WaitForSeconds(0.6f);

        // KIỂM TRA: NẾU VẪN CÒN ĐÈ CHUỘT THÌ CHO PHÉP ĐÁNH TIẾP NGAY LẬP TỨC
        if (Input.GetMouseButton(0))
        {
            _isAttacking = false;
        }
        else
        {
            // Nếu đã nhả chuột: Bật cờ ActionStop để Animator thu tay về
            _animator.SetBool(_paramActionStop, true);
            _isAttacking = false;
        }
    }

    private IEnumerator PickupRoutine(bool isHeavy)
    {
        _isPickingUp = true;

        int pickupMode = isHeavy ? 3 : 0;
        _animator.SetInteger(_paramMoveMode, pickupMode);
        _animator.SetTrigger(_paramActionGatherHands);

        yield return new WaitForSeconds(1.5f);

        _animator.SetInteger(_paramMoveMode, 0);
        _isPickingUp = false;
    }

    public void PlaySoundStep() { }
}