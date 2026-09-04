using UnityEngine;

public class JumpPad : MonoBehaviour
{

    // CONFIGURAÇÃO DE FORÇA

    [Header("Configuração da Força")]
    [Tooltip("Intensidade da força aplicada para cima (eixo Y)")]
    public float forcaVertical = 10f;


    // COOLDOWN

    [Header("Cooldown")]
    [Tooltip("Tempo em segundos que o jump pad espera antes de poder ser ativado novamente")]
    public float cooldown = 1f;

    // Guarda o tempo em que o pad pode ser usado de novo
    private float proximoUso = 0f;

    // Chamado automaticamente pela Unity quando outro collider sólido encosta neste objeto
    void OnCollisionEnter(Collision collision)
    {
        // Verifica se quem colidiu foi o Player (pela tag)
        if (collision.gameObject.CompareTag("Player"))
        {
            // Se o tempo atual ainda for menor que o horário liberado pelo cooldown,
            // a função para aqui e ignora essa colisão
            if (Time.time < proximoUso)
                return;

            // Pega o componente Rigidbody do player, necessário para alterar a velocidade dele
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();

            // Só continua se o player realmente tiver um Rigidbody
            if (rb != null)
            {
                // Zera a velocidade vertical atual antes de aplicar o impulso,
                // evitando que a força do pad se some de forma inconsistente
                // (por exemplo, se o player já estivesse caindo rápido)
                Vector3 velocidadeAtual = rb.velocity;
                velocidadeAtual.y = 0f;
                rb.velocity = velocidadeAtual;

                // Aplica a força para cima diretamente na velocidade do player
                rb.velocity += Vector3.up * forcaVertical;

                // Atualiza o timer liberado para o próximo uso, somando o cooldown ao tempo atual
                proximoUso = Time.time + cooldown;
            }
        }
    }
}