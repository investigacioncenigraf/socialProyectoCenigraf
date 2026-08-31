using UnityEngine;

public class ObjetoRecogible : MonoBehaviour
{
    public Transform player;
    public float distanciaParaRecoger = 2f;

    [Header("Objetos que se desbloquean al recoger")]
    public GameObject[] objetosBloqueo;

    private bool recogido = false;

    void Update()
    {
        if (recogido || player == null)
            return;

        float distancia = Vector3.Distance(transform.position, player.position);

        if (distancia <= distanciaParaRecoger)
        {
            Recoger();
        }
    }

    void Recoger()
    {
        recogido = true;

        // Desactivar los objetos que bloquean las zonas
        foreach (GameObject objeto in objetosBloqueo)
        {
            if (objeto != null)
            {
                objeto.SetActive(false);
            }
        }

        // Desactivar el carnet
        gameObject.SetActive(false);
    }
}
