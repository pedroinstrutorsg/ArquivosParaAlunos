using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Como o jogador aciona a alavanca
public enum FormaDeAcionar
{
    Colidir,    // encostar na alavanca
    Atravessar, // passar por dentro dela
    Interagir   // clique mirando ou tecla perto
}

// O que acontece a cada acionamento
public enum ModoAlavanca
{
    Alternar,            // cada uso troca entre ligada e desligada
    UmaVez,              // aciona uma vez e trava
    EnquantoPressionada  // fica acionada só enquanto o player encosta ou segura o botão
}

// Como o objeto aparece quando está desligado
public enum VisualDesligado
{
    Normal,      // aparece igual
    Invisivel,   // some
    Transparente // fica meio transparente
}

// Um objeto da lista da alavanca e como ele se comporta desligado
[System.Serializable]
public class ObjetoControlado
{
    [Tooltip("Arraste aqui o objeto da cena")]
    public GameObject objeto;

    [Tooltip("Ligado: o objeto fica ligado quando a alavanca está desligada, e desligado quando ela está ligada")]
    public bool invertido = false;

    [Header("Quando desligado")]
    [Tooltip("Ligado: os scripts do objeto continuam funcionando")]
    public bool scriptsContinuam = false;

    [Tooltip("Ligado: se estiver caindo ou sendo empurrado, continua. Desligado: congela no lugar")]
    public bool fisicaContinua = false;

    [Tooltip("Como o objeto aparece")]
    public VisualDesligado visual = VisualDesligado.Transparente;

    [Tooltip("Quanto aparece no modo Transparente. 0 = some, 1 = normal")]
    [Range(0f, 1f)]
    public float transparencia = 0.3f;

    [Tooltip("Ligado: continua sólido. Desligado: dá para atravessar")]
    public bool temColisao = false;

    // Valores originais, guardados na primeira vez que desliga
    private bool guardado = false;
    private bool ligadoAgora = true;
    private readonly Dictionary<Behaviour, bool> scriptsOriginais = new Dictionary<Behaviour, bool>();
    private readonly Dictionary<Renderer, bool> desenhosOriginais = new Dictionary<Renderer, bool>();
    private readonly Dictionary<Renderer, Material[]> materiaisOriginais = new Dictionary<Renderer, Material[]>();
    private readonly Dictionary<Renderer, Material[]> materiaisTransparentes = new Dictionary<Renderer, Material[]>();
    private readonly Dictionary<Collider, bool> colisoresOriginais = new Dictionary<Collider, bool>();
    private readonly Dictionary<Rigidbody, bool> kinematicOriginal = new Dictionary<Rigidbody, bool>();
    private readonly Dictionary<Rigidbody, Vector3> velocidadeGuardada = new Dictionary<Rigidbody, Vector3>();
    private readonly Dictionary<Rigidbody, Vector3> giroGuardado = new Dictionary<Rigidbody, Vector3>();

    // Liga ou desliga o objeto
    public void Aplicar(bool ligado)
    {
        if (objeto == null || ligado == ligadoAgora) return;

        if (!ligado && !guardado)
            GuardarOriginais();

        if (!guardado) return; // nunca foi desligado: já está no estado original

        ligadoAgora = ligado;
        AplicarScripts(ligado);
        AplicarFisica(ligado);
        AplicarVisual(ligado);
        AplicarColisao(ligado);
    }

    // Guarda como o objeto está antes de desligar pela primeira vez
    void GuardarOriginais()
    {
        foreach (Behaviour script in objeto.GetComponentsInChildren<Behaviour>(true))
        {
            if (script is MonoBehaviour && !(script is Alavanca))
                scriptsOriginais[script] = script.enabled;
        }
        foreach (Renderer desenho in objeto.GetComponentsInChildren<Renderer>(true))
        {
            desenhosOriginais[desenho] = desenho.enabled;
            materiaisOriginais[desenho] = desenho.sharedMaterials;
        }
        foreach (Collider colisor in objeto.GetComponentsInChildren<Collider>(true))
            colisoresOriginais[colisor] = colisor.enabled;

        guardado = true;
    }

