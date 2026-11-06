using UnityEngine;

// Efeitos da câmera do player: balanço dos passos, FOV na corrida e tremor leve.
// Só mexe na posição local, no FOV e soma um tremor pequeno. Não lê o mouse.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Camera))]
public class CameraCorrida : MonoBehaviour
{
    [Tooltip("O Player. Se ficar vazio, procura sozinho nos pais da câmera")]
    public Player player;

    [Header("Balanço dos Passos")]
    public bool ativarBalanco = true;

    [Tooltip("Quanto a câmera sobe e desce a cada passo")]
    public float alturaBalanco = 0.05f;

    [Tooltip("Quanto a câmera vai para os lados a cada passo. 0 = sem balanço lateral")]
    public float larguraBalanco = 0.02f;

    [Tooltip("Passos por segundo andando. Correndo fica mais rápido sozinho")]
    public float passosPorSegundo = 1.8f;

    [Header("Campo de Visão (FOV)")]
    public bool ativarFov = true;

    [Tooltip("Quantos graus o FOV abre na velocidade de corrida")]
    public float aumentoFovCorrendo = 10f;

    [Tooltip("Quão rápido o FOV muda. Maior = mais rápido")]
    public float suavidadeFov = 5f;

    [Header("Tremor na Corrida")]
    public bool ativarTremor = true;

    [Tooltip("Tamanho do tremor em graus. 0.3 = leve, 1 = forte")]
    public float intensidadeTremor = 0.3f;

    [Tooltip("Rapidez do tremor. Maior = vibra mais rápido")]
    public float velocidadeTremor = 12f;

    private Camera cam;
    private Vector3 posicaoBase;     // posição original da câmera (na cabeça do player)
    private float fovBase;           // FOV original da câmera
    private float tempoBalanco = 0f; // relógio dos passos
    private Vector3 balancoAtual;    // deslocamento atual do balanço
    private float forcaTremor = 0f;  // 0 = sem tremor, 1 = tremor cheio

    void Start()
    {
        cam = GetComponent<Camera>();
        posicaoBase = transform.localPosition;
        fovBase = cam.fieldOfView;

        if (player == null)
            player = GetComponentInParent<Player>();

        if (player == null)
            Debug.LogWarning("CameraCorrida: não achei o Player. Arraste ele no Inspector.");
    }

    // Devolve o FOV original se o script for desligado
    void OnDisable()
    {
        if (cam != null)
            cam.fieldOfView = fovBase;
    }

    // Roda depois do Update do Player, então a rotação do mouse já foi aplicada
    void LateUpdate()
    {
        if (player == null) return;

        float velocidade = player.VelocidadeAtual;
        bool andandoNoChao = player.EstaNoChao && velocidade > 0.1f;

        // 0 = andando, 1 = velocidade de corrida
        float nivelCorrida = Mathf.InverseLerp(player.velocidadeAndar, player.velocidadeCorrer, velocidade);

        AtualizarBalanco(velocidade, andandoNoChao);
        AtualizarFov(nivelCorrida);
        AtualizarTremor(nivelCorrida, andandoNoChao);
    }

    // Suavização que não depende do FPS (valor maior = chega mais rápido)
    static float Suavizar(float velocidade)
    {
        return 1f - Mathf.Exp(-velocidade * Time.deltaTime);
    }

    // Balanço: a câmera faz um "8" deitado a cada par de passos
    void AtualizarBalanco(float velocidade, bool andandoNoChao)
    {
        Vector3 alvo = Vector3.zero; // parado ou no ar: volta ao centro

        if (ativarBalanco && andandoNoChao)
        {
            float ritmo = velocidade / Mathf.Max(player.velocidadeAndar, 0.01f);
            tempoBalanco += Time.deltaTime * passosPorSegundo * ritmo * Mathf.PI;

            alvo.x = Mathf.Cos(tempoBalanco) * larguraBalanco;     // lado a lado
            alvo.y = Mathf.Sin(tempoBalanco * 2f) * alturaBalanco; // sobe e desce
        }

        balancoAtual = Vector3.Lerp(balancoAtual, alvo, Suavizar(10f));
        transform.localPosition = posicaoBase + balancoAtual;
    }

    // FOV: abre conforme a velocidade sobe até a corrida
    void AtualizarFov(float nivelCorrida)
    {
        if (!ativarFov) return;

        float fovAlvo = fovBase + aumentoFovCorrendo * nivelCorrida;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fovAlvo, Suavizar(suavidadeFov));
    }

    // Tremor: pequenas viradas suaves enquanto corre
    void AtualizarTremor(float nivelCorrida, bool andandoNoChao)
    {
        if (!ativarTremor) return;

        // O Player redefine a rotação da câmera todo frame; sem isso o tremor se acumularia
        if (player.cameraPersonagem != transform) return;

        float alvo = (andandoNoChao && player.EstadoAtual == EstadoMovimento.Correndo) ? nivelCorrida : 0f;
        forcaTremor = Mathf.Lerp(forcaTremor, alvo, Suavizar(5f));
        if (forcaTremor < 0.001f) return;

        float t = Time.time * velocidadeTremor;
        float tremorX = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * intensidadeTremor * forcaTremor;
        float tremorZ = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * intensidadeTremor * forcaTremor;

        transform.localRotation *= Quaternion.Euler(tremorX, 0f, tremorZ);
    }
}