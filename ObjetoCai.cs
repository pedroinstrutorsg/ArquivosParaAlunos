using System.Collections;
using UnityEngine;

public class ObjetoCai : MonoBehaviour
{

    // CONFIGURAÇÃO DE QUEDA

    [Header("Configuração de Queda")]
    [Tooltip("Tempo em segundos entre o player encostar e a plataforma começar a cair")]
    public float delayParaCair = 1f;

    // Flag para garantir que a queda só seja ativada uma única vez
    // (evita que o player, encostando várias vezes, dispare repetidamente)
    private bool jaAtivada = false;

    // Chamado automaticamente pela Unity quando outro collider sólido encosta neste objeto
    void OnCollisionEnter(Collision collision)
    {
        // Verifica se quem colidiu foi o Player (pela tag) e se a queda ainda não foi ativada
        if (collision.gameObject.CompareTag("Player") && !jaAtivada)
        {
            // Marca como ativada imediatamente, para não iniciar de novo
            // caso o player encoste várias vezes antes do delay terminar
            jaAtivada = true;

            // Inicia o delay e depois ativa a gravidade
            StartCoroutine(AtivarQueda());
        }
    }

    // espera um tempo (delay) e então ativa a física de queda da plataforma
    IEnumerator AtivarQueda()
    {
        // Espera o tempo definido em "delayParaCair" antes de continuar
        // Isso dá o "aviso" de que a plataforma vai cair (dá tempo do player sair de cima, se quiser)
        yield return new WaitForSeconds(delayParaCair);

        // Como a plataforma NÃO tem Rigidbody (é totalmente estática), precisamos adicionar um em tempo de execução
        // AddComponent<Rigidbody>() cria e anexa um Rigidbody novo ao GameObject, com valores padrão
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();

        // Garante que a gravidade está ativada (já vem true por padrão)
        rb.useGravity = true;

        // Garante que o Rigidbody NÃO está kinematic, ou seja, ele vai reagir à física normalmente
        // (isKinematic = false permite que a gravidade realmente puxe o objeto para baixo)
        rb.isKinematic = false;

        // A partir daqui, a plataforma passa a cair livremente por gravidade e continua caindo
    }
}