    // Desliga ou religa os scripts
    void AplicarScripts(bool ligado)
    {
        foreach (KeyValuePair<Behaviour, bool> par in scriptsOriginais)
        {
            if (par.Key == null) continue;
            par.Key.enabled = ligado || scriptsContinuam ? par.Value : false;
        }
    }

    // Congela ou solta a física
    void AplicarFisica(bool ligado)
    {
        if (fisicaContinua) return;

        // Pega os Rigidbody na hora, porque alguns são criados durante o jogo (ex: ObjetoCai)
        foreach (Rigidbody corpo in objeto.GetComponentsInChildren<Rigidbody>(true))
        {
            if (!ligado)
            {
                if (kinematicOriginal.ContainsKey(corpo)) continue;
                kinematicOriginal[corpo] = corpo.isKinematic;
                velocidadeGuardada[corpo] = corpo.isKinematic ? Vector3.zero : corpo.velocity;
                giroGuardado[corpo] = corpo.isKinematic ? Vector3.zero : corpo.angularVelocity;
                corpo.isKinematic = true; // para no lugar
            }
            else if (kinematicOriginal.ContainsKey(corpo))
            {
                corpo.isKinematic = kinematicOriginal[corpo];
                if (!corpo.isKinematic)
                {
                    corpo.velocity = velocidadeGuardada[corpo];       // volta com o mesmo movimento
                    corpo.angularVelocity = giroGuardado[corpo];
                }
            }
        }

        if (ligado)
        {
            kinematicOriginal.Clear();
            velocidadeGuardada.Clear();
            giroGuardado.Clear();
        }
    }

    // Deixa normal, invisível ou transparente
    void AplicarVisual(bool ligado)
    {
        foreach (KeyValuePair<Renderer, bool> par in desenhosOriginais)
        {
            Renderer desenho = par.Key;
            if (desenho == null) continue;

            // Volta ao normal
            desenho.enabled = par.Value;
            desenho.sharedMaterials = materiaisOriginais[desenho];

            if (ligado) continue;

            if (visual == VisualDesligado.Invisivel)
                desenho.enabled = false;
            else if (visual == VisualDesligado.Transparente)
            {
                // Cria as cópias transparentes só uma vez
                if (!materiaisTransparentes.ContainsKey(desenho))
                    materiaisTransparentes[desenho] = MateriaisTransparentes(materiaisOriginais[desenho]);
                desenho.sharedMaterials = materiaisTransparentes[desenho];
            }
        }
    }

    // Liga ou desliga a colisão
    void AplicarColisao(bool ligado)
    {
        foreach (KeyValuePair<Collider, bool> par in colisoresOriginais)
        {
            if (par.Key == null) continue;
            par.Key.enabled = ligado || temColisao ? par.Value : false;
        }
    }

    // Cópias dos materiais em modo transparente (shader Standard)
    Material[] MateriaisTransparentes(Material[] originais)
    {
        Material[] copias = new Material[originais.Length];
        for (int i = 0; i < originais.Length; i++)
        {
            if (originais[i] == null) continue;

            Material copia = new Material(originais[i]);
            copia.SetFloat("_Mode", 2f); // modo Fade
            copia.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            copia.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            copia.SetInt("_ZWrite", 0);
            copia.DisableKeyword("_ALPHATEST_ON");
            copia.EnableKeyword("_ALPHABLEND_ON");
            copia.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            copia.renderQueue = (int)RenderQueue.Transparent;

            if (copia.HasProperty("_Color"))
            {
                Color cor = copia.color;
                cor.a = transparencia;
                copia.color = cor;
            }
            copias[i] = copia;
        }
        return copias;
    }
}

// Alavanca que liga e desliga os objetos da lista
public class Alavanca : MonoBehaviour
{
    [Header("Acionamento")]
    [Tooltip("Colidir = encostar. Atravessar = passar por dentro. Interagir = clique mirando ou tecla perto")]
    public FormaDeAcionar formaDeAcionar = FormaDeAcionar.Colidir;

    [Tooltip("Alternar = cada uso troca. UmaVez = aciona e trava. EnquantoPressionada = só enquanto encosta ou segura")]
    public ModoAlavanca modo = ModoAlavanca.Alternar;

