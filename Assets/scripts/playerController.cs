using UnityEngine;

public class playerController : MonoBehaviour
{
    
    private PlayerControls                       _playerControls;
    [SerializeField] private Vector2             _playerDirection;

    private Rigidbody2D                          _playerRB2D;
    private Animator                             _playerAnimator;
    public float                                 _playerSpeed;

    private void OnEnable()
    {
        _playerControls.Enable();
    }

    private void OnDisable()
    {
        _playerControls.Disable();
    }

    void Awake()
    {
        _playerControls = new PlayerControls();
        _playerAnimator = GetComponent<Animator>();
    }

    void playerInput()
    {
        _playerDirection = _playerControls.Player.Move.ReadValue<Vector2>();

        _playerAnimator.SetInteger("idTransition",1);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerRB2D = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        playerInput();
    }

    private void FixedUpdate()
    {
        PlayerMove();
    }

    void PlayerMove()
    {
        _playerRB2D.MovePosition(_playerRB2D.position + _playerDirection * (_playerSpeed * Time.deltaTime));
    }

}
