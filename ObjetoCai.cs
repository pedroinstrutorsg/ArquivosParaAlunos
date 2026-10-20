using System.Collections;
using UnityEngine;

public class ObjetoCai : MonoBehaviour
{

    // CONFIGURAÇÃO DE QUEDA

    [Header("Configuração de Queda")]
    [Tooltip("Tempo em segundos entre o player encostar e a plataforma começar a cair")]
    public float delayParaCair = 1f;

    [Tooltip("Força da gravidade nesta plataforma.\n1 = gravidade normal\n2 = cai duas vezes mais forte\n0.5 = cai devagar, como na Lua\n0 = não cai (fica flutuando)")]
    [Min(0f)]
    public float multiplicadorGravidade = 1f;

    // Flag para garantir que a queda só seja ativada uma única vez
    // (evita que o player, encostando várias vezes, dispare repetidamente)
    private bool jaAtivada = false;

    // Rigidbody criado na hora da queda (fica vazio até a plataforma começar a cair)
    private Rigidbody rb;

    // Chamado automaticamente pela Unity quando outro collider sólido encosta neste objeto
    void OnCollisionEnter(Collision collision)
    {
        // Verifica se quem colidiu foi o Player (pela tag) e se a queda ainda não foi ativada
        if (collision.gameObject.CompareTag("Player") && !jaAtivada)
        {
            // Marca como ativada imediatamente, para não iniciar de novo
            // caso o player encoste várias vezes antes do delay terminar
            jaAtivada = true;

            // Inicia o delay e depois ativa a queda
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
        rb = gameObject.AddComponent<Rigidbody>();

        // DESLIGAMOS a gravidade padrão da Unity, porque ela é igual para todo mundo.
        // No lugar dela, aplicamos a NOSSA gravidade no FixedUpdate (com o multiplicador).
        rb.useGravity = false;

        // Garante que o Rigidbody NÃO está kinematic, ou seja, ele vai reagir à física normalmente
        rb.isKinematic = false;
    }

    // Roda a cada passo de física (é aqui que forças devem ser aplicadas)
    void FixedUpdate()
    {
        // Enquanto a plataforma ainda não começou a cair, não faz nada
        if (rb == null) return;

        // Physics.gravity é a gravidade do projeto (normalmente 0, -9.81, 0)
        // Multiplicando, deixamos ela mais forte ou mais fraca SÓ nesta plataforma.
        // ForceMode.Acceleration ignora a massa: plataforma leve ou pesada cai do mesmo jeito.
        rb.AddForce(Physics.gravity * multiplicadorGravidade, ForceMode.Acceleration);
    }
}
