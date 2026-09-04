using System.Collections;
using UnityEngine;

public class Teleporte : MonoBehaviour
{
    // DESTINO DO TELEPORTE

    // Transform da plataforma para onde o player será enviado.
    // Se deixado vazio (None) no Inspector, este portal é apenas de ida
    // ele não tem para onde teleportar, então nada acontece ao ser tocado.
    [Tooltip("Plataforma de destino")]
    public Transform destino;

    // Distância vertical (eixo Y) somada acima da posição do destino.
    // Usada para o player não nascer preso dentro do chão ou da própria plataforma de destino.
    [Tooltip("Altura de spawn acima do destino")]
    public float alturaSpawn = 1f;

    // CONFIGURAÇÃO DE TEMPO

    // Tempo em segundos que o player fica "preso" no portal antes do teleporte
    // efetivamente acontecer, contado a partir do momento do contato.
    [Header("Configuração de Tempo")]
    [Tooltip("Tempo até teleportar")]
    public float delay = 1f;

    // Tempo em segundos que o portal fica desativado depois de um teleporte,
    // antes de poder ser usado novamente.
    [Tooltip("Tempo de recarga do portal")]
    public float cooldown = 1f;

    // Guarda o momento, a partir do qual a plataforma pode ser usada de novo
    private float proximoUso = 0f;

    // para evitar que o player dispare o teleporte várias vezes enquanto o delay ainda está rodando
    // (por exemplo, se ele ficar parado em cima da plataforma durante o delay)
    private bool teleportando = false;

    // Chamado automaticamente pela Unity quando outro collider encosta neste objeto
    void OnCollisionEnter(Collision collision)
    {
        // Verifica se quem colidiu foi o Player (pela tag)
        if (collision.gameObject.CompareTag("Player"))
        {
            // Se ainda estiver em cooldown, ignora o contato
            if (Time.time < proximoUso)
                return;
            // Se já estiver no meio de um teleporte, ignora novos contatos
            if (teleportando)
                return;
            // Se o campo "destino" estiver vazio, este portal não tem para onde teleportar
            // é um portal só de ida sem retorno configurado, então não faz nada
            if (destino == null)
            {
                return;
            }
            // espera o delay e depois teleporta o player
            StartCoroutine(TeleportarAposDelay(collision.gameObject));
        }
    }

    // espera o delay configurado e então move o player para a posição do destino
    IEnumerator TeleportarAposDelay(GameObject player)
    {
        // Marca que o teleporte está em andamento, para evitar disparos duplicados
        teleportando = true;
        // Espera o tempo definido em "delay" antes de continuar
        yield return new WaitForSeconds(delay);

        // Calcula a posição final somando a altura de spawn no eixo Y,
        // para o player nascer um pouco acima do destino em vez de exatamente na posição dele
        Vector3 posicaoFinal = destino.position + Vector3.up * alturaSpawn;

        // Move o player diretamente para a posição do destino
        // Usa o Rigidbody para mover de forma mais segura com física,
        // evitando problemas de colisão ao mudar a posição bruscamente
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Zera a velocidade atual do player antes de teleportar,
            // evitando que ele saia "voando" no novo local por inércia
            rb.velocity = Vector3.zero;
            rb.position = posicaoFinal;
        }
        // Define o horário liberado para o próximo uso, somando o cooldown ao tempo atual
        proximoUso = Time.time + cooldown;
        // Libera a flag de teleporte em andamento
        teleportando = false;
    }
}