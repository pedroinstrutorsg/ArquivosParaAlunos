using UnityEngine;

// Estados do movimento do player
public enum EstadoMovimento
{
    Parado,
    Andando,
    Correndo,
    Deslizando   // soltou as teclas, mas ainda está freando
}

public class Player : MonoBehaviour
{
    [Header("Andar / Correr")]
    [Tooltip("Velocidade ao andar")]
    public float velocidadeAndar = 5f;

    [Tooltip("Velocidade ao correr")]
    public float velocidadeCorrer = 9f;

    [Tooltip("Segundos andando até começar a correr. 0 = corre na hora")]
    public float tempoParaCorrer = 1.5f;

    [Tooltip("Ligado: a velocidade sobe aos poucos até a de correr.\nDesligado: troca de uma vez.")]
    public bool aumentoGradual = true;

    [Tooltip("Formato da subida de velocidade.\n1 = sobe por igual\nMaior que 1 = começa devagar\nMenor que 1 = começa rápido")]
    [Min(0.1f)]
    public float curvaAceleracao = 1f;

    [Tooltip("Ligado: só corre segurando a tecla de correr.")]
    public bool exigirTeclaCorrer = false;

    public KeyCode teclaCorrer = KeyCode.LeftShift; // tecla de correr

    [Header("Aceleração e Frenagem")]
    [Tooltip("Segundos para sair do parado até a velocidade de andar. 0 = arranca na hora")]
    [Min(0f)]
    public float tempoAcelerar = 0.1f;

    [Tooltip("Segundos para frear até parar. 0 = para na hora, 1 ou mais = parece gelo")]
    [Min(0f)]
    public float tempoFrear = 0.1f;

    [Tooltip("Controle da direção no ar. 0 = nenhum, 1 = igual ao chão")]
    [Range(0f, 1f)]
    public float controleNoAr = 0.5f;

    private EstadoMovimento estadoAtual = EstadoMovimento.Parado; // estado do movimento agora
    private float velocidadeAtual = 0f;                           // velocidade que o player quer ter

    // ===== Informações para outros scripts =====
    public EstadoMovimento EstadoAtual => estadoAtual; // estado do movimento

    // Velocidade real no chão (X e Z)
    public float VelocidadeAtual => rb != null ? new Vector3(rb.velocity.x - velocidadePlataforma.x, 0f, rb.velocity.z - velocidadePlataforma.z).magnitude : 0f;

    public bool EstaNoChao => contatosNoChao > 0; // true se está tocando o chão

    // Quantos pulos o player pode dar agora
    public int PulosDisponiveis
    {
        get
        {
            bool podePularDoChao = (Time.time - ultimoTempoNoChao <= tempoCoyote) && !pulouDoChao;
            return podePularDoChao ? pulosMaximos : pulosNoAr;
        }
    }

    // Quanto tempo de coyote time ainda resta
    public float CoyoteRestante
    {
        get
        {
            if (EstaNoChao) return tempoCoyote;
            if (pulouDoChao) return 0f;
            return Mathf.Max(0f, tempoCoyote - (Time.time - ultimoTempoNoChao));
        }
    }

    [Header("Pulo")]
    public float forcaPulo = 6f; // força do pulo

    [Min(0)]
    [Tooltip("Quantidade de pulos. 0 = não pula, 1 = normal, 2 = duplo...")]
    public int pulosMaximos = 1;

    [Tooltip("Segundos em que ainda dá para pular depois de sair de uma borda")]
    public float tempoCoyote = 0.15f;

    [Tooltip("Ligado: depois do pulo de coyote, ainda pode usar os pulos no ar.")]
    public bool coyotePermitePulosExtras = true;

    [Tooltip("Tempo mínimo entre um pulo e outro")]
    [Min(0f)]
    public float cooldownEntrePulos = 0.2f;

    // Quanto tempo falta para poder pular de novo
    public float CooldownPuloRestante => Mathf.Max(0f, cooldownEntrePulos - (Time.time - tempoUltimoPulo));

    [Header("Rotação do Mouse")]
    public float sensibilidadeMouse = 200f;   // velocidade do giro com o mouse
    public Transform cameraPersonagem;        // câmera do player (filha do player)
    public float limiteAnguloVertical = 80f;  // ângulo máximo para olhar para cima e para baixo

