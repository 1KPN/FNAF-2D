using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private enum EstadoJogo { EmAndamento, Jumpscare, GameOver, ProximaNoite, JogoConcluido }

    private const int TotalNoites = 5;
    private const string NoiteSalvaKey = "FNAF2D.HighestNightUnlocked";
    private static int _noiteDaSessao = 1;
    private static bool _ignorarMenuNaProximaCarga;

    public static GameManager Instance;

    [Header("Painéis")]
    public GameObject painelGameOver;
    public GameObject painelVitoria;
    [SerializeField] private GameObject painelZerado;

    [Header("Animatrônicos da cena")]
    public GameObject freddy;
    public GameObject bonnie;
    public GameObject chica;
    public GameObject foxy;

    [Header("Menu")]
    public string cenaMenu = "Menu";

    [Header("Jumpscare (opcional)")]
    public GameObject visualJumpscare;
    public Image imagemJumpscare;
    public Animator animacaoJumpscare;
    public string estadoAnimacaoJumpscare;
    public AudioSource audioJumpscare;
    public float duracaoJumpscare = 1.5f;

    [Header("Audio Sources")]
    public AudioSource audioAmbiente;
    public AudioSource audioMusica;
    public AudioSource audioUI;
    public AudioSource audioGameplay;

    [Header("Audio Clips (opcionais)")]
    public AudioClip clipAmbiente;
    public AudioClip clipMusica;
    public AudioClip clipUI;
    public AudioClip clipPorta;
    public AudioClip clipLuz;
    public AudioClip clipPassosAnimatronico;
    public AudioClip clipAlertaAnimatronico;
    public AudioClip clipPerseguicaoAnimatronico;
    public AudioClip clipFusivel;
    public AudioClip clipGerador;
    public AudioClip clipVitoria;
    public AudioClip clipGameOver;
    public AudioClip clipJumpscare;
    public AudioClip clipZeramento;
    public AudioClip clipSinalNoiteConcluida;
    public AudioClip clipMultidaoNoiteConcluida;
    public AudioClip clipPassosPlayer;
    public AudioClip clipCorridaPlayer;
    [SerializeField] private AudioClip clipAberturaNoite;
    [SerializeField] private AudioClip[] clipsEventosNoturnos = new AudioClip[7];
    [SerializeField] private AudioClip[] clipsPortasMadeira = new AudioClip[3];

    [Header("Transições visuais")]
    [SerializeField] private Sprite spriteSangue;
    [SerializeField] private Image imagemTintVermelha;
    [SerializeField] private Image imagemSangue;
    [SerializeField] private Image imagemFade;
    [SerializeField] private TextMeshProUGUI textoHorarioTransicao;
    [SerializeField] private RectTransform janelaDigitoHorario;
    [SerializeField] private RectTransform digitoCinco;
    [SerializeField] private RectTransform digitoSeis;
    [SerializeField, Range(0f, 1f)] private float opacidadeSangue = 0.75f;
    [SerializeField, Range(0f, 1f)] private float intensidadeVermelha = 0.25f;
    [SerializeField, Min(0.01f)] private float duracaoFadeJumpscare = 0.16f;
    [SerializeField, Min(0.01f)] private float duracaoFadeVitoria = 0.3f;
    [SerializeField, Min(0f)] private float duracaoAnimacaoVitoria = 7f;
    [SerializeField, Min(0f)] private float duracaoEsperaAntesPainelVitoria = 12f;
    [SerializeField, Min(0f)] private float duracaoFadePainelVitoria = 0.8f;

    [Header("Pause e abertura da noite")]
    [SerializeField] private GameObject painelPause;
    [SerializeField] private GameObject painelAberturaNoite;
    [SerializeField] private TextMeshProUGUI textoAberturaNoite;
    [SerializeField, Min(0f)] private float duracaoAberturaNoite = 4f;
    [SerializeField, Min(0.01f)] private float duracaoFadeAberturaNoite = 0.4f;
    [SerializeField] private Button botaoContinuarPause;
    [SerializeField] private Button botaoReiniciarPause;
    [SerializeField] private Button botaoMenuPause;
    [SerializeField] private Button botaoSairPause;

    [Header("Fonte da interface")]
    [SerializeField] private TMP_FontAsset fonteFnaf;
    [SerializeField] private Material materialFonteFnaf;

    [Header("HUD dos fusíveis")]
    [SerializeField] private Image[] iconesFusivelHUD = new Image[7];
    [SerializeField] private Color corFusivelApagado = new Color(0.32f, 0.32f, 0.32f, 1f);
    [SerializeField] private Color corFusivelAceso = Color.white;

    [Header("Progressão")]
    public int fusiveisNecessarios = 5;
    [SerializeField] private float aumentoDificuldadePorNoite = 0.15f;
    [SerializeField] private Button botaoVoltarAoMenu;

    [Header("HUD da cena Gameplay")]
    public TextMeshProUGUI textoProgresso;

    private EstadoJogo _estado = EstadoJogo.EmAndamento;
    private int _noiteAtual;
    private bool _transicaoPendente;
    private bool _pausado;
    private bool _noiteEmAbertura;
    private float _timeScaleAntesDePausar = 1f;
    private bool _audioListenerPausadoAntes;
    private readonly Dictionary<AudioSource, float> _ultimoAudioPorFonte = new Dictionary<AudioSource, float>();
    private static GameObject _audioConclusaoPersistente;
    private static AudioSource _audioSinalNoitePersistente;
    private static AudioSource _audioMultidaoNoitePersistente;
    private AudioSource _audioEventosNoturnos;
    private CanvasGroup _grupoPainelVitoria;
    private GameObject _textoPainelVitoria;
    private GameObject _botaoPainelVitoria;
    private float _alturaDigitoHorario;
    private Vector2 _posicaoInicialDigitoCinco;
    private Vector2 _posicaoInicialDigitoSeis;

    public int NoiteAtual => _noiteAtual;
    public float MultiplicadorDificuldade => 1f + (_noiteAtual - 1) * aumentoDificuldadePorNoite;
    public bool JogoEncerrado => _estado != EstadoJogo.EmAndamento;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = mainMenuUI.IsMenuOpen ? 0f : 1f;
        PrepararAudioSources();
        _noiteAtual = Mathf.Clamp(_noiteDaSessao, 1, TotalNoites);
        ConfigurarNoite();
    }

    void Start()
    {
        if (mainMenuUI.IsMenuOpen)
        {
            Time.timeScale = 0f;
            return;
        }

        Time.timeScale = 1f;
        if (painelGameOver != null) painelGameOver.SetActive(false);
        if (painelVitoria != null) painelVitoria.SetActive(false);
        if (painelZerado != null) painelZerado.SetActive(false);
        if (painelPause != null) painelPause.SetActive(false);
        if (painelAberturaNoite != null) painelAberturaNoite.SetActive(false);
        if (botaoVoltarAoMenu != null) botaoVoltarAoMenu.gameObject.SetActive(false);
        AtualizarTextoBotaoGameOver();
        if (visualJumpscare != null) visualJumpscare.SetActive(false);
        CriarCamadaTransicoes();
        PrepararAnimacaoHorario();
        if (botaoVoltarAoMenu != null && botaoVoltarAoMenu.onClick.GetPersistentEventCount() == 0)
            botaoVoltarAoMenu.onClick.AddListener(VoltarAoMenu);
        ConfigurarAudioPortas();

        if (textoProgresso == null)
            Debug.LogError("Configure o HUD da noite na cena Gameplay e vincule seu texto ao GameManager.", this);
        if (iconesFusivelHUD == null || iconesFusivelHUD.Length < fusiveisNecessarios)
            Debug.LogError("Vincule um Image por fusível da noite ao HUD da cena Gameplay.", this);
        else
        {
            for (int i = 0; i < fusiveisNecessarios; i++)
            {
                if (iconesFusivelHUD[i] == null)
                    Debug.LogError($"Vincule o ícone de fusível {i + 1} ao GameManager.", this);
            }
        }

        AtualizarHud();
        Time.timeScale = 0f;
        _noiteEmAbertura = true;
        StartCoroutine(IniciarNoiteAposAbertura());
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            AlternarPause();
    }

    public void AlternarPause()
    {
        if (_estado != EstadoJogo.EmAndamento || _noiteEmAbertura || _transicaoPendente)
            return;

        if (_pausado)
            ContinuarJogo();
        else
            PausarJogo();
    }

    public void PausarJogo()
    {
        if (_estado != EstadoJogo.EmAndamento || _noiteEmAbertura || _transicaoPendente || _pausado)
            return;

        _timeScaleAntesDePausar = Time.timeScale;
        _audioListenerPausadoAntes = AudioListener.pause;
        _pausado = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (painelPause != null)
            painelPause.SetActive(true);
        TocarSomUI();
    }

    public void ContinuarJogo()
    {
        if (!_pausado)
            return;

        if (painelPause != null)
            painelPause.SetActive(false);
        _pausado = false;
        AudioListener.pause = _audioListenerPausadoAntes;
        Time.timeScale = _timeScaleAntesDePausar;
        TocarSomUI();
    }

    public void SairDoJogo()
    {
        if (_pausado)
        {
            if (painelPause != null)
                painelPause.SetActive(false);
            _pausado = false;
            AudioListener.pause = _audioListenerPausadoAntes;
            Time.timeScale = _timeScaleAntesDePausar;
        }

        TocarSomUI();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDestroy()
    {
        if (_pausado || _noiteEmAbertura)
        {
            AudioListener.pause = false;
            Time.timeScale = 1f;
        }

        PararAudio(audioAmbiente);
        PararAudio(audioMusica);
        PararAudio(audioUI);
        PararAudio(audioGameplay);
        PararAudio(audioJumpscare);
        PararAudio(_audioEventosNoturnos);
        if (Instance == this)
            Instance = null;
    }

    void PrepararAudioSources()
    {
        audioAmbiente = PrepararAudioSource(audioAmbiente);
        audioMusica = PrepararAudioSource(audioMusica);
        audioUI = PrepararAudioSource(audioUI);
        audioGameplay = PrepararAudioSource(audioGameplay);
        audioJumpscare = PrepararAudioSource(audioJumpscare);
        _audioEventosNoturnos = PrepararAudioSource(_audioEventosNoturnos);

        audioAmbiente.volume = 0.3f;
        audioMusica.volume = 0.3f;
        audioUI.volume = 0.65f;
        audioGameplay.volume = 0.45f;
        audioJumpscare.volume = 0.9f;
        _audioEventosNoturnos.volume = 0.35f;
        audioUI.ignoreListenerPause = true;
    }

    AudioSource PrepararAudioSource(AudioSource source)
    {
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.Stop();
        return source;
    }

    void TocarLoop(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null)
            return;

        if (source.clip != clip)
        {
            source.Stop();
            source.clip = clip;
        }

        source.loop = true;
        if (!source.isPlaying)
            source.Play();
    }

    void TocarOneShot(AudioSource source, AudioClip clip, float intervaloMinimo = 0.08f)
    {
        if (source == null || clip == null)
            return;

        float agora = Time.unscaledTime;
        if (_ultimoAudioPorFonte.TryGetValue(source, out float ultimoAudio)
            && agora - ultimoAudio < Mathf.Max(intervaloMinimo, clip.length))
            return;

        _ultimoAudioPorFonte[source] = agora;
        source.PlayOneShot(clip);
    }

    void PararAudio(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();
        source.clip = null;
    }

    void PararAudiosDeFundo()
    {
        PararAudio(audioAmbiente);
        PararAudio(audioMusica);
        PararAudio(audioGameplay);

        foreach (animatronicHibrid animatronico in FindObjectsByType<animatronicHibrid>())
            animatronico.PararAudio();
        foreach (AnimatronicoIA animatronico in FindObjectsByType<AnimatronicoIA>())
            animatronico.PararAudio();
    }

    void CriarCamadaTransicoes()
    {
        if (imagemFade == null || imagemTintVermelha == null || imagemSangue == null
            || textoHorarioTransicao == null)
        {
            Debug.LogError("Vincule Fade, Tint Vermelho, Sangue e Texto da Transição na cena Gameplay.", this);
        }

        if (imagemFade != null)
        {
            imagemFade.color = new Color(0f, 0f, 0f, 1f);
            imagemFade.gameObject.SetActive(true);
        }

        if (spriteSangue == null)
            Debug.LogError("Configure o sprite de sangue no GameManager para exibir o efeito de jumpscare.", this);
        if (imagemSangue != null)
            imagemSangue.sprite = spriteSangue;
        if (imagemTintVermelha != null)
            imagemTintVermelha.gameObject.SetActive(false);
        if (imagemSangue != null)
            imagemSangue.gameObject.SetActive(false);
        if (textoHorarioTransicao != null)
            textoHorarioTransicao.gameObject.SetActive(false);
    }

    IEnumerator IniciarNoiteAposAbertura()
    {
        if (textoAberturaNoite != null)
        {
            AplicarFonteFnaf(textoAberturaNoite);
            textoAberturaNoite.text = $"12:00\nNoite {_noiteAtual}";
        }
        else
        {
            Debug.LogError("Vincule o texto '12:00 / Noite' do painel Abertura da Noite.", this);
        }

        if (clipAberturaNoite != null)
        {
            TocarOneShot(audioUI, clipAberturaNoite);
            yield return new WaitForSecondsRealtime(Mathf.Min(clipAberturaNoite.length, 0.12f));
        }
        else
            Debug.LogError("Configure blip3.wav como clip de abertura da noite.", this);

        if (painelAberturaNoite != null)
            painelAberturaNoite.SetActive(true);

        yield return FadePara(0f, duracaoFadeAberturaNoite);
        yield return new WaitForSecondsRealtime(duracaoAberturaNoite);
        yield return FadePara(1f, duracaoFadeAberturaNoite);

        if (painelAberturaNoite != null)
            painelAberturaNoite.SetActive(false);

        if (clipAberturaNoite != null)
            TocarOneShot(audioUI, clipAberturaNoite);
        yield return FadePara(0f, duracaoFadeAberturaNoite);
        _noiteEmAbertura = false;
        Time.timeScale = 1f;
        TocarLoop(audioAmbiente, clipAmbiente);
        TocarLoop(audioMusica, clipMusica);
        StartCoroutine(TocarAudiosNoturnos());
    }

    void ConfigurarAudioPortas()
    {
        if (clipsPortasMadeira == null || clipsPortasMadeira.Length == 0)
        {
            Debug.LogError("Configure door2a.wav, door2b.wav e door2c.wav como sons das portas de madeira.", this);
            return;
        }

        foreach (GameObject objeto in FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (!objeto.name.StartsWith("door", System.StringComparison.OrdinalIgnoreCase)
                || objeto.name.Equals("door_1 (cozinha)", System.StringComparison.OrdinalIgnoreCase))
                continue;

            DoorAudioRandomizer audioPorta = objeto.GetComponent<DoorAudioRandomizer>();
            if (audioPorta == null)
                audioPorta = objeto.AddComponent<DoorAudioRandomizer>();
            audioPorta.Configurar(clipsPortasMadeira);
        }
    }

    IEnumerator TocarAudiosNoturnos()
    {
        while (_estado == EstadoJogo.EmAndamento)
        {
            yield return new WaitForSeconds(Random.Range(18f, 36f));
            if (_estado != EstadoJogo.EmAndamento)
                yield break;

            if (Random.value > 0.45f || _audioEventosNoturnos == null)
                continue;

            if (clipsEventosNoturnos == null || clipsEventosNoturnos.Length == 0)
            {
                Debug.LogError("Configure os áudios ambiente opcionais da noite.", this);
                yield break;
            }

            AudioClip clip = clipsEventosNoturnos[Random.Range(0, clipsEventosNoturnos.Length)];
            if (clip == null)
            {
                Debug.LogError("Há um áudio noturno não configurado no GameManager.", this);
                continue;
            }

            _audioEventosNoturnos.clip = clip;
            _audioEventosNoturnos.loop = false;
            _audioEventosNoturnos.Play();
            yield return new WaitForSeconds(clip.length);
        }
    }

    void PrepararVisualJumpscare()
    {
        if (imagemTintVermelha != null)
        {
            imagemTintVermelha.color = new Color(1f, 0f, 0f, intensidadeVermelha);
            imagemTintVermelha.gameObject.SetActive(true);
        }

        if (imagemSangue != null)
        {
            imagemSangue.sprite = spriteSangue;
            imagemSangue.color = new Color(1f, 1f, 1f, opacidadeSangue);
            imagemSangue.gameObject.SetActive(true);
        }
    }

    void LimparVisualTransicao()
    {
        if (imagemTintVermelha != null)
            imagemTintVermelha.gameObject.SetActive(false);
        if (imagemSangue != null)
            imagemSangue.gameObject.SetActive(false);
    }

    IEnumerator FadePara(float alphaDestino, float duracao)
    {
        if (imagemFade == null)
            yield break;

        imagemFade.gameObject.SetActive(true);
        float alphaInicial = imagemFade.color.a;
        float tempo = 0f;
        float duracaoSegura = Mathf.Max(0.01f, duracao);

        while (tempo < duracaoSegura)
        {
            tempo += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(alphaInicial, alphaDestino, Mathf.Clamp01(tempo / duracaoSegura));
            imagemFade.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        imagemFade.color = new Color(0f, 0f, 0f, alphaDestino);
    }

    IEnumerator MostrarVitoriaAposFade()
    {
        float inicioSequencia = Time.unscaledTime;
        bool animacaoHorarioConfigurada = janelaDigitoHorario != null
            && digitoCinco != null && digitoSeis != null;
        OcultarHudGameplay();

        if (painelVitoria != null)
        {
            PrepararPainelVitoria();
            painelVitoria.SetActive(true);
            DefinirConteudoPainelVitoria(false);
        }
        if (animacaoHorarioConfigurada)
            janelaDigitoHorario.gameObject.SetActive(true);
        if (textoHorarioTransicao != null)
            textoHorarioTransicao.gameObject.SetActive(true);

        yield return FadePara(1f, duracaoFadeVitoria);
        if (animacaoHorarioConfigurada)
            yield return AnimarViradaHorario();
        else if (textoHorarioTransicao != null)
            yield return new WaitForSecondsRealtime(0.3f);

        float tempoRestante = duracaoEsperaAntesPainelVitoria - (Time.unscaledTime - inicioSequencia);
        if (tempoRestante > 0f)
            yield return new WaitForSecondsRealtime(tempoRestante);

        if (textoHorarioTransicao != null)
            textoHorarioTransicao.gameObject.SetActive(false);
        if (animacaoHorarioConfigurada)
            janelaDigitoHorario.gameObject.SetActive(false);

        if (_estado == EstadoJogo.JogoConcluido)
        {
            if (painelVitoria != null)
                painelVitoria.SetActive(false);
            DefinirConteudoPainelVitoria(true);
            if (painelZerado == null)
                Debug.LogError("Vincule o painelZerado com end.png na cena Gameplay.", this);
            else
                painelZerado.SetActive(true);
            if (botaoVoltarAoMenu != null)
                botaoVoltarAoMenu.gameObject.SetActive(true);
        }
        else if (painelVitoria != null)
        {
            DefinirConteudoPainelVitoria(true);
            _grupoPainelVitoria.alpha = 0f;
            _grupoPainelVitoria.interactable = false;
            _grupoPainelVitoria.blocksRaycasts = false;
            StartCoroutine(FadeGrupoPainelVitoria());
        }

        yield return FadePara(0f, duracaoFadeVitoria);
    }

    void PrepararPainelVitoria()
    {
        if (_grupoPainelVitoria == null)
            _grupoPainelVitoria = painelVitoria.GetComponent<CanvasGroup>();
        if (_grupoPainelVitoria == null)
            _grupoPainelVitoria = painelVitoria.AddComponent<CanvasGroup>();
        _grupoPainelVitoria.alpha = 1f;
        _grupoPainelVitoria.interactable = false;
        _grupoPainelVitoria.blocksRaycasts = false;

        Transform texto = painelVitoria.transform.Find("Text");
        Transform botao = painelVitoria.transform.Find("Button");
        if (texto == null || botao == null)
        {
            Debug.LogError("O painelVitória precisa conter os objetos Text e Button.", painelVitoria);
            return;
        }

        _textoPainelVitoria = texto.gameObject;
        _botaoPainelVitoria = botao.gameObject;
    }

    void DefinirConteudoPainelVitoria(bool ativo)
    {
        if (_textoPainelVitoria != null)
            _textoPainelVitoria.SetActive(ativo);
        if (_botaoPainelVitoria != null)
            _botaoPainelVitoria.SetActive(ativo);
    }

    IEnumerator FadeGrupoPainelVitoria()
    {
        float tempo = 0f;
        float duracao = Mathf.Max(0.01f, duracaoFadePainelVitoria);
        while (tempo < duracao)
        {
            tempo += Time.unscaledDeltaTime;
            _grupoPainelVitoria.alpha = Mathf.Clamp01(tempo / duracao);
            yield return null;
        }

        _grupoPainelVitoria.alpha = 1f;
        _grupoPainelVitoria.interactable = true;
        _grupoPainelVitoria.blocksRaycasts = true;
    }

    void OcultarHudGameplay()
    {
        if (textoProgresso != null)
            textoProgresso.gameObject.SetActive(false);

        if (iconesFusivelHUD != null && iconesFusivelHUD.Length > 0
            && iconesFusivelHUD[0] != null && iconesFusivelHUD[0].transform.parent != null)
        {
            iconesFusivelHUD[0].transform.parent.gameObject.SetActive(false);
        }
    }

    void PrepararAnimacaoHorario()
    {
        if (textoHorarioTransicao == null || janelaDigitoHorario == null
            || digitoCinco == null || digitoSeis == null)
        {
            Debug.LogError(
                "Vincule AM, Janela do Digito do Horario, Hora 5 e Hora 6 na Hierarchy da Gameplay.",
                this);
            return;
        }

        _alturaDigitoHorario = janelaDigitoHorario.rect.height;
        if (_alturaDigitoHorario <= 0f)
        {
            Debug.LogError("A Janela do Digito do Horario precisa ter altura maior que zero.", janelaDigitoHorario);
            return;
        }

        _posicaoInicialDigitoCinco = digitoCinco.anchoredPosition;
        _posicaoInicialDigitoSeis = digitoSeis.anchoredPosition;
        janelaDigitoHorario.gameObject.SetActive(false);
        digitoCinco.gameObject.SetActive(false);
        digitoSeis.gameObject.SetActive(false);
    }

    IEnumerator AnimarViradaHorario()
    {
        float duracao = Mathf.Max(0f, duracaoAnimacaoVitoria);
        float tempo = 0f;
        digitoCinco.gameObject.SetActive(true);
        digitoSeis.gameObject.SetActive(true);

        while (tempo < duracao)
        {
            tempo += Time.unscaledDeltaTime;
            float progresso = duracao <= 0f ? 1f : Mathf.Clamp01(tempo / duracao);
            digitoCinco.anchoredPosition = _posicaoInicialDigitoCinco
                + new Vector2(0f, _alturaDigitoHorario * progresso);
            digitoSeis.anchoredPosition = _posicaoInicialDigitoSeis
                + new Vector2(0f, _alturaDigitoHorario * (progresso - 1f));
            yield return null;
        }

        digitoCinco.anchoredPosition = _posicaoInicialDigitoCinco
            + new Vector2(0f, _alturaDigitoHorario);
        digitoSeis.anchoredPosition = _posicaoInicialDigitoSeis;
    }

    void TocarSequenciaConclusaoNoite()
    {
        PrepararAudioConclusaoPersistente();

        if (clipSinalNoiteConcluida == null)
            Debug.LogError("Configure o clip chimes 2 para a conclusão de cada noite.", this);
        else
            _audioSinalNoitePersistente.PlayOneShot(clipSinalNoiteConcluida);

        if (clipMultidaoNoiteConcluida == null)
        {
            Debug.LogError("Configure o clip CROWD_SMALL_CHIL_EC049202 para a conclusão de cada noite.", this);
            return;
        }

        _audioMultidaoNoitePersistente.clip = clipMultidaoNoiteConcluida;
        _audioMultidaoNoitePersistente.loop = false;
        _audioMultidaoNoitePersistente.PlayScheduled(AudioSettings.dspTime + 6d);
    }

    static void PrepararAudioConclusaoPersistente()
    {
        if (_audioConclusaoPersistente == null)
        {
            _audioConclusaoPersistente = new GameObject("Audio de Conclusão da Noite");
            DontDestroyOnLoad(_audioConclusaoPersistente);
            _audioSinalNoitePersistente = _audioConclusaoPersistente.AddComponent<AudioSource>();
            _audioMultidaoNoitePersistente = _audioConclusaoPersistente.AddComponent<AudioSource>();
        }

        _audioSinalNoitePersistente.Stop();
        _audioMultidaoNoitePersistente.Stop();
        _audioSinalNoitePersistente.playOnAwake = false;
        _audioSinalNoitePersistente.loop = false;
        _audioSinalNoitePersistente.spatialBlend = 0f;
        _audioSinalNoitePersistente.volume = 0.55f;
        _audioMultidaoNoitePersistente.playOnAwake = false;
        _audioMultidaoNoitePersistente.loop = false;
        _audioMultidaoNoitePersistente.spatialBlend = 0f;
        _audioMultidaoNoitePersistente.volume = 0.55f;
    }

    public void AtualizarAudioPlayer(AudioSource fonte, bool movendo, bool correndo)
    {
        if (fonte == null)
            return;

        AudioClip clip = correndo
            ? (clipCorridaPlayer != null ? clipCorridaPlayer : clipPassosPlayer)
            : clipPassosPlayer;

        if (!movendo || clip == null)
        {
            if (fonte.isPlaying || fonte.clip != null)
            {
                fonte.Stop();
                fonte.clip = null;
            }
            return;
        }

        if (fonte.clip != clip)
        {
            fonte.Stop();
            fonte.clip = clip;
        }

        fonte.loop = true;
        fonte.volume = correndo ? 0.35f : 0.2f;
        if (!fonte.isPlaying)
            fonte.Play();
    }

    public void TocarSomUI()
    {
        TocarOneShot(audioUI, clipUI);
    }

    public void TocarSomPorta()
    {
        TocarOneShot(audioUI, clipPorta);
    }

    public void TocarSomLuz()
    {
        TocarOneShot(audioUI, clipLuz);
    }

    public void TocarSomGerador()
    {
        TocarOneShot(audioGameplay, clipGerador, 0.15f);
    }

    public void TocarAlertaAnimatronico(GameObject animatronico = null)
    {
        if (animatronico != null && !animatronico.name.Contains("Freddy"))
            return;

        TocarOneShot(audioUI, clipAlertaAnimatronico, 0.5f);
    }

    public void AtualizarAudioAnimatronico(AudioSource fonte, bool perseguindo, bool movendo)
    {
        if (fonte == null)
            return;

        AudioClip clip = perseguindo
            ? (clipPerseguicaoAnimatronico != null ? clipPerseguicaoAnimatronico : clipPassosAnimatronico)
            : clipPassosAnimatronico;

        if (!movendo || clip == null)
        {
            if (fonte.isPlaying || fonte.clip != null)
                PararAudio(fonte);
            return;
        }

        if (fonte.clip != clip)
        {
            fonte.Stop();
            fonte.clip = clip;
        }

        fonte.loop = true;
        fonte.volume = perseguindo ? 0.35f : 0.2f;
        if (!fonte.isPlaying)
            fonte.Play();
    }

    void ConfigurarNoite()
    {
        int[] fusiveisPorNoite = { 3, 3, 4, 5, 7 };
        fusiveisNecessarios = fusiveisPorNoite[_noiteAtual - 1];

        ConfigurarAnimatronico(ref freddy, "Freddy", true);
        ConfigurarAnimatronico(ref bonnie, "Bonnie", _noiteAtual >= 2);
        ConfigurarAnimatronico(ref chica, "Chica", _noiteAtual >= 3);
        ConfigurarAnimatronico(ref foxy, "Foxy", _noiteAtual >= 4);
    }

    void ConfigurarAnimatronico(ref GameObject animatronico, string nome, bool ativo)
    {
        if (animatronico == null)
            animatronico = GameObject.Find(nome);

        if (animatronico == null)
        {
            Debug.LogError($"Animatrônico '{nome}' não encontrado na cena.", this);
            return;
        }

        animatronico.SetActive(ativo);
    }

    public bool IniciarJumpscare(animatronicHibrid animatronico = null)
    {
        if (_estado != EstadoJogo.EmAndamento)
            return false;

        _estado = EstadoJogo.Jumpscare;
        PararAudiosDeFundo();
        if (painelVitoria != null)
            painelVitoria.SetActive(false);
        playerController player = FindAnyObjectByType<playerController>();
        if (player != null)
            player.PararMovimento();

        if (animatronico != null)
            animatronico.PausarParaJumpscare();
        foreach (animatronicHibrid ativo in FindObjectsByType<animatronicHibrid>())
        {
            if (ativo != animatronico)
                ativo.PausarParaJumpscare();
        }

        StartCoroutine(ExecutarJumpscare());
        return true;
    }

    public void AtivarGameOver()
    {
        IniciarJumpscare();
    }

    IEnumerator ExecutarJumpscare()
    {
        float duracao = Mathf.Max(0f, duracaoJumpscare);
        AnimatorUpdateMode modoAnterior = AnimatorUpdateMode.Normal;
        bool restaurarModoAnimacao = false;

        PrepararVisualJumpscare();
        yield return FadePara(1f, duracaoFadeJumpscare);

        if (visualJumpscare != null)
            visualJumpscare.SetActive(true);
        if (imagemJumpscare != null)
        {
            imagemJumpscare.enabled = true;
            imagemJumpscare.gameObject.SetActive(true);
        }
        if (animacaoJumpscare != null)
        {
            animacaoJumpscare.gameObject.SetActive(true);
            modoAnterior = animacaoJumpscare.updateMode;
            animacaoJumpscare.updateMode = AnimatorUpdateMode.UnscaledTime;
            restaurarModoAnimacao = true;

            if (animacaoJumpscare.runtimeAnimatorController != null)
            {
                animacaoJumpscare.Rebind();
                if (!string.IsNullOrWhiteSpace(estadoAnimacaoJumpscare))
                    animacaoJumpscare.Play(estadoAnimacaoJumpscare, 0, 0f);
                animacaoJumpscare.Update(0f);

                float duracaoAnimacao = animacaoJumpscare.GetCurrentAnimatorStateInfo(0).length;
                duracao = Mathf.Max(duracao, duracaoAnimacao);
            }
        }
        if (clipJumpscare != null)
            audioJumpscare.clip = clipJumpscare;

        if (audioJumpscare != null && audioJumpscare.clip != null)
        {
            audioJumpscare.loop = false;
            audioJumpscare.Play();
            duracao = Mathf.Max(duracao, audioJumpscare.clip.length);
        }

        yield return FadePara(0f, duracaoFadeJumpscare);
        yield return new WaitForSecondsRealtime(duracao);

        if (restaurarModoAnimacao && animacaoJumpscare != null)
            animacaoJumpscare.updateMode = modoAnterior;
        yield return FadePara(1f, duracaoFadeJumpscare);
        PararAudio(audioJumpscare);
        if (visualJumpscare != null)
            visualJumpscare.SetActive(false);
        if (imagemJumpscare != null && visualJumpscare == null)
            imagemJumpscare.enabled = false;
        LimparVisualTransicao();

        _estado = EstadoJogo.GameOver;
        if (painelVitoria != null)
            painelVitoria.SetActive(false);
        OcultarHudGameplay();
        if (painelGameOver != null)
            painelGameOver.SetActive(true);
        if (audioUI != null && clipGameOver != null)
        {
            audioUI.Stop();
            audioUI.PlayOneShot(clipGameOver);
        }
        else if (clipGameOver == null)
        {
            Debug.LogError("Configure static2.wav como clip de Game Over.", this);
        }
        Time.timeScale = 0f;
        StartCoroutine(FadePara(0f, duracaoFadeJumpscare));
    }

    public void RegistrarFusivel(playerController player)
    {
        if (_estado != EstadoJogo.EmAndamento || player == null)
            return;

        player.fusiveisColetados = Mathf.Min(player.fusiveisColetados + 1, fusiveisNecessarios);
        TocarOneShot(audioGameplay, clipFusivel, 0.05f);
        AtualizarHud(player.fusiveisColetados);
    }

    public void AtivarVitoria()
    {
        playerController player = FindAnyObjectByType<playerController>();
        ConcluirNoite(player);
    }

    public void ConcluirNoite(playerController player)
    {
        if (_estado != EstadoJogo.EmAndamento || player == null)
            return;
        if (player.fusiveisColetados < fusiveisNecessarios)
            return;

        int noiteDesbloqueada = Mathf.Min(_noiteAtual + 1, TotalNoites);
        if (noiteDesbloqueada > GetSavedNight())
        {
            PlayerPrefs.SetInt(NoiteSalvaKey, noiteDesbloqueada);
            PlayerPrefs.Save();
        }

        Time.timeScale = 0f;
        PararAudiosDeFundo();
        if (painelGameOver != null)
            painelGameOver.SetActive(false);

        if (_noiteAtual < TotalNoites)
        {
            _estado = EstadoJogo.ProximaNoite;
        }
        else
        {
            _estado = EstadoJogo.JogoConcluido;
        }

        TocarSequenciaConclusaoNoite();
        StartCoroutine(MostrarVitoriaAposFade());
    }

    public void ReiniciarJogo()
    {
        if (_transicaoPendente)
            return;

        if (_pausado)
        {
            if (painelPause != null)
                painelPause.SetActive(false);
            _pausado = false;
            AudioListener.pause = _audioListenerPausadoAntes;
        }

        PararAudiosDeFundo();
        PararAudio(audioJumpscare);
        TocarSomUI();
        Time.timeScale = 1f;

        if (_estado == EstadoJogo.ProximaNoite)
        {
            _noiteDaSessao = Mathf.Min(_noiteAtual + 1, TotalNoites);
        }
        else if (_estado == EstadoJogo.JogoConcluido)
        {
            _noiteDaSessao = 1;
        }
        else
        {
            _noiteDaSessao = _noiteAtual;
        }

        _ignorarMenuNaProximaCarga = true;
        CarregarCenaDeTransicao(SceneManager.GetActiveScene().name);
    }

    public static void IniciarNovaPartida()
    {
        _noiteDaSessao = 1;
        _ignorarMenuNaProximaCarga = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Gameplay");
    }

    public static void IniciarPartidaSalva()
    {
        _noiteDaSessao = GetSavedNight();
        _ignorarMenuNaProximaCarga = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Gameplay");
    }

    public static bool ConsumirIgnorarMenuNaProximaCarga()
    {
        bool ignorarMenu = _ignorarMenuNaProximaCarga;
        _ignorarMenuNaProximaCarga = false;
        return ignorarMenu;
    }

    public static int GetSavedNight()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(NoiteSalvaKey, 1), 1, TotalNoites);
    }

    public void VoltarAoMenu()
    {
        if (_transicaoPendente)
            return;

        if (_pausado)
        {
            if (painelPause != null)
                painelPause.SetActive(false);
            _pausado = false;
            AudioListener.pause = _audioListenerPausadoAntes;
        }

        PararAudiosDeFundo();
        PararAudio(audioJumpscare);
        _noiteDaSessao = 1;
        Time.timeScale = 1f;
        if (string.IsNullOrWhiteSpace(cenaMenu))
        {
            Debug.LogError("Configure o nome da cena de menu no GameManager.", this);
            return;
        }

        TocarSomUI();
        CarregarCenaDeTransicao(cenaMenu);
    }

    void CarregarCenaDeTransicao(string cena)
    {
        if (clipUI == null)
        {
            PararAudio(audioUI);
            SceneManager.LoadScene(cena);
            return;
        }

        _transicaoPendente = true;
        StartCoroutine(CarregarCenaAposSomUI(cena));
    }

    IEnumerator CarregarCenaAposSomUI(string cena)
    {
        yield return new WaitForSecondsRealtime(Mathf.Min(clipUI.length, 0.35f));
        PararAudio(audioUI);
        SceneManager.LoadScene(cena);
    }

    void AtualizarHud(int fusíveis = -1)
    {
        if (fusíveis < 0)
        {
            playerController player = FindAnyObjectByType<playerController>();
            fusíveis = player != null ? player.fusiveisColetados : 0;
        }

        if (textoProgresso != null)
        {
            AplicarFonteFnaf(textoProgresso);
            textoProgresso.text = $"Noite {_noiteAtual}";
        }

        if (iconesFusivelHUD == null)
            return;

        int fusiveisColetados = Mathf.Clamp(fusíveis, 0, fusiveisNecessarios);
        for (int i = 0; i < iconesFusivelHUD.Length; i++)
        {
            Image icone = iconesFusivelHUD[i];
            if (icone == null)
                continue;

            bool necessarioNestaNoite = i < fusiveisNecessarios;
            icone.gameObject.SetActive(necessarioNestaNoite);
            if (necessarioNestaNoite)
                icone.color = i < fusiveisColetados ? corFusivelAceso : corFusivelApagado;
        }
    }

    void AtualizarTextoBotaoGameOver()
    {
        if (painelGameOver == null)
            return;

        Button botao = painelGameOver.GetComponentInChildren<Button>(true);
        TMP_Text texto = botao != null ? botao.GetComponentInChildren<TMP_Text>(true) : null;
        if (texto == null)
            return;

        AplicarFonteFnaf(texto);
        texto.text = "Retry";
        texto.raycastTarget = false;
    }

    void AplicarFonteFnaf(TMP_Text texto)
    {
        if (texto == null)
            return;

        if (fonteFnaf == null || materialFonteFnaf == null)
        {
            Debug.LogError("Configure FNAF-Regular SDF e FNAF-Regular SDF Material no GameManager.", this);
            return;
        }

        texto.font = fonteFnaf;
        texto.fontSharedMaterial = materialFonteFnaf;
        texto.raycastTarget = false;
    }

}
