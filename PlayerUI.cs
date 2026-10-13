using UnityEngine;
using TMPro;

// Mostra na tela as informações do Player (velocidade, estado, pulos, coyote, chão).
// Coloque este script em qualquer objeto (ex: no próprio Canvas) e arraste as referências no Inspector.
public class PlayerUI : MonoBehaviour
{
    public Player player;     // Arraste o objeto do Player aqui
    public TMP_Text texto;    // Arraste o texto (TextMeshPro) do Canvas aqui

    void Update()
    {
        // Se faltar alguma referência, não faz nada (evita erro no Console)
        if (player == null || texto == null)
            return;

        texto.text =
            "Velocidade: " + player.VelocidadeAtual.ToString("F1") + "\n" +
            "Estado: " + player.EstadoAtual + "\n" +
            "Pulos: " + player.PulosDisponiveis + " / " + player.pulosMaximos + "\n" +
            "Coyote: " + player.CoyoteRestante.ToString("F2") + "s / " + player.tempoCoyote.ToString("F2") + "s\n" +
            "No chão: " + (player.EstaNoChao ? "Sim" : "Não");
    }
}