    // ===== Valores guardados no checkpoint =====
    // Guarda as configurações do player para devolver quando ele voltar ao checkpoint
    private class ValoresSalvos
    {
        public int pulosMaximos;
        public float velocidadeAndar, velocidadeCorrer, tempoParaCorrer;
        public bool aumentoGradual;
        public float tempoAcelerar, tempoFrear, controleNoAr;
        public float forcaPulo, tempoCoyote, cooldownEntrePulos;
        public bool coyotePermitePulosExtras;
    }

    // "static" = não se perde quando a cena recarrega
    private static ValoresSalvos valoresDoCheckpoint;   // valores guardados
    private static Vector3? checkpointDosValores;       // checkpoint onde os valores foram guardados

    private Rigidbody rb;               // física do player
    private float rotacaoVertical = 0f; // ângulo atual da câmera para cima/baixo
    private int contatosNoChao = 0;     // quantas plataformas o player está tocando

    private int pulosNoAr = 0;                // pulos que ainda sobram no ar
    private float proximaTrocaPermitida = 0f; // quando uma zona pode alterar o player de novo

    private float ultimoTempoNoChao = -999f; // último momento em que tocou o chão
    private bool pulouDoChao = false;        // true depois do pulo do chão, até tocar o chão de novo

    private float tempoUltimoPulo = -999f;   // momento do último pulo

    // Tempo depois do pulo em que o chão é ignorado (o pé ainda encosta no chão)
    private const float tempoIgnorarChaoAposPulo = 0.1f;

    private float tempoMovendo = 0f; // há quanto tempo o player está andando sem parar

    private GameObject plataformaEmbaixo;                 // plataforma que anda ou gira, embaixo do player
    private Vector3 velocidadePlataforma = Vector3.zero;  // velocidade dessa plataforma neste passo

    private float rotacaoHorizontal = 0f; // giro do player para os lados, aplicado na física

    // Converte o movimento do mouse em graus (sensibilidadeMouse 200 vira cerca de 4 graus por unidade)
    private const float escalaDoMouse = 0.02f;

    // Roda uma vez quando o jogo começa
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Deixa o movimento mais suave na tela
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Prende e esconde o cursor do mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Se tem checkpoint salvo, o player nasce nele
        if (CheckpointManager.posicaoCheckpoint.HasValue)
        {
            transform.position = CheckpointManager.posicaoCheckpoint.Value;
            transform.rotation = Quaternion.Euler(0f, CheckpointManager.rotacaoYCheckpoint, 0f);
        }

        // A física não gira o player; só o script gira
        rb.freezeRotation = true;
        rotacaoHorizontal = transform.eulerAngles.y;

