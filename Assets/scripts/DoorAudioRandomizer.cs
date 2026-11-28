using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DoorAudioRandomizer : MonoBehaviour
{
    [SerializeField, Min(0f)] private float thresholdMovementAngle = 0.15f;
    [SerializeField, Min(0f)] private float tempoParadaParaRearmar = 0.2f;

    private AudioClip[] _clips;
    private AudioSource _audioSource;
    private Quaternion _rotacaoAnterior;
    private bool _movendo;
    private float _tempoParado;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
        _rotacaoAnterior = transform.localRotation;
    }

    public void Configurar(AudioClip[] clipsPorta)
    {
        _clips = clipsPorta;
    }

    void Update()
    {
        float variacao = Quaternion.Angle(_rotacaoAnterior, transform.localRotation);
        _rotacaoAnterior = transform.localRotation;
        if (variacao >= thresholdMovementAngle)
        {
            if (!_movendo)
                TocarSomAleatorio();

            _movendo = true;
            _tempoParado = 0f;
            return;
        }

        if (!_movendo)
            return;

        _tempoParado += Time.deltaTime;
        if (_tempoParado >= tempoParadaParaRearmar)
            _movendo = false;
    }

    void TocarSomAleatorio()
    {
        if (_clips == null || _clips.Length == 0)
        {
            Debug.LogError("Configure os sons aleatórios da porta.", this);
            return;
        }

        AudioClip clip = _clips[Random.Range(0, _clips.Length)];
        if (clip == null)
        {
            Debug.LogError("Há um som de porta não configurado.", this);
            return;
        }

        _audioSource.PlayOneShot(clip);
    }
}
