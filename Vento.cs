using System.Collections.Generic;
using UnityEngine;

// As 26 direções de 45° (no espaço do próprio objeto: gire o objeto para girar o vento)
[System.Flags]
public enum DirecaoVento
{
    Frente = 1 << 0,
    Tras = 1 << 1,
    Direita = 1 << 2,
    Esquerda = 1 << 3,
    Cima = 1 << 4,
    Baixo = 1 << 5,
    FrenteDireita = 1 << 6,
    FrenteEsquerda = 1 << 7,
    TrasDireita = 1 << 8,
    TrasEsquerda = 1 << 9,
    FrenteCima = 1 << 10,
    FrenteBaixo = 1 << 11,
    TrasCima = 1 << 12,
    TrasBaixo = 1 << 13,
    DireitaCima = 1 << 14,
    DireitaBaixo = 1 << 15,
    EsquerdaCima = 1 << 16,
    EsquerdaBaixo = 1 << 17,
    FrenteDireitaCima = 1 << 18,
    FrenteDireitaBaixo = 1 << 19,
    FrenteEsquerdaCima = 1 << 20,
    FrenteEsquerdaBaixo = 1 << 21,
    TrasDireitaCima = 1 << 22,
    TrasDireitaBaixo = 1 << 23,
    TrasEsquerdaCima = 1 << 24,
    TrasEsquerdaBaixo = 1 << 25
}

// Modos de força do vento
public enum ModoForcaVento
{
    Fixa,            // mesma força no corredor inteiro
    MaisForteDePerto // mais forte perto do objeto, mais fraca na ponta
}

// Objeto que empurra o player e os objetos com Rigidbody que estiverem nos seus corredores de vento
[DefaultExecutionOrder(-10)] // roda antes do Player, para ele usar o vento no mesmo passo
public class Vento : MonoBehaviour
{
    [Header("Direções")]
    [Tooltip("Marque uma ou mais direções. Cada uma vira um corredor de vento")]
    public DirecaoVento direcoes = DirecaoVento.Frente;

    [Header("Área de cada corredor")]
    [Tooltip("Comprimento do corredor, a partir do centro do objeto")]
    [Min(0.1f)]
    public float alcance = 6f;

    [Tooltip("Largura e altura do corredor")]
    [Min(0.1f)]
    public float largura = 2f;

    [Header("Força")]
    [Tooltip("Velocidade máxima com que o vento leva as coisas")]
    [Min(0f)]
    public float velocidadeVento = 8f;

    [Tooltip("Fixa = igual no corredor todo. MaisForteDePerto = mais forte perto do objeto")]
    public ModoForcaVento modoForca = ModoForcaVento.Fixa;

    [Tooltip("Força na ponta do corredor, em relação à força perto do objeto (só no modo MaisForteDePerto). 0 = nada, 1 = igual")]
    [Range(0f, 1f)]
    public float forcaNaPonta = 0.2f;

    [Tooltip("Quão rápido os objetos pegam a velocidade do vento. Maior = mais rápido")]
    [Min(0f)]
    public float resposta = 5f;

    [Tooltip("Ligado: objetos com mais massa são menos empurrados")]
    public bool considerarMassa = false;

    [Header("Ritmo")]
    [Tooltip("Ligado: o vento liga e desliga sozinho. Desligado: sopra o tempo todo")]
    public bool usarRitmo = false;

    [Tooltip("Segundos com o vento ligado")]
    [Min(0f)]
    public float tempoLigado = 2f;

    [Tooltip("Segundos com o vento desligado")]
    [Min(0f)]
    public float tempoDesligado = 2f;

    [Tooltip("Atrasa o ritmo deste vento. Use em ventos vizinhos para eles não ligarem juntos")]
    [Min(0f)]
    public float atraso = 0f;

    [Tooltip("Segundos para o vento ganhar e perder força ao ligar e desligar. 0 = liga e desliga de uma vez")]
    [Min(0f)]
    public float tempoTransicao = 0.3f;

    // Força do ritmo agora: 0 = desligado, 1 = ligado
    public float ForcaDoRitmo => CalcularForcaDoRitmo();

    // true enquanto o vento está soprando
    public bool EstaLigado => ForcaDoRitmo > 0f;

