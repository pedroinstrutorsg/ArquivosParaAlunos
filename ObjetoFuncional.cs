using UnityEngine;
using UnityEngine.SceneManagement; // Necessário para poder recarregar a cena
[RequireComponent(typeof(Rigidbody))] // A plataforma precisa de um Rigidbody para andar pela física
public class ObjetoFuncional : MonoBehaviour
{
    [Tooltip("Configurações de Movimento")] // Nos permite escolher o texto que aparece no inspetor
    public Vector3 eixoMovimento = Vector3.right; // Variável especial da Unity que guarda a posição do objeto
    [Tooltip("Velocidade de deslocamento")]
    public float velocidade = 2f; // Variável de velocidade com valor padrão 2
    [Tooltip("Se ativa, a plataforma reinicia a cena ao tocar o Player")]
    public bool dano = false; // Variável que define se a plataforma causa dano (reinicia a cena) ao encostar no player
    private int sentido = 1; // Variável que determina se a plataforma segue a direção determinada ou o seu oposto
    private Rigidbody rb; // física da plataforma

    // Velocidade da plataforma agora (o Player usa para andar junto)
    public Vector3 Velocidade => eixoMovimento.normalized * sentido * velocidade;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;  // só o script move a plataforma
        rb.useGravity = false;  // a plataforma não cai
        rb.interpolation = RigidbodyInterpolation.Interpolate; // movimento suave na tela
    }

    // Move a plataforma junto com a física
    void FixedUpdate()
    {
        rb.MovePosition(rb.position + Velocidade * Time.fixedDeltaTime);
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
            }
        }
    }
}
