using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class playerController : MonoBehaviour
{
    private PlayerControls _playerControls;
    [SerializeField] private Vector2 _playerDirection;

    private Rigidbody2D _playerRB2D;
    private Animator _playerAnimator;
    public float _playerSpeed;

    private bool _isRun;

    [SerializeField] private float _playerInitialSpeed;
    [SerializeField] private float _playerRunSpeed = 12.5f;

    private InputAction _actionRun;

    [Header("Stamina System")]
    public Slider staminaSlider;
    public CanvasGroup backgroundCanvasGroup;
    public CanvasGroup fillCanvasGroup;
    public float velocidadeFadeBackground = 3f;
    public float velocidadeFadeFill = 6f;
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 20f;
    public float staminaRegenRate = 15f;

    private bool _isExhausted = false;

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
        _actionRun = _playerControls.FindAction("Run");
    }

    void Start()
    {
        _playerRB2D = GetComponent<Rigidbody2D>();
        
        _playerInitialSpeed = _playerSpeed; 
        currentStamina = maxStamina;
        
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }

        if (backgroundCanvasGroup != null) backgroundCanvasGroup.alpha = 0f;
        if (fillCanvasGroup != null) fillCanvasGroup.alpha = 0f;
    }

    void playerInput()
    {
        _playerDirection = _playerControls.Player.Move.ReadValue<Vector2>();

        int id = 0; 

        if (_playerDirection.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(_playerDirection.x) > Mathf.Abs(_playerDirection.y))
                id = _playerDirection.x > 0 ? 4 : 3;   
            else
                id = _playerDirection.y > 0 ? 1 : 2;   
        }

        _playerAnimator.SetInteger("idTransition", id);
    }

    void Update()
    {
        playerInput();
        playerRun();
        HandleStamina();
        ControlaVisibilidadeBarra();
    }

    private void FixedUpdate()
    {
        PlayerMove();
    }

    void PlayerMove()
    {
        _playerRB2D.MovePosition(_playerRB2D.position + _playerDirection * (_playerSpeed * Time.deltaTime));
    }

    void playerRun()
    {
        bool estaTentandoCorrer = _actionRun.IsPressed();
        bool estaSeMovendo = _playerDirection.sqrMagnitude > 0;

        if (estaTentandoCorrer && !_isExhausted && currentStamina > 0 && estaSeMovendo)
        {
            _isRun = true;
            _playerSpeed = _playerRunSpeed;
        }   
        else
        {
            _isRun = false;
            _playerSpeed = _playerInitialSpeed;
        }

        if (currentStamina <= 0)
        {
            _isExhausted = true;
        }
    }

    void HandleStamina()
    {
        if (_isRun) 
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
        }
        else 
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }

        if (_isExhausted && currentStamina >= maxStamina)
        {
            _isExhausted = false;
        }
    }

    void ControlaVisibilidadeBarra()
    {
        bool deveMostrar = _isRun || _isExhausted;
        float alphaAlvo = deveMostrar ? 1f : 0f;

        if (backgroundCanvasGroup != null)
        {
            backgroundCanvasGroup.alpha = Mathf.MoveTowards(backgroundCanvasGroup.alpha, alphaAlvo, Time.deltaTime * velocidadeFadeBackground);
        }

        if (fillCanvasGroup != null)
        {
            fillCanvasGroup.alpha = Mathf.MoveTowards(fillCanvasGroup.alpha, alphaAlvo, Time.deltaTime * velocidadeFadeFill);
        }
    }
}