    [Header("Objeto")]
    [Tooltip("Mostra ou esconde o desenho do objeto durante o jogo")]
    public bool visivel = true;

    [Tooltip("Ligado: o objeto é sólido. Desligado: dá para atravessar")]
    public bool temColisao = true;

    [Tooltip("Ligado: paredes e objetos no caminho bloqueiam o vento")]
    public bool paredesBloqueiam = false;

    [Header("Editor")]
    [Tooltip("Cor da área do vento na cena")]
    public Color corDaArea = new Color(0.3f, 0.8f, 1f, 1f);

    // Vetores das 26 direções, na mesma ordem do enum
    private static readonly Vector3[] vetores =
    {
        new Vector3(0, 0, 1), new Vector3(0, 0, -1), new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
        new Vector3(0, 1, 0), new Vector3(0, -1, 0),
        new Vector3(1, 0, 1), new Vector3(-1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1),
        new Vector3(0, 1, 1), new Vector3(0, -1, 1), new Vector3(0, 1, -1), new Vector3(0, -1, -1),
        new Vector3(1, 1, 0), new Vector3(1, -1, 0), new Vector3(-1, 1, 0), new Vector3(-1, -1, 0),
        new Vector3(1, 1, 1), new Vector3(1, -1, 1), new Vector3(-1, 1, 1), new Vector3(-1, -1, 1),
        new Vector3(1, 1, -1), new Vector3(1, -1, -1), new Vector3(-1, 1, -1), new Vector3(-1, -1, -1)
    };

    private readonly HashSet<Rigidbody> empurrados = new HashSet<Rigidbody>(); // objetos já empurrados neste corredor

    // Roda uma vez quando o jogo começa
    void Start()
    {
        // Mostra ou esconde o desenho
        foreach (Renderer desenho in GetComponentsInChildren<Renderer>())
            desenho.enabled = visivel;

        // Liga ou desliga a colisão
        foreach (Collider colisor in GetComponentsInChildren<Collider>())
            colisor.enabled = temColisao;
    }

    // Empurra tudo que está nos corredores, a cada passo da física
    void FixedUpdate()
    {
        if (!EstaLigado) return;

        for (int i = 0; i < vetores.Length; i++)
        {
            if (((int)direcoes & (1 << i)) != 0)
                EmpurrarCorredor(DirecaoNoMundo(i));
        }
    }

    // Empurra os objetos de um corredor
    void EmpurrarCorredor(Vector3 direcao)
    {
        Vector3 origem = transform.position;
        Vector3 centro = origem + direcao * alcance / 2f;
        Vector3 metadeTamanho = new Vector3(largura / 2f, largura / 2f, alcance / 2f);

        Collider[] tocados = Physics.OverlapBox(centro, metadeTamanho, RotacaoDoCorredor(direcao), ~0, QueryTriggerInteraction.Ignore);
        empurrados.Clear();

        foreach (Collider colisor in tocados)
        {
            Rigidbody corpo = colisor.attachedRigidbody;

            // Ignora objetos fixos, o próprio vento e objetos já empurrados
            if (corpo == null || corpo.isKinematic) continue;
            if (colisor.transform.IsChildOf(transform)) continue;
            if (!empurrados.Add(corpo)) continue;

            // Distância do objeto até o começo do corredor
            float distancia = Mathf.Clamp(Vector3.Dot(corpo.position - origem, direcao), 0f, alcance);

            if (paredesBloqueiam && Bloqueado(corpo, direcao, distancia))
                continue;

            Empurrar(corpo, direcao * velocidadeVento * Intensidade(distancia));
        }
    }

    // Força de 0 a 1, de acordo com a distância e o modo escolhido
    float Intensidade(float distancia)
    {
        float distanciaFator = modoForca == ModoForcaVento.Fixa ? 1f : Mathf.Lerp(1f, forcaNaPonta, distancia / alcance);
        return distanciaFator * ForcaDoRitmo;
    }

