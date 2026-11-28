using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class animatronicHibrid : MonoBehaviour
{
    private static readonly RaycastHit2D[] impactosRaycast = new RaycastHit2D[32];
    private static readonly ContactFilter2D filtroPortas = ContactFilter2D.noFilter;
    private static readonly int parametroDirecaoHash = Animator.StringToHash("idTransition");

    public enum EstadoIA { Patrulha, Perseguicao, Retornando }
    public EstadoIA estadoAtual = EstadoIA.Patrulha;

    public List<Transform> todosOsNosDoMapa; 
    private Transform noAlvoAtual;

    public Transform alvoPlayer;
    public ControleBotoes botaoPortaEsquerda;
    public ControleBotoes botaoPortaDireita;
    public float velocidadePatrulha = 2f;
    public float velocidadePerseguicao = 4f;
    public float distanciaDeVisao = 7f;
    public float distanciaAtaque = 1.1f;
    [SerializeField, Min(0f)] private float tempoSemVisaoAntesDeRetornar = 1.5f;
    public float raioAgenteSeguro = 0.75f;
    public LayerMask layerParedes;

    private NavMeshAgent _agente;
    private NavMeshPath _caminhoPlanejado;
    private Vector3 _posicaoInicial;
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private AudioSource _audioPassos;
    private Collider2D[] _collidersPlayer;
    private bool jogoAcabou = false;
    private bool _rotaBloqueadaPorPorta;
    private float _multiplicadorDificuldade = 1f;
    private Vector3 _ultimoDestinoSolicitado;
    private bool _haDestinoSolicitado;
    private float _proximaTentativaDestino;
    private float _proximaEscolhaNo;
    private float _tempoSemProgresso;
    private int _direcaoAnimacaoAtual = -1;
    private bool _jaAvisouTravamento;
    private bool _possuiParametroDirecao;
    private float _tempoSemVerPlayer;

    public bool EstaBloqueadoPorPorta => _rotaBloqueadaPorPorta;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        if (_animator != null)
        {
            foreach (AnimatorControllerParameter parametro in _animator.parameters)
            {
                if (parametro.nameHash == parametroDirecaoHash
                    && parametro.type == AnimatorControllerParameterType.Int)
                {
                    _possuiParametroDirecao = true;
                    break;
                }
            }

            if (!_possuiParametroDirecao)
                Debug.LogError($"O Animator de {gameObject.name} precisa do parâmetro inteiro 'idTransition'.", this);
        }

        _agente = GetComponent<NavMeshAgent>();
        _caminhoPlanejado = new NavMeshPath();
        _audioPassos = GetComponent<AudioSource>();
        if (_audioPassos == null)
            _audioPassos = gameObject.AddComponent<AudioSource>();
        _audioPassos.playOnAwake = false;
        _audioPassos.loop = true;
        _audioPassos.spatialBlend = 0f;
        _posicaoInicial = transform.position;
        _collidersPlayer = alvoPlayer != null
            ? alvoPlayer.GetComponentsInChildren<Collider2D>()
            : new Collider2D[0];

        foreach (BoxCollider2D collider in GetComponents<BoxCollider2D>())
        {
            collider.size = new Vector2(
                Mathf.Min(collider.size.x, 0.9f),
                Mathf.Min(collider.size.y, 1.3f));
            collider.offset = new Vector2(collider.offset.x, -0.9f);
        }

        if (GameManager.Instance != null)
            _multiplicadorDificuldade = GameManager.Instance.MultiplicadorDificuldade;

        if (_agente == null)
        {
            Debug.LogError("O animatrônico precisa de um NavMeshAgent.", this);
            enabled = false;
            return;
        }

        _agente.updateRotation = false;
        _agente.updateUpAxis = false;
        _agente.radius = Mathf.Max(_agente.radius, Mathf.Max(raioAgenteSeguro, 0.5f));
        _agente.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(
            transform.position,
            out hit,
            Mathf.Max(raioAgenteSeguro * 2f, 1f),
            _agente.areaMask))
        {
            transform.position = hit.position;
            _agente.enabled = true;
            if (!_agente.Warp(hit.position))
            {
                Debug.LogWarning("Não foi possível posicionar o agente do animatrônico na NavMesh.", this);
                _agente.enabled = false;
            }
            else
            {
                _posicaoInicial = hit.position;
            }
        }
        else
        {
            Debug.LogWarning("Não há NavMesh próxima para posicionar este animatrônico.", this);
        }

        AtualizarAnimacaoDirecional(Vector3.zero);

        if (todosOsNosDoMapa != null && todosOsNosDoMapa.Count > 0)
        {
            EscolherNovoNoAleatorio();
        }
    }

    void OnDestroy()
    {
        PararAudio();
    }

    void Update()
    {
        if (jogoAcabou) return;

        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);

        if (_agente == null || !_agente.enabled || !_agente.isOnNavMesh)
        {
            AtualizarAnimacaoDirecional(Vector3.zero);
            AtualizarAudio(false, false);
            return;
        }

        if (estadoAtual == EstadoIA.Perseguicao)
        {
            if (JogadorVisivel())
            {
                _tempoSemVerPlayer = 0f;
            }
            else
            {
                _tempoSemVerPlayer += Time.deltaTime;
                if (_tempoSemVerPlayer >= tempoSemVisaoAntesDeRetornar)
                {
                    estadoAtual = EstadoIA.Retornando;
                    _agente.ResetPath();
                    _haDestinoSolicitado = false;
                    _tempoSemVerPlayer = 0f;
                }
            }
        }

        switch (estadoAtual)
        {
            case EstadoIA.Patrulha:
                _agente.speed = velocidadePatrulha * _multiplicadorDificuldade;
                PatrulharComNavMesh();
                VerificarVisao();
                break;

            case EstadoIA.Perseguicao:
                _agente.speed = velocidadePerseguicao * _multiplicadorDificuldade;
                if (alvoPlayer != null)
                    TentarDefinirDestino(alvoPlayer.position);
                break;

            case EstadoIA.Retornando:
                _agente.speed = velocidadePerseguicao * _multiplicadorDificuldade;
                if (!TentarDefinirDestino(_posicaoInicial)) break;

                if (!_agente.pathPending && _agente.remainingDistance < 0.5f)
                {
                    estadoAtual = EstadoIA.Patrulha;
                    EscolherNovoNoAleatorio();
                }
                VerificarVisao();
                break;
        }

        if (estadoAtual == EstadoIA.Perseguicao)
            VerificarAtaquePlayer(alvoPlayer);

        AtualizarAnimacaoDirecional(_agente.velocity);
        DetectarFaltaDeProgresso();

        AtualizarAudio(
            estadoAtual == EstadoIA.Perseguicao,
            !_agente.isStopped && _agente.velocity.sqrMagnitude > 0.04f);
    }

    bool PatrulharComNavMesh()
    {
        if (todosOsNosDoMapa == null || todosOsNosDoMapa.Count == 0) return false;
        if (noAlvoAtual == null) EscolherNovoNoAleatorio();
        if (noAlvoAtual == null) return false;
        if (!TentarDefinirDestino(noAlvoAtual.position))
        {
            if (!_rotaBloqueadaPorPorta && Time.time >= _proximaEscolhaNo)
            {
                EscolherNovoNoAleatorio();
                _proximaEscolhaNo = Time.time + 0.35f;
            }
            return false;
        }

        if (!_agente.pathPending && _agente.hasPath && _agente.remainingDistance < 0.5f)
        {
            EscolherNovoNoAleatorio();
        }
        return true;
    }

    bool TentarDefinirDestino(Vector3 destino)
    {
        if (PortaFechadaNoCaminho(destino))
        {
            _rotaBloqueadaPorPorta = true;
            _agente.isStopped = true;
            _agente.ResetPath();
            _haDestinoSolicitado = false;
            return false;
        }

        _rotaBloqueadaPorPorta = false;
        float margemAmostragem = Mathf.Max(_agente.radius, raioAgenteSeguro);
        if (!NavMesh.SamplePosition(
            destino,
            out NavMeshHit pontoSeguro,
            margemAmostragem,
            _agente.areaMask))
        {
            PararRotaSemDestino(destino);
            return false;
        }

        float distanciaMudancaDestino = Mathf.Max(_agente.radius * 0.5f, 0.25f);
        bool destinoMudou = !_haDestinoSolicitado
            || (pontoSeguro.position - _ultimoDestinoSolicitado).sqrMagnitude
                > distanciaMudancaDestino * distanciaMudancaDestino;
        if (!destinoMudou && (_agente.pathPending || _agente.hasPath))
            return true;

        if (!destinoMudou && Time.time < _proximaTentativaDestino)
            return false;

        if (!NavMesh.CalculatePath(
            _agente.nextPosition,
            pontoSeguro.position,
            _agente.areaMask,
            _caminhoPlanejado)
            || _caminhoPlanejado.status != NavMeshPathStatus.PathComplete)
        {
            PararRotaSemDestino(pontoSeguro.position);
            return false;
        }

        _agente.isStopped = false;
        if (!_agente.SetDestination(pontoSeguro.position))
        {
            PararRotaSemDestino(pontoSeguro.position);
            return false;
        }

        _ultimoDestinoSolicitado = pontoSeguro.position;
        _haDestinoSolicitado = true;
        _proximaTentativaDestino = Time.time + 0.35f;
        return true;
    }

    void PararRotaSemDestino(Vector3 destino)
    {
        _agente.ResetPath();
        _agente.isStopped = true;
        _ultimoDestinoSolicitado = destino;
        _haDestinoSolicitado = true;
        _proximaTentativaDestino = Time.time + 0.35f;
    }

    bool PortaFechadaNoCaminho(Vector3 destino)
    {
        return PortaBloqueiaDestino(botaoPortaEsquerda, destino)
            || PortaBloqueiaDestino(botaoPortaDireita, destino);
    }

    bool PortaBloqueiaDestino(ControleBotoes botao, Vector3 destino)
    {
        if (botao == null || !botao.PortaFechada || botao.porta == null)
            return false;

        Collider2D colliderPorta = botao.porta.GetComponent<Collider2D>();
        if (colliderPorta == null || !colliderPorta.enabled)
            return false;

        int quantidadeImpactos = Physics2D.Linecast(
            transform.position,
            destino,
            filtroPortas,
            impactosRaycast);
        for (int i = 0; i < quantidadeImpactos; i++)
        {
            Collider2D collider = impactosRaycast[i].collider;
            if (collider != null && (collider == colliderPorta
                || collider.transform.IsChildOf(botao.porta.transform)))
                return true;
        }

        return false;
    }

    void EscolherNovoNoAleatorio()
    {
        if (todosOsNosDoMapa == null || todosOsNosDoMapa.Count == 0)
        {
            noAlvoAtual = null;
            return;
        }

        int inicio = Random.Range(0, todosOsNosDoMapa.Count);
        Transform noAnterior = noAlvoAtual;
        Transform alternativa = null;
        for (int i = 0; i < todosOsNosDoMapa.Count; i++)
        {
            Transform candidato = todosOsNosDoMapa[(inicio + i) % todosOsNosDoMapa.Count];
            if (candidato == null)
                continue;

            if (alternativa == null)
                alternativa = candidato;

            if (candidato != noAnterior)
            {
                noAlvoAtual = candidato;
                return;
            }
        }

        noAlvoAtual = alternativa;
    }

    void AtualizarAnimacaoDirecional(Vector3 velocidade)
    {
        if (_animator == null || !_possuiParametroDirecao)
            return;

        const float velocidadeMinima = 0.04f;
        int direcao = 0;

        if (velocidade.sqrMagnitude >= velocidadeMinima && _agente != null && !_agente.isStopped)
        {
            if (Mathf.Abs(velocidade.x) > Mathf.Abs(velocidade.y))
            {
                direcao = velocidade.x < 0f ? 3 : 4;
                if (_spriteRenderer != null)
                    _spriteRenderer.flipX = velocidade.x < 0f;
            }
            else
            {
                direcao = velocidade.y > 0f ? 1 : 2;
                if (_spriteRenderer != null)
                    _spriteRenderer.flipX = false;
            }
        }
        else if (_spriteRenderer != null)
        {
            _spriteRenderer.flipX = false;
        }

        if (direcao == _direcaoAnimacaoAtual)
            return;

        _animator.SetInteger(parametroDirecaoHash, direcao);
        _direcaoAnimacaoAtual = direcao;
    }

    void DetectarFaltaDeProgresso()
    {
        bool deveEstarAndando = !_rotaBloqueadaPorPorta
            && !_agente.isStopped
            && !_agente.pathPending
            && _agente.hasPath
            && _agente.remainingDistance > Mathf.Max(_agente.stoppingDistance + 0.25f, 0.5f)
            && _agente.desiredVelocity.sqrMagnitude > 0.04f;

        if (!deveEstarAndando)
        {
            _tempoSemProgresso = 0f;
            _jaAvisouTravamento = false;
            return;
        }

        if (_agente.velocity.sqrMagnitude > 0.01f)
        {
            _tempoSemProgresso = 0f;
            _jaAvisouTravamento = false;
            return;
        }

        _tempoSemProgresso += Time.deltaTime;
        if (_tempoSemProgresso < 1.25f)
            return;

        if (!_jaAvisouTravamento)
        {
            Debug.LogWarning(
                $"O agente de {gameObject.name} não avançou; a rota será recalculada.",
                this);
            _jaAvisouTravamento = true;
        }

        _agente.ResetPath();
        _haDestinoSolicitado = false;
        _proximaTentativaDestino = 0f;
        if (estadoAtual == EstadoIA.Patrulha)
            EscolherNovoNoAleatorio();

        _tempoSemProgresso = 0f;
    }

    void VerificarVisao()
    {
        if (estadoAtual != EstadoIA.Perseguicao && JogadorVisivel())
        {
            estadoAtual = EstadoIA.Perseguicao;
            _tempoSemVerPlayer = 0f;
            if (GameManager.Instance != null)
                GameManager.Instance.TocarAlertaAnimatronico(gameObject);
        }
    }

    bool JogadorVisivel()
    {
        if (alvoPlayer == null || PortaFechadaNoCaminho(alvoPlayer.position))
            return false;

        if (_collidersPlayer == null || _collidersPlayer.Length == 0)
            _collidersPlayer = alvoPlayer.GetComponentsInChildren<Collider2D>();

        Vector2 origem = transform.position + Vector3.up * 0.25f;
        foreach (Collider2D colliderPlayer in _collidersPlayer)
        {
            if (colliderPlayer == null || !colliderPlayer.enabled || !colliderPlayer.gameObject.activeInHierarchy)
                continue;

            Vector2 pontoAlvo = colliderPlayer.ClosestPoint(origem);
            Vector2 direcao = pontoAlvo - origem;
            float distancia = direcao.magnitude;
            if (distancia > distanciaDeVisao)
                continue;
            if (distancia <= 0.001f || layerParedes.value == 0)
                return true;

            if (Physics2D.Linecast(origem, pontoAlvo, layerParedes).collider == null)
                return true;
        }

        return false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        VerificarMortePlayer(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        VerificarMortePlayer(collision.gameObject);
    }

    void VerificarMortePlayer(GameObject obj)
    {
        playerController player = obj.GetComponentInParent<playerController>();
        if (player == null && !obj.CompareTag("Player") && obj.name != "Player")
            return;

        Transform alvo = player != null ? player.transform : alvoPlayer;
        VerificarAtaquePlayer(alvo);
    }

    void VerificarAtaquePlayer(Transform alvo)
    {
        if (alvo == null
            || Vector2.Distance(transform.position, alvo.position) > distanciaAtaque
            || PortaFechadaNoCaminho(alvo.position)
            || !JogadorVisivel())
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager não encontrado; não foi possível iniciar o jumpscare.", this);
            return;
        }

        if (GameManager.Instance.IniciarJumpscare(this))
            PausarParaJumpscare();
    }

    public void PausarParaJumpscare()
    {
        jogoAcabou = true;
        PararAudio();
        if (_agente != null && _agente.isActiveAndEnabled && _agente.isOnNavMesh)
        {
            _agente.isStopped = true;
            _agente.velocity = Vector3.zero;
        }
    }

    void AtualizarAudio(bool perseguindo, bool movendo)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AtualizarAudioAnimatronico(_audioPassos, perseguindo, movendo);
        else if (!movendo && _audioPassos != null && _audioPassos.isPlaying)
            _audioPassos.Stop();
    }

    public void PararAudio()
    {
        if (_audioPassos == null)
            return;

        _audioPassos.Stop();
        _audioPassos.clip = null;
    }
}