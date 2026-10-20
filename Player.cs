using UnityEngine;

// Estados possíveis do movimento do player
public enum EstadoMovimento
{
    Parado,
    Andando,
    Correndo,
    Deslizando   // soltou as teclas, mas ainda está freando (escorregando)
}

public class Player : MonoBehaviour
{
    [Header("Andar / Correr")]
    [Tooltip("Velocidade enquanto o player está andando (unidades por segundo)")]
    public float velocidadeAndar = 5f;

    [Tooltip("Velocidade quando o player entra no estado de corrida")]
    public float velocidadeCorrer = 9f;

    [Tooltip("Tempo (em segundos) andando sem parar até virar corrida. 0 = corre na hora")]
    public float tempoParaCorrer = 1.5f;

    [Tooltip("Ligado: a velocidade vai subindo aos poucos enquanto anda até virar corrida.\nDesligado: anda na velocidade normal e muda de uma vez para a de corrida.")]
    public bool aumentoGradual = true;

    [Tooltip("Formato da aceleração quando o Aumento Gradual está ligado.\n1 = sobe por igual (linear)\nMaior que 1 = começa devagar e acelera no final (ex: 2)\nMenor que 1 = acelera rápido no começo e suaviza no final (ex: 0.5)")]
    [Min(0.1f)]
    public float curvaAceleracao = 1f;

    [Tooltip("Ligado: só acumula tempo de corrida enquanto segura a tecla de correr.\nDesligado: basta andar sem parar.")]
    public bool exigirTeclaCorrer = false;

    public KeyCode teclaCorrer = KeyCode.LeftShift;

    // ========================================
    // ACELERAÇÃO E FRENAGEM (substitui o antigo "deslizamento")
    // Em vez de um número de 0 a 1, usamos TEMPOS em segundos:
    // é mais fácil de entender e o player nunca fica travado.
    // ========================================
    [Header("Aceleração e Frenagem")]
    [Tooltip("Segundos para sair do parado e chegar na velocidade de andar.\n0 = arranca na hora\n0.1 = ágil (padrão de plataforma)\n0.5 ou mais = pesado")]
    [Min(0f)]
    public float tempoAcelerar = 0.1f;

    [Tooltip("Segundos para frear da velocidade de andar até parar.\n0 = para na hora\n0.1 = suave\n1 ou mais = parece gelo")]
    [Min(0f)]
    public float tempoFrear = 0.1f;

    [Tooltip("Quanto o jogador consegue mudar a direção no ar.\n0 = nenhum controle (mantém o embalo do pulo)\n0.5 = metade do controle do chão\n1 = igual ao chão")]
    [Range(0f, 1f)]
    public float controleNoAr = 0.5f;

    private EstadoMovimento estadoAtual = EstadoMovimento.Parado; // estado atual do movimento
    private float velocidadeAtual = 0f;                           // velocidade que o player QUER ter agora

    public EstadoMovimento EstadoAtual => estadoAtual; // outros scripts (ex: animação) podem ler o estado

    // ===== Informações para outros scripts (ex: UI de debug) =====
    // Velocidade REAL no chão (X e Z), já contando a aceleração e a frenagem
    public float VelocidadeAtual => rb != null ? new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude : 0f;
    public bool EstaNoChao => contatosNoChao > 0;

    // Quantos pulos o player consegue dar AGORA
    public int PulosDisponiveis
    {
        get
        {
            // No chão ou dentro do coyote time (sem ter pulado): tem todos os pulos
            bool podePularDoChao = (Time.time - ultimoTempoNoChao <= tempoCoyote) && !pulouDoChao;
            return podePularDoChao ? pulosMaximos : pulosNoAr;
        }
    }

    // Quanto tempo de coyote time ainda resta
    public float CoyoteRestante
    {
        get
        {
            if (EstaNoChao) return tempoCoyote;   // no chão: janela cheia
            if (pulouDoChao) return 0f;           // já pulou: coyote não vale mais
            return Mathf.Max(0f, tempoCoyote - (Time.time - ultimoTempoNoChao));
        }
    }

    [Header("Pulo")]
    public float forcaPulo = 6f;       // Força aplicada verticalmente ao pular

