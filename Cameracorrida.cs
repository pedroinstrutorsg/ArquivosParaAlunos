using UnityEngine;


// Ela reage ao movimento do player:
//   - balança um pouco a cada passo (como a cabeça de quem anda)
//   - abre o campo de visão (FOV) quando corre, dando sensação de velocidade
//   - treme levemente enquanto corre
[RequireComponent(typeof(Camera))]
public class CameraCorrida : MonoBehaviour
{
    [Tooltip("O Player. Se ficar vazio, procura sozinho nos \"pais\" da câmera")]
    public Player player;

    [Header("Balanço dos Passos")]
    public bool ativarBalanco = true;

    [Tooltip("Quanto a câmera sobe e desce a cada passo")]
    public float alturaBalanco = 0.05f;

    [Tooltip("Quanto a câmera vai para os lados a cada passo")]
    public float larguraBalanco = 0.03f;

    [Tooltip("Passos por segundo andando. Correndo fica mais rápido sozinho")]
    public float passosPorSegundo = 1.8f;

    [Header("Campo de Visão (FOV)")]
    public bool ativarFov = true;

    [Tooltip("Quantos graus o FOV abre quando o player está na velocidade de corrida")]
    public float aumentoFovCorrendo = 10f;

    [Tooltip("Quão rápido o FOV muda. Maior = mais rápido")]
    public float suavidadeFov = 5f;

    [Header("Tremor na Corrida")]
    public bool ativarTremor = true;

    [Tooltip("Tamanho do tremor em graus. 0.3 = leve, 1 = forte")]
    public float intensidadeTremor = 0.3f;

    [Tooltip("Rapidez do tremor. Maior = vibra mais rápido")]
    public float velocidadeTremor = 12f;

    // ===== Variáveis internas =====
    private Camera cam;
    private Vector3 posicaoBase;       // posição original da câmera (na cabeça do player)
    private float fovBase;             // FOV original da câmera
    private float tempoBalanco = 0f;   // "relógio" dos passos
    private Vector3 balancoAtual;      // quanto a câmera está deslocada agora
    private float forcaTremor = 0f;    // 0 = sem tremor, 1 = tremor cheio (sobe e desce suave)

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

    // LateUpdate roda DEPOIS do Update do Player.
    // Assim o Player já girou a câmera com o mouse, e só somamos os efeitos por cima.
    void LateUpdate()
    {
        if (player == null) return;

        float velocidade = player.VelocidadeAtual;
        bool andandoNoChao = player.EstaNoChao && velocidade > 0.1f;

        // De 0 (andando) até 1 (velocidade de corrida). Usado para o FOV e o tremor crescerem juntos com a corrida.
        float nivelCorrida = Mathf.InverseLerp(player.velocidadeAndar, player.velocidadeCorrer, velocidade);

        AtualizarBalanco(velocidade, andandoNoChao);
        AtualizarFov(nivelCorrida);
        AtualizarTremor(nivelCorrida, andandoNoChao);
    }

    // ========================================
    // BALANÇO: a câmera faz um "8" deitado a cada par de passos
    // ========================================
    void AtualizarBalanco(float velocidade, bool andandoNoChao)
    {
        Vector3 alvo = Vector3.zero; // parado ou no ar: câmera volta para o centro

        if (ativarBalanco && andandoNoChao)
        {
            // Quanto mais rápido, mais rápido os passos
            float ritmo = velocidade / Mathf.Max(player.velocidadeAndar, 0.01f);
            tempoBalanco += Time.deltaTime * passosPorSegundo * ritmo * Mathf.PI;

            // Seno e cosseno fazem a câmera ir e voltar de forma suave
            alvo.x = Mathf.Cos(tempoBalanco) * larguraBalanco;      // lado a lado: 1 vez a cada 2 passos
            alvo.y = Mathf.Sin(tempoBalanco * 2f) * alturaBalanco;  // sobe e desce: 1 vez a cada passo
        }

        // Chega no alvo aos poucos (sem trancos ao começar ou parar de andar)
        balancoAtual = Vector3.Lerp(balancoAtual, alvo, Time.deltaTime * 10f);
        transform.localPosition = posicaoBase + balancoAtual;
    }

    // ========================================
    // FOV: abre a visão conforme a velocidade sobe até a corrida
    // ========================================
    void AtualizarFov(float nivelCorrida)
    {
        if (!ativarFov) return;

        float fovAlvo = fovBase + aumentoFovCorrendo * nivelCorrida;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fovAlvo, Time.deltaTime * suavidadeFov);
    }

    // ========================================
    // TREMOR: pequenas viradas aleatórias enquanto corre
    // ========================================
    void AtualizarTremor(float nivelCorrida, bool andandoNoChao)
    {
        if (!ativarTremor) return;

        // O tremor depende do Player girar a câmera todo frame (ele "zera" a rotação).
        // Se a câmera não estiver em "Camera Personagem", o tremor iria se acumular, então não aplicamos.
        if (player.cameraPersonagem != transform) return;

        // Treme só correndo no chão; aumenta e diminui suave
        float alvo = (andandoNoChao && player.EstadoAtual == EstadoMovimento.Correndo) ? nivelCorrida : 0f;
        forcaTremor = Mathf.Lerp(forcaTremor, alvo, Time.deltaTime * 5f);

        // PerlinNoise dá números "aleatórios" mas suaves (melhor que Random para tremer)
        float t = Time.time * velocidadeTremor;
        float tremorX = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * intensidadeTremor * forcaTremor;
        float tremorZ = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * intensidadeTremor * forcaTremor;

        // Soma o tremor por cima da rotação que o Player deu com o mouse
        transform.localRotation *= Quaternion.Euler(tremorX, 0f, tremorZ);
    }
}