    [Tooltip("Estado da alavanca quando a fase começa")]
    public bool comecaLigada = true;

    [Tooltip("Tempo mínimo entre um acionamento e outro")]
    [Min(0f)]
    public float cooldown = 0.3f;

    [Header("Tempo")]
    [Tooltip("Segundos até a alavanca voltar sozinha ao estado inicial depois de acionada. 0 = não volta (não vale no modo EnquantoPressionada)")]
    [Min(0f)]
    public float tempoParaVoltar = 0f;

    [Tooltip("Nos últimos segundos antes de voltar, a alavanca pisca para avisar o jogador. 0 = não pisca")]
    [Min(0f)]
    public float tempoAviso = 1.5f;

    [Header("Interagir")]
    [Tooltip("Ligado: aciona clicando com a mira (centro da tela) na alavanca")]
    public bool usarClique = true;

    [Tooltip("Distância máxima do clique")]
    [Min(0f)]
    public float distanciaClique = 4f;

    [Tooltip("Ligado: aciona apertando a tecla perto da alavanca")]
    public bool usarTecla = true;

    public KeyCode tecla = KeyCode.E; // tecla de interagir

    [Tooltip("Distância máxima para usar a tecla")]
    [Min(0f)]
    public float distanciaTecla = 2.5f;

    [Header("Cor")]
    public Color corLigada = new Color(0.2f, 0.8f, 0.2f);   // cor com a alavanca ligada
    public Color corDesligada = new Color(0.8f, 0.2f, 0.2f); // cor com a alavanca desligada

    [Header("Objetos")]
    [Tooltip("Objetos que esta alavanca liga e desliga")]
    public List<ObjetoControlado> objetos = new List<ObjetoControlado>();

    private bool ligada;                   // estado atual da alavanca
    private bool travada = false;          // true depois do uso no modo UmaVez
    private float proximoUso = 0f;         // quando pode acionar de novo
    private int contatosDoPlayer = 0;      // colliders do player encostando na alavanca
    private Player player;                 // o player da cena
    private Renderer desenho;              // desenho da alavanca, para trocar a cor
    private float voltaEm = -1f;           // quando a alavanca volta sozinha (-1 = não volta)

    // true se a alavanca está ligada agora
    public bool Ligada => ligada;

    // Segundos até a alavanca voltar sozinha (0 se não estiver contando)
    public float TempoRestante => voltaEm < 0f ? 0f : Mathf.Max(0f, voltaEm - Time.time);

    // Roda uma vez quando o jogo começa
    void Start()
    {
        player = FindObjectOfType<Player>();
        desenho = GetComponent<Renderer>();

        // Atravessar precisa de trigger; Colidir precisa de colisão sólida
        foreach (Collider colisor in GetComponents<Collider>())
        {
            if (formaDeAcionar == FormaDeAcionar.Atravessar) colisor.isTrigger = true;
            if (formaDeAcionar == FormaDeAcionar.Colidir) colisor.isTrigger = false;
        }

        ligada = comecaLigada;
        AplicarEstado();
    }

    // Roda a cada frame: cuida do tempo e do modo Interagir
    void Update()
    {
        AtualizarTempo();

        if (formaDeAcionar != FormaDeAcionar.Interagir) return;

        bool apertou = (usarClique && Input.GetMouseButtonDown(0) && MirandoNaAlavanca())
                    || (usarTecla && Input.GetKeyDown(tecla) && PlayerPerto());

        if (modo == ModoAlavanca.EnquantoPressionada)
        {
            bool segurando = (usarClique && Input.GetMouseButton(0) && MirandoNaAlavanca())
                          || (usarTecla && Input.GetKey(tecla) && PlayerPerto());
            Pressionar(segurando);
        }
        else if (apertou)
        {
            Acionar();
        }
    }

    // Aciona a alavanca (Alternar ou UmaVez)
    public void Acionar()
    {
        if (travada || Time.time < proximoUso) return;
        proximoUso = Time.time + cooldown;

        ligada = !ligada;
        if (modo == ModoAlavanca.UmaVez) travada = true;

        // Começa a contar para voltar sozinha (só quando saiu do estado inicial)
        voltaEm = tempoParaVoltar > 0f && ligada != comecaLigada ? Time.time + tempoParaVoltar : -1f;

        AplicarEstado();
    }