    [Min(0)]
    [Tooltip("0 = não pula, 1 = pulo normal (padrão), 2 = pulo duplo, 3 = pulo triplo...")]
    public int pulosMaximos = 1;

    [Tooltip("Coyote time: tempo (em segundos) que o player ainda pode dar o pulo do chão depois de sair de uma borda")]
    public float tempoCoyote = 0.15f;

    [Tooltip("Ligado: depois de pular pelo coyote time, ainda pode usar os pulos de ar (ex: pulo duplo).\nDesligado: o pulo do coyote é o ÚNICO pulo até encostar no chão de novo.")]
    public bool coyotePermitePulosExtras = true;

    [Tooltip("Tempo mínimo (em segundos) entre um pulo e outro.\n0 = pode pular de novo na hora\n0.2 = evita pulos duplos sem querer (padrão)\n0.5 ou mais = pulo duplo bem lento")]
    [Min(0f)]
    public float cooldownEntrePulos = 0.2f;

    // Quanto tempo falta para poder pular de novo (útil para UI de debug)
    public float CooldownPuloRestante => Mathf.Max(0f, cooldownEntrePulos - (Time.time - tempoUltimoPulo));

    [Header("Rotação do Mouse")]
    public float sensibilidadeMouse = 200f;   // Quanto o mouse influencia a rotação (quanto maior, mais sensível)
    public Transform cameraPersonagem;        // Referência da câmera (deve ser filha do personagem na Hierarchy)
    public float limiteAnguloVertical = 80f;  // Limite máximo de ângulo para olhar pra cima/baixo (evita giro de 360°)

    // ========================================
    // VALORES DO CHECKPOINT
    // Quando o player passa por um checkpoint novo, guardamos os valores dele.
    // Quando ele volta para o checkpoint (a cena recarrega), recebe esses valores de volta.
    // "static" = sobrevive ao reload da cena, igual ao CheckpointManager.
    // ========================================
    private class ValoresSalvos
    {
        public int pulosMaximos;
        public float velocidadeAndar, velocidadeCorrer, tempoParaCorrer;
        public bool aumentoGradual;
        public float tempoAcelerar, tempoFrear, controleNoAr;
        public float forcaPulo, tempoCoyote, cooldownEntrePulos;
        public bool coyotePermitePulosExtras;
    }

    private static ValoresSalvos valoresDoCheckpoint;   // os valores guardados
    private static Vector3? checkpointDosValores;       // de qual checkpoint esses valores são

    private Rigidbody rb;              // Referência ao componente Rigidbody do personagem, usado para física e movimento
    private float rotacaoVertical = 0f; // Guarda o ângulo vertical atual da câmera (acumulado a cada frame)
    private int contatosNoChao = 0;    // Conta quantas plataformas o personagem está tocando ao mesmo tempo (usado para permitir o pulo)

    private int pulosNoAr = 0;                // quantos pulos ainda podem ser usados no ar
    private float proximaTrocaPermitida = 0f; // momento (Time.time) a partir do qual pode trocar os pulos de novo

    private float ultimoTempoNoChao = -999f; // COYOTE: último momento em que o player estava tocando o chão
    private bool pulouDoChao = false;        // COYOTE: impede usar o pulo do chão duas vezes

    private float tempoUltimoPulo = -999f;   // COOLDOWN: momento (Time.time) do último pulo

    // Logo depois de pular, o pé ainda encosta no chão por alguns instantes.
    // Durante esse tempo o chão NÃO recarrega os pulos (senão o pulo voltaria na hora).
    private const float tempoIgnorarChaoAposPulo = 0.1f;

    private float tempoMovendo = 0f;         // CORRIDA: há quanto tempo o player está andando sem parar

    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Pega o Rigidbody que está no mesmo GameObject deste script

        // Interpolação deixa o movimento visualmente mais suave (a física roda em passos fixos,
        // a interpolação "preenche" os frames entre um passo e outro)
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Trava o cursor do mouse no centro da tela e o deixa invisível (padrão em jogos FPS/terceira pessoa)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Se já existe um checkpoint salvo (sobrevive ao reload de cena),
        // reposiciona o player nele em vez de nascer no ponto padrão da cena.
        if (CheckpointManager.posicaoCheckpoint.HasValue)
        {
            transform.position = CheckpointManager.posicaoCheckpoint.Value;
            transform.rotation = Quaternion.Euler(0f, CheckpointManager.rotacaoYCheckpoint, 0f);
        }

