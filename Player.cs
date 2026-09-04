using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 5f;      // Velocidade de deslocamento do personagem (unidades por segundo)
    public float forcaPulo = 6f;       // Força aplicada verticalmente ao pular

    [Header("Rotação do Mouse")]
    public float sensibilidadeMouse = 200f;   // Quanto o mouse influencia a rotação (quanto maior, mais sensível)
    public Transform cameraPersonagem;        // Referência da câmera (deve ser filha do personagem na Hierarchy)
    public float limiteAnguloVertical = 80f;  // Limite máximo de ângulo para olhar pra cima/baixo (evita giro de 360°)

    private Rigidbody rb;              // Referência ao componente Rigidbody do personagem, usado para física e movimento
    private float rotacaoVertical = 0f; // Guarda o ângulo vertical atual da câmera (acumulado a cada frame)
    private int contatosNoChao = 0;    // Conta quantas plataformas o personagem está tocando ao mesmo tempo (usado para permitir o pulo)

    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Pega o Rigidbody que está no mesmo GameObject deste script
        // Trava o cursor do mouse no centro da tela e o deixa invisível (padrão em jogos FPS/terceira pessoa)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Rotação é feita no Update porque o mouse é lido a cada frame renderizado (mais suave visualmente)
        RotacionarComMouse();

        // Verifica se apertou espaço e se está tocando pelo menos uma plataforma antes de permitir o pulo
        if (Input.GetKeyDown(KeyCode.Space) && contatosNoChao > 0)
        {
            // Aplica uma força instantânea para cima (ForceMode.VelocityChange ignora a massa do objeto)
            rb.AddForce(Vector3.up * forcaPulo, ForceMode.VelocityChange);
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
        // Movimento é feito no FixedUpdate porque envolve física (Rigidbody),
        MovimentarTeclas();
    }

    // Lê as teclas WASD e movimenta o personagem com base na direção que ele está olhando
    void MovimentarTeclas()
    {
        Vector3 movimento = Vector3.zero; // Vetor que vai acumular a direção do movimento

        // transform.forward/right são baseados na rotação atual do personagem,
        // ou seja, "frente" sempre é para onde ele está olhando
        if (Input.GetKey(KeyCode.W))
            movimento += transform.forward;  // Anda para frente
        if (Input.GetKey(KeyCode.S))
            movimento -= transform.forward;  // Anda para trás
        if (Input.GetKey(KeyCode.A))
            movimento -= transform.right;    // Anda para a esquerda
        if (Input.GetKey(KeyCode.D))
            movimento += transform.right;    // Anda para a direita

        // Normalized garante que andar na diagonal (ex: W+D) não seja mais rápido que andar reto
        // Depois multiplica pela velocidade para definir a intensidade do movimento
        movimento = movimento.normalized * velocidade;

        // Mantém a velocidade vertical atual (queda/pulo) e só altera o movimento horizontal (X e Z)
        // Isso evita que o movimento nas teclas "cancele" a gravidade ou o pulo
        movimento.y = rb.velocity.y;

        // Aplica a velocidade final diretamente no Rigidbody
        rb.velocity = movimento;
    }

    // Controla a rotação do personagem e da câmera com base no movimento do mouse
    void RotacionarComMouse()
    {
        // Pega o quanto o mouse se moveu no eixo X (horizontal) e Y (vertical) desde o último frame
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadeMouse * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadeMouse * Time.deltaTime;

        // Rotaciona o personagem inteiro no eixo Y (esquerda/direita)
        // Isso faz o corpo virar junto, já que a câmera é filha dele
        transform.Rotate(Vector3.up * mouseX);

        // Só mexe na rotação vertical se a câmera estiver configurada no Inspector
        if (cameraPersonagem != null)
        {
            // Subtrai o mouseY: mover o mouse pra cima deve olhar pra cima (rotação negativa no eixo X)
            rotacaoVertical -= mouseY;

            // Limita o ângulo vertical para não deixar o personagem "olhar de cabeça para baixo" (giro completo)
            rotacaoVertical = Mathf.Clamp(rotacaoVertical, -limiteAnguloVertical, limiteAnguloVertical);

            // Aplica a rotação SOMENTE na câmera (localRotation), não no personagem inteiro
            // Por isso só o "olhar" sobe/desce, sem inclinar o corpo do personagem
            cameraPersonagem.localRotation = Quaternion.Euler(rotacaoVertical, 0f, 0f);
        }
    }

    // Chamado automaticamente pela Unity quando o Collider deste objeto ENCOSTA em outro
    void OnCollisionEnter(Collision colisao)
    {
        // Verifica se o objeto colidido tem a tag "Plataforma" (configurada no Inspector)
        if (colisao.gameObject.CompareTag("Plataforma"))
        {
            contatosNoChao++; // Soma 1 contato: cada plataforma tocada conta separadamente
        }
    }

    // Chamado automaticamente pela Unity quando o Collider deste objeto não está tocando no outro
    void OnCollisionExit(Collision colisao)
    {
        if (colisao.gameObject.CompareTag("Plataforma"))
        {
            contatosNoChao--; // Remove esse contato específico; o pulo só é bloqueado quando o contador chegar a 0
        }
    }
}