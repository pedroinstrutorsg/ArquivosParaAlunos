using UnityEngine;

public class PlataformaMovel : MonoBehaviour
{
    [Tooltip("Configurações de Movimento")] // Nos permite escolher o texto que aparece no inspetor
    public Vector3 eixoMovimento = Vector3.right; // Variável especial da Unity que guarda a posição do objeto

    [Tooltip("Velocidade de deslocamento")]
    public float velocidade = 2f; // Variável de velocidade com valor padrão 2

    private int sentido = 1; // Variável que determina se a plataforma segue a direção determinada ou o seu oposto
    private bool playerNaPlataforma = false; // Variável que verifica se o player está encostando na plataforma

    void Update() // Função chamada o tempo todo
    {
        transform.Translate(eixoMovimento.normalized * sentido * velocidade * Time.deltaTime, Space.World); // Código que movimenta a plataforma
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
            if (!playerNaPlataforma) // Verifica se a variável playerNaPlataforma é falsa, caso seja ele executa o código abaixo
            {
                playerNaPlataforma = true; // transforma a variável em verdadeira
                collision.transform.SetParent(transform); // Transforma o objeto Player em um "filho" do obejto plataforma fazendo com que ele sofra toda a força que é exercidad nele
            }
        }
    }

    void OnCollisionExit(Collision collision) // Função (OnCollisionExit) é chamada quando um objeto deixa de encostar na plataforma
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerNaPlataforma = false;
            collision.transform.SetParent(null); // Faz com que o objeto player deixe de ser "filho" do objeto plataforma
        }
    }
}