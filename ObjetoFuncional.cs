using UnityEngine;
using UnityEngine.SceneManagement; // Necessário para poder recarregar a cena
public class ObjetoFuncional : MonoBehaviour
{
    [Tooltip("Configurações de Movimento")] // Nos permite escolher o texto que aparece no inspetor
    public Vector3 eixoMovimento = Vector3.right; // Variável especial da Unity que guarda a posição do objeto
    [Tooltip("Velocidade de deslocamento")]
    public float velocidade = 2f; // Variável de velocidade com valor padrão 2
    [Tooltip("Se ativa, a plataforma reinicia a cena ao tocar o Player")]
    public bool dano = false; // Variável que define se a plataforma causa dano (reinicia a cena) ao encostar no player
    private int sentido = 1; // Variável que determina se a plataforma segue a direção determinada ou o seu oposto
    private bool playerNaPlataforma = false; // Variável que verifica se o player está encostando na plataforma
    private Transform playerTransform; // Guarda a referência do player enquanto ele estiver em contato
    void Update() // Função chamada o tempo todo
    {
        Vector3 deslocamento = eixoMovimento.normalized * sentido * velocidade * Time.deltaTime; // Quanto a plataforma vai se mover nesse frame
        transform.Translate(deslocamento, Space.World); // Código que movimenta a plataforma

        if (playerNaPlataforma && playerTransform != null) // Se o player estiver em contato, aplica o mesmo deslocamento nele
        {
            playerTransform.position += deslocamento; // Move o player junto, sem trocar de "pai", evitando o teleporte
        }
    }
    void OnTriggerEnter(Collider other) //Função (OnTriggerEnter) só acionada quando um objeto colide no outro
    {
        if (other.CompareTag("Limite")) // Verifica se a tag do objeto é exatamente "Limite"
        {
            sentido *= -1; // No caso do plataforma de fato encostar no "Limite o seu sentido é invertido, isso vale para ambos os casos
        }
    }
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) // Quando a plataforma toca algum objeto ele chama essa função que verifica se a tag é exatamente "Player"
        {
            // Se a plataforma causa dano, reinicia a cena assim que encosta no player
            if (dano)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return; // Encerra a função aqui, já que a cena vai reiniciar
            }
            if (!playerNaPlataforma) // Verifica se a variável playerNaPlataforma é falsa, caso seja ele executa o código abaixo
            {
                playerNaPlataforma = true; // transforma a variável em verdadeira
                playerTransform = collision.transform; // Guarda a referência do player para movê-lo junto no Update, sem usar SetParent
            }
        }
    }
    void OnCollisionExit(Collision collision) // Função (OnCollisionExit) é chamada quando um objeto deixa de encostar na plataforma
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerNaPlataforma = false;
            playerTransform = null; // Remove a referência, o player deixa de se mover junto
        }
    }
}