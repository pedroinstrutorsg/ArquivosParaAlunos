using UnityEngine;

public class ObjetoRotativo : MonoBehaviour
{
    [Tooltip("Eixo em que o objeto vai rotacionar (ex: Vector3.up para girar no eixo Y)")]
    public Vector3 eixoRotacao = Vector3.up; // Define em qual eixo o objeto gira (X, Y, Z ou combinação)

    [Tooltip("Se marcado, inverte o sentido da rotação")]
    public bool sentidoInvertido = false; // Permite escolher a direção da rotação (horário/anti-horário)

    [Tooltip("Velocidade de rotação (graus por segundo)")]
    public float velocidadeRotacao = 50f; // Variável de velocidade com valor padrão 50

    [Tooltip("Se marcado, a rotação é feita em relação ao próprio objeto (Space.Self). Se desmarcado, usa o eixo do mundo (Space.World)")]
    public bool espacoLocal = false; // Variável que define o Space usado no transform.Rotate

    private int sentido = 1; // Variável interna que aplica o sentido escolhido no inspetor
    private bool playerNoObjeto = false; // Variável que verifica se o player está encostando no objeto
    private Transform playerTransform; // Guarda a referência do player enquanto ele estiver em contato

    void Start() // Função chamada uma vez no início
    {
        sentido = sentidoInvertido ? -1 : 1; // Define o sentido com base no bool marcado no inspetor
    }

    void Update() // Função chamada o tempo todo
    {
        Space espaco = espacoLocal ? Space.Self : Space.World; // Escolhe o Space com base na variável pública
        float anguloDelta = sentido * velocidadeRotacao * Time.deltaTime; // Quanto o objeto vai girar nesse frame

        transform.Rotate(eixoRotacao.normalized * anguloDelta, espaco); // Código que rotaciona o objeto

        if (playerNoObjeto && playerTransform != null) // Se o player estiver em contato, orbita só a posição dele, sem mexer na rotação
        {
            // Se o eixo for local, converte para o eixo correspondente no mundo antes de calcular a órbita
            Vector3 eixoMundo = espacoLocal ? transform.TransformDirection(eixoRotacao.normalized) : eixoRotacao.normalized;

            Vector3 direcaoAtePlayer = playerTransform.position - transform.position; // Vetor do centro do objeto até o player
            Quaternion rotacaoDelta = Quaternion.AngleAxis(anguloDelta, eixoMundo); // Rotação equivalente ao ângulo girado nesse frame
            playerTransform.position = transform.position + rotacaoDelta * direcaoAtePlayer; // Recalcula só a POSIÇÃO orbitando o player, sem tocar na rotação dele

            // Note: a rotação do playerTransform (playerTransform.rotation) nunca é alterada aqui,
            // então o Player sempre continua na rotação em que ele mesmo se colocou (sempre em pé)
        }
    }

    void OnTriggerEnter(Collider other) // Função (OnTriggerEnter) só acionada quando um objeto colide no outro
    {
        if (other.CompareTag("Limite")) // Verifica se a tag do objeto é exatamente "Limite"
        {
            sentido *= -1; // Ao encostar no "Limite" o sentido da rotação é invertido
        }
    }

    void OnCollisionStay(Collision collision) // Função chamada enquanto o objeto continua encostando em outro
    {
        if (collision.gameObject.CompareTag("Player")) // Quando o objeto toca algo, verifica se a tag é exatamente "Player"
        {
            if (!playerNoObjeto) // Verifica se a variável playerNoObjeto é falsa, caso seja ele executa o código abaixo
            {
                playerNoObjeto = true; // transforma a variável em verdadeira
                playerTransform = collision.transform; // Guarda a referência do player para girá-lo junto no Update, sem usar SetParent
            }
        }
    }

    void OnCollisionExit(Collision collision) // Função (OnCollisionExit) é chamada quando um objeto deixa de encostar no objeto
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerNoObjeto = false;
            playerTransform = null; // Remove a referência, o player deixa de girar junto
        }
    }
}