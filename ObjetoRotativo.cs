using UnityEngine;

[RequireComponent(typeof(Rigidbody))] // O objeto precisa de um Rigidbody para girar pela física
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
    private Rigidbody rb;    // física do objeto

    void Start() // Função chamada uma vez no início
    {
        sentido = sentidoInvertido ? -1 : 1; // Define o sentido com base no bool marcado no inspetor

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;  // só o script gira o objeto
        rb.useGravity = false;  // o objeto não cai
        rb.interpolation = RigidbodyInterpolation.Interpolate; // giro suave na tela
    }

    // Gira o objeto junto com a física
    void FixedUpdate()
    {
        rb.MoveRotation(GiroDoPasso() * rb.rotation);
    }

    // Quanto o objeto gira em um passo de física
    Quaternion GiroDoPasso()
    {
        Vector3 eixoMundo = espacoLocal ? rb.rotation * eixoRotacao.normalized : eixoRotacao.normalized;
        float angulo = sentido * velocidadeRotacao * Time.fixedDeltaTime;
        return Quaternion.AngleAxis(angulo, eixoMundo);
    }

    // Velocidade de um ponto em cima do objeto (o Player usa para girar junto)
    public Vector3 VelocidadeNoPonto(Vector3 ponto)
    {
        if (rb == null) return Vector3.zero;

        Vector3 distancia = ponto - rb.position;          // do centro até o ponto
        Vector3 novaDistancia = GiroDoPasso() * distancia; // onde o ponto vai estar depois do giro
        return (novaDistancia - distancia) / Time.fixedDeltaTime;
    }

    void OnTriggerEnter(Collider other) // Função (OnTriggerEnter) só acionada quando um objeto colide no outro
    {
        if (other.CompareTag("Limite")) // Verifica se a tag do objeto é exatamente "Limite"
        {
            sentido *= -1; // Ao encostar no "Limite" o sentido da rotação é invertido
        }
    }
}
