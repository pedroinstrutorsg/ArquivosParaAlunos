using UnityEngine;
using UnityEngine.SceneManagement; // Necessário para poder carregar outras cenas (LoadScene)

public class Botao : MonoBehaviour
{
    // Método chamado pelo evento OnClick() do componente Button no Inspector
    public void IrParaJogo()
    {
        // NOVO: zera o checkpoint salvo, garantindo que uma partida nova
        // sempre comece do início, sem herdar o progresso de uma tentativa anterior.
        CheckpointManager.Resetar();

        // Carrega a cena "Teste" (ela precisa estar adicionada em File > Build Settings > Scenes In Build)
        SceneManager.LoadScene("Teste");
    }

    // Chamado automaticamente pela Unity assim que este script é carregado em uma cena
    // Como o botão fica na cena "Jogo", esse método já roda logo que ela inicia
    void Awake()
    {
        // Destrava o cursor do mouse, permitindo que ele se mova livremente pela tela
        Cursor.lockState = CursorLockMode.None;

        // Torna o cursor visível novamente na tela
        Cursor.visible = true;
    }
}