    // Volta ao estado inicial quando o tempo acaba e pisca antes disso
    void AtualizarTempo()
    {
        if (voltaEm < 0f) return;

        if (Time.time >= voltaEm)
        {
            voltaEm = -1f;
            ligada = comecaLigada;
            AplicarEstado();
            return;
        }

        // Pisca nos últimos segundos, trocando a cor 6 vezes por segundo
        if (desenho != null && TempoRestante <= tempoAviso)
        {
            bool piscando = Mathf.FloorToInt(Time.time * 6f) % 2 == 0;
            Color corAtual = ligada ? corLigada : corDesligada;
            Color corOutra = ligada ? corDesligada : corLigada;
            desenho.material.color = piscando ? corOutra : corAtual;
        }
    }

    // Modo EnquantoPressionada: acionada só enquanto pressionada
    void Pressionar(bool pressionada)
    {
        bool novoEstado = pressionada ? !comecaLigada : comecaLigada;
        if (novoEstado == ligada) return;

        ligada = novoEstado;
        AplicarEstado();
    }

    // Liga ou desliga cada objeto da lista e troca a cor
    void AplicarEstado()
    {
        foreach (ObjetoControlado controlado in objetos)
            controlado.Aplicar(ligada != controlado.invertido);

        if (desenho != null)
            desenho.material.color = ligada ? corLigada : corDesligada;
    }

    // true se a mira (centro da tela) está na alavanca
    bool MirandoNaAlavanca()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Ray raio = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        if (Physics.Raycast(raio, out RaycastHit acerto, distanciaClique, ~0, QueryTriggerInteraction.Collide))
            return acerto.collider.transform.IsChildOf(transform);
        return false;
    }

    // true se o player está perto o bastante para usar a tecla
    bool PlayerPerto()
    {
        return player != null && Vector3.Distance(player.transform.position, transform.position) <= distanciaTecla;
    }

    // true se o collider é do player
    bool EhPlayer(Collider colisor)
    {
        return colisor.GetComponentInParent<Player>() != null;
    }

    // Encostar no player (modo Colidir)
    void OnCollisionEnter(Collision colisao)
    {
        if (formaDeAcionar == FormaDeAcionar.Colidir && EhPlayer(colisao.collider))
            Entrou();
    }

    void OnCollisionExit(Collision colisao)
    {
        if (formaDeAcionar == FormaDeAcionar.Colidir && EhPlayer(colisao.collider))
            Saiu();
    }

    // Passar por dentro (modo Atravessar)
    void OnTriggerEnter(Collider outro)
    {
        if (formaDeAcionar == FormaDeAcionar.Atravessar && EhPlayer(outro))
            Entrou();
    }

    void OnTriggerExit(Collider outro)
    {
        if (formaDeAcionar == FormaDeAcionar.Atravessar && EhPlayer(outro))
            Saiu();
    }

    // O player começou a encostar
    void Entrou()
    {
        contatosDoPlayer++;
        if (contatosDoPlayer > 1) return; // já estava encostando

        if (modo == ModoAlavanca.EnquantoPressionada) Pressionar(true);
        else Acionar();
    }

    // O player deixou de encostar
    void Saiu()
    {
        contatosDoPlayer = Mathf.Max(0, contatosDoPlayer - 1);
        if (contatosDoPlayer == 0 && modo == ModoAlavanca.EnquantoPressionada)
            Pressionar(false);
    }

    // Desenha linhas até os objetos controlados (só no editor, com a alavanca selecionada)
    void OnDrawGizmosSelected()
    {
        foreach (ObjetoControlado controlado in objetos)
        {
            if (controlado == null || controlado.objeto == null) continue;
            Gizmos.color = controlado.invertido ? corDesligada : corLigada;
            Gizmos.DrawLine(transform.position, controlado.objeto.transform.position);
            Gizmos.DrawWireSphere(controlado.objeto.transform.position, 0.3f);
        }
    }
}
