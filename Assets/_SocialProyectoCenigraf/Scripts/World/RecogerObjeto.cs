using UnityEngine;

public class ObjetoRecogible : MonoBehaviour
{
    public Transform player;
    public float distanciaParaRecoger = 2f;

    void Update()
    {
        float distancia = Vector3.Distance(transform.position, player.position);

        if (distancia <= distanciaParaRecoger)
        {
            Recoger();
        }
    }

    void Recoger()
    {
        gameObject.SetActive(false);
    }
}