    // Força do ritmo de 0 a 1, de acordo com o tempo desde que a fase começou
    float CalcularForcaDoRitmo()
    {
        if (!usarRitmo) return 1f;

        float ciclo = tempoLigado + tempoDesligado;
        if (ciclo <= 0f) return 1f;

        // Em que ponto do ciclo estamos (volta ao 0 a cada ciclo)
        float t = Mathf.Repeat(Time.timeSinceLevelLoad - atraso, ciclo);

        if (t >= tempoLigado) return 0f; // parte desligada do ciclo
        if (tempoTransicao <= 0f) return 1f;

        // Ganha força no começo e perde no fim da parte ligada
        float subindo = t / tempoTransicao;
        float descendo = (tempoLigado - t) / tempoTransicao;
        return Mathf.Clamp01(Mathf.Min(subindo, descendo));
    }

    // Empurra um objeto até ele chegar na velocidade do vento
    void Empurrar(Rigidbody corpo, Vector3 velocidade)
    {
        if (considerarMassa)
            velocidade /= Mathf.Max(corpo.mass, 0.01f);

        // O player recebe o vento dos lados direto na velocidade dele
        Player player = corpo.GetComponent<Player>();
        if (player != null)
        {
            player.AdicionarVento(new Vector3(velocidade.x, 0f, velocidade.z));
            velocidade = new Vector3(0f, velocidade.y, 0f); // o vento para cima ou para baixo segue igual aos outros objetos
        }

        if (velocidade == Vector3.zero) return;

        Vector3 direcao = velocidade.normalized;
        float velocidadeAlvo = velocidade.magnitude;
        float velocidadeAtual = Vector3.Dot(corpo.velocity, direcao); // quanto o objeto já anda nessa direção

        // Só empurra até o objeto chegar na velocidade do vento
        if (velocidadeAtual < velocidadeAlvo)
            corpo.AddForce(direcao * (velocidadeAlvo - velocidadeAtual) * resposta, ForceMode.Acceleration);
    }

    // true se tem alguma coisa entre o começo do corredor e o objeto
    bool Bloqueado(Rigidbody corpo, Vector3 direcao, float distancia)
    {
        Vector3 inicio = corpo.position - direcao * distancia; // ponto do começo do corredor alinhado com o objeto
        RaycastHit[] acertos = Physics.RaycastAll(inicio, direcao, distancia, ~0, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit acerto in acertos)
        {
            if (acerto.collider.transform.IsChildOf(transform)) continue; // o próprio vento
            if (acerto.rigidbody == corpo) continue;                      // o próprio objeto
            return true;
        }
        return false;
    }

    // Direção número i, já girada junto com o objeto
    Vector3 DirecaoNoMundo(int i)
    {
        return transform.TransformDirection(vetores[i].normalized);
    }

    // Rotação da caixa do corredor apontando para a direção
    Quaternion RotacaoDoCorredor(Vector3 direcao)
    {
        Vector3 cima = Mathf.Abs(Vector3.Dot(direcao, transform.up)) > 0.99f ? transform.forward : transform.up;
        return Quaternion.LookRotation(direcao, cima);
    }

    // Desenha a área do vento na cena (só no editor)
    void OnDrawGizmos()
    {
        // Durante o jogo, a área fica mais apagada enquanto o vento está desligado
        float brilho = Application.isPlaying ? Mathf.Lerp(0.25f, 1f, ForcaDoRitmo) : 1f;
        Color cor = new Color(corDaArea.r, corDaArea.g, corDaArea.b, corDaArea.a * brilho);

        for (int i = 0; i < vetores.Length; i++)
        {
            if (((int)direcoes & (1 << i)) == 0) continue;

            Vector3 direcao = DirecaoNoMundo(i);
            Vector3 centro = transform.position + direcao * alcance / 2f;
            Vector3 tamanho = new Vector3(largura, largura, alcance);

            // Caixa do corredor
            Gizmos.matrix = Matrix4x4.TRS(centro, RotacaoDoCorredor(direcao), Vector3.one);
            Gizmos.color = new Color(cor.r, cor.g, cor.b, 0.12f * brilho);
            Gizmos.DrawCube(Vector3.zero, tamanho);
            Gizmos.color = cor;
            Gizmos.DrawWireCube(Vector3.zero, tamanho);

            // Seta no meio do corredor
            Gizmos.matrix = Matrix4x4.identity;
            Vector3 ponta = transform.position + direcao * alcance;
            Gizmos.DrawLine(transform.position, ponta);
            Gizmos.DrawWireSphere(ponta, Mathf.Min(0.2f, largura / 4f));
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
}