        // Se nasceu no checkpoint dos valores guardados, recebe esses valores
        if (CheckpointManager.posicaoCheckpoint.HasValue
            && valoresDoCheckpoint != null
            && checkpointDosValores == CheckpointManager.posicaoCheckpoint.Value)
        {
            RestaurarValoresDoCheckpoint();
        }
    }

    // Roda a cada frame
    void Update()
    {
        RotacionarComMouse();

        // Passou por um checkpoint novo: guarda os valores
        if (CheckpointManager.posicaoCheckpoint.HasValue
            && checkpointDosValores != CheckpointManager.posicaoCheckpoint.Value)
        {
            SalvarValoresNoCheckpoint();
        }

        // true logo depois de pular
        bool acabouDePular = Time.time - tempoUltimoPulo < tempoIgnorarChaoAposPulo;

        // No chão: recarrega os pulos
        if (contatosNoChao > 0 && !acabouDePular)
        {
            ultimoTempoNoChao = Time.time;
            pulouDoChao = false;
            pulosNoAr = Mathf.Max(0, pulosMaximos - 1);
        }

        // Pode dar o pulo do chão: está no chão ou ainda dentro do coyote time
        bool podePularDoChao = (Time.time - ultimoTempoNoChao <= tempoCoyote) && !pulouDoChao;

        // Já passou o tempo mínimo desde o último pulo
        bool cooldownLiberado = Time.time - tempoUltimoPulo >= cooldownEntrePulos;

        if (Input.GetKeyDown(KeyCode.Space) && cooldownLiberado)
        {
            if (podePularDoChao && pulosMaximos > 0)
            {
                // Pulo do chão (ou de coyote, se já saiu do chão)
                bool puloDeCoyote = contatosNoChao == 0;

                Pular();

                if (puloDeCoyote && !coyotePermitePulosExtras)
                    pulosNoAr = 0;                 // sem pulos extras
                else
                    pulosNoAr = pulosMaximos - 1;  // pulos que sobram para o ar

                pulouDoChao = true;
                ultimoTempoNoChao = -999f;         // fecha o coyote time
            }
            else if (pulosNoAr > 0)
            {
                // Pulo no ar
                Pular();
                pulosNoAr--;
            }
        }

        // ESC solta o cursor do mouse
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Roda em passos fixos, junto com a física
    void FixedUpdate()
    {
        // Gira o player pela física, sem brigar com a interpolação
        rb.MoveRotation(Quaternion.Euler(0f, rotacaoHorizontal, 0f));
        MovimentarTeclas();
    }

    // Faz o player pular
    void Pular()
    {
        tempoUltimoPulo = Time.time;

        // Zera a velocidade de queda para todo pulo ter a mesma altura
        Vector3 v = rb.velocity;
        v.y = 0f;
        rb.velocity = v;

        // Empurra o player para cima
        rb.AddForce(Vector3.up * forcaPulo, ForceMode.VelocityChange);
    }

    // Troca a quantidade de pulos (usado pela ZonaDePulo). Retorna true se trocou.
    public bool DefinirPulos(int quantidade, float cooldown)
    {
        if (Time.time < proximaTrocaPermitida)
            return false;

        pulosMaximos = Mathf.Max(0, quantidade);
        pulosNoAr = pulosMaximos;
        proximaTrocaPermitida = Time.time + cooldown;
        return true;
    }

    // Guarda os valores atuais do player no checkpoint
    public void SalvarValoresNoCheckpoint()
    {
        valoresDoCheckpoint = new ValoresSalvos
        {
            pulosMaximos = pulosMaximos,
            velocidadeAndar = velocidadeAndar,
            velocidadeCorrer = velocidadeCorrer,
            tempoParaCorrer = tempoParaCorrer,
            aumentoGradual = aumentoGradual,
            tempoAcelerar = tempoAcelerar,
            tempoFrear = tempoFrear,
            controleNoAr = controleNoAr,
            forcaPulo = forcaPulo,
            tempoCoyote = tempoCoyote,
            cooldownEntrePulos = cooldownEntrePulos,
            coyotePermitePulosExtras = coyotePermitePulosExtras
        };
        checkpointDosValores = CheckpointManager.posicaoCheckpoint;

        Debug.Log("Checkpoint: valores do player salvos (" + pulosMaximos + " pulo(s))");
    }

    // Devolve ao player os valores guardados no checkpoint
    public void RestaurarValoresDoCheckpoint()
    {
        if (valoresDoCheckpoint == null) return;

        pulosMaximos = valoresDoCheckpoint.pulosMaximos;
        velocidadeAndar = valoresDoCheckpoint.velocidadeAndar;
        velocidadeCorrer = valoresDoCheckpoint.velocidadeCorrer;
        tempoParaCorrer = valoresDoCheckpoint.tempoParaCorrer;
        aumentoGradual = valoresDoCheckpoint.aumentoGradual;
        tempoAcelerar = valoresDoCheckpoint.tempoAcelerar;
        tempoFrear = valoresDoCheckpoint.tempoFrear;
        controleNoAr = valoresDoCheckpoint.controleNoAr;
        forcaPulo = valoresDoCheckpoint.forcaPulo;
        tempoCoyote = valoresDoCheckpoint.tempoCoyote;
        cooldownEntrePulos = valoresDoCheckpoint.cooldownEntrePulos;
        coyotePermitePulosExtras = valoresDoCheckpoint.coyotePermitePulosExtras;

        pulosNoAr = Mathf.Max(0, pulosMaximos - 1);
    }

    // ===== Usado pela ZonaDePulo =====
    // true enquanto nenhuma zona pode alterar o player
    public bool EmCooldownDeTroca => Time.time < proximaTrocaPermitida;

    // Começa o tempo de espera entre uma zona e outra
    public void IniciarCooldownDeTroca(float cooldown)
    {
        proximaTrocaPermitida = Time.time + cooldown;
    }

    // Transforma um tempo em segundos em aceleração (quanto a velocidade muda por segundo)
    float TempoParaAceleracao(float tempo)
    {
        if (tempo <= 0f)
            return 1000f; // muda na hora

        return velocidadeAndar / tempo;
    }

    // Move o player com as teclas WASD
    void MovimentarTeclas()
    {
        velocidadePlataforma = CalcularVelocidadePlataforma();

        // Parte da velocidade da plataforma no chão (X e Z)
        Vector3 plataformaNoChao = new Vector3(velocidadePlataforma.x, 0f, velocidadePlataforma.z);

        Vector3 direcao = Vector3.zero; // direção apertada nas teclas

        if (Input.GetKey(KeyCode.W))
            direcao += transform.forward;  // frente
        if (Input.GetKey(KeyCode.S))
            direcao -= transform.forward;  // trás
        if (Input.GetKey(KeyCode.A))
            direcao -= transform.right;    // esquerda
        if (Input.GetKey(KeyCode.D))
            direcao += transform.right;    // direita

        bool estaMovendo = direcao != Vector3.zero; // true se alguma tecla está apertada

        AtualizarEstado(estaMovendo);

        // Velocidade que o player quer ter (normalized: a diagonal não fica mais rápida)
        Vector3 alvo = direcao.normalized * velocidadeAtual;

        // Velocidade que o player tem agora em relação à plataforma, sem a queda (Y)
        Vector3 atual = new Vector3(rb.velocity.x, 0f, rb.velocity.z) - plataformaNoChao;

        float aceleracao; // quanto a velocidade pode mudar por segundo

        if (!estaMovendo)
        {
            // Sem teclas: freando
            aceleracao = TempoParaAceleracao(tempoFrear);
        }
        else if (Vector3.Dot(atual, alvo) < 0f)
        {
            // Virou para o lado contrário: usa a mais forte para virar rápido
            aceleracao = Mathf.Max(TempoParaAceleracao(tempoAcelerar), TempoParaAceleracao(tempoFrear));
        }
        else
        {
            // Andando: acelerando
            aceleracao = TempoParaAceleracao(tempoAcelerar);
        }

        // No ar tem menos controle
        if (!EstaNoChao)
            aceleracao *= controleNoAr;

        // Chega um pouco mais perto da velocidade alvo
        Vector3 novaVelocidade = Vector3.MoveTowards(atual, alvo, aceleracao * Time.fixedDeltaTime);

        // Soma a velocidade da plataforma: o player anda junto com ela
        novaVelocidade += plataformaNoChao;

        // Mantém a velocidade de queda/pulo
        novaVelocidade.y = rb.velocity.y;

        // Plataforma descendo: desce junto, sem descolar dela
        bool acabouDePular = Time.time - tempoUltimoPulo < tempoIgnorarChaoAposPulo;
        if (plataformaEmbaixo != null && !acabouDePular && velocidadePlataforma.y < 0f)
            novaVelocidade.y = Mathf.Min(novaVelocidade.y, velocidadePlataforma.y);

        rb.velocity = novaVelocidade;
    }

    // Velocidade da plataforma embaixo do player (zero se não tiver)
    Vector3 CalcularVelocidadePlataforma()
    {
        if (plataformaEmbaixo == null)
            return Vector3.zero;

        ObjetoFuncional movel = plataformaEmbaixo.GetComponent<ObjetoFuncional>();
        if (movel != null)
            return movel.Velocidade;

        ObjetoRotativo giratoria = plataformaEmbaixo.GetComponent<ObjetoRotativo>();
        if (giratoria != null)
            return giratoria.VelocidadeNoPonto(rb.position);

        return Vector3.zero;
    }

    // Devolve a plataforma que anda ou gira do objeto tocado (null se não for uma)
    GameObject PlataformaMovelDe(Collision colisao)
    {
        ObjetoFuncional movel = colisao.collider.GetComponentInParent<ObjetoFuncional>();
        if (movel != null)
            return movel.gameObject;

        ObjetoRotativo giratoria = colisao.collider.GetComponentInParent<ObjetoRotativo>();
        if (giratoria != null)
            return giratoria.gameObject;

        return null;
    }

    // Decide o estado (parado, andando, correndo, deslizando) e a velocidade
    void AtualizarEstado(bool estaMovendo)
    {
        // true se pode contar o tempo de corrida
        bool podeAcumularCorrida = !exigirTeclaCorrer || Input.GetKey(teclaCorrer);

        if (!estaMovendo)
        {
            tempoMovendo = 0f;
            velocidadeAtual = 0f;

            // Ainda se mexendo sem teclas = deslizando
            if (VelocidadeAtual > 0.1f)
                estadoAtual = EstadoMovimento.Deslizando;
            else
                estadoAtual = EstadoMovimento.Parado;
            return;
        }

        if (podeAcumularCorrida)
            tempoMovendo += Time.fixedDeltaTime; // soma o tempo andando
        else
            tempoMovendo = 0f;

        // Vai de 0 (começou a andar) até 1 (correndo)
        float progresso = tempoParaCorrer <= 0f ? 1f : Mathf.Clamp01(tempoMovendo / tempoParaCorrer);

        if (aumentoGradual)
        {
            // Velocidade sobe aos poucos, seguindo a curva
            float t = Mathf.Pow(progresso, curvaAceleracao);
            velocidadeAtual = Mathf.Lerp(velocidadeAndar, velocidadeCorrer, t);
        }
        else
        {
            // Troca de uma vez quando o tempo completa
            velocidadeAtual = progresso >= 1f ? velocidadeCorrer : velocidadeAndar;
        }

        estadoAtual = progresso >= 1f ? EstadoMovimento.Correndo : EstadoMovimento.Andando;
    }

    // Gira o player e a câmera com o mouse
    void RotacionarComMouse()
    {
        // Quanto o mouse andou neste frame (sem suavização e sem depender do FPS)
        float mouseX = Input.GetAxisRaw("Mouse X") * sensibilidadeMouse * escalaDoMouse;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensibilidadeMouse * escalaDoMouse;

        // Guarda o giro para os lados; o FixedUpdate aplica
        rotacaoHorizontal += mouseX;

        if (cameraPersonagem != null)
        {
            // Mouse para cima = olhar para cima
            rotacaoVertical -= mouseY;

            // Limita o quanto dá para olhar para cima e para baixo
            rotacaoVertical = Mathf.Clamp(rotacaoVertical, -limiteAnguloVertical, limiteAnguloVertical);

            // Gira só a câmera para cima e para baixo
            cameraPersonagem.localRotation = Quaternion.Euler(rotacaoVertical, 0f, 0f);
        }
    }

    // Chamado quando o player encosta em algo
    void OnCollisionEnter(Collision colisao)
    {
        if (colisao.gameObject.CompareTag("Plataforma"))
        {
            contatosNoChao++; // mais uma plataforma tocando
        }
    }

    // Chamado a cada passo enquanto o player encosta em algo
    void OnCollisionStay(Collision colisao)
    {
        GameObject plataforma = PlataformaMovelDe(colisao);
        if (plataforma == null) return;

        // Só conta se o player estiver em cima dela (o contato empurra para cima)
        for (int i = 0; i < colisao.contactCount; i++)
        {
            if (colisao.GetContact(i).normal.y > 0.5f)
            {
                plataformaEmbaixo = plataforma;
                return;
            }
        }
    }

    // Chamado quando o player deixa de encostar em algo
    void OnCollisionExit(Collision colisao)
    {
        if (colisao.gameObject.CompareTag("Plataforma"))
        {
            contatosNoChao = Mathf.Max(0, contatosNoChao - 1); // uma plataforma a menos (nunca negativo)
        }

        // Saiu da plataforma que anda ou gira
        if (plataformaEmbaixo != null && PlataformaMovelDe(colisao) == plataformaEmbaixo)
            plataformaEmbaixo = null;
    }
}