        // CHECKPOINT: se está nascendo no checkpoint que tem valores guardados, recebe esses valores
        if (CheckpointManager.posicaoCheckpoint.HasValue
            && valoresDoCheckpoint != null
            && checkpointDosValores == CheckpointManager.posicaoCheckpoint.Value)
        {
            RestaurarValoresDoCheckpoint();
        }
    }

    void Update()
    {
        // Rotação é feita no Update porque o mouse é lido a cada frame renderizado (mais suave visualmente)
        RotacionarComMouse();

        // CHECKPOINT: o CheckpointManager mudou de posição = o player passou por um checkpoint novo
        if (CheckpointManager.posicaoCheckpoint.HasValue
            && checkpointDosValores != CheckpointManager.posicaoCheckpoint.Value)
        {
            SalvarValoresNoCheckpoint();
        }

        // Acabou de pular? (o pé ainda pode estar encostando no chão)
        bool acabouDePular = Time.time - tempoUltimoPulo < tempoIgnorarChaoAposPulo;

        // CHÃO: tocando o chão (e não no instante do pulo) recarrega tudo
        if (contatosNoChao > 0 && !acabouDePular)
        {
            ultimoTempoNoChao = Time.time;              // COYOTE: último momento no chão
            pulouDoChao = false;                        // libera o pulo do chão
            pulosNoAr = Mathf.Max(0, pulosMaximos - 1); // recarrega os pulos de ar
        }

        // COYOTE: conta como "no chão" se está tocando o chão OU saiu dele há menos de tempoCoyote segundos
        bool podePularDoChao = (Time.time - ultimoTempoNoChao <= tempoCoyote) && !pulouDoChao;

        // COOLDOWN: só pode pular se já passou o tempo mínimo desde o último pulo
        bool cooldownLiberado = Time.time - tempoUltimoPulo >= cooldownEntrePulos;

        if (Input.GetKeyDown(KeyCode.Space) && cooldownLiberado)
        {
            if (podePularDoChao && pulosMaximos > 0)
            {
                // Pulo do chão OU do coyote time: os dois gastam exatamente 1 pulo.
                // Se não está tocando o chão, este é um pulo de coyote
                bool puloDeCoyote = contatosNoChao == 0;

                Pular();

                if (puloDeCoyote && !coyotePermitePulosExtras)
                    pulosNoAr = 0;                 // coyote sem extras: acabaram os pulos
                else
                    pulosNoAr = pulosMaximos - 1;  // sobram (pulosMaximos - 1) para usar no ar
                pulouDoChao = true;            // só volta a ser false quando encostar no chão de novo
                ultimoTempoNoChao = -999f;     // fecha a janela do coyote na hora
            }
            else if (pulosNoAr > 0)
            {
                // Pulo no ar (ex: segundo pulo do pulo duplo)
                Pular();
                pulosNoAr--;
            }
        }

        // Tecla de escape (ESC): libera o cursor do mouse
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void FixedUpdate()
    {
        // Movimento é feito no FixedUpdate porque envolve física (Rigidbody)
        MovimentarTeclas();
    }

    // Aplica o pulo
    void Pular()
    {
        tempoUltimoPulo = Time.time; // COOLDOWN: guarda o momento do pulo

        // Zera a velocidade vertical antes de pular, assim o pulo duplo tem sempre a mesma altura
        Vector3 v = rb.velocity;
        v.y = 0f;
        rb.velocity = v;

        // Aplica uma força instantânea para cima (ForceMode.VelocityChange ignora a massa do objeto)
        rb.AddForce(Vector3.up * forcaPulo, ForceMode.VelocityChange);
    }

    // Chamado pela ZonaDePulo. Troca a quantidade de pulos se não estiver em cooldown.
    // Retorna true se a troca aconteceu.
    public bool DefinirPulos(int quantidade, float cooldown)
    {
        if (Time.time < proximaTrocaPermitida)
            return false;

        pulosMaximos = Mathf.Max(0, quantidade); // sem limite máximo, só não deixa ser negativo
        pulosNoAr = pulosMaximos; // redefine os pulos na hora
        proximaTrocaPermitida = Time.time + cooldown;
        return true;
    }

    // Guarda os valores ATUAIS do player (chamado sozinho ao passar por um checkpoint novo)
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

    // Devolve ao player os valores guardados no checkpoint.
    // Público: se algum script seu levar o player ao checkpoint SEM recarregar a cena, chame este método.
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

        // Recarrega os pulos de ar com a quantidade restaurada
        pulosNoAr = Mathf.Max(0, pulosMaximos - 1);
    }

    // ===== Usado pela ZonaDePulo para alterar QUALQUER variável =====
    // true enquanto o player ainda não pode ser alterado por outra zona
    public bool EmCooldownDeTroca => Time.time < proximaTrocaPermitida;

    // Começa a contar o cooldown (nenhuma zona altera o player até ele acabar)
    public void IniciarCooldownDeTroca(float cooldown)
    {
        proximaTrocaPermitida = Time.time + cooldown;
    }

    // Transforma um TEMPO (segundos) em ACELERAÇÃO (quanto a velocidade muda por segundo)
    // Ex: andar a 5 u/s e tempo 0.1s -> a velocidade muda 50 u/s a cada segundo
    float TempoParaAceleracao(float tempo)
    {
        if (tempo <= 0f)
            return 1000f; // tempo zero = muda praticamente na hora (em 1 passo de física)

        return velocidadeAndar / tempo;
    }

    // Lê as teclas WASD e movimenta o personagem com base na direção que ele está olhando
    void MovimentarTeclas()
    {
        Vector3 direcao = Vector3.zero; // Vetor que vai acumular a direção do movimento

        // transform.forward/right são baseados na rotação atual do personagem,
        // ou seja, "frente" sempre é para onde ele está olhando
        if (Input.GetKey(KeyCode.W))
            direcao += transform.forward;  // Anda para frente
        if (Input.GetKey(KeyCode.S))
            direcao -= transform.forward;  // Anda para trás
        if (Input.GetKey(KeyCode.A))
            direcao -= transform.right;    // Anda para a esquerda
        if (Input.GetKey(KeyCode.D))
            direcao += transform.right;    // Anda para a direita

        bool estaMovendo = direcao != Vector3.zero;

        // Descobre a velocidade de acordo com o estado (parado / andando / correndo)
        AtualizarEstado(estaMovendo);

        // Normalized garante que andar na diagonal (ex: W+D) não seja mais rápido que andar reto
        // Essa é a velocidade que o player QUER ter (o "alvo")
        Vector3 alvo = direcao.normalized * velocidadeAtual;

        // Velocidade horizontal que o player TEM agora (sem o Y, que é queda/pulo)
        Vector3 atual = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // ========================================
        // ESCOLHE A ACELERAÇÃO DESTE PASSO
        // ========================================
        float aceleracao;

        if (!estaMovendo)
        {
            // Soltou as teclas: está freando
            aceleracao = TempoParaAceleracao(tempoFrear);
        }
        else if (Vector3.Dot(atual, alvo) < 0f)
        {
            // Virou para o lado oposto (ex: andava pra frente e apertou S):
            // usa a mais forte entre acelerar e frear, para a virada ser rápida
            aceleracao = Mathf.Max(TempoParaAceleracao(tempoAcelerar), TempoParaAceleracao(tempoFrear));
        }
        else
        {
            // Andando normalmente: acelerando até o alvo
            aceleracao = TempoParaAceleracao(tempoAcelerar);
        }

        // No ar o jogador tem menos controle (mantém mais o embalo do pulo)
        if (!EstaNoChao)
            aceleracao *= controleNoAr;

        // ========================================
        // APROXIMA A VELOCIDADE DO ALVO
        // MoveTowards anda uma quantidade FIXA por passo (aceleração x tempo).
        // Diferente do Lerp, ela sempre chega exatamente no alvo e nunca "trava" no caminho.
        // ========================================
        Vector3 novaVelocidade = Vector3.MoveTowards(atual, alvo, aceleracao * Time.fixedDeltaTime);

        // Mantém a velocidade vertical atual (queda/pulo) e só altera o movimento horizontal (X e Z)
        novaVelocidade.y = rb.velocity.y;

        // Aplica a velocidade final diretamente no Rigidbody
        rb.velocity = novaVelocidade;
    }

    // CORRIDA: decide o estado atual e a velocidade que deve ser usada
    void AtualizarEstado(bool estaMovendo)
    {
        // Se exigir a tecla de correr e ela não estiver pressionada, não acumula tempo de corrida
        bool podeAcumularCorrida = !exigirTeclaCorrer || Input.GetKey(teclaCorrer);

        if (!estaMovendo)
        {
            // Soltou as teclas: zera o tempo de corrida e o alvo de velocidade
            tempoMovendo = 0f;
            velocidadeAtual = 0f;

            // DESLIZANDO: sem teclas, mas o corpo ainda está se mexendo no chão (X e Z)
            // Abaixo de 0.1 já conta como parado
            if (VelocidadeAtual > 0.1f)
                estadoAtual = EstadoMovimento.Deslizando;
            else
                estadoAtual = EstadoMovimento.Parado;
            return;
        }

        if (podeAcumularCorrida)
            tempoMovendo += Time.fixedDeltaTime; // continua andando: soma o tempo
        else
            tempoMovendo = 0f;                   // soltou a tecla de correr: volta a andar

        // Progresso de 0 (acabou de começar a andar) até 1 (virou corrida)
        float progresso = tempoParaCorrer <= 0f ? 1f : Mathf.Clamp01(tempoMovendo / tempoParaCorrer);

        if (aumentoGradual)
        {
            // Velocidade sobe aos poucos; elevar o progresso à curva muda o "formato" da subida
            float t = Mathf.Pow(progresso, curvaAceleracao);
            velocidadeAtual = Mathf.Lerp(velocidadeAndar, velocidadeCorrer, t);
        }
        else
        {
            // Anda na velocidade normal e só troca quando o tempo completa
            velocidadeAtual = progresso >= 1f ? velocidadeCorrer : velocidadeAndar;
        }

        estadoAtual = progresso >= 1f ? EstadoMovimento.Correndo : EstadoMovimento.Andando;
    }

    // Controla a rotação do personagem e da câmera com base no movimento do mouse
    void RotacionarComMouse()
    {
        // Pega o quanto o mouse se moveu no eixo X (horizontal) e Y (vertical) desde o último frame
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadeMouse * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadeMouse * Time.deltaTime;

        // Rotaciona o personagem inteiro no eixo Y (esquerda/direita)
        transform.Rotate(Vector3.up * mouseX);

        // Só mexe na rotação vertical se a câmera estiver configurada no Inspector
        if (cameraPersonagem != null)
        {
            // Subtrai o mouseY: mover o mouse pra cima deve olhar pra cima (rotação negativa no eixo X)
            rotacaoVertical -= mouseY;

            // Limita o ângulo vertical para não deixar o personagem "olhar de cabeça para baixo"
            rotacaoVertical = Mathf.Clamp(rotacaoVertical, -limiteAnguloVertical, limiteAnguloVertical);

            // Aplica a rotação SOMENTE na câmera (localRotation), não no personagem inteiro
            cameraPersonagem.localRotation = Quaternion.Euler(rotacaoVertical, 0f, 0f);
        }
    }

    // Chamado automaticamente pela Unity quando o Collider deste objeto ENCOSTA em outro
    void OnCollisionEnter(Collision colisao)
    {
        if (colisao.gameObject.CompareTag("Plataforma"))
        {
            contatosNoChao++; // Soma 1 contato: cada plataforma tocada conta separadamente
            // A recarga dos pulos agora acontece no Update (veja "CHÃO")
        }
    }

    // Chamado automaticamente pela Unity quando o Collider deste objeto não está tocando no outro
    void OnCollisionExit(Collision colisao)
    {
        if (colisao.gameObject.CompareTag("Plataforma"))
        {
            // Remove esse contato; o pulo só é bloqueado quando o contador chegar a 0
            // (Max evita ficar negativo depois de morrer e ser teletransportado)
            contatosNoChao = Mathf.Max(0, contatosNoChao - 1);
        }
    }
}
