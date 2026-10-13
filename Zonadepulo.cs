using UnityEngine;

// Coloque este script em uma parede/objeto com Collider marcado como "Is Trigger".
// Quando o Player encosta, ele passa a ter a quantidade de pulos definida aqui.
public class ZonaDePulo : MonoBehaviour
{
    [Header("Configuração de Pulo")]
    [Range(0, 3)]
    public int quantidadePulos = 1;     // 0 = não pode pular, 1 = pulo normal (padrão), 2 = pulo duplo

    [Tooltip("Tempo (em segundos) que o player precisa esperar antes de poder trocar a quantidade de pulos de novo")]
    public float cooldown = 2f;

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

        if (player != null)
        {
            // Tenta trocar a quantidade de pulos. Se o player ainda estiver em cooldown, nada acontece.
            bool trocou = player.DefinirPulos(quantidadePulos, cooldown);

            if (trocou)
                Debug.Log("Player agora tem " + quantidadePulos + " pulo(s)");
        }
    }
}