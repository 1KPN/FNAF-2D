using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private enum EstadoJogo { EmAndamento, Jumpscare, GameOver, ProximaNoite, JogoConcluido }

    private const int TotalNoites = 5;
    private static int _noiteDaSessao = 1;

    public static GameManager Instance;

    [Header("Painéis")]
    public GameObject painelGameOver;
    public GameObject painelVitoria;

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

    [Header("Transições visuais")]
    [SerializeField] private Sprite spriteSangue;
    [SerializeField, Range(0f, 1f)] private float opacidadeSangue = 0.75f;
    [SerializeField, Range(0f, 1f)] private float intensidadeVermelha = 0.25f;
    [SerializeField, Min(0.01f)] private float duracaoFadeJumpscare = 0.16f;
    [SerializeField, Min(0.01f)] private float duracaoFadeVitoria = 0.3f;

    [Header("Progressão")]
    public int fusiveisNecessarios = 5;
    [SerializeField] private float aumentoDificuldadePorNoite = 0.15f;
    [SerializeField] private Button botaoVoltarAoMenu;

    [Header("HUD (opcional; criado automaticamente se vazio)")]
    public TextMeshProUGUI textoProgresso;

    private EstadoJogo _estado = EstadoJogo.EmAndamento;
    private int _noiteAtual;
    private bool _transicaoPendente;
    private readonly Dictionary<AudioSource, float> _ultimoAudioPorFonte = new Dictionary<AudioSource, float>();
    private Image _imagemFade;
    private Image _imagemTintVermelha;
    private Image _imagemSangue;
    private TextMeshProUGUI _textoHorarioTransicao;
    private GameObject _canvasTransicoes;
    private static GameObject _audioConclusaoPersistente;
    private static AudioSource _audioSinalNoitePersistente;
    private static AudioSource _audioMultidaoNoitePersistente;

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
        Time.timeScale = 1f;
        PrepararAudioSources();
        _noiteAtual = Mathf.Clamp(_noiteDaSessao, 1, TotalNoites);
        ConfigurarNoite();
    }

    void Start()
    {
        Time.timeScale = 1f;
        if (painelGameOver != null) painelGameOver.SetActive(false);
        if (painelVitoria != null) painelVitoria.SetActive(false);
        if (visualJumpscare != null) visualJumpscare.SetActive(false);
        CriarCamadaTransicoes();

        TocarLoop(audioAmbiente, clipAmbiente);
        TocarLoop(audioMusica, clipMusica);

        if (textoProgresso == null)
            CriarHud();

        AtualizarHud();
    }

    void OnDestroy()
    {
        PararAudio(audioAmbiente);
        PararAudio(audioMusica);
        PararAudio(audioUI);
        PararAudio(audioGameplay);
        PararAudio(audioJumpscare);
        if (_canvasTransicoes != null)
            Destroy(_canvasTransicoes);
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

        audioAmbiente.volume = 0.3f;
        audioMusica.volume = 0.3f;
        audioUI.volume = 0.65f;
        audioGameplay.volume = 0.45f;
        audioJumpscare.volume = 0.9f;
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
        if (spriteSangue == null)
            Debug.LogError("Configure o sprite de sangue no GameManager para exibir o efeito de jumpscare.", this);

        _canvasTransicoes = new GameObject("Camada de Transições", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = _canvasTransicoes.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32767;

        CanvasScaler scaler = _canvasTransicoes.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        _imagemTintVermelha = CriarImagemTela(canvas.transform, "Tint Vermelho", Color.red);
        _imagemSangue = CriarImagemTela(canvas.transform, "Sangue", Color.white);
        _imagemSangue.sprite = spriteSangue;
        _imagemFade = CriarImagemTela(canvas.transform, "Fade", Color.black);
        _textoHorarioTransicao = CriarTextoHorario(canvas.transform);
        _imagemTintVermelha.gameObject.SetActive(false);
        _imagemSangue.gameObject.SetActive(false);
        _imagemFade.color = new Color(0f, 0f, 0f, 0f);
        _textoHorarioTransicao.gameObject.SetActive(false);
    }

    Image CriarImagemTela(Transform pai, string nome, Color cor)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        objeto.transform.SetParent(pai, false);

        RectTransform retangulo = objeto.GetComponent<RectTransform>();
        retangulo.anchorMin = Vector2.zero;
        retangulo.anchorMax = Vector2.one;
        retangulo.offsetMin = Vector2.zero;
        retangulo.offsetMax = Vector2.zero;

        Image imagem = objeto.GetComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
        return imagem;
    }

    TextMeshProUGUI CriarTextoHorario(Transform pai)
    {
        GameObject objeto = new GameObject(
            "Transição 5 AM - 6 AM",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        objeto.transform.SetParent(pai, false);

        RectTransform retangulo = objeto.GetComponent<RectTransform>();
        retangulo.anchorMin = new Vector2(0.5f, 0.5f);
        retangulo.anchorMax = new Vector2(0.5f, 0.5f);
        retangulo.anchoredPosition = Vector2.zero;
        retangulo.sizeDelta = new Vector2(600f, 150f);

        TextMeshProUGUI texto = objeto.GetComponent<TextMeshProUGUI>();
        texto.fontSize = 72f;
        texto.color = Color.white;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;
        return texto;
    }

    void PrepararVisualJumpscare()
    {
        if (_imagemTintVermelha != null)
        {
            _imagemTintVermelha.color = new Color(1f, 0f, 0f, intensidadeVermelha);
            _imagemTintVermelha.gameObject.SetActive(true);
        }

        if (_imagemSangue != null)
        {
            _imagemSangue.sprite = spriteSangue;
            _imagemSangue.color = new Color(1f, 1f, 1f, opacidadeSangue);
            _imagemSangue.gameObject.SetActive(true);
        }
    }

    void LimparVisualTransicao()
    {
        if (_imagemTintVermelha != null)
            _imagemTintVermelha.gameObject.SetActive(false);
        if (_imagemSangue != null)
            _imagemSangue.gameObject.SetActive(false);
    }

    IEnumerator FadePara(float alphaDestino, float duracao)
    {
        if (_imagemFade == null)
            yield break;

        _imagemFade.gameObject.SetActive(true);
        float alphaInicial = _imagemFade.color.a;
        float tempo = 0f;
        float duracaoSegura = Mathf.Max(0.01f, duracao);

        while (tempo < duracaoSegura)
        {
            tempo += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(alphaInicial, alphaDestino, Mathf.Clamp01(tempo / duracaoSegura));
            _imagemFade.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        _imagemFade.color = new Color(0f, 0f, 0f, alphaDestino);
    }

    IEnumerator MostrarVitoriaAposFade()
    {
        if (_textoHorarioTransicao != null)
        {
            _textoHorarioTransicao.text = "5 AM";
            _textoHorarioTransicao.gameObject.SetActive(true);
        }

        yield return FadePara(1f, duracaoFadeVitoria);
        if (_textoHorarioTransicao != null)
        {
            _textoHorarioTransicao.text = "6 AM";
            yield return new WaitForSecondsRealtime(0.3f);
            _textoHorarioTransicao.gameObject.SetActive(false);
        }

        if (painelVitoria != null)
            painelVitoria.SetActive(true);
        yield return FadePara(0f, duracaoFadeVitoria);
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
        if (painelGameOver != null)
            painelGameOver.SetActive(true);
        TocarOneShot(audioUI, clipGameOver);
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

        Time.timeScale = 0f;
        PararAudiosDeFundo();
        if (painelGameOver != null)
            painelGameOver.SetActive(false);

        if (_noiteAtual < TotalNoites)
        {
            _estado = EstadoJogo.ProximaNoite;
            AtualizarTextoVitoria(
                $"6 AM\nNoite {_noiteAtual} concluída! Prepare-se para a noite {_noiteAtual + 1}.");
            AtualizarTextoBotaoVitoria("Próxima noite");
        }
        else
        {
            _estado = EstadoJogo.JogoConcluido;
            AtualizarTextoVitoria("6 AM\nParabéns! Você zerou as cinco noites.");
            AtualizarTextoBotaoVitoria("Jogar da Noite 1");
            CriarBotaoVoltarAoMenu();
        }

        TocarSequenciaConclusaoNoite();
        StartCoroutine(MostrarVitoriaAposFade());
    }

    public void ReiniciarJogo()
    {
        if (_transicaoPendente)
            return;

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

        CarregarCenaDeTransicao(SceneManager.GetActiveScene().name);
    }

    public static void IniciarNovaPartida()
    {
        _noiteDaSessao = 1;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Gameplay");
    }

    public void VoltarAoMenu()
    {
        if (_transicaoPendente)
            return;

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
        if (textoProgresso == null)
            return;

        if (fusíveis < 0)
        {
            playerController player = FindAnyObjectByType<playerController>();
            fusíveis = player != null ? player.fusiveisColetados : 0;
        }

        int faltamColetar = Mathf.Max(0, fusiveisNecessarios - fusíveis);
        textoProgresso.text =
            $"Noite {_noiteAtual}/{TotalNoites}\n" +
            $"Fusíveis coletados: {fusíveis}/{fusiveisNecessarios}\n" +
            $"Faltam coletar: {faltamColetar}";
    }

    void CriarHud()
    {
        GameObject canvasObject = new GameObject("HUD de Progresso", typeof(RectTransform), typeof(Canvas));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject fundo = new GameObject("Fundo do Progresso", typeof(RectTransform), typeof(Image));
        fundo.transform.SetParent(canvas.transform, false);
        RectTransform fundoRect = fundo.GetComponent<RectTransform>();
        fundoRect.anchorMin = new Vector2(0f, 1f);
        fundoRect.anchorMax = new Vector2(0f, 1f);
        fundoRect.pivot = new Vector2(0f, 1f);
        fundoRect.anchoredPosition = new Vector2(20f, -20f);
        fundoRect.sizeDelta = new Vector2(360f, 112f);

        Image fundoImagem = fundo.GetComponent<Image>();
        fundoImagem.color = new Color(0f, 0f, 0f, 0.65f);
        fundoImagem.raycastTarget = false;

        GameObject texto = new GameObject("Texto do Progresso", typeof(RectTransform), typeof(TextMeshProUGUI));
        texto.transform.SetParent(fundo.transform, false);
        RectTransform textoRect = texto.GetComponent<RectTransform>();
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = new Vector2(12f, 6f);
        textoRect.offsetMax = new Vector2(-12f, -6f);

        textoProgresso = texto.GetComponent<TextMeshProUGUI>();
        textoProgresso.fontSize = 20f;
        textoProgresso.color = Color.white;
        textoProgresso.alignment = TextAlignmentOptions.TopLeft;
        textoProgresso.raycastTarget = false;
    }

    void AtualizarTextoVitoria(string mensagem)
    {
        if (painelVitoria == null)
            return;

        TMP_Text[] textos = painelVitoria.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text texto in textos)
        {
            if (texto.GetComponentInParent<Button>() == null)
            {
                texto.text = mensagem;
                return;
            }
        }
    }

    void AtualizarTextoBotaoVitoria(string rotulo)
    {
        if (painelVitoria == null)
            return;

        Button botao = painelVitoria.GetComponentInChildren<Button>(true);
        TMP_Text texto = botao != null ? botao.GetComponentInChildren<TMP_Text>(true) : null;
        if (texto != null)
            texto.text = rotulo;
    }

    void CriarBotaoVoltarAoMenu()
    {
        if (botaoVoltarAoMenu != null || painelVitoria == null)
            return;

        Button botaoPrincipal = painelVitoria.GetComponentInChildren<Button>(true);
        if (botaoPrincipal == null)
        {
            Debug.LogError("O painel de vitória precisa de um botão para posicionar o botão do menu.", this);
            return;
        }

        RectTransform retanguloPrincipal = botaoPrincipal.GetComponent<RectTransform>();
        GameObject objetoBotao = new GameObject(
            "BotaoVoltarAoMenu",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        objetoBotao.transform.SetParent(botaoPrincipal.transform.parent, false);

        RectTransform retangulo = objetoBotao.GetComponent<RectTransform>();
        retangulo.anchorMin = retanguloPrincipal.anchorMin;
        retangulo.anchorMax = retanguloPrincipal.anchorMax;
        retangulo.pivot = retanguloPrincipal.pivot;
        retangulo.sizeDelta = retanguloPrincipal.sizeDelta;
        retangulo.anchoredPosition = retanguloPrincipal.anchoredPosition + new Vector2(0f, -42f);

        Image imagem = objetoBotao.GetComponent<Image>();
        Image imagemPrincipal = botaoPrincipal.GetComponent<Image>();
        if (imagemPrincipal != null)
        {
            imagem.sprite = imagemPrincipal.sprite;
            imagem.type = imagemPrincipal.type;
            imagem.color = imagemPrincipal.color;
        }

        botaoVoltarAoMenu = objetoBotao.GetComponent<Button>();
        botaoVoltarAoMenu.targetGraphic = imagem;
        botaoVoltarAoMenu.onClick.AddListener(VoltarAoMenu);

        GameObject objetoTexto = new GameObject(
            "Texto",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        objetoTexto.transform.SetParent(objetoBotao.transform, false);
        RectTransform retanguloTexto = objetoTexto.GetComponent<RectTransform>();
        retanguloTexto.anchorMin = Vector2.zero;
        retanguloTexto.anchorMax = Vector2.one;
        retanguloTexto.offsetMin = Vector2.zero;
        retanguloTexto.offsetMax = Vector2.zero;

        TextMeshProUGUI texto = objetoTexto.GetComponent<TextMeshProUGUI>();
        TMP_Text textoPrincipal = botaoPrincipal.GetComponentInChildren<TMP_Text>(true);
        if (textoPrincipal != null)
        {
            texto.font = textoPrincipal.font;
            texto.fontSharedMaterial = textoPrincipal.fontSharedMaterial;
            texto.fontSize = textoPrincipal.fontSize;
            texto.color = textoPrincipal.color;
            texto.alignment = textoPrincipal.alignment;
            texto.fontStyle = textoPrincipal.fontStyle;
        }
        texto.text = "Voltar ao Menu";
        texto.raycastTarget = false;
    }
}
