using UnityEngine;


[System.Serializable]
public class Ajuste<T>
{
    public bool alterar = false;
    public T valor;

    public Ajuste(T valorInicial)
    {
        valor = valorInicial;
    }
}

// Coloque este script em uma parede/objeto com Collider marcado como "Is Trigger".
// Quando o Player encosta, ele recebe os valores de todas as variáveis marcadas em "Alterar".
public class ZonaDePulo : MonoBehaviour
{
    [Header("Zona")]
    [Tooltip("Tempo (em segundos) que o player precisa esperar antes de outra zona poder alterá-lo de novo")]
    public float cooldown = 2f;

    [Tooltip("Mostra no Console o que foi alterado")]
    public bool mostrarNoConsole = true;

    // Os valores iniciais abaixo são os mesmos padrões do Player
    [Header("Andar / Correr")]
    public Ajuste<float> velocidadeAndar = new Ajuste<float>(5f);
    public Ajuste<float> velocidadeCorrer = new Ajuste<float>(9f);
    public Ajuste<float> tempoParaCorrer = new Ajuste<float>(1.5f);
    public Ajuste<bool> aumentoGradual = new Ajuste<bool>(true);

    [Header("Aceleração e Frenagem")]
    public Ajuste<float> tempoAcelerar = new Ajuste<float>(0.1f);
    public Ajuste<float> tempoFrear = new Ajuste<float>(0.1f);
    [Tooltip("De 0 a 1")]
    public Ajuste<float> controleNoAr = new Ajuste<float>(0.5f);

    [Header("Pulo")]
    public Ajuste<float> forcaPulo = new Ajuste<float>(6f);
    [Tooltip("0 = não pula, 1 = pulo normal, 2 = pulo duplo")]
    public Ajuste<int> pulosMaximos = new Ajuste<int>(1);
    public Ajuste<float> tempoCoyote = new Ajuste<float>(0.15f);
    [Tooltip("Tempo mínimo (em segundos) entre um pulo e outro")]
    public Ajuste<float> cooldownEntrePulos = new Ajuste<float>(0.2f);

    // Chamado uma vez quando a fase começa
    void Start()
    {
        // Se a zona tiver a tag "Limite", ela fica invisível durante o jogo.
        // Só o desenho some: o collider continua funcionando normalmente.
        if (CompareTag("Limite"))
        {
            Renderer desenho = GetComponent<Renderer>();
            if (desenho != null)
                desenho.enabled = false;
        }
    }

    // Chamado automaticamente quando algo ENTRA no trigger
    void OnTriggerEnter(Collider outro)
    {
        // Procura o script Player no objeto que entrou (ou nos "pais" dele, caso o collider esteja num filho)
        Player player = outro.GetComponentInParent<Player>();
        if (player == null) return;

        // Se outra zona alterou o player há pouco tempo, não faz nada
        if (player.EmCooldownDeTroca) return;

        // ===== Andar / Correr =====
        Aplicar(velocidadeAndar, ref player.velocidadeAndar, "Velocidade Andar");
        Aplicar(velocidadeCorrer, ref player.velocidadeCorrer, "Velocidade Correr");
        Aplicar(tempoParaCorrer, ref player.tempoParaCorrer, "Tempo Para Correr");
        Aplicar(aumentoGradual, ref player.aumentoGradual, "Aumento Gradual");

        // ===== Aceleração e Frenagem =====
        Aplicar(tempoAcelerar, ref player.tempoAcelerar, "Tempo Acelerar");
        Aplicar(tempoFrear, ref player.tempoFrear, "Tempo Frear");
        Aplicar(controleNoAr, ref player.controleNoAr, "Controle No Ar");
        player.controleNoAr = Mathf.Clamp01(player.controleNoAr); // garante que fica entre 0 e 1

        // ===== Pulo =====
        Aplicar(forcaPulo, ref player.forcaPulo, "Força Pulo");
        Aplicar(tempoCoyote, ref player.tempoCoyote, "Tempo Coyote");
        Aplicar(cooldownEntrePulos, ref player.cooldownEntrePulos, "Cooldown Entre Pulos");
        player.cooldownEntrePulos = Mathf.Max(0f, player.cooldownEntrePulos); // não deixa ficar negativo

        // Os pulos usam a função do Player, porque além de trocar o número
        // ela também recarrega os pulos na hora
        if (pulosMaximos.alterar)
        {
            player.DefinirPulos(pulosMaximos.valor, cooldown);
            if (mostrarNoConsole)
                Debug.Log("Player agora tem " + player.pulosMaximos + " pulo(s)");
        }

        // Começa o cooldown: nenhuma zona altera o player até ele acabar
        player.IniciarCooldownDeTroca(cooldown);
    }

    // Se o ajuste estiver marcado, copia o valor para a variável do player.
    // "ref" quer dizer que a função mexe direto na variável do player, e não numa cópia dela.
    void Aplicar<T>(Ajuste<T> ajuste, ref T variavelDoPlayer, string nome)
    {
        if (!ajuste.alterar) return;

        variavelDoPlayer = ajuste.valor;

        if (mostrarNoConsole)
            Debug.Log(nome + " agora é " + ajuste.valor);
